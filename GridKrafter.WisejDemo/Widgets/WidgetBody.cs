using System.Text.Json.Nodes;
using GridKrafter.WisejDemo.Controls;
using GridKrafter.WisejDemo.Services;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Widgets
{
	/// <summary>
	/// The body of a widget whose content people change (tasks, notes, links, a chart's range). It keeps
	/// that content by the widget's id, as the Blazor demo keeps it in the widget's options.
	/// </summary>
	public abstract class WidgetBody : Panel
	{
		/// <summary>The id the content was loaded for; empty for a widget just taken from the catalog.</summary>
		protected string ContentId { get; private set; } = "";

		/// <summary>Shows what was saved for the widget with this id, or the starting content when nothing was.</summary>
		public void LoadContent(string widgetId)
		{
			this.ContentId = widgetId ?? "";
			OnLoadContent();
		}

		/// <summary>Fills the body from <see cref="Load"/>.</summary>
		protected abstract void OnLoadContent();

		/// <summary>A value saved for this widget, or <see langword="null"/>.</summary>
		protected JsonNode Load(string key) => WidgetContentStore.Get(this.ContentId, key);

		/// <summary>Saves a value for this widget, under the id the widget has now.</summary>
		protected void Save(string key, JsonNode value)
		{
			if (this.Parent is GridWidget widget && !string.IsNullOrEmpty(widget.EffectiveId))
				this.ContentId = widget.EffectiveId;

			WidgetContentStore.Set(this.ContentId, key, value);
		}
	}
}
