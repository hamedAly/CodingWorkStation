using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;
using SemanticSearch.Domain.Interfaces;
using SemanticSearch.Infrastructure.Common;

namespace SemanticSearch.Infrastructure.Embedding;

public sealed class OnnxEmbeddingService : IEmbeddingService, IDisposable
{
    private const int MaxTokens = 256;
    private const int EmbeddingDimensions = 384;

    private readonly InferenceSession _session;
    private readonly BertTokenizer _tokenizer;

    public OnnxEmbeddingService(string modelDirectory)
    {
        var modelPath = Path.Combine(modelDirectory, "model.onnx");
        var vocabPath = Path.Combine(modelDirectory, "vocab.txt");

        if (!File.Exists(modelPath))
            throw new FileNotFoundException($"ONNX model not found at '{modelPath}'. Download from HuggingFace: sentence-transformers/all-MiniLM-L6-v2", modelPath);

        if (!File.Exists(vocabPath))
            throw new FileNotFoundException($"Vocabulary file not found at '{vocabPath}'. Download vocab.txt from HuggingFace: sentence-transformers/all-MiniLM-L6-v2", vocabPath);

        var sessionOptions = new SessionOptions();
        sessionOptions.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
        _session = new InferenceSession(modelPath, sessionOptions);

        _tokenizer = BertTokenizer.Create(vocabPath, new BertOptions
        {
            LowerCaseBeforeTokenization = true
        });
    }

    public Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sanitizedText = TextSanitizer.Sanitize(text);

        if (string.IsNullOrWhiteSpace(sanitizedText))
            sanitizedText = " ";

        // Tokenize with [CLS] and [SEP] special tokens, truncate to MaxTokens
        var tokenIds = _tokenizer.EncodeToIds(
            sanitizedText,
            maxTokenCount: MaxTokens,
            addSpecialTokens: true,
            normalizedText: out _,
            charsConsumed: out _,
            considerPreTokenization: true,
            considerNormalization: true);

        var seqLen = tokenIds.Count;

        var inputIdsBuffer = new long[seqLen];
        var attentionMaskBuffer = new long[seqLen];
        var tokenTypeIdsBuffer = new long[seqLen]; // all zeros for single-sentence

        for (int i = 0; i < seqLen; i++)
        {
            inputIdsBuffer[i] = tokenIds[i];
            attentionMaskBuffer[i] = 1L;
        }

        var dims = new[] { 1, seqLen };
        var inputIdsTensor = new DenseTensor<long>(inputIdsBuffer, dims);
        var attentionMaskTensor = new DenseTensor<long>(attentionMaskBuffer, dims);
        var tokenTypeIdsTensor = new DenseTensor<long>(tokenTypeIdsBuffer, dims);

        var inputs = new[]
        {
            NamedOnnxValue.CreateFromTensor("input_ids", inputIdsTensor),
            NamedOnnxValue.CreateFromTensor("attention_mask", attentionMaskTensor),
            NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeIdsTensor)
        };

        using var outputs = _session.Run(inputs);
        var lastHiddenState = outputs[0].AsTensor<float>(); // shape: [1, seqLen, 384]

        // Mean pooling over all token positions (no padding — all tokens are valid)
        var embedding = new float[EmbeddingDimensions];
        for (int t = 0; t < seqLen; t++)
        {
            for (int d = 0; d < EmbeddingDimensions; d++)
            {
                embedding[d] += lastHiddenState[0, t, d];
            }
        }
        for (int d = 0; d < EmbeddingDimensions; d++)
        {
            embedding[d] /= seqLen;
        }

        // L2 normalize so cosine similarity = dot product
        var norm = MathF.Sqrt(embedding.Sum(x => x * x));
        if (norm > 0f)
        {
            for (int d = 0; d < EmbeddingDimensions; d++)
                embedding[d] /= norm;
        }

        return Task.FromResult(embedding);
    }

    public void Dispose()
    {
        _session.Dispose();
    }
}
