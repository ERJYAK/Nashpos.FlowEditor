using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Fluxor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Регистрация стейт-менеджера Fluxor
builder.Services.AddFluxor(o => o
    .ScanAssemblies(typeof(Program).Assembly)
    .UseRouting()
);

await builder.Build().RunAsync();