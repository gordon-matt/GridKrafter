using System;
using System.Collections.Generic;
using Wisej.Web;

namespace GridKrafter.WisejDemo
{
    /// <summary>
    /// A form to try FreightLaneMatrix on: open it in the designer to see the component, or show it
    /// from your application to try it live.
    /// </summary>
    public partial class FreightLaneMatrixTestForm : Form
    {
        public FreightLaneMatrixTestForm()
        {
            InitializeComponent();

            this.freightLaneMatrix1.Lanes = new List<FreightLaneRow>
            {
                Lane("Iowa -> Houston", "Iowa", "Houston (Kinder Morgan)", 0.215, 8, 1.9, 0, 0, 0, 0.162, 12, -1.2),
                Lane("Nebraska -> Stockton CA", "Nebraska", "Stockton CA", 0.288, 10, 3.4, 0.455, 4, 2.0, 0, 0, 0),
                Lane("Minnesota -> Chicago Argo", "Minnesota", "Chicago Argo", 0.118, 2, -0.8, 0.134, 1, 1.1, 0.092, 5, -2.6),
                Lane("South Dakota -> Port Arthur TX", "South Dakota", "Port Arthur TX", 0.236, 7, 0.9, 0, 0, 0, 0, 0, 0),
                Lane("Illinois -> New Orleans", "Illinois", "New Orleans", 0.142, 5, 0.4, 0.198, 3, 1.6, 0.081, 9, -3.1),
                Lane("Indiana -> Carteret NJ", "Indiana", "Carteret NJ", 0.176, 4, -1.4, 0.221, 2, 2.3, 0, 0, 0),
                Lane("Kansas -> Watson CA", "Kansas", "Watson CA", 0.274, 8, 2.7, 0, 0, 0, 0, 0, 0),
                Lane("Iowa -> Chicago Argo", "Iowa", "Chicago Argo", 0.121, 2, 0.2, 0.129, 1, -0.6, 0.094, 4, -1.8),
                Lane("Ohio -> Tampa FL", "Ohio", "Tampa FL", 0.246, 6, 1.2, 0.318, 3, 3.8, 0.148, 14, -0.9)
            };

            this.freightLaneMatrix1.Demurrage = new List<DemurrageBucketRow>
            {
                new DemurrageBucketRow { Bucket = "0-2", Cars = 74, Amount = 3700 },
                new DemurrageBucketRow { Bucket = "3-5", Cars = 41, Amount = 26800 },
                new DemurrageBucketRow { Bucket = "6-10", Cars = 19, Amount = 34200 },
                new DemurrageBucketRow { Bucket = "10+", Cars = 8, Amount = 28900 }
            };

            this.freightLaneMatrix1.Selected = "Minnesota -> Chicago Argo";

            this.freightLaneMatrix1.LaneSelected += (s, e) => this.labelEvents.Text = "LaneSelected: " + e.Lane;
            this.freightLaneMatrix1.ModeChanged += (s, e) => this.labelEvents.Text = "ModeChanged: " + e.Mode;
        }

        private static FreightLaneRow Lane(string lane, string origin, string destination,
            double railRate, int railDays, double railTrend,
            double truckRate, int truckDays, double truckTrend,
            double bargeRate, int bargeDays, double bargeTrend)
        {
            return new FreightLaneRow
            {
                Lane = lane, Origin = origin, Destination = destination,
                RailRate = railRate, RailDays = railDays, RailTrend = railTrend,
                TruckRate = truckRate, TruckDays = truckDays, TruckTrend = truckTrend,
                BargeRate = bargeRate, BargeDays = bargeDays, BargeTrend = bargeTrend
            };
        }
    }
}
