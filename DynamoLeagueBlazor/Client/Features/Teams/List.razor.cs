using DynamoLeagueBlazor.Shared.Features.Teams;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using System.Net.Http.Json;

namespace DynamoLeagueBlazor.Client.Features.Teams;

public partial class List
{
    [Inject] private HttpClient HttpClient { get; set; } = null!;

    private GetTeamListResult? _result;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _result = await HttpClient.GetFromJsonAsync<GetTeamListResult>("teams");
        }
        catch (AccessTokenNotAvailableException exception)
        {
            exception.Redirect();
        }
    }
}
