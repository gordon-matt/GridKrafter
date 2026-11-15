using GridKrafter.Demo.Api.Data;
using GridKrafter.Demo.Api.Endpoints;

// The demo's data API: the only process that opens the SQLite database. The Wisej demo calls it with the
// application key and, once someone has signed in, their session token.
var builder = WebApplication.CreateBuilder(args);

string appKey = builder.Configuration["GridKrafter:AppKey"] ?? "";
if (appKey.Length < 16)
	throw new InvalidOperationException("Set GridKrafter:AppKey (at least 16 characters) in appsettings.json or an environment variable.");

string databasePath = Path.Combine(builder.Environment.ContentRootPath, builder.Configuration["GridKrafter:Database"] ?? "App_Data/gridkrafter.db");
var database = new Database(databasePath);
database.Initialize(builder.Configuration["GridKrafter:DemoPassword"] ?? "Ethanol!2026");

builder.Services.AddSingleton(database);
builder.Services.AddSingleton<Accounts>();
builder.Services.AddSingleton<Boards>();
builder.Services.AddSingleton<AuditTrail>();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.PropertyNameCaseInsensitive = true);

var app = builder.Build();

app.UseAppKey(appKey);
app.MapGet("/", () => Results.Text("GridKrafter demo API"));
app.MapApi();

app.Run();
