using System.Globalization;
using TradingApp.Models;

namespace TradingApp.Services;

/// <summary>
/// Provides methods for calculating trade quotes based on order book data.
/// </summary>
public static class QuoteCalculator
{
    /// <summary>
    /// Calculates the total cost in EUR to purchase the requested BTC amount from the given order book.
    /// </summary>
    /// <param name="book">The order book containing ask prices and sizes.</param>
    /// <param name="btcAmount">The requested BTC amount to buy.</param>
    /// <returns>
    /// The total EUR cost if the order can be fully filled, or -1 if insufficient liquidity is available.
    /// </returns>
    public static decimal CalculateQuote(BitstampOrderBookData book, decimal btcAmount)
    {
        if (book.asks.Count == 0) return -1m;

        decimal remaining = btcAmount, cost = 0;
        foreach (var lvl in book.asks)
        {
            if (lvl.Count < 2 ||
                !decimal.TryParse(lvl[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var price) ||
                !decimal.TryParse(lvl[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var size)) continue;

            var take = Math.Min(remaining, size);
            cost += take * price;
            remaining -= take;
            if (remaining <= 0) break;
        }

        return remaining > 0 ? -1m : cost;
    }
}
