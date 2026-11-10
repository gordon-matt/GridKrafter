using GridKrafter;
using GridKrafter.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddGridKrafter();

// <gk-dashboard board='{"items":[...]}' catalog='[...]' storage-key="home" show-toolbar="true"></gk-dashboard>.
// Parameters map to kebab-case attributes; the element raises gk-changed, gk-layout, gk-mounts,
// gk-refresh, gk-edit-mode and gk-ready DOM events.
builder.RootComponents.RegisterCustomElement<DashboardElement>("gk-dashboard");

await builder.Build().RunAsync();
