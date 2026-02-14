using ApexLegal.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ApexLegal.Web.Pages;

public class MatterDetailsModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public MatterDetailsModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public MatterDetails? Matter { get; set; }
    public List<EventRecord> History { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var client = _httpClientFactory.CreateClient("api");
        
        try 
        {
            Matter = await client.GetFromJsonAsync<MatterDetails>($"api/matters/{Id}");
            var historyResponse = await client.GetFromJsonAsync<List<EventRecord>>($"api/matters/{Id}/history");
            if (historyResponse != null)
            {
                History = historyResponse;
            }
        }
        catch (HttpRequestException)
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync([FromForm] MatterStatus newStatus, [FromForm] string reason)
    {
        var client = _httpClientFactory.CreateClient("api");
        var response = await client.PostAsJsonAsync($"api/matters/{Id}/status", new { newStatus, reason });

        if (response.IsSuccessStatusCode)
        {
            return RedirectToPage(new { id = Id });
        }

        ModelState.AddModelError(string.Empty, "Failed to update status.");
        return await OnGetAsync();
    }
}
