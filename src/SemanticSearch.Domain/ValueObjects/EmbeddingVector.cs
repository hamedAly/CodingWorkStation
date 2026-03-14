namespace SemanticSearch.Domain.ValueObjects;

public sealed class EmbeddingVector : IEquatable<EmbeddingVector>
{
    public const int Dimensions = 384;

    public float[] Values { get; }

    public EmbeddingVector(float[] values)
    {
        if (values.Length != Dimensions)
            throw new ArgumentException($"Embedding must have exactly {Dimensions} dimensions, got {values.Length}.", nameof(values));
        Values = values;
    }

    public bool Equals(EmbeddingVector? other)
    {
        if (other is null) return false;
        return Values.AsSpan().SequenceEqual(other.Values.AsSpan());
    }

    public override bool Equals(object? obj) => obj is EmbeddingVector other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var v in Values) hash.Add(v);
        return hash.ToHashCode();
    }
}
