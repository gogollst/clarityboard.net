using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace ClarityBoard.Infrastructure.Services.Documents;

public sealed class DocumentTextExtractor
{
    private static readonly Regex LiteralTextRegex = new(@"\((?<text>(?:\\.|[^\\)])*)\)\s*(?:Tj|')", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex DoubleQuoteTextRegex = new(@"[-+]?\d*\.?\d+\s+[-+]?\d*\.?\d+\s+\((?<text>(?:\\.|[^\\)])*)\)\s*""", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex HexTextRegex = new(@"<(?<text>[0-9A-Fa-f\s]+)>\s*Tj", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex ArrayTextRegex = new(@"\[(?<body>.*?)\]\s*TJ", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex ArrayTokenRegex = new(@"\((?<literal>(?:\\.|[^\\)])*)\)|<(?<hex>[0-9A-Fa-f\s]+)>", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly string[] PdfNoiseTokens = ["obj", "endobj", "xref", "stream", "endstream", "/Type", "/Length"];

    public async Task<string> ExtractTextAsync(Stream fileStream, string contentType, CancellationToken ct)
    {
        if (contentType.StartsWith("application/pdf", StringComparison.OrdinalIgnoreCase))
            return await ExtractPdfTextAsync(fileStream, ct);

        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return "[Image document -- OCR extraction pending. Please implement OCR provider.]";

        using var reader = new StreamReader(fileStream);
        return await reader.ReadToEndAsync(ct);
    }

    private static async Task<string> ExtractPdfTextAsync(Stream fileStream, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        if (fileStream.CanSeek)
            fileStream.Position = 0;

        await fileStream.CopyToAsync(buffer, ct);
        var bytes = buffer.ToArray();
        if (bytes.Length == 0)
            return string.Empty;

        var pdfText = Encoding.Latin1.GetString(bytes);
        var candidates = new List<string>();

        AddCandidate(candidates, ExtractStructuredPdfText(pdfText));
        foreach (var streamText in EnumeratePdfStreamContents(bytes, pdfText))
        {
            AddCandidate(candidates, ExtractStructuredPdfText(streamText));
            AddCandidate(candidates, ExtractPrintableText(streamText));
        }

        AddCandidate(candidates, ExtractPrintableText(pdfText));
        var bestCandidate = candidates.OrderByDescending(ScoreCandidate).FirstOrDefault();
        return bestCandidate is not null && ScoreCandidate(bestCandidate) > 5
            ? bestCandidate
            : string.Empty;
    }

    private static IEnumerable<string> EnumeratePdfStreamContents(byte[] pdfBytes, string pdfText)
    {
        const string streamToken = "stream";
        const string endStreamToken = "endstream";

        for (var searchIndex = 0; searchIndex < pdfText.Length;)
        {
            var streamIndex = pdfText.IndexOf(streamToken, searchIndex, StringComparison.Ordinal);
            if (streamIndex < 0)
                yield break;

            var dataStart = streamIndex + streamToken.Length;
            if (dataStart < pdfBytes.Length && pdfBytes[dataStart] == '\r') dataStart++;
            if (dataStart < pdfBytes.Length && pdfBytes[dataStart] == '\n') dataStart++;

            var endStreamIndex = pdfText.IndexOf(endStreamToken, dataStart, StringComparison.Ordinal);
            if (endStreamIndex < 0)
                yield break;

            var length = Math.Max(0, endStreamIndex - dataStart);
            if (length > 0)
            {
                var streamBytes = pdfBytes.AsSpan(dataStart, length).ToArray();
                yield return Encoding.Latin1.GetString(TrimTrailingLineBreaks(streamBytes));
                if (TryInflate(streamBytes, out var inflatedBytes))
                    yield return Encoding.Latin1.GetString(inflatedBytes);
            }

            searchIndex = endStreamIndex + endStreamToken.Length;
        }
    }

    private static bool TryInflate(byte[] streamBytes, out byte[] inflatedBytes)
    {
        foreach (var factory in new Func<Stream, Stream>[] { s => new ZLibStream(s, CompressionMode.Decompress, leaveOpen: false), s => new DeflateStream(s, CompressionMode.Decompress, leaveOpen: false) })
        {
            try
            {
                using var input = new MemoryStream(streamBytes);
                using var inflater = factory(input);
                using var output = new MemoryStream();
                inflater.CopyTo(output);
                inflatedBytes = output.ToArray();
                return inflatedBytes.Length > 0;
            }
            catch
            {
                // Try next decompression strategy.
            }
        }

        inflatedBytes = [];
        return false;
    }

    private static string ExtractStructuredPdfText(string content)
    {
        var snippets = new List<string>();
        snippets.AddRange(LiteralTextRegex.Matches(content).Cast<Match>().Select(m => DecodeLiteralPdfString(m.Groups["text"].Value)));
        snippets.AddRange(DoubleQuoteTextRegex.Matches(content).Cast<Match>().Select(m => DecodeLiteralPdfString(m.Groups["text"].Value)));
        snippets.AddRange(HexTextRegex.Matches(content).Cast<Match>().Select(m => DecodeHexPdfString(m.Groups["text"].Value)));

        foreach (Match match in ArrayTextRegex.Matches(content))
        {
            var parts = ArrayTokenRegex.Matches(match.Groups["body"].Value).Cast<Match>()
                .Select(token => token.Groups["literal"].Success
                    ? DecodeLiteralPdfString(token.Groups["literal"].Value)
                    : DecodeHexPdfString(token.Groups["hex"].Value));
            snippets.Add(string.Join(" ", parts));
        }

        return NormalizeText(snippets);
    }

    private static string DecodeLiteralPdfString(string value)
    {
        var sb = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];
            if (current != '\\') { sb.Append(current); continue; }
            if (++i >= value.Length) break;
            var escaped = value[i];
            if (escaped is >= '0' and <= '7')
            {
                var octal = new StringBuilder().Append(escaped);
                while (i + 1 < value.Length && octal.Length < 3 && value[i + 1] is >= '0' and <= '7') octal.Append(value[++i]);
                sb.Append((char)Convert.ToInt32(octal.ToString(), 8));
                continue;
            }

            sb.Append(escaped switch { 'n' => '\n', 'r' => '\r', 't' => '\t', 'b' => '\b', 'f' => '\f', '(' => '(', ')' => ')', '\\' => '\\', _ => escaped });
        }

        return sb.ToString();
    }

    private static string DecodeHexPdfString(string hex)
    {
        var cleanHex = new string(hex.Where(Uri.IsHexDigit).ToArray());
        if (string.IsNullOrWhiteSpace(cleanHex)) return string.Empty;
        if (cleanHex.Length % 2 != 0) cleanHex += "0";
        var bytes = Convert.FromHexString(cleanHex);
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF) return Encoding.BigEndianUnicode.GetString(bytes[2..]);
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE) return Encoding.Unicode.GetString(bytes[2..]);
        var utf8Text = Encoding.UTF8.GetString(bytes);
        return utf8Text.Contains("\uFFFD", StringComparison.Ordinal)
            ? Encoding.Latin1.GetString(bytes)
            : utf8Text;
    }

    private static string ExtractPrintableText(string content)
    {
        var runs = new List<string>();
        var current = new StringBuilder();
        foreach (var character in content)
        {
            if (!char.IsControl(character) || character is '\n' or '\r' or '\t') current.Append(character);
            else FlushRun();
        }
        FlushRun();
        return NormalizeText(runs);

        void FlushRun()
        {
            if (current.Length >= 8 && current.ToString().Any(char.IsLetterOrDigit)) runs.Add(current.ToString());
            current.Clear();
        }
    }

    private static string NormalizeText(IEnumerable<string> snippets) => string.Join(Environment.NewLine, snippets
        .Select(s => Regex.Replace(s, @"\s+", " ").Trim())
        .Where(s => s.Length >= 3 && s.Any(char.IsLetterOrDigit))
        .Distinct(StringComparer.Ordinal));

    private static int ScoreCandidate(string candidate)
    {
        var alphaNumeric = candidate.Count(char.IsLetterOrDigit);
        var penalty = PdfNoiseTokens.Count(token => candidate.Contains(token, StringComparison.OrdinalIgnoreCase)) * 25;
        return alphaNumeric - penalty;
    }

    private static void AddCandidate(List<string> candidates, string candidate)
    {
        if (!string.IsNullOrWhiteSpace(candidate)) candidates.Add(candidate);
    }

    private static byte[] TrimTrailingLineBreaks(byte[] value)
    {
        var end = value.Length;
        while (end > 0 && value[end - 1] is (byte)'\r' or (byte)'\n') end--;
        return value[..end];
    }
}