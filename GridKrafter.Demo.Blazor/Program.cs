using GridKrafter.Demo.Blazor.Components;
using Heroicsoft.NetForge.WebAssets;
using GridKrafter.Demo.Blazor.Widgets;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddGridKrafter(options => DemoWidgets.Register(options.Widgets));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapEmbeddedContent(typeof(Dashboard).Assembly);
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
