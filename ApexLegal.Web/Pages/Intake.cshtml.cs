using System.Text.Json;
using ApexLegal.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ApexLegal.Web.Pages;

public class IntakeModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JsonSerializerOptions _jsonOptions;

    public IntakeModel(IHttpClientFactory httpClientFactory, JsonSerializerOptions jsonOptions)
    {
        _httpClientFactory = httpClientFactory;
        _jsonOptions = jsonOptions;
    }

    [BindProperty]
    public IntakeInput Input { get; set; } = new();

    public List<SelectListItem> ClientOptions { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadClientOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Input.ClientId == Guid.Empty)
        {
            ModelState.AddModelError("Input.ClientId", "Please select a client.");
        }

        if (!ModelState.IsValid)
        {
            await LoadClientOptionsAsync();
            return Page();
        }

        var client = _httpClientFactory.CreateClient("api");
        var response = await client.PostAsJsonAsync("api/intake", Input, _jsonOptions);

        if (response.IsSuccessStatusCode)
        {
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var matterId = doc.RootElement.GetProperty("matterId").GetGuid();
            return RedirectToPage("/MatterDetails", new { id = matterId });
        }

        ModelState.AddModelError(string.Empty, "Error creating matter. It might be a conflict of interest.");
        await LoadClientOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostCreateClientAsync([FromForm] string newClientName)
    {
        // Clear validation errors from the intake form — we're only creating a client here
        ModelState.Clear();

        if (string.IsNullOrWhiteSpace(newClientName))
        {
            ModelState.AddModelError(string.Empty, "Client name is required.");
            await LoadClientOptionsAsync();
            return Page();
        }

        var httpClient = _httpClientFactory.CreateClient("api");
        var response = await httpClient.PostAsJsonAsync("api/clients",
            new { Name = newClientName.Trim() }, _jsonOptions);

        if (response.IsSuccessStatusCode)
        {
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var newClientId = doc.RootElement.GetProperty("clientId").GetGuid();
            Input.ClientId = newClientId;
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Error creating client. The name may already exist.");
        }

        await LoadClientOptionsAsync();
        return Page();
    }

    private async Task LoadClientOptionsAsync()
    {
        var httpClient = _httpClientFactory.CreateClient("api");
        var clients = await httpClient.GetFromJsonAsync<List<ClientDetails>>("api/clients", _jsonOptions);

        ClientOptions = (clients ?? new())
            .Select(c => new SelectListItem(c.Name, c.Id.ToString()))
            .ToList();
    }

    public class IntakeInput
    {
        public Guid ClientId { get; set; }
        public string OpposingParty { get; set; } = string.Empty;
        public CaseType CaseType { get; set; }
        public decimal InitialClaimAmount { get; set; }
    }
}
