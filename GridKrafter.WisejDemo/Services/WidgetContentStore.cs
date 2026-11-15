using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using GridKrafter.Demo.Api;
using GridKrafter.WisejDemo.Services.Api;
using GridKrafter.WisejDemo.Services.Security;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Services
{
	/// <summary>
	/// Keeps what people write into widgets (to-do tasks, notes, bookmarks, a chart's range) by person and widget id,
	/// through the data API. A widget's content is read once per session and kept here until it changes.
	/// </summary>
	public static class WidgetContentStore
	{
		private const string CACHE_KEY = "gk.content";

		/// <summary>A value saved for a widget, or <see langword="null"/>.</summary>
		public static JsonNode Get(string widgetId, string key)
		{
			if (string.IsNullOrEmpty(widgetId))
				return null;

			var cache = Cache;
			if (!cache.TryGetValue(widgetId, out var content))
			{
				try
				{
					cache[widgetId] = content = DataApi.Content(widgetId);
				}
				catch (DataApiException)
				{
					// the widget starts empty rather than failing the board
					return null;
				}
			}

			return content.TryGetValue(key, out var value) ? value?.DeepClone() : null;
		}

		/// <summary>Saves a value for a widget at once; nothing is kept for a role that may not change boards.</summary>
		public static void Set(string widgetId, string key, JsonNode value)
		{
			if (string.IsNullOrEmpty(widgetId) || !CurrentUser.Can(Permissions.EditBoards))
				return;

			DataApi.SetContent(widgetId, key, value);
			if (Cache.TryGetValue(widgetId, out var content))
				content[key] = value?.DeepClone();
		}

		/// <summary>Forgets what was saved for one widget: a new widget that reuses its id starts empty.</summary>
		public static void Remove(string widgetId)
		{
			if (string.IsNullOrEmpty(widgetId))
				return;

			Cache.Remove(widgetId);
			DataApi.ClearContent(widgetId);
		}

		/// <summary>Forgets everything the person wrote into widgets, with the board.</summary>
		public static void Reset()
		{
			Cache.Clear();
			DataApi.ClearContent(null);
		}

		private static Dictionary<string, Dictionary<string, JsonNode>> Cache
		{
			get
			{
				if (!(Application.Session[CACHE_KEY] is Dictionary<string, Dictionary<string, JsonNode>> cache))
					Application.Session[CACHE_KEY] = cache = new Dictionary<string, Dictionary<string, JsonNode>>(StringComparer.Ordinal);
				return cache;
			}
		}
	}
}
