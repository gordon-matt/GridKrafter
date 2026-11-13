using System;
using Wisej.Web;

namespace GridKrafter.WisejDemo
{
    /// <summary>
    /// A form to try CrushMarginGauge on: open it in the designer to see the component, or show it
    /// from your application to try it live.
    /// </summary>
    public partial class CrushMarginGaugeTestForm : Form
    {
        public CrushMarginGaugeTestForm()
        {
            InitializeComponent();

            // the sketch's sample data: 30 trading days of margins and today's breakdown, in $/gal
            double[] margins = { 0.52, 0.55, 0.51, 0.49, 0.53, 0.58, 0.60, 0.57, 0.55, 0.59, 0.62, 0.61, 0.58, 0.56, 0.54,
                                 0.57, 0.60, 0.64, 0.66, 0.63, 0.61, 0.65, 0.68, 0.70, 0.66, 0.62, 0.60, 0.63, 0.63, 0.67 };
            var history = new object[margins.Length];
            var day = new DateTime(2026, 11, 12);
            for (int i = margins.Length - 1; i >= 0; i--)
            {
                history[i] = new { Date = day, Value = margins[i] };
                do { day = day.AddDays(-1); } while (day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday);
            }
            this.crushMarginGauge1.History = history;

            this.crushMarginGauge1.Breakdown = new[]
            {
                new { Name = "Ethanol", Value = 1.78 },
                new { Name = "DDGS", Value = 0.48 },
                new { Name = "Corn oil", Value = 0.09 },
                new { Name = "Corn", Value = -1.56 },
                new { Name = "Natural gas", Value = -0.12 }
            };

            this.crushMarginGauge1.RangeChanged += (s, e) => this.labelEvents.Text = "RangeChanged: " + e.Range;
            this.crushMarginGauge1.ComponentClicked += (s, e) => this.labelEvents.Text = "ComponentClicked: " + e.Name;
        }
    }
}
