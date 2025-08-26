namespace YourApp.Models;

public class BitstampEnvelope
{
    public string? Event { get; set; }
    public BitstampOrderBookData? Data { get; set; }
}

public class BitstampOrderBookData
{
    public List<List<string>> bids { get; set; } = new();
    public List<List<string>> asks { get; set; } = new();
}
