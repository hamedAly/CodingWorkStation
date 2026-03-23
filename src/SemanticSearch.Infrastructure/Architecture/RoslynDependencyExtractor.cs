using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.Interfaces;
using SemanticSearch.Domain.ValueObjects;
using SemanticSearch.Infrastructure.Common;
using SemanticSearch.Infrastructure.Quality;
using SemanticSearch.Infrastructure.VectorStore;

namespace SemanticSearch.Infrastructure.Architecture;

/// <summary>
/// Extracts class and method dependency relationships from C# source files using Roslyn syntax analysis.
/// </summary>
public sealed class RoslynDependencyExtractor : IDependencyExtractor
{
    private readonly IProjectFileRepository _projectFileRepository;
    private readonly QualityFileFilter _fileFilter;

    public RoslynDependencyExtractor(IProjectFileRepository projectFileRepository, QualityFileFilter fileFilter)
    {
        _projectFileRepository = projectFileRepository;
        _fileFilter = fileFilter;
    }

    public async Task<DependencyExtractionResult> ExtractAsync(string projectKey, CancellationToken cancellationToken = default)
    {
        var files = await _projectFileRepository.ListFilesAsync(projectKey, cancellationToken);
        var csFiles = files
            .Where(f => string.Equals(f.Extension, ".cs", StringComparison.OrdinalIgnoreCase)
                     && _fileFilter.ShouldAnalyze(f.RelativeFilePath, scopePath: null))
            .ToList();

        // Symbol table: FullName -> (NodeId, NodeKind)
        var symbolTable = new Dictionary<string, (string NodeId, string FilePath, int StartLine, string Namespace, string Name, DependencyNodeKind Kind, string? ParentNodeId)>(StringComparer.Ordinal);
        var parsedFiles = new List<(string RelativePath, SyntaxTree Tree)>();

        // --- PASS 1: Build symbol table from class/method declarations ---
        foreach (var file in csFiles)
        {
            if (!File.Exists(file.AbsoluteFilePath))
                continue;

            var textResult = await TextFileLoader.TryReadSanitizedTextAsync(file.AbsoluteFilePath, cancellationToken);
            if (!textResult.Success || textResult.IsBinary || string.IsNullOrWhiteSpace(textResult.Content))
                continue;

            var tree = CSharpSyntaxTree.ParseText(textResult.Content, cancellationToken: cancellationToken);
            parsedFiles.Add((file.RelativeFilePath, tree));

            var root = await tree.GetRootAsync(cancellationToken);
            var currentNamespace = ExtractNamespace(root);

            foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var className = classDecl.Identifier.Text;
                var outerClass = classDecl.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                var parentName = outerClass is not null ? $"{currentNamespace}.{outerClass.Identifier.Text}" : null;
                var parentNodeId = parentName is not null ? SqliteVectorStore.ComputeContentHash($"{projectKey}|{parentName}|Class") : null;

                var fullName = outerClass is not null
                    ? $"{currentNamespace}.{outerClass.Identifier.Text}.{className}"
                    : $"{currentNamespace}.{className}";
                var nodeId = SqliteVectorStore.ComputeContentHash($"{projectKey}|{fullName}|Class");
                var startLine = classDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

                symbolTable[fullName] = (nodeId, file.RelativeFilePath, startLine, currentNamespace, className, DependencyNodeKind.Class, parentNodeId);
            }

            foreach (var methodDecl in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                var methodName = methodDecl.Identifier.Text;
                var containingClass = methodDecl.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                if (containingClass is null)
                    continue;

                var classFullName = $"{currentNamespace}.{containingClass.Identifier.Text}";
                var methodFullName = $"{classFullName}.{methodName}";
                var classNodeId = SqliteVectorStore.ComputeContentHash($"{projectKey}|{classFullName}|Class");
                var nodeId = SqliteVectorStore.ComputeContentHash($"{projectKey}|{methodFullName}|Method");
                var startLine = methodDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

                // Use first occurrence to avoid overwriting with overloads
                if (!symbolTable.ContainsKey(methodFullName))
                    symbolTable[methodFullName] = (nodeId, file.RelativeFilePath, startLine, currentNamespace, methodName, DependencyNodeKind.Method, classNodeId);
            }
        }

        // Build node list from symbol table
        var nodes = symbolTable.Select(kv => new DependencyNode
        {
            NodeId = kv.Value.NodeId,
            ProjectKey = projectKey,
            RunId = string.Empty,    // Assigned by ReplaceDependencyGraphAsync using run.RunId
            Name = kv.Value.Name,
            FullName = kv.Key,
            Kind = kv.Value.Kind,
            Namespace = kv.Value.Namespace,
            FilePath = kv.Value.FilePath,
            StartLine = kv.Value.StartLine,
            ParentNodeId = kv.Value.ParentNodeId
        }).ToList();

        // --- PASS 2: Produce edges from method bodies ---
        var edgeSet = new HashSet<string>();
        var edges = new List<DependencyEdge>();

