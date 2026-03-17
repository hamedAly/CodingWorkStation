using System.Text.RegularExpressions;
using Markdig;

namespace SemanticSearch.WebApi.Services;

public sealed class MarkdownRenderService
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private static readonly Regex CSharpBlockPattern = new(
        "<pre><code class=\"language-(?:csharp|cs)\">(?<code>.*?)</code></pre>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    private static readonly Regex KeywordPattern = new(
        "\\b(?<keyword>abstract|as|base|bool|break|case|catch|class|const|continue|decimal|default|delegate|do|else|enum|event|explicit|extern|false|finally|fixed|for|foreach|if|implicit|in|int|interface|internal|is|lock|namespace|new|null|object|operator|out|override|params|private|protected|public|readonly|record|ref|return|sealed|static|string|struct|switch|this|throw|true|try|using|var|virtual|void|while)\\b",
        RegexOptions.Compiled);

    public string Render(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        var html = Markdown.ToHtml(markdown, Pipeline);
        return CSharpBlockPattern.Replace(
            html,
            match => match.Value.Replace(
                match.Groups["code"].Value,
                KeywordPattern.Replace(
                    match.Groups["code"].Value,
                    "<span class=\"md-token-keyword\">${keyword}</span>")));
    }
}
