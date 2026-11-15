using System.Reflection;

// Starts the demo's data API and the Wisej demo together, and shows both in the Aspire dashboard (logs, state,
// endpoints). The Wisej demo is given the API's address and the key the API expects, so neither file has to match
// the other: Aspire generates the key once and keeps it in this project's user secrets.
var builder = DistributedApplication.CreateBuilder(args);

var appKey = builder.AddParameter("app-key", new GenerateParameterDefault { MinLength = 32, Special = false }, secret: true, persist: true);

// the only process that opens the SQLite database
var api = builder.AddProject<Projects.GridKrafter_Demo_Api>("api")
	.WithEnvironment("GridKrafter__AppKey", appKey);

// The Wisej demo targets both net10.0-windows (which the Visual Studio designer uses) and net10.0, and Aspire can't
// pick one for a project, so it is started with dotnet run on the Windows target. This project's reference to it
// builds it first, in the same configuration.
string wisejProject = new Projects.GridKrafter_WisejDemo().ProjectPath;
string configuration = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration ?? "Debug";

builder.AddExecutable("wisej-demo", "dotnet", Path.GetDirectoryName(wisejProject)!,
		"run", "--project", wisejProject, "--framework", "net10.0-windows", "--configuration", configuration, "--no-build", "--no-launch-profile")
	.WithHttpEndpoint(port: 5000, isProxied: false)
	.WithEnvironment("ASPNETCORE_URLS", "http://localhost:5000")
	.WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
	// the Wisej app serves its pages from its own folder, which Visual Studio passes in its launch profile
	.WithEnvironment("WEBSITE_PATH", Path.GetDirectoryName(wisejProject)!)
	.WithEnvironment("GRIDKRAFTER_API_URL", api.GetEndpoint("http"))
	.WithEnvironment("GRIDKRAFTER_API_KEY", appKey)
	.WaitFor(api)
	.WithExternalHttpEndpoints();

builder.Build().Run();
