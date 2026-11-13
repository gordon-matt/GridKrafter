using System;
using System.Collections.Generic;
using Wisej.Web;

namespace GridKrafter.WisejDemo
{
    /// <summary>
    /// A form to try HedgeCoverageMeter on: open it in the designer to see the component, or show it
    /// from your application to try it live.
    /// </summary>
    public partial class HedgeCoverageMeterTestForm : Form
    {
        public HedgeCoverageMeterTestForm()
        {
            InitializeComponent();

            this.hedgeCoverageMeter1.Title = "Natural gas hedge coverage";
            this.hedgeCoverageMeter1.Unit = "MMBtu";
            this.hedgeCoverageMeter1.Months = new List<HedgeCoverageMonth>
            {
                new HedgeCoverageMonth("Dec 26", 1200000, 720000, 300000),
                new HedgeCoverageMonth("Jan 27", 1250000, 700000, 300000),
                new HedgeCoverageMonth("Feb 27", 1150000, 620000, 280000),
                new HedgeCoverageMonth("Mar 27", 1100000, 560000, 250000),
                new HedgeCoverageMonth("Apr 27", 1000000, 480000, 200000),
                new HedgeCoverageMonth("May 27", 950000, 420000, 170000),
                new HedgeCoverageMonth("Jun 27", 950000, 360000, 170000),
                new HedgeCoverageMonth("Jul 27", 1000000, 330000, 150000),
                new HedgeCoverageMonth("Aug 27", 1000000, 280000, 120000),
                new HedgeCoverageMonth("Sep 27", 950000, 210000, 100000),
                new HedgeCoverageMonth("Oct 27", 1000000, 170000, 90000),
                new HedgeCoverageMonth("Nov 27", 1100000, 150000, 70000)
            };

            this.hedgeCoverageMeter1.MonthSelected += this.hedgeCoverageMeter1_MonthSelected;
        }

        // What the component fires is shown in labelEvents: its handlers go here.
        private void hedgeCoverageMeter1_MonthSelected(object sender, HedgeCoverageMeterMonthSelectedEventArgs e)
        {
            this.labelEvents.Text = "MonthSelected: " + e.Month;
        }
    }
}
