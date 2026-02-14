using System.Text.Json;
using ApexLegal.Web.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ApexLegal.Web.Pages;

public class DashboardModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JsonSerializerOptions _jsonOptions;

    public DashboardModel(IHttpClientFactory httpClientFactory, JsonSerializerOptions jsonOptions)
    {
        _httpClientFactory = httpClientFactory;
        _jsonOptions = jsonOptions;
    }

    public DashboardStatisticsView? Stats { get; set; }

    public async Task OnGetAsync()
    {
        var client = _httpClientFactory.CreateClient("api");
        Stats = await client.GetFromJsonAsync<DashboardStatisticsView>("api/dashboard", _jsonOptions);
    }
}
