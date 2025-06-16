using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Leaderboard.Models;
using Leaderboard.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Collections.Concurrent;

namespace Leaderboard.Services;

public class LeaderboardService
{
    public readonly ConcurrentDictionary<long, Customer> _customers = new();
    public readonly ConcurrentSkipList<Customer> _leaderboard;

    public LeaderboardService()
    {
        _leaderboard = new ConcurrentSkipList<Customer>();
    }

    public decimal UpdateScore(long customerId, decimal scoreChange)
    {
        if (customerId <= 0)
        {
            throw new ArgumentException("Customer ID must be a positive number", nameof(customerId));
        }

        if (scoreChange < -1000 || scoreChange > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(scoreChange), "Score change must be between -1000 and 1000");
        }

        if (!_customers.TryGetValue(customerId, out var customer))
        {
            customer = new Customer(customerId, 0);
            _customers[customerId] = customer;
        }

        if(customer.Score>0)
        {
            _leaderboard.Remove(customer);
        }

        customer.ChangeLeaderboardScore(scoreChange);

        if (customer.Score > 0)
        {
            _leaderboard.Add(customer);
        }

        return customer.Score;
    }

    public List<CustomerListDto> GetLeaderboard(int start, int end)
    {
        if (start < 1)
        {
            throw new ArgumentException("Start rank must be greater than 0", nameof(start));
        }

        if (end < start)
        {
            throw new ArgumentException("End rank must be greater than or equal to start rank", nameof(end));
        }

        if (start > _leaderboard.Count)
        {
            return [];
        }

        var customers = _leaderboard
            .GetRange(start - 1, Math.Min(end - start + 1, _leaderboard.Count - start + 1))
            .Select((c, index) => new CustomerListDto
            {
                CustomerId = c.CustomerId,
                Score = c.Score,
                Rank = start + index  // 直接使用起始排名 + 索引
            })
            .ToList();

        return customers;
    }

    public List<CustomerListDto> GetCustomerWithNeighbors(long customerId, int high = 0, int low = 0)
    {
        if (customerId <= 0)
        {
            throw new ArgumentException("Customer ID must be a positive number", nameof(customerId));
        }

        if (high < 0 || low < 0)
        {
            throw new ArgumentException("High and low parameters must be non-negative");
        }

        if (!_customers.TryGetValue(customerId, out var customer) || customer.Score <= 0)
        {
            throw new KeyNotFoundException($"Customer {customerId} not found in leaderboard");
        }

        var rank = _leaderboard.GetRank(customer);
        if (rank == -1)
        {
            throw new KeyNotFoundException($"Customer {customerId} not found in leaderboard");
        }

        // 计算高邻居的起始排名
        var highStartRank = Math.Max(1, rank - high);
        // 计算低邻居的结束排名
        var lowEndRank = Math.Min(_leaderboard.Count, rank + low);
        // 计算总区间长度
        var totalCount = lowEndRank - highStartRank + 1;

        var customers = _leaderboard
            .GetRange(highStartRank - 1, totalCount)
            .Select((c, index) => new CustomerListDto
            {
                CustomerId = c.CustomerId,
                Score = c.Score,
                Rank = highStartRank + index  // 直接使用起始排名 + 索引
            })
            .ToList();

        return customers;
    }


    public void Clear()
    {
        _leaderboard.Clear();
        _customers.Clear();
    }

} 