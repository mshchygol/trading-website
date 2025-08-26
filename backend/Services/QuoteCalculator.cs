using System.Globalization;
using YourApp.Models;

namespace YourApp.Services;

public static class QuoteCalculator
{
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
