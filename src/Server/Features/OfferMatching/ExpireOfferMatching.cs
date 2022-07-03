﻿using Coravel.Invocable;
using DynamoLeagueBlazor.Server.Infrastructure;
using Microsoft.EntityFrameworkCore;
using static DynamoLeagueBlazor.Server.Models.Player;

namespace DynamoLeagueBlazor.Server.Features.OfferMatching;

public class ExpireOfferMatchingService : IInvocable
{
    private readonly ApplicationDbContext _dbContext;

    public ExpireOfferMatchingService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Invoke()
    {
        var players = _dbContext.Players
            .AsTracking()
            .Where(p => p.State == PlayerState.OfferMatching
                && p.EndOfFreeAgency!.Value.AddDays(3) <= DateTime.Today);

        foreach (var player in players)
        {
            player.ExpireMatch();
        }

        await _dbContext.SaveChangesAsync();
    }
}
