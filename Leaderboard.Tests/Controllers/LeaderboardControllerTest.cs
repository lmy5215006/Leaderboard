using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Leaderboard.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Leaderboard.Tests.Controllers
{
    public class LeaderboardControllerTest : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public LeaderboardControllerTest(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        /// <summary>
        /// 更新有效分数
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task UpdateScore_ValidScore_ReturnsUpdatedScore()
        {

            var response = await _client.PostAsync("/customer/1/score/100", null);
            var content = await response.Content.ReadAsStringAsync();
            var score = JsonSerializer.Deserialize<int>(content);
            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(100, score);
        }
        /// <summary>
        /// 更新无效分数
        /// </summary>
        /// <returns></returns>

        [Fact]
        public async Task UpdateScore_InvalidScore_ReturnsBadRequest()
        {
            var response = await _client.PostAsync("/customer/1/score/1001", null);
            
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        /// <summary>
        /// 反复更新分数
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task UpdateScore_NegativeScore_ReturnsUpdatedScore()
        {
            await ClearTestDataAsync();
            // 先添加分数
            await _client.PostAsync("/customer/1/score/100", null);

            // 测试减分
            var response = await _client.PostAsync("/customer/1/score/-20", null);
            var content = await response.Content.ReadAsStringAsync();
            var score = JsonSerializer.Deserialize<int>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(80, score);
        }

        /// <summary>
        /// 获取排名
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task GetLeaderboard_ValidRange_ReturnsCorrectCustomers()
        {
            await ClearTestDataAsync();
            await _client.PostAsync("/customer/1/score/100", null);
            await _client.PostAsync("/customer/2/score/200", null);
            await _client.PostAsync("/customer/3/score/150", null);

            var response = await _client.GetAsync("/leaderboard?start=1&end=3");
            var content = await response.Content.ReadAsStringAsync();
            var customers = JsonSerializer.Deserialize<List<CustomerListDto>>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(3, customers.Count);
            Assert.Equal(2, customers[0].CustomerId); // 分数最高的在前
            Assert.Equal(3, customers[1].CustomerId);
            Assert.Equal(1, customers[2].CustomerId);
        }

        /// <summary>
        /// 获取无效的排名
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task GetLeaderboard_InvalidRange_ReturnsBadRequest()
        {
            var response = await _client.GetAsync("/leaderboard?start=2&end=1");
            
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        /// <summary>
        /// 按排名查询客户
        /// </summary>
        /// <returns></returns>

        [Fact]
        public async Task GetCustomerWithNeighbors_ValidRequest_ReturnsCorrectCustomers()
        {
            await _client.PostAsync("/customer/1/score/100", null);
            await _client.PostAsync("/customer/2/score/200", null);
            await _client.PostAsync("/customer/3/score/150", null);
            await _client.PostAsync("/customer/4/score/120", null);
            await _client.PostAsync("/customer/5/score/80", null);

            var response = await _client.GetAsync("/leaderboard/3?high=1&low=1");
            var content = await response.Content.ReadAsStringAsync();
            var customers = JsonSerializer.Deserialize<List<CustomerListDto>>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(3, customers.Count);
            Assert.Equal(2, customers[0].CustomerId); // 高排名
            Assert.Equal(3, customers[1].CustomerId); // 目标客户
            Assert.Equal(4, customers[2].CustomerId); // 低排名
        }
        /// <summary>
        /// 按排名查询客户，客户不存在
        /// </summary>
        /// <returns></returns>

        [Fact]
        public async Task GetCustomerWithNeighbors_NonExistingCustomer_ReturnsNotFound()
        {
            var response = await _client.GetAsync("/leaderboard/999?high=1&low=1");
            
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        /// <summary>
        /// 反复添加分数，客户被移除
        /// </summary>
        /// <returns></returns>

        [Fact]
        public async Task UpdateScore_ZeroScore_RemovesCustomer()
        {
            await ClearTestDataAsync();

            await _client.PostAsync("/customer/1/score/100", null);
            await _client.PostAsync("/customer/1/score/-100", null);

            var response = await _client.GetAsync("/leaderboard/1?high=0&low=0");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        /// <summary>
        /// 空排行榜
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task GetLeaderboard_EmptyLeaderboard_ReturnsEmptyList()
        {
            await ClearTestDataAsync();
            var response = await _client.GetAsync("/leaderboard?start=1&end=10");
            var content = await response.Content.ReadAsStringAsync();
            var customers = JsonSerializer.Deserialize<List<CustomerListDto>>(content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(customers);
        }

        /// <summary>
        /// 移除测试数据
        /// </summary>
        /// <returns></returns>
        private async Task ClearTestDataAsync()
        {
            await _client.DeleteAsync("/leaderboard/clear");
        }
    }
}
