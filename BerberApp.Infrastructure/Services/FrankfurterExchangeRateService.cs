using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BerberApp.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace BerberApp.Infrastructure.Services;

public class FrankfurterExchangeRateService : IExchangeRateService
{
    private readonly HttpClient _http;
    private readonly ILogger<FrankfurterExchangeRateService> _logger;

    public FrankfurterExchangeRateService(HttpClient http, ILogger<FrankfurterExchangeRateService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<Dictionary<string, decimal>> GetRatesToTryAsync(IEnumerable<string> currencies, CancellationToken ct = default)
    {
        var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { ["TRY"] = 1m };

        var foreign = currencies.Select(c => c.ToUpper()).Where(c => c != "TRY").Distinct().ToList();
        if (foreign.Count == 0) return result;

        try
        {
            // https://api.frankfurter.app/latest?from=USD,EUR&to=TRY
            var from = string.Join(",", foreign);
            var url = $"https://api.frankfurter.app/latest?from={from}&to=TRY";

            // Multiple base currencies need separate calls; use /latest?to=TRY and extract
            // Actually frankfurter supports only one base at a time, so call per currency
            var tasks = foreign.Select(async currency =>
            {
                try
                {
                    var r = await _http.GetFromJsonAsync<FrankfurterResponse>(
                        $"https://api.frankfurter.app/latest?from={currency}&to=TRY", ct);
                    return (currency, rate: r?.Rates?.GetValueOrDefault("TRY") ?? 0m);
                }
                catch
                {
                    return (currency, rate: 0m);
                }
            });

            var results = await Task.WhenAll(tasks);
            foreach (var (currency, rate) in results)
                if (rate > 0) result[currency] = rate;

            _logger.LogInformation("Exchange rates fetched: {Rates}",
                string.Join(", ", result.Select(kv => $"1 {kv.Key} = {kv.Value:F2} TRY")));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch exchange rates, using 1:1 fallback");
        }

        return result;
    }

    private class FrankfurterResponse
    {
        [JsonPropertyName("rates")]
        public Dictionary<string, decimal>? Rates { get; set; }

        [JsonPropertyName("date")]
        public string? Date { get; set; }
    }
}
