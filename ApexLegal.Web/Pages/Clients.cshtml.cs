using System.Text.Json;
using ApexLegal.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ApexLegal.Web.Pages;

public class ClientsModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JsonSerializerOptions _jsonOptions;

    public ClientsModel(IHttpClientFactory httpClientFactory, JsonSerializerOptions jsonOptions)
    {
        _httpClientFactory = httpClientFactory;
        _jsonOptions = jsonOptions;
    }

    [BindProperty]
    public ClientInput Input { get; set; } = new();

    public List<ClientDetails> Clients { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadClientsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadClientsAsync();
            return Page();
        }

        var client = _httpClientFactory.CreateClient("api");
        var response = await client.PostAsJsonAsync("api/clients", new { Name = Input.Name }, _jsonOptions);

        if (response.IsSuccessStatusCode)
        {
            return RedirectToPage();
        }

        ModelState.AddModelError(string.Empty, "Error creating client. The name may already exist.");
        await LoadClientsAsync();
        return Page();
    }

    private async Task LoadClientsAsync()
    {
        var client = _httpClientFactory.CreateClient("api");
        var result = await client.GetFromJsonAsync<List<ClientDetails>>("api/clients", _jsonOptions);
        Clients = result ?? new();
    }

    public class ClientInput
    {
        public string Name { get; set; } = string.Empty;
    }
}
