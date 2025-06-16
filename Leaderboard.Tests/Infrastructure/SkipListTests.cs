using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Leaderboard.Infrastructure;
using Leaderboard.Models;

namespace Leaderboard.Tests.Infrastructure;

public class SkipListTests
{
    private readonly ConcurrentSkipList<Customer> _skipList;

    public SkipListTests()
    {
        _skipList = new ConcurrentSkipList<Customer>();
    }

    /// <summary>
    /// 验证 Add 方法能正确添加客户
    /// </summary>
    [Fact]
    public void Add_AddCustomerCorrectly()
    {
        var customer = new Customer(1, 100);
        _skipList.Add(customer);
        Assert.Equal(1, _skipList.Count);
        Assert.Equal(1, _skipList.GetRank(customer));
    }

    /// <summary>
    /// 能按分数正确排序
    /// </summary>
    [Fact]
    public void Add_MaintainOrderByScore()
    {
        var customer1 = new Customer(1, 100);
        var customer2 = new Customer(2, 200);
        var customer3 = new Customer(3, 150);
        _skipList.Add(customer1);
        _skipList.Add(customer2);
        _skipList.Add(customer3);
        var customers = _skipList.GetRange(0, 3);
        Assert.Equal(3, customers.Count);
        Assert.Equal(200, customers[0].Score); // 最高分
        Assert.Equal(150, customers[1].Score); // 中间分
        Assert.Equal(100, customers[2].Score); // 最低分
    }

    /// <summary>
    /// 正确移除客户
    /// </summary>
    [Fact]
    public void Remove_RemoveCustomerCorrectly()
    {
        var customer = new Customer(1, 100);
        _skipList.Add(customer);
        _skipList.Remove(customer);
        Assert.Equal(0, _skipList.Count);
        Assert.Equal(-1, _skipList.GetRank(customer));
    }

    /// <summary>
    /// 返回正确排名
    /// </summary>
    [Fact]
    public void GetRank_ReturnCorrectRank()
    {
        var customer1 = new Customer(1, 100);
        var customer2 = new Customer(2, 200);
        var customer3 = new Customer(3, 150);
        _skipList.Add(customer1);
        _skipList.Add(customer2);
        _skipList.Add(customer3);
        Assert.Equal(1, _skipList.GetRank(customer2)); // 最高分
        Assert.Equal(2, _skipList.GetRank(customer3)); // 中间分
        Assert.Equal(3, _skipList.GetRank(customer1)); // 最低分
    }

    /// <summary>
    /// 返回指定区间的客户
    /// </summary>
    [Fact]
    public void GetRange_ReturnCorrectRange()
    {
        var customers = new List<Customer>
        {
            new Customer(1, 100),
            new Customer(2, 200),
            new Customer(3, 150),
            new Customer(4, 300),
            new Customer(5, 250)
        };
        foreach (var customer in customers)
        {
            _skipList.Add(customer);
        }
        var range = _skipList.GetRange(1, 3); // 获取第2-4名
        Assert.Equal(3, range.Count);
        Assert.Equal(250, range[0].Score);
        Assert.Equal(200, range[1].Score);
        Assert.Equal(150, range[2].Score);
    }

    /// <summary>
    /// 分数更新后排名是否正确
    /// </summary>
    [Fact]
    public void UpdateScore_MaintainOrder()
    {
        var customer = new Customer(1, 100);
        _skipList.Add(customer);
        _skipList.Remove(customer);
        customer.ChangeLeaderboardScore(200);
        _skipList.Add(customer);
        Assert.Equal(1, _skipList.GetRank(customer));
    }

    /// <summary>
    /// 越界参数的容错
    /// </summary>
    [Fact]
    public void GetRange_WithInvalidParameters_ShouldHandleGracefully()
    {
        var customer = new Customer(1, 100);
        _skipList.Add(customer);
        var emptyRange = _skipList.GetRange(10, 5); // 超出范围
        Assert.Empty(emptyRange);
        var partialRange = _skipList.GetRange(0, 5); // 请求数量大于实际数量
        Assert.Single(partialRange);
    }

    /// <summary>
    /// 对不存在客户返回 -1
    /// </summary>
    [Fact]
    public void GetRank_ForNonExistentCustomer_ShouldReturnMinusOne()
    {
        var customer = new Customer(1, 100);
        var rank = _skipList.GetRank(customer);
        Assert.Equal(-1, rank);
    }

    /// <summary>
    /// 相同分数时插入顺序
    /// </summary>
    [Fact]
    public void Add_WithSameScore_MaintainInsertionOrder()
    {
        //若两个客户分数相同，客户 ID 较小的排名更靠前。
        var customer1 = new Customer(3, 100);
        var customer2 = new Customer(1, 100);
        var customer3 = new Customer(2, 100);
        _skipList.Add(customer1);
        _skipList.Add(customer2);
        _skipList.Add(customer3);
        var customers = _skipList.GetRange(0, 3);
        Assert.Equal(3, customers.Count);
        Assert.Equal(1, customers[0].CustomerId);
        Assert.Equal(2, customers[1].CustomerId);
        Assert.Equal(3, customers[2].CustomerId);
    }
    /// <summary>
    /// 移除不存在客户
    /// </summary>
    [Fact]
    public void Remove_NonExistentCustomer_NotAffectList()
    {
        var customer1 = new Customer(1, 100);
        var customer2 = new Customer(2, 200);
        _skipList.Add(customer1);
        _skipList.Remove(customer2);
        Assert.Equal(1, _skipList.Count);
        Assert.Equal(1, _skipList.GetRank(customer1));
    }
} 