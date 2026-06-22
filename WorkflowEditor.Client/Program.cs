using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Fluxor;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Services;
using WorkflowEditor.Client;
using WorkflowEditor.Client.Services.Files;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Указываем, что компонент App будет рендериться в теге <div id="app">
builder.RootComponents.Add<App>("#app");
// Указываем, куда вставлять мета-теги
builder.RootComponents.Add<HeadOutlet>("head::after");

// Регистрируем стандартный HttpClient
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<IFileDownloader, BrowserFileDownloader>();
builder.Services.AddMudServices();

// Регистрация стейт-менеджера Fluxor
builder.Services.AddFluxor(o => o
    .ScanAssemblies(typeof(Program).Assembly)
    .UseRouting()
);

await builder.Build().RunAsync();