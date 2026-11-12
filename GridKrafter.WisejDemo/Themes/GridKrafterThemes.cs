using System;
using Wisej.Web;

namespace GridKrafter.WisejDemo
{
	/// <summary>
	/// The variants of the GridKrafter theme, and the switch between them. Each is a .theme of
	/// the Themes folder, loaded by its name.
	/// </summary>
	public static class GridKrafterThemes
	{
		public const string Light = "GridKrafter-Light";
		public const string Dark = "GridKrafter-Dark";

		/// <summary>Every variant, in order.</summary>
		public static readonly string[] All = { Light, Dark };

		/// <summary>The variant the browser asks for: dark when it is in dark mode. Call it before the first page is shown.</summary>
		public static void FollowBrowser()
		{
			Use(Application.Browser != null && Application.Browser.IsDarkMode ? Dark : Light);
		}

		/// <summary>Shows the application in the variant <paramref name="name"/>, one of the constants of this class.</summary>
		public static void Use(string name)
		{
			Application.LoadTheme(name);
		}

		/// <summary>The variant the application shows; null when it shows a theme that is not one of them.</summary>
		public static string Current
		{
			get
			{
				var name = Application.Theme != null ? Application.Theme.Name : null;
				return Array.IndexOf(All, name) >= 0 ? name : null;
			}
		}

		/// <summary>Shows the next variant: light, then dark, then light again.</summary>
		public static void Toggle()
		{
			Use(All[(Array.IndexOf(All, Current) + 1) % All.Length]);
		}
	}
}
