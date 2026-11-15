using System;
using Wisej.Web;

namespace GridKrafter.WisejDemo
{
	internal static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main()
		{
			// The theme switcher starts on Auto: light or dark, as the browser is.
			GridKrafterThemes.FollowBrowser();

			// nothing shows before sign-in; a reload in the same browser session stays signed in
			if (Services.Security.CurrentUser.IsSignedIn)
				Application.MainPage = new Page1();
			else
				Application.MainPage = new LoginPage();
		}

		//
		// You can use the entry method below
		// to receive the parameters from the URL in the args collection.
		//
		//static void Main(NameValueCollection args)
		//{
		//}
	}
}