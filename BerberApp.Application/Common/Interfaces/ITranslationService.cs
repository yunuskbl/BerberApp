namespace BerberApp.Application.Common.Interfaces;

public interface ITranslationService
{
    Task<string?> TranslateAsync(string text, string targetLang, CancellationToken ct = default);
}
