using System;
using System.Linq;
using Xunit;
using Leaderboard.Services;
using Leaderboard.Models;
using System.Runtime.InteropServices;

namespace Leaderboard.Tests.Services;

public class LeaderboardServiceTests
{
    private readonly LeaderboardService _service;

    public LeaderboardServiceTests()
    {
        _service = new LeaderboardService();
    }

    /// <summary>
    /// 多个客户分数更新后的排序
    /// </summary>
    [Fact]
    public void UpdateScore_MultipleCustomers_MaintainCorrectOrder_V1()
    {
        _service.UpdateScore(1, -100);
        _service.UpdateScore(1, 200);
        _service.UpdateScore(1, -100);

        Assert.Null(_service._leaderboard.FirstOrDefault());
        _service.UpdateScore(1, 1);
        Assert.Equal(1, _service._leaderboard.First().Score);


    }

    /// <summary>
    /// 多个客户分数更新后的排序
    /// </summary>
    [Fact]
    public void UpdateScore_MultipleCustomers_MaintainCorrectOrder_V2()
    {
        _service.UpdateScore(1, 100);
        _service.UpdateScore(1, -10);
        _service.UpdateScore(2, 20);
        _service.UpdateScore(2, -90);
        _service.UpdateScore(3, 8);
        _service.UpdateScore(3, -6);
        _service.UpdateScore(4, 200);
        _service.UpdateScore(4, -900);
        _service.UpdateScore(5, 200);
        _service.UpdateScore(6, -400);


        Assert.Equal(_service._leaderboard?.Count, 3);
        Assert.Equal(_service._customers?.Count, 6);
    }

    /// <summary>
    /// 多个客户分数更新后的排序
    /// </summary>
    [Fact]
    public void UpdateScore_MultipleCustomers_MaintainCorrectOrder_V3()
    {
        _service.UpdateScore(1, 100);
        _service.UpdateScore(2, 200);
        _service.UpdateScore(3, 150);

        var customers = _service._leaderboard.ToList();
        Assert.Equal(3, customers.Count);
        Assert.Equal(200, customers[0].Score); // 最高分
        Assert.Equal(150, customers[1].Score); // 中间分
        Assert.Equal(100, customers[2].Score); // 最低分
    }

    /// <summary>
    /// 分数变为0时从排行榜移除
    /// </summary>
    [Fact]
    public void UpdateScore_WhenScoreBecomesZero_RemoveFromLeaderboard()
    {
        _service.UpdateScore(1, 100);
        _service.UpdateScore(1, -100);
        Assert.Null(_service._leaderboard.FirstOrDefault());
    }

    /// <summary>
    /// 分数变为负数时从排行榜移除
    /// </summary>
    [Fact]
    public void UpdateScore_WhenScoreBecomesNegative_RemoveFromLeaderboard()
    {
        _service.UpdateScore(1, 100);
        _service.UpdateScore(1, -200);
        Assert.Null(_service._leaderboard.FirstOrDefault());
    }

    [Fact]
    public void UpdateScore_WhenScoreBecomesPositive_AddToLeaderboard()
    {
        // 测试分数变为正数时添加到排行榜
        _service.UpdateScore(1, -100);
        _service.UpdateScore(1, 200);
        var customer = _service._leaderboard.First();
        Assert.Equal(1, customer.CustomerId);
        Assert.Equal(100, customer.Score);
    }

    /// <summary>
    /// 同一客户多次更新分数
    /// </summary>
    [Fact]
    public void UpdateScore_SameCustomerMultipleTimes_UpdateCorrectly()
    {
        _service.UpdateScore(1, 100);
        _service.UpdateScore(1, 200);
        _service.UpdateScore(1, 50);
        
        var customer = _service._leaderboard.First();
        Assert.Equal(1, customer.CustomerId);
        Assert.Equal(350, customer.Score);
    }

    /// <summary>
    /// 分数为0时不应添加到排行榜
    /// </summary>
    [Fact]
    public void UpdateScore_WithZeroScore_NotAddToLeaderboard()
    {
        _service.UpdateScore(1, 0);
        Assert.Null(_service._leaderboard.FirstOrDefault());
    }

    /// <summary>
    /// 大数字边界处理
    /// </summary>
    [Fact]
    public void UpdateScore_WithLargeNumbers_HandleCorrectly()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _service.UpdateScore(1, int.MaxValue);
        });
        
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _service.UpdateScore(2, int.MaxValue - 1);
        });
    }

    /// <summary>
    /// 初始负分不应添加到排行榜
    /// </summary>
    [Fact]
    public void UpdateScore_WithNegativeInitialScore_NotAddToLeaderboard()
    {
        _service.UpdateScore(1, -100);
        Assert.Null(_service._leaderboard.FirstOrDefault());
    }

    /// <summary>
    /// 多个客户的添加和移除
    /// </summary>
    [Fact]
    public void UpdateScore_WithMultipleCustomersAndRemovals_MaintainCorrectState()
    {
        _service.UpdateScore(1, 100);
        _service.UpdateScore(2, 200);
        _service.UpdateScore(3, 300);
        _service.UpdateScore(2, -200); // 移除客户2
        _service.UpdateScore(4, 400);
        
        var customers = _service._leaderboard.ToList();
        Assert.Equal(3, customers.Count);
        Assert.Equal(400, customers[0].Score);
        Assert.Equal(300, customers[1].Score);
        Assert.Equal(100, customers[2].Score);
    }

    /// <summary>
    /// 相同分数的插入顺序
    /// </summary>
    [Fact]
    public void UpdateScore_WithSameScore_MaintainInsertionOrder()
    {
        _service.UpdateScore(1, 100);
        _service.UpdateScore(2, 100);
        _service.UpdateScore(3, 100);
        
        var customers = _service._leaderboard.ToList();
        Assert.Equal(3, customers.Count);
        Assert.Equal(1, customers[0].CustomerId);
        Assert.Equal(2, customers[1].CustomerId);
        Assert.Equal(3, customers[2].CustomerId);
    }
}