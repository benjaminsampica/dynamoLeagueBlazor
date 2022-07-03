﻿using DynamoLeagueBlazor.Shared.Features.Teams;
using DynamoLeagueBlazor.Shared.Utilities;
using System.Net.Http.Json;

namespace DynamoLeagueBlazor.Tests.Features.Teams;

public class DetailTests : IntegrationTestBase
{
    [Fact]
    public async Task GivenUnauthenticatedUser_ThenDoesNotAllowAccess()
    {
        var application = CreateUnauthenticatedApplication();
        var stubTeam = CreateFakeTeam();
        await application.AddAsync(stubTeam);
        var client = application.CreateClient();
        var endpoint = TeamDetailRouteFactory.Create(stubTeam.Id);

        var response = await client.GetAsync(endpoint);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GivenAnyAuthenticatedUser_WhenGivenValidTeamId_ThenReturnsExpectedResult()
    {
        var application = CreateUserAuthenticatedApplication();

        var stubTeam = CreateFakeTeam();
        await application.AddAsync(stubTeam);

        var mockRosteredPlayer = CreateFakePlayer();
        mockRosteredPlayer.TeamId = stubTeam.Id;
        mockRosteredPlayer.SignForCurrentTeam(DateTime.MaxValue.Year, 1);
        mockRosteredPlayer.State = PlayerState.Rostered;
        await application.AddAsync(mockRosteredPlayer);

        var mockUnrosteredPlayer = CreateFakePlayer();
        mockUnrosteredPlayer.TeamId = stubTeam.Id;
        mockUnrosteredPlayer.SetToUnrostered();
        mockUnrosteredPlayer.State = PlayerState.Unrostered;
        await application.AddAsync(mockUnrosteredPlayer);

        var mockUnsignedPlayer = CreateFakePlayer();
        mockUnsignedPlayer.TeamId = stubTeam.Id;
        mockUnsignedPlayer.State = PlayerState.Unsigned;
        await application.AddAsync(mockUnsignedPlayer);

        var client = application.CreateClient();
        var endpoint = TeamDetailRouteFactory.Create(stubTeam.Id);

        var response = await client.GetFromJsonAsync<TeamDetailResult>(endpoint);
        response.Should().NotBeNull();
        response!.Name.Should().Be(stubTeam.Name);
        var expectedCapSpace = CapSpaceUtilities.GetRemainingCapSpace(
            DateOnly.FromDateTime(DateTime.Today),
            mockRosteredPlayer.ContractValue,
            mockUnrosteredPlayer.ContractValue,
            mockUnsignedPlayer.ContractValue).ToString("C0");
        response.CapSpace.Should().Be(expectedCapSpace);

        response.RosteredPlayers.Should().NotBeEmpty();
        var rosteredPlayer = response.RosteredPlayers.First();
        rosteredPlayer.HeadShotUrl.Should().Be(mockRosteredPlayer.HeadShotUrl);
        rosteredPlayer.Name.Should().Be(mockRosteredPlayer.Name);
        rosteredPlayer.Position.Should().Be(mockRosteredPlayer.Position);
        rosteredPlayer.YearContractExpires.Should().Be(mockRosteredPlayer.YearContractExpires);
        rosteredPlayer.ContractValue.Should().Be(mockRosteredPlayer.ContractValue);

        response.UnrosteredPlayers.Should().NotBeEmpty();
        var unrosteredPlayer = response.UnrosteredPlayers.First();
        unrosteredPlayer.HeadShotUrl.Should().Be(mockUnrosteredPlayer.HeadShotUrl);
        unrosteredPlayer.Name.Should().Be(mockUnrosteredPlayer.Name);
        unrosteredPlayer.Position.Should().Be(mockUnrosteredPlayer.Position);
        unrosteredPlayer.YearContractExpires.Should().Be(mockUnrosteredPlayer.YearContractExpires);
        unrosteredPlayer.ContractValue.Should().Be(mockUnrosteredPlayer.ContractValue);

        response.UnsignedPlayers.Should().NotBeEmpty();
        var unsignedPlayer = response.UnsignedPlayers.First();
        unsignedPlayer.HeadShotUrl.Should().Be(mockUnsignedPlayer.HeadShotUrl);
        unsignedPlayer.Name.Should().Be(mockUnsignedPlayer.Name);
        unsignedPlayer.Position.Should().Be(mockUnsignedPlayer.Position);
        unsignedPlayer.YearContractExpires.Should().Be(mockUnsignedPlayer.YearContractExpires);
        unsignedPlayer.ContractValue.Should().Be(mockUnsignedPlayer.ContractValue);
    }
}
