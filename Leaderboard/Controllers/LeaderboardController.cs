using System;
using Microsoft.AspNetCore.Mvc;
using Leaderboard.Services;
using Leaderboard.Models;
using System.ComponentModel.DataAnnotations;

namespace Leaderboard.Controllers;

[ApiController]
[Route("leaderboard")]
public class LeaderboardController : ControllerBase
{
    private readonly LeaderboardService _leaderboardService;

    public LeaderboardController(LeaderboardService leaderboardService)
    {
        _leaderboardService = leaderboardService;
    }

    [HttpPost("/customer/{customerId}/score/{score}")]
    public ActionResult<decimal> UpdateScore(long customerId, decimal score)
    {
        var newScore = _leaderboardService.UpdateScore(customerId, score);
        return Ok(newScore);

    }

    [HttpGet]
    public ActionResult<List<CustomerListDto>> GetLeaderboard([FromQuery,Required] int start, [FromQuery,Required] int end)
    {
        var response = _leaderboardService.GetLeaderboard(start, end);
        return Ok(response);
    }

    [HttpGet("{customerId}")]
    public ActionResult<List<CustomerListDto>> GetCustomerWithNeighbors(long customerId,[FromQuery] int high = 0,[FromQuery] int low = 0)
    {
        var response = _leaderboardService.GetCustomerWithNeighbors(customerId, high, low);
        return Ok(response);
    }

    [HttpDelete("clear")]
    public ActionResult Clear([FromServices] IHostEnvironment env)
    {
        if (!env.IsDevelopment())
            return NotFound();

        _leaderboardService.Clear();
        return Ok();
    }
} 