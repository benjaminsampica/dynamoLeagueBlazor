﻿using DynamoLeagueBlazor.Client.Features.OfferMatching;
using DynamoLeagueBlazor.Server.Models;
using DynamoLeagueBlazor.Shared.Features.OfferMatching;
using System.Net.Http.Json;

namespace DynamoLeagueBlazor.Tests.Features.OfferMatching;

public class ListServerTests : IntegrationTestBase
{
    [Fact]
    public async Task GivenUnauthenticatedUser_ThenDoesNotAllowAccess()
    {
        var application = CreateUnauthenticatedApplication();

        var client = application.CreateClient();

        var response = await client.GetAsync(OfferMatchingListRouteFactory.Uri);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GivenAnyAuthenticatedUser_WhenThereIsNoPlayersWhoAreOfferMatching_ThenReturnsNothing()
    {
        var application = CreateUserAuthenticatedApplication();

        var client = application.CreateClient();

        var result = await client.GetFromJsonAsync<OfferMatchingListResult>(OfferMatchingListRouteFactory.Uri);

        result.Should().NotBeNull();
        result!.OfferMatches.Should().HaveCount(0);
    }

    [Fact]
    public async Task GivenAnyAuthenticatedUser_WhenThereIsOnePlayerWhoIsInOfferMatching_ThenReturnsOneOfferMatching()
    {
        var application = CreateUserAuthenticatedApplication();

        var mockTeam = CreateFakeTeam();
        await application.AddAsync(mockTeam);

        var mockPlayer = CreateFakePlayer();
        mockPlayer.TeamId = mockTeam.Id;
        mockPlayer.SetToRostered(DateTime.MinValue.Year, int.MaxValue);
        var biddingEnds = DateTime.Today.AddDays(-1);
        mockPlayer.SetToFreeAgent(biddingEnds);
        await application.AddAsync(mockPlayer);

        var bidAmount = int.MaxValue;
        mockPlayer.AddBid(bidAmount, mockTeam.Id);
        await application.UpdateAsync(mockPlayer);

        var client = application.CreateClient();

        var result = await client.GetFromJsonAsync<OfferMatchingListResult>(OfferMatchingListRouteFactory.Uri);

        result.Should().NotBeNull();
        result!.OfferMatches.Should().HaveCount(1);

        var freeAgent = result.OfferMatches.First();
        freeAgent.Id.Should().Be(mockPlayer.Id);
        freeAgent.Name.Should().Be(mockPlayer.Name);
        freeAgent.Position.Should().Be(mockPlayer.Position);
        freeAgent.HeadShotUrl.Should().Be(mockPlayer.HeadShotUrl);
        freeAgent.OfferingTeam.Should().Be(mockTeam.Name);
        freeAgent.Offer.Should().Be(bidAmount);
    }
    [Fact]
    public async Task GivenAnyAuthenticatedUser_WhenPlayerIsMatched_ThenPlayerIsMovedToUnsignedStatus()
    {
        var application = CreateUserAuthenticatedApplication();
        var team = CreateFakeTeam();
        await application.AddAsync(team);

        var player = CreateFakePlayer();
        player.YearContractExpires = DateTime.MaxValue.Year;
        player.AddBid(int.MaxValue, team.Id);
        await application.AddAsync(player);

        var request = AutoFaker.Generate<MatchPlayerRequest>();
        request.PlayerId = player.Id;
        var client = application.CreateClient();

        await client.PostAsJsonAsync(OfferMatchingListRouteFactory.Uri, request);

        var result = await application.FirstOrDefaultAsync<Player>();
        result!.Rostered.Should().Be(false);
        result.YearContractExpires.Should().Be(null);
        result.EndOfFreeAgency.Should().Be(null);
        result.YearAcquired.Should().Be(DateTime.Today.Year);
        result.ContractValue.Should().Be(int.MaxValue);
    }

    [Fact]
    public async Task GivenAnyAuthenticatedUser_WhenPlayerHasNoBids_ThenContractValueIsOne()
    {
        int minimumBid = 1;
        var application = CreateUserAuthenticatedApplication();
        var team = CreateFakeTeam();
        await application.AddAsync(team);

        var player = CreateFakePlayer();
        player.YearContractExpires = DateTime.MaxValue.Year;
        await application.AddAsync(player);

        var request = AutoFaker.Generate<MatchPlayerRequest>();
        request.PlayerId = player.Id;
        var client = application.CreateClient();

        await client.PostAsJsonAsync(OfferMatchingListRouteFactory.Uri, request);

        var result = await application.FirstOrDefaultAsync<Player>();
        result.ContractValue.Should().Be(minimumBid);

    }
}

public class ListClientTests : UITestBase
{
    [Fact]
    public void WhenThePageIsFirstLoaded_ThenShowsAListOfOfferMatches()
    {
        GetHttpHandler.When(HttpMethod.Get, OfferMatchingListRouteFactory.Uri)
            .RespondsWithJson(AutoFaker.Generate<OfferMatchingListResult>());

        var cut = RenderComponent<List>();

        cut.Markup.Contains("<tr>");

        GetHttpHandler.Verify();
    }
}
