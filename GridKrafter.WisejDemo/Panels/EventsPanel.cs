using System;
using System.Linq;
using GridKrafter.WisejDemo.Services;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Panels
{
	/// <summary>
	/// The board's latest events, newest first: what the MVC page lists under "Events".
	/// </summary>
	public partial class EventsPanel : Wisej.Web.UserControl
	{
		public EventsPanel()
		{
			InitializeComponent();
			this.Disposed += this.Unsubscribe;
		}

		private void EventsPanel_Load(object sender, EventArgs e)
		{
			BoardEvents.Changed += this.BoardEvents_Changed;
			Fill();
		}

		private void Unsubscribe(object sender, EventArgs e)
		{
			BoardEvents.Changed -= this.BoardEvents_Changed;
		}

		private void BoardEvents_Changed(object sender, EventArgs e)
		{
			Fill();
		}

		private void Fill()
		{
			this.dataGridViewEvents.Rows.Clear();
			foreach (var entry in BoardEvents.Entries)
				this.dataGridViewEvents.Rows.Add(entry.At.ToLongTimeString(), entry.Kind, entry.Detail);

			this.labelEmpty.Visible = !BoardEvents.Entries.Any();
		}
	}
}