        foreach (var (relativePath, tree) in parsedFiles)
        {
            var root = await tree.GetRootAsync(cancellationToken);
            var currentNamespace = ExtractNamespace(root);

            foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var classFullName = $"{currentNamespace}.{classDecl.Identifier.Text}";
                if (!symbolTable.TryGetValue(classFullName, out var classEntry))
                    continue;

                // Inheritance
                if (classDecl.BaseList is not null)
                {
                    foreach (var baseType in classDecl.BaseList.Types)
                    {
                        var baseName = baseType.Type.ToString();
                        var resolvedBase = ResolveTypeName(baseName, currentNamespace, symbolTable);
                        if (resolvedBase is not null && resolvedBase != classEntry.NodeId)
                            TryAddEdge(edges, edgeSet, projectKey, classEntry.NodeId, resolvedBase, DependencyRelationshipType.Inheritance);
                    }
                }

                // Type references in fields/properties
                foreach (var fieldDecl in classDecl.Members.OfType<FieldDeclarationSyntax>())
                {
                    var typeName = fieldDecl.Declaration.Type.ToString();
                    var resolvedType = ResolveTypeName(typeName, currentNamespace, symbolTable);
                    if (resolvedType is not null && resolvedType != classEntry.NodeId)
                        TryAddEdge(edges, edgeSet, projectKey, classEntry.NodeId, resolvedType, DependencyRelationshipType.TypeReference);
                }

                foreach (var methodDecl in classDecl.Members.OfType<MethodDeclarationSyntax>())
                {
                    var methodFullName = $"{classFullName}.{methodDecl.Identifier.Text}";
                    if (!symbolTable.TryGetValue(methodFullName, out var methodEntry))
                        continue;

                    // Invocations within method bodies
                    foreach (var invocation in methodDecl.DescendantNodes().OfType<InvocationExpressionSyntax>())
                    {
                        var calledName = ExtractCalledName(invocation);
                        if (calledName is null)
                            continue;

                        // Try to match <ClassName>.<MethodName> pattern
                        var resolvedTarget = ResolveCallTarget(calledName, currentNamespace, symbolTable);
                        if (resolvedTarget is not null && resolvedTarget != methodEntry.NodeId)
                            TryAddEdge(edges, edgeSet, projectKey, methodEntry.NodeId, resolvedTarget, DependencyRelationshipType.Invocation);
                    }

                    // Object creations
                    foreach (var creation in methodDecl.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
                    {
                        var typeName = creation.Type.ToString();
                        var resolvedType = ResolveTypeName(typeName, currentNamespace, symbolTable);
                        if (resolvedType is not null && resolvedType != classEntry.NodeId)
                            TryAddEdge(edges, edgeSet, projectKey, methodEntry.NodeId, resolvedType, DependencyRelationshipType.Construction);
                    }
                }
            }
        }

        return new DependencyExtractionResult(nodes, edges, parsedFiles.Count);
    }

    private static string ExtractNamespace(SyntaxNode root)
    {
        var ns = root.DescendantNodes()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .FirstOrDefault();
        return ns?.Name.ToString() ?? string.Empty;
    }

    private static string? ResolveTypeName(
        string typeName,
        string currentNamespace,
        Dictionary<string, (string NodeId, string FilePath, int StartLine, string Namespace, string Name, DependencyNodeKind Kind, string? ParentNodeId)> symbolTable)
    {
        // Try fully-qualified first
        if (symbolTable.TryGetValue(typeName, out var entry))
            return entry.NodeId;

        // Try with current namespace prefix
        var qualified = $"{currentNamespace}.{typeName}";
        if (symbolTable.TryGetValue(qualified, out entry))
            return entry.NodeId;

        return null;
    }

    private static string? ResolveCallTarget(
        string calledName,
        string currentNamespace,
        Dictionary<string, (string NodeId, string FilePath, int StartLine, string Namespace, string Name, DependencyNodeKind Kind, string? ParentNodeId)> symbolTable)
    {
        // calledName may be "ClassName.MethodName" or just "MethodName"
        if (symbolTable.TryGetValue(calledName, out var entry))
            return entry.NodeId;

        var qualified = $"{currentNamespace}.{calledName}";
        if (symbolTable.TryGetValue(qualified, out entry))
            return entry.NodeId;

        return null;
    }

    private static string? ExtractCalledName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess =>
                $"{memberAccess.Expression}.{memberAccess.Name.Identifier.Text}",
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };
    }

    private static void TryAddEdge(
        List<DependencyEdge> edges,
        HashSet<string> edgeSet,
        string projectKey,
        string sourceNodeId,
        string targetNodeId,
        DependencyRelationshipType type)
    {
        var edgeKey = $"{sourceNodeId}|{targetNodeId}|{type}";
        if (!edgeSet.Add(edgeKey))
            return;

        var edgeId = SqliteVectorStore.ComputeContentHash(edgeKey);
        edges.Add(new DependencyEdge
        {
            EdgeId = edgeId,
            ProjectKey = projectKey,
            RunId = string.Empty,    // Assigned by ReplaceDependencyGraphAsync
            SourceNodeId = sourceNodeId,
            TargetNodeId = targetNodeId,
            RelationshipType = type
        });
    }
}
