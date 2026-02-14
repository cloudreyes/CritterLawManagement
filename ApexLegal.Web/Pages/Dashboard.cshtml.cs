using System.Text.Json;
using ApexLegal.Web.Models;
using Microsoft.AspNetCore.Mvc;
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
    public PagedResult<MatterDetails>? Matters { get; set; }

    [BindProperty(SupportsGet = true)]
    public new int Page { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public string SortBy { get; set; } = "CreatedAt";

    [BindProperty(SupportsGet = true)]
    public string SortDirection { get; set; } = "desc";

    public async Task OnGetAsync()
    {
        var client = _httpClientFactory.CreateClient("api");

        var statsTask = client.GetFromJsonAsync<DashboardStatisticsView>("api/dashboard", _jsonOptions);
        var mattersTask = client.GetFromJsonAsync<PagedResult<MatterDetails>>(
            $"api/matters?page={Page}&pageSize=10&sortBy={SortBy}&sortDirection={SortDirection}", _jsonOptions);

        await Task.WhenAll(statsTask, mattersTask);

        Stats = statsTask.Result;
        Matters = mattersTask.Result;
    }

    public string ToggleSortDirection(string column)
    {
        if (SortBy == column && SortDirection == "asc") return "desc";
        if (SortBy == column && SortDirection == "desc") return "asc";
        return "asc";
    }

    public string SortIndicator(string column)
    {
        if (SortBy != column) return "";
        return SortDirection == "asc" ? " ▲" : " ▼";
    }
}
