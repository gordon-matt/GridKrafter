using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Services
{
	/// <summary>
	/// Keeps what people write into widgets (to-do tasks, notes, bookmarks, a chart's range) by widget id,
	/// in a JSON file in App_Data beside the board, as the Blazor and React demos keep it in the widget's options.
	/// </summary>
	public static class WidgetContentStore
	{
		private static readonly object gate = new object();

		private static string FilePath => Path.Combine(Application.StartupPath, "App_Data", "widgets.json");

		/// <summary>A value saved for a widget, or <see langword="null"/>.</summary>
		public static JsonNode Get(string widgetId, string key)
		{
			if (string.IsNullOrEmpty(widgetId))
				return null;

			lock (gate)
			{
				var all = Read();
				return all.TryGetValue(widgetId, out var content) ? content[key]?.DeepClone() : null;
			}
		}

		/// <summary>Saves a value for a widget at once.</summary>
		public static void Set(string widgetId, string key, JsonNode value)
		{
			if (string.IsNullOrEmpty(widgetId))
				return;

			lock (gate)
			{
				var all = Read();
				if (!all.TryGetValue(widgetId, out var content))
					all[widgetId] = content = new JsonObject();

				content[key] = value?.DeepClone();
				Write(all);
			}
		}

		/// <summary>Forgets what was saved for one widget: a new widget that reuses its id starts empty.</summary>
		public static void Remove(string widgetId)
		{
			if (string.IsNullOrEmpty(widgetId))
				return;

			lock (gate)
			{
				var all = Read();
				if (all.Remove(widgetId))
					Write(all);
			}
		}

		/// <summary>Forgets everything, with the board.</summary>
		public static void Reset()
		{
			lock (gate)
			{
				File.Delete(FilePath);
			}
		}

		private static Dictionary<string, JsonObject> Read()
		{
			var all = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
			if (!File.Exists(FilePath))
				return all;

			try
			{
				if (JsonNode.Parse(File.ReadAllText(FilePath)) is JsonObject root)
				{
					foreach (var pair in root)
					{
						if (pair.Value is JsonObject content)
							all[pair.Key] = (JsonObject)content.DeepClone();
					}
				}
			}
			catch (JsonException)
			{
				// a damaged file starts over
			}

			return all;
		}

		private static void Write(Dictionary<string, JsonObject> all)
		{
			var root = new JsonObject();
			foreach (var pair in all)
				root[pair.Key] = pair.Value;

			Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
			string temporary = FilePath + ".tmp";
			File.WriteAllText(temporary, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
			File.Move(temporary, FilePath, overwrite: true);
		}
	}
}
