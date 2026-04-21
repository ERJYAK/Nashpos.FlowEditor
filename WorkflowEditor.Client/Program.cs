using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Fluxor;
using Grpc.Net.Client.Web;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Services;
using WorkflowEditor.Client;
using WorkflowEditor.Contracts.Grpc;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Указываем, что компонент App будет рендериться в теге <div id="app">
builder.RootComponents.Add<App>("#app");
// Указываем, куда вставлять мета-теги
builder.RootComponents.Add<HeadOutlet>("head::after");

// Регистрируем стандартный HttpClient
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddGrpcClient<WorkflowStorage.WorkflowStorageClient>(o =>
    {
        o.Address = new Uri("https://localhost:7001"); // ИЗМЕНИ ПОРТ НА СВОЙ
    })
    .ConfigurePrimaryHttpMessageHandler(() => new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));

builder.Services.AddMudServices();

// Регистрация стейт-менеджера Fluxor
builder.Services.AddFluxor(o => o
    .ScanAssemblies(typeof(Program).Assembly)
    .UseRouting()
);

await builder.Build().RunAsync();