using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TroyOS.UI.Client;
using TroyOS.UI.Client.Features.PromptBuilder.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
if (string.IsNullOrWhiteSpace(apiBaseUrl))
{
	if (builder.HostEnvironment.IsDevelopment())
	{
		apiBaseUrl = "https://localhost:7295/";
	}
	else
	{
		throw new InvalidOperationException("ApiBaseUrl must be configured for production.");
	}
}

var apiUri = new Uri(apiBaseUrl, UriKind.Absolute);

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = apiUri });
builder.Services.AddScoped<IPromptRefinementService, PromptRefinementService>();

await builder.Build().RunAsync();
