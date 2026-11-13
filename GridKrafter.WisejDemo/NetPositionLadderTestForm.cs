using System;
using Wisej.Web;

namespace GridKrafter.WisejDemo
{
    /// <summary>
    /// A form to try NetPositionLadder on: open it in the designer to see the component, or show it
    /// from your application to try it live.
    /// </summary>
    public partial class NetPositionLadderTestForm : Form
    {
        public NetPositionLadderTestForm()
        {
            InitializeComponent();

            // the sketch's sample book, in kgal: purchases, sales and futures (+ long / - short) per delivery month
            this.netPositionLadder1.Months = new[]
            {
                new { Month = "Dec 26", PhysicalLong = 8400, PhysicalShort = 7900, Futures = -420 },
                new { Month = "Jan 27", PhysicalLong = 7600, PhysicalShort = 5200, Futures = -1680 },
                new { Month = "Feb 27", PhysicalLong = 7100, PhysicalShort = 3900, Futures = -1260 },
                new { Month = "Mar 27", PhysicalLong = 6800, PhysicalShort = 2600, Futures = -2520 },
                new { Month = "Apr 27", PhysicalLong = 5200, PhysicalShort = 4100, Futures = -840 },
                new { Month = "May 27", PhysicalLong = 3900, PhysicalShort = 5600, Futures = 1260 },
                new { Month = "Jun 27", PhysicalLong = 2400, PhysicalShort = 1200, Futures = 0 }
            };
            this.netPositionLadder1.Selected = "Feb 27";

            this.netPositionLadder1.ViewChanged += (s, e) => this.labelEvents.Text = "ViewChanged: " + e.View;
            this.netPositionLadder1.MonthSelected += (s, e) => this.labelEvents.Text = "MonthSelected: " + e.Month;
        }
    }
}
