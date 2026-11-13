using System;
using System.IO;
using GridKrafter.Models;
using GridKrafter.Serialization;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Services
{
	/// <summary>
	/// Keeps the board people arrange in a JSON file in App_Data, so it follows them from browser to
	/// browser, as the MVC demo does: the Dashboard's in board.json, each commodity board's in board-&lt;key&gt;.json.
	/// A real app would key it by user and keep it in its database.
	/// </summary>
	public static class BoardStore
	{
		private static readonly object gate = new object();

		/// <summary>The key of the Dashboard's own board, which keeps its file name, board.json.</summary>
		public const string HomeKey = "home";

		// "home" is board.json, as before there were other boards; every other board is board-<key>.json
		private static string FilePath(string key)
		{
			if (string.IsNullOrWhiteSpace(key) || key == HomeKey)
				return Path.Combine(Application.StartupPath, "App_Data", "board.json");

			// the key names a file: only letters, digits and dashes
			foreach (char c in key)
			{
				if (!char.IsLetterOrDigit(c) && c != '-')
					throw new ArgumentException("A board key has only letters, digits and dashes.", nameof(key));
			}

			return Path.Combine(Application.StartupPath, "App_Data", "board-" + key.ToLowerInvariant() + ".json");
		}

		/// <summary>The Dashboard's saved board, or <see langword="null"/> when nothing has been saved or it can't be read.</summary>
		public static DashboardBoard Load() => Load(HomeKey);

		/// <summary>The saved board of the key, or <see langword="null"/> when nothing has been saved or it can't be read.</summary>
		/// <param name="key">The board's key: "home" for the Dashboard, or a commodity board's key.</param>
		public static DashboardBoard Load(string key)
		{
			string path = FilePath(key);
			string json;
			lock (gate)
			{
				if (!File.Exists(path))
					return null;

				json = File.ReadAllText(path);
			}

			var parsed = DashboardJson.TryParse(json);
			return parsed.IsSuccess ? parsed.Value : null;
		}

		/// <summary>
		/// Saves the Dashboard's board. Content is dropped: the page's own board and the catalog supply it again by id and kind.
		/// </summary>
		public static void Save(DashboardBoard board) => Save(HomeKey, board);

		/// <summary>
		/// Saves the board of the key. Content is dropped: the page's own board and the catalog supply it again by id and kind.
		/// </summary>
		/// <param name="key">The board's key: "home" for the Dashboard, or a commodity board's key.</param>
		/// <param name="b">The board to save.</param>
		public static void Save(string key, DashboardBoard b)
		{
			string path = FilePath(key);
			var board = DashboardJson.Clone(b);
			foreach (var item in board.Items)
			{
				item.Html = null;
				item.Url = null;
				item.Template = null;
			}

			string json = DashboardJson.Serialize(board, indented: true);
			lock (gate)
			{
				Directory.CreateDirectory(Path.GetDirectoryName(path));
				string temporary = path + ".tmp";
				File.WriteAllText(temporary, json);
				File.Move(temporary, path, overwrite: true);
			}
		}

		/// <summary>Forgets the Dashboard's saved board, so the page's own board shows again.</summary>
		public static void Reset() => Reset(HomeKey);

		/// <summary>Forgets the saved board of the key only, so that board's own definition shows again.</summary>
		/// <param name="key">The board's key: "home" for the Dashboard, or a commodity board's key.</param>
		public static void Reset(string key)
		{
			string path = FilePath(key);
			lock (gate)
			{
				File.Delete(path);
			}
		}
	}
}
