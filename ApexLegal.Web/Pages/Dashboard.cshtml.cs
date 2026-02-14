using ApexLegal.Web.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ApexLegal.Web.Pages;

public class DashboardModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DashboardModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public DashboardStatisticsView? Stats { get; set; }

    public async Task OnGetAsync()
    {
        var client = _httpClientFactory.CreateClient("api");
        Stats = await client.GetFromJsonAsync<DashboardStatisticsView>("api/dashboard");
    }
}
