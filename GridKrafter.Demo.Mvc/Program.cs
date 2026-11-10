var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddAntiforgery(options => options.HeaderName = BoardStore.TOKEN_HEADER);

// The dashboard is a Blazor component rendered into the MVC view over a Blazor Server circuit,
// so the catalog is registered here, just as in a Blazor app.
builder.Services.AddServerSideBlazor();
builder.Services.AddGridKrafter(options => DemoBoard.Register(options.Widgets));
builder.Services.AddSingleton<BoardStore>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapControllers();
app.MapBlazorHub();
app.Run();
