// Models used for handling Bitstamp WebSocket messages in the TradingApp.
namespace TradingApp.Models;

// BitstampEnvelope:
//   - Represents the outer message structure received from Bitstamp.
//   - Contains:
//       • Event: The type of event (e.g., "data", "bts:subscription_succeeded").
//       • Data:  The payload containing order book data.
public class BitstampEnvelope
{
    public string? Event { get; set; }
    public BitstampOrderBookData? Data { get; set; }
}

// BitstampOrderBookData:
//   - Represents the actual order book snapshot or update data.
//   - Contains:
//       • bids: A list of bid entries, where each entry is [price, amount] as strings.
//       • asks: A list of ask entries, where each entry is [price, amount] as strings.
public class BitstampOrderBookData
{
    public List<List<string>> bids { get; set; } = new();
    public List<List<string>> asks { get; set; } = new();
}
