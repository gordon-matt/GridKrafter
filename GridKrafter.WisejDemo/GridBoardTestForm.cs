using System;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Controls
{
    /// <summary>
    /// A form to try GridBoard on: open it in the designer to see the component, or show it
    /// from your application to try it live.
    /// </summary>
    public partial class GridBoardTestForm : Form
    {
        public GridBoardTestForm()
        {
            InitializeComponent();

            this.gridBoard1.Changed += (sender, e) =>
                this.labelEvents.Text = "Changed: " + e.Kind + " " + (e.ItemId ?? "") + " in " + e.SectionId + " (" + e.Layout?.Id + ")";
            this.gridBoard1.LayoutChanged += (sender, e) =>
                this.labelEvents.Text = "LayoutChanged: " + e.Breakpoint + ", " + e.Columns + " columns";
            this.gridBoard1.WidgetRefresh += (sender, e) =>
                this.labelEvents.Text = "WidgetRefresh: " + e.Widget.ItemId + " at " + DateTime.Now.ToLongTimeString();
            this.gridBoard1.SectionAdding += (sender, e) =>
                this.labelEvents.Text = "SectionAdding: " + e.Title;
            this.gridBoard1.AddWidgetRequested += GridBoard1_AddWidgetRequested;
        }

        // the page makes the widget for the kind, with its body, and the board finds it a place
        private void GridBoard1_AddWidgetRequested(object sender, GridWidgetKindEventArgs e)
        {
            var widget = new GridWidget
            {
                Kind = e.Kind,
                Title = e.Item.DisplayName,
                Icon = e.Item.Icon,
                Color = e.Item.Color,
                GridW = e.Item.W,
                GridH = e.Item.H,
                MinW = e.Item.MinW,
                MinH = e.Item.MinH,
                RefreshInterval = e.Item.Refresh,
                Chrome = e.Kind == "stat" ? GridWidgetChrome.Plain : GridWidgetChrome.Card,
            };
            widget.Controls.Add(new Label { Dock = DockStyle.Fill, Padding = new Padding(14, 12, 14, 12), Text = e.Item.Description });

            this.gridBoard1.AddWidget(widget);
            this.labelEvents.Text = "AddWidgetRequested: " + e.Kind;
        }
    }
}
