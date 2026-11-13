using System;
using Wisej.Web;

namespace GridKrafter.WisejDemo
{
    /// <summary>
    /// A form to try PnlWaterfall on: open it in the designer to see the component, or show it
    /// from your application to try it live.
    /// </summary>
    public partial class PnlWaterfallTestForm : Form
    {
        public PnlWaterfallTestForm()
        {
            InitializeComponent();

            this.pnlWaterfall1.Title = "Gross margin";
            this.pnlWaterfall1.Steps = new[]
            {
                new PnlWaterfallStep("Revenue", 48200000, "total"),
                new PnlWaterfallStep("Corn", -31400000, "down"),
                new PnlWaterfallStep("Natural gas", -4300000, "down"),
                new PnlWaterfallStep("Freight", -3100000, "down"),
                new PnlWaterfallStep("DDGS", 5900000, "up"),
                new PnlWaterfallStep("Corn oil", 2100000, "up"),
                new PnlWaterfallStep("Margin", 17400000, "total"),
            };

            this.pnlWaterfall1.StepSelected += (s, e) => this.labelEvents.Text = "StepSelected: " + e.Label;
        }
    }
}
