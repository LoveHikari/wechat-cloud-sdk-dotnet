using System.Text.Json.Serialization;

namespace TestProject1;

public class Building
{
    [JsonPropertyName("_id")]
    public string Id { get; set; }
    [JsonPropertyName("no")]
    public int No { get; set; }
    [JsonPropertyName("building")]
    public int building { get; set; }
    [JsonPropertyName("salePrice")]
    public decimal? SalePrice { get; set; }
    [JsonPropertyName("park")]
    public string ParkId { get; set; }
}