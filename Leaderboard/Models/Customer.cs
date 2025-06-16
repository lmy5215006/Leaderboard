using System.Text.Json.Serialization;

namespace Leaderboard.Models;

public class Customer : IComparable<Customer>
{
    [JsonPropertyName("customerId")]
    public long CustomerId { get; private set; }
    [JsonPropertyName("score")]
    public decimal Score { get; private set; }

    /// <summary>
    /// Rich Domain Model
    /// </summary>
    /// <param name="customerId"></param>
    /// <param name="score"></param>
    public Customer(long customerId, decimal score)
    {
        CustomerId = customerId;
        Score = score;
    }


    public Customer ChangeLeaderboardScore(decimal scoreChange)
    {
        Score = Score += scoreChange;
        return this;
    }

    public int CompareTo(Customer? other)
    {
        if (other == null) return 1;

        var scoreComparison = other.Score.CompareTo(Score);
        return scoreComparison != 0 ? scoreComparison : CustomerId.CompareTo(other.CustomerId);
    }
} 