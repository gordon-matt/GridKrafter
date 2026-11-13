using System;
using Wisej.Web;

namespace GridKrafter.WisejDemo
{
    /// <summary>
    /// A form to try CarbonCreditTracker on: open it in the designer to see the component, or show it
    /// from your application to try it live.
    /// </summary>
    public partial class CarbonCreditTrackerTestForm : Form
    {
        public CarbonCreditTrackerTestForm()
        {
            InitializeComponent();

            // the sketch's sample book: RINs in cents per RIN, state credits in dollars per tonne
            this.carbonCreditTracker1.Credits = new[]
            {
                new { Id = "d6", Name = "D6 RIN", Program = "RFS", Price = 104.5, Unit = "¢", Change = 2.25, Held = 3150000, PerGallon = 104.5,
                      History = new[] { 96.0, 97.5, 96.8, 98.2, 99.0, 98.4, 100.1, 101.3, 100.6, 102.0, 102.25, 104.5 } },
                new { Id = "d4", Name = "D4 RIN", Program = "RFS", Price = 112.0, Unit = "¢", Change = 1.5, Held = 420000, PerGallon = 0.0,
                      History = new[] { 105.0, 106.2, 105.8, 107.5, 108.1, 107.6, 109.0, 110.2, 109.7, 110.5, 110.5, 112.0 } },
                new { Id = "lcfs", Name = "California LCFS", Program = "LCFS", Price = 72.5, Unit = "$", Change = -1.25, Held = 18500, PerGallon = 9.8,
                      History = new[] { 76.0, 75.5, 76.2, 75.0, 74.6, 75.1, 74.2, 73.8, 74.0, 73.5, 73.75, 72.5 } },
                new { Id = "cfp", Name = "Oregon CFP", Program = "CFP", Price = 118.0, Unit = "$", Change = 0.75, Held = 6200, PerGallon = 14.6,
                      History = new[] { 112.0, 113.1, 112.6, 114.0, 115.2, 114.8, 116.0, 116.6, 116.1, 117.0, 117.25, 118.0 } },
                new { Id = "cfs", Name = "Washington CFS", Program = "CFS", Price = 61.0, Unit = "$", Change = 0.0, Held = 3100, PerGallon = 7.4,
                      History = new[] { 60.2, 60.8, 60.5, 61.1, 60.9, 61.3, 61.0, 60.7, 61.0, 61.2, 61.0, 61.0 } }
            };
            this.carbonCreditTracker1.Rvo = 4200000;
            this.carbonCreditTracker1.Selected = "lcfs";

            this.carbonCreditTracker1.CreditSelected += (s, e) => this.labelEvents.Text = "CreditSelected: " + e.Id;
        }
    }
}
