namespace BerberApp.Application.Common.Interfaces;

public interface IExchangeRateService
{
    /// <summary>
    /// Returns how many TRY equals 1 unit of each given currency.
    /// TRY itself always returns 1.0.
    /// </summary>
    Task<Dictionary<string, decimal>> GetRatesToTryAsync(IEnumerable<string> currencies, CancellationToken ct = default);
}
