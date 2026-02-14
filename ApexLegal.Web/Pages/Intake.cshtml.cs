using System.Text.Json;
using ApexLegal.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ApexLegal.Web.Pages;

public class IntakeModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public IntakeModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [BindProperty]
    public IntakeInput Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var client = _httpClientFactory.CreateClient("api");
        var response = await client.PostAsJsonAsync("api/intake", Input);

        if (response.IsSuccessStatusCode)
        {
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var matterId = doc.RootElement.GetProperty("matterId").GetGuid();
            return RedirectToPage("/MatterDetails", new { id = matterId });
        }

        ModelState.AddModelError(string.Empty, "Error creating matter. It might be a conflict of interest.");
        return Page();
    }

    public class IntakeInput
    {
        public string ClientName { get; set; } = string.Empty;
        public string OpposingParty { get; set; } = string.Empty;
        public CaseType CaseType { get; set; }
        public decimal InitialClaimAmount { get; set; }
    }
}
