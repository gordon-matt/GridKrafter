using System;
using Wisej.Web;

namespace GridKrafter.WisejDemo
{
    /// <summary>
    /// A form to try TerminalInventoryTanks on: open it in the designer to see the component, or show it
    /// from your application to try it live.
    /// </summary>
    public partial class TerminalInventoryTanksTestForm : Form
    {
        public TerminalInventoryTanksTestForm()
        {
            InitializeComponent();

            // the sketch's sample terminals: capacity and level in barrels, alarms in percent, net flow in bbl/day
            this.terminalInventoryTanks1.Tanks = new[]
            {
                new { Id = "kc", Terminal = "Kansas City", Capacity = 180000, Level = 121400, Low = 15, High = 90, DaysCover = 6.2, NetFlow = 4200 },
                new { Id = "chi", Terminal = "Chicago Argo", Capacity = 250000, Level = 228600, Low = 15, High = 90, DaysCover = 9.8, NetFlow = 11800 },
                new { Id = "ny", Terminal = "NY Harbor", Capacity = 320000, Level = 176000, Low = 15, High = 90, DaysCover = 4.1, NetFlow = -9600 },
                new { Id = "hou", Terminal = "Houston", Capacity = 200000, Level = 22500, Low = 15, High = 90, DaysCover = 1.3, NetFlow = -6300 },
                new { Id = "la", Terminal = "Los Angeles", Capacity = 150000, Level = 88200, Low = 15, High = 90, DaysCover = 5.4, NetFlow = 1500 },
                new { Id = "tpa", Terminal = "Tampa", Capacity = 90000, Level = 51300, Low = 15, High = 90, DaysCover = 3.7, NetFlow = -2100 }
            };
            this.terminalInventoryTanks1.Selected = "kc";

            this.terminalInventoryTanks1.UnitChanged += (s, e) => this.labelEvents.Text = "UnitChanged: " + e.Unit;
            this.terminalInventoryTanks1.TankSelected += (s, e) => this.labelEvents.Text = "TankSelected: " + e.Id;
        }
    }
}
