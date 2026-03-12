namespace ClarityBoard.Application.Common.Interfaces;

public interface ITranslationService
{
    /// <summary>
    /// Translates <paramref name="text"/> from <paramref name="sourceLanguage"/> to each target language.
    /// Returns a dictionary keyed by target language code (e.g. "de", "en", "ru").
    /// On failure returns an empty dictionary (caller should fall back gracefully).
    /// </summary>
    Task<Dictionary<string, string>> TranslateAsync(
        string text,
        string sourceLanguage,
        IEnumerable<string> targetLanguages,
        CancellationToken ct = default);
}
