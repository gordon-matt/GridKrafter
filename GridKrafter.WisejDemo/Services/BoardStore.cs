using System;
using System.IO;
using GridKrafter.Models;
using GridKrafter.Serialization;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Services
{
	/// <summary>
	/// Keeps the board people arrange in a JSON file in App_Data, so it follows them from browser to
	/// browser, as the MVC demo does. A real app would key it by user and keep it in its database.
	/// </summary>
	public static class BoardStore
	{
		private static readonly object gate = new object();

		private static string FilePath => Path.Combine(Application.StartupPath, "App_Data", "board.json");

		/// <summary>The saved board, or <see langword="null"/> when nothing has been saved or it can't be read.</summary>
		public static DashboardBoard Load()
		{
			string json;
			lock (gate)
			{
				if (!File.Exists(FilePath))
					return null;

				json = File.ReadAllText(FilePath);
			}

			var parsed = DashboardJson.TryParse(json);
			return parsed.IsSuccess ? parsed.Value : null;
		}

		/// <summary>
		/// Saves a board. Content is dropped: the page's own board and the catalog supply it again by id and kind.
		/// </summary>
		public static void Save(DashboardBoard board)
		{
			board = DashboardJson.Clone(board);
			foreach (var item in board.Items)
			{
				item.Html = null;
				item.Url = null;
				item.Template = null;
			}

			string json = DashboardJson.Serialize(board, indented: true);
			lock (gate)
			{
				Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
				string temporary = FilePath + ".tmp";
				File.WriteAllText(temporary, json);
				File.Move(temporary, FilePath, overwrite: true);
			}
		}

		/// <summary>Forgets the saved board, so the page's own board shows again.</summary>
		public static void Reset()
		{
			lock (gate)
			{
				File.Delete(FilePath);
			}
		}
	}
}
