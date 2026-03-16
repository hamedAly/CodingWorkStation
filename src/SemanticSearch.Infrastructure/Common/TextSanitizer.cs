using System.Text;

namespace SemanticSearch.Infrastructure.Common;

internal static class TextSanitizer
{
    public static string Sanitize(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var builder = new StringBuilder(value.Length);

        for (var index = 0; index < value.Length; index++)
        {
            var current = value[index];
            Rune rune;

            if (char.IsHighSurrogate(current))
            {
                if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                    continue;

                rune = new Rune(current, value[index + 1]);
                index++;
            }
            else if (char.IsLowSurrogate(current))
            {
                continue;
            }
            else
            {
                rune = new Rune(current);
            }

            if (Rune.IsControl(rune) && rune.Value is not '\r' and not '\n' and not '\t')
                continue;

            builder.Append(rune.ToString());
        }

        return builder.ToString();
    }
}
