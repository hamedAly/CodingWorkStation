using System.Text;

namespace SemanticSearch.Infrastructure.Common;

internal static class TextFileLoader
{
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);
    private static readonly UnicodeEncoding Utf16LittleEndian = new(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: false);
    private static readonly UnicodeEncoding Utf16BigEndian = new(bigEndian: true, byteOrderMark: false, throwOnInvalidBytes: false);

    public static bool TryReadSanitizedText(
        string filePath,
        out string content,
        out bool isBinary,
        out string? failureReason)
    {
        try
        {
            var bytes = File.ReadAllBytes(filePath);
            return TryDecode(bytes, out content, out isBinary, out failureReason);
        }
        catch (Exception ex)
        {
            content = string.Empty;
            isBinary = false;
            failureReason = ex.Message;
            return false;
        }
    }

    public static async Task<(bool Success, string Content, bool IsBinary, string? FailureReason)> TryReadSanitizedTextAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var bytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
            var success = TryDecode(bytes, out var content, out var isBinary, out var failureReason);
            return (success, content, isBinary, failureReason);
        }
        catch (Exception ex)
        {
            return (false, string.Empty, false, ex.Message);
        }
    }

    private static bool TryDecode(
        byte[] bytes,
        out string content,
        out bool isBinary,
        out string? failureReason)
    {
        var (encoding, preambleLength, skipBinaryHeuristic) = DetectEncoding(bytes);

        if (!skipBinaryHeuristic && LooksBinary(bytes))
        {
            content = string.Empty;
            isBinary = true;
            failureReason = "The file appears to be binary or non-text content.";
            return false;
        }

        content = TextSanitizer.Sanitize(encoding.GetString(bytes, preambleLength, bytes.Length - preambleLength));
        isBinary = false;
        failureReason = null;
        return true;
    }

    private static (Encoding Encoding, int PreambleLength, bool SkipBinaryHeuristic) DetectEncoding(byte[] bytes)
    {
        if (bytes.Length >= 3 &&
            bytes[0] == 0xEF &&
            bytes[1] == 0xBB &&
            bytes[2] == 0xBF)
        {
            return (Utf8, 3, false);
        }

        if (bytes.Length >= 2 &&
            bytes[0] == 0xFF &&
            bytes[1] == 0xFE)
        {
            return (Utf16LittleEndian, 2, true);
        }

        if (bytes.Length >= 2 &&
            bytes[0] == 0xFE &&
            bytes[1] == 0xFF)
        {
            return (Utf16BigEndian, 2, true);
        }

        return (Utf8, 0, false);
    }

    private static bool LooksBinary(byte[] bytes)
    {
        var checkLength = Math.Min(bytes.Length, 8192);
        var suspiciousBytes = 0;

        for (var index = 0; index < checkLength; index++)
        {
            var value = bytes[index];

            if (value == 0)
                return true;

            if (value < 0x08 || (value > 0x0D && value < 0x20))
                suspiciousBytes++;
        }

        return checkLength > 0 && suspiciousBytes > 32 && suspiciousBytes * 100 / checkLength > 10;
    }
}
