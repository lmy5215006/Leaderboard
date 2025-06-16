using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Leaderboard.Models;

public class CustomerListDto
{
    [JsonPropertyName("customerId")]
    public long CustomerId { get; set; }
    [JsonPropertyName("score")]
    public decimal Score { get; set; }
    [JsonPropertyName("rank")]
    public int Rank { get; set; }
} 