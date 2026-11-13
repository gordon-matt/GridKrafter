using System;
using Wisej.Web;

namespace GridKrafter.WisejDemo
{
    /// <summary>
    /// A form to try RailcarFleetTracker on: open it in the designer to see the component, or show it
    /// from your application to try it live.
    /// </summary>
    public partial class RailcarFleetTrackerTestForm : Form
    {
        public RailcarFleetTrackerTestForm()
        {
            InitializeComponent();

            // the sketch's sample fleet: unit trains and manifest moves from Dakota and Iowa plants
            this.railcarFleetTracker1.Shipments = new[]
            {
                new { Id = "UTX-4421", UnitTrain = true, Origin = "Aberdeen, SD", OriginLat = 45.46, OriginLon = -98.49, Destination = "Port Arthur, TX", DestLat = 29.9, DestLon = -93.93,
                      Cars = 96, Status = "transit", Progress = 62, Eta = "Nov 15", DaysOut = 6, ExpectedDays = 9,
                      CarNumbers = new[] { "UTLX 205114", "UTLX 205131", "UTLX 205167", "TILX 290455", "TILX 290462", "TILX 290478", "GATX 221306", "GATX 221319" } },
                new { Id = "UTX-4433", UnitTrain = true, Origin = "Jewell, IA", OriginLat = 42.31, OriginLon = -93.64, Destination = "Carteret, NJ", DestLat = 40.58, DestLon = -74.23,
                      Cars = 98, Status = "transit", Progress = 35, Eta = "Nov 18", DaysOut = 11, ExpectedDays = 10,
                      CarNumbers = new[] { "TILX 291004", "TILX 291017", "TILX 291023", "TILX 291048", "UTLX 206602", "UTLX 206615", "UTLX 206639", "PROX 44120", "PROX 44137", "PROX 44151", "GATX 222780", "GATX 222794", "GATX 222801" } },
                new { Id = "MAN-2107", UnitTrain = false, Origin = "Mitchell, SD", OriginLat = 43.71, OriginLon = -98.03, Destination = "Stockton, CA", DestLat = 37.96, DestLon = -121.29,
                      Cars = 18, Status = "loading", Progress = 0, Eta = "Nov 22", DaysOut = 0, ExpectedDays = 12,
                      CarNumbers = new[] { "UTLX 207301", "UTLX 207318", "UTLX 207322" } },
                new { Id = "UTX-4398", UnitTrain = true, Origin = "Hudson, SD", OriginLat = 43.13, OriginLon = -96.45, Destination = "Watson, CA", DestLat = 33.84, DestLon = -118.25,
                      Cars = 94, Status = "destination", Progress = 100, Eta = "Nov 12", DaysOut = 10, ExpectedDays = 11,
                      CarNumbers = new[] { "GATX 223410", "GATX 223427" } },
                new { Id = "EMP-0871", UnitTrain = true, Origin = "Port Arthur, TX", OriginLat = 29.9, OriginLon = -93.93, Destination = "Aberdeen, SD", DestLat = 45.46, DestLon = -98.49,
                      Cars = 96, Status = "empty", Progress = 48, Eta = "Nov 16", DaysOut = 4, ExpectedDays = 8,
                      CarNumbers = new[] { "TILX 290455" } },
                new { Id = "MAN-2093", UnitTrain = false, Origin = "Watertown, SD", OriginLat = 44.9, OriginLon = -97.11, Destination = "Tampa, FL", DestLat = 27.95, DestLon = -82.46,
                      Cars = 12, Status = "badorder", Progress = 71, Eta = "—", DaysOut = 14, ExpectedDays = 10,
                      CarNumbers = new[] { "UTLX 208806", "UTLX 208813", "UTLX 208820", "UTLX 208837", "UTLX 208844", "UTLX 208851", "UTLX 208868", "UTLX 208875", "UTLX 208882", "UTLX 208899", "UTLX 208903", "UTLX 208910" } }
            };
            this.railcarFleetTracker1.Selected = "UTX-4421";
            this.railcarFleetTracker1.Expanded = "UTX-4433";

            this.railcarFleetTracker1.StatusFilterChanged += (s, e) => this.labelEvents.Text = "StatusFilterChanged: " + e.StatusFilter;
            this.railcarFleetTracker1.GroupByChanged += (s, e) => this.labelEvents.Text = "GroupByChanged: " + e.GroupBy;
            this.railcarFleetTracker1.ShipmentSelected += (s, e) => this.labelEvents.Text = "ShipmentSelected: " + e.Id;
            this.railcarFleetTracker1.ShipmentExpanded += (s, e) => this.labelEvents.Text = "ShipmentExpanded: " + e.Id + (e.Expanded ? " (open)" : " (closed)");
        }
    }
}
