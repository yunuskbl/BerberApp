using System.Net.Http.Json;
using BerberApp.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace BerberApp.Infrastructure.Services;

public class MyMemoryTranslationService : ITranslationService
{
    private readonly HttpClient _http;
    private readonly ILogger<MyMemoryTranslationService> _logger;

    public MyMemoryTranslationService(HttpClient http, ILogger<MyMemoryTranslationService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<string?> TranslateAsync(string text, string targetLang, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        try
        {
            var langPair = $"tr|{targetLang}";
            var url = $"https://api.mymemory.translated.net/get?q={Uri.EscapeDataString(text)}&langpair={langPair}";

            var response = await _http.GetFromJsonAsync<MyMemoryResponse>(url, ct);
            var translated = response?.ResponseData?.TranslatedText;

            // MyMemory bazen orijinal metni geri döner — bunu filtrele
            if (string.IsNullOrWhiteSpace(translated) || translated.Equals(text, StringComparison.OrdinalIgnoreCase))
                return null;

            return translated;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Translation failed for text: {Text} -> {Lang}", text, targetLang);
            return null;
        }
    }

    private class MyMemoryResponse
    {
        public ResponseData? ResponseData { get; set; }
    }

    private class ResponseData
    {
        public string? TranslatedText { get; set; }
    }
}
