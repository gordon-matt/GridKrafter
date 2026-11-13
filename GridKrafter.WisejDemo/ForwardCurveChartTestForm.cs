using System;
using System.Collections.Generic;
using Wisej.Web;

namespace GridKrafter.WisejDemo
{
    /// <summary>
    /// A form to try ForwardCurveChart on: open it in the designer to see the component, or show it
    /// from your application to try it live.
    /// </summary>
    public partial class ForwardCurveChartTestForm : Form
    {
        public ForwardCurveChartTestForm()
        {
            InitializeComponent();

            // 13 months of NYMEX natural gas, Dec-26 to Dec-27, with last week's curve and the hedges
            this.forwardCurveChart1.Points = new List<ForwardCurvePoint>
            {
                new ForwardCurvePoint("Dec-26", 3.952, 3.871, 3.62, 620000),
                new ForwardCurvePoint("Jan-27", 4.185, 4.098, 3.85, 600000),
                new ForwardCurvePoint("Feb-27", 3.934, 3.866, 3.71, 540000),
                new ForwardCurvePoint("Mar-27", 3.548, 3.502, 3.44, 480000),
                new ForwardCurvePoint("Apr-27", 3.182, 3.161, 3.21, 400000),
                new ForwardCurvePoint("May-27", 3.118, 3.104, 3.15, 360000),
                new ForwardCurvePoint("Jun-27", 3.247, 3.229, 3.19, 300000),
                new ForwardCurvePoint("Jul-27", 3.412, 3.388, 3.30, 240000),
                new ForwardCurvePoint("Aug-27", 3.455, 3.431, 3.33, 180000),
                new ForwardCurvePoint("Sep-27", 3.381, 3.370, 0, 0),
                new ForwardCurvePoint("Oct-27", 3.476, 3.458, 0, 0),
                new ForwardCurvePoint("Nov-27", 3.826, 3.790, 0, 0),
                new ForwardCurvePoint("Dec-27", 4.118, 4.069, 0, 0)
            };

            this.forwardCurveChart1.MonthSelected += this.forwardCurveChart1_MonthSelected;
        }

        private void forwardCurveChart1_MonthSelected(object sender, ForwardCurveChartMonthSelectedEventArgs e)
        {
            this.labelEvents.Text = "monthSelected: " + e.Month + " (Selected = " + this.forwardCurveChart1.Selected + ")";
        }
    }
}
