
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Wisej.Core;

namespace GridKrafter.WisejDemo
{
	/// <summary>
	/// The Startup class configures services and the app's request pipeline.
	/// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940.
	/// </summary>
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}
		public IConfiguration Configuration { get; }

		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(new WebApplicationOptions
			{
				Args = args,
				WebRootPath = "./"
			});

			var app = builder.Build();
			app.UseWisej();
			// Add FileServer middleware to serve content files excluding .json files.
			app.UseWhen(
				context => !context.Request.Path.Value.EndsWith(".json", StringComparison.OrdinalIgnoreCase),
				appBuilder => appBuilder.UseFileServer());
			app.Run();
		}
	}
}
