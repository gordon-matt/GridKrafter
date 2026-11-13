using System;
using Wisej.Web;

namespace GridKrafter.WisejDemo
{
    /// <summary>
    /// A form to try LiftingScheduleTracker on: open it in the designer to see the component, or show it
    /// from your application to try it live.
    /// </summary>
    public partial class LiftingScheduleTrackerTestForm : Form
    {
        public LiftingScheduleTrackerTestForm()
        {
            InitializeComponent();

            this.liftingScheduleTracker1.Title = "Ethanol liftings";
            this.liftingScheduleTracker1.PlannedLabel = "Contracted";
            this.liftingScheduleTracker1.ActualLabel = "Lifted";
            this.liftingScheduleTracker1.Period = "November MTD";
            this.liftingScheduleTracker1.Unit = "gal";
            this.liftingScheduleTracker1.PaceRatio = 0.43;
            this.liftingScheduleTracker1.Rows = new LiftingScheduleRow[]
            {
                new LiftingScheduleRow("Marathon Petroleum", "Ethanol", 5000000, 2480000),
                new LiftingScheduleRow("Valero Marketing", "Ethanol", 4200000, 2350000),
                new LiftingScheduleRow("Phillips 66", "Ethanol", 3600000, 1200000),
                new LiftingScheduleRow("Shell Trading", "Ethanol", 3000000, 1310000),
                new LiftingScheduleRow("Chevron Products", "Ethanol", 2400000, 780000),
                new LiftingScheduleRow("BP Products", "Ethanol", 2000000, 2060000),
                new LiftingScheduleRow("Sunoco LP", "Ethanol", 1500000, 640000),
                new LiftingScheduleRow("Cenex Harvest States", "Ethanol", 1200000, 610000)
            };

            this.liftingScheduleTracker1.RowSelected += (s, e) => this.labelEvents.Text = "RowSelected: " + e.Label;
            this.liftingScheduleTracker1.SortChanged += (s, e) => this.labelEvents.Text = "SortChanged: " + e.SortBy + (e.SortDescending ? " descending" : " ascending");
        }
    }
}
