using System;
using Wisej.Web;

namespace GridKrafter.WisejDemo
{
    /// <summary>
    /// A form to try TerminalSlotBoard on: open it in the designer to see the component, or show it
    /// from your application to try it live.
    /// </summary>
    public partial class TerminalSlotBoardTestForm : Form
    {
        public TerminalSlotBoardTestForm()
        {
            InitializeComponent();

            this.terminalSlotBoard1.Title = "Hereford terminal · loading slots";
            this.terminalSlotBoard1.Now = "13:20";
            this.terminalSlotBoard1.Slots = new[]
            {
                new TerminalSlotBoardSlot("S-101", "Rack 1", "06:30", "08:00", "Valero", "Ethanol", "T", "done"),
                new TerminalSlotBoardSlot("S-102", "Rack 1", "09:00", "11:00", "Marathon", "Ethanol", "T", "done"),
                new TerminalSlotBoardSlot("S-103", "Rack 1", "12:30", "14:30", "Sinclair", "E85", "T", "loading"),
                new TerminalSlotBoardSlot("S-104", "Rack 1", "16:00", "18:00", "Valero", "Ethanol", "T", "booked"),
                new TerminalSlotBoardSlot("S-201", "Rack 2", "07:00", "09:30", "Phillips 66", "Ethanol", "T", "done"),
                new TerminalSlotBoardSlot("S-202", "Rack 2", "11:00", "13:00", "Casey's", "E15", "T", "late"),
                new TerminalSlotBoardSlot("S-203", "Rack 2", "15:00", "17:30", "Kum & Go", "E85", "T", "booked"),
                new TerminalSlotBoardSlot("S-301", "Rail spot A", "06:00", "12:00", "BNSF unit train", "Ethanol", "R", "done"),
                new TerminalSlotBoardSlot("S-302", "Rail spot A", "13:00", "19:00", "Cargill", "DDGS", "R", "loading"),
                new TerminalSlotBoardSlot("S-401", "Rail spot B", "10:00", "16:00", "ADM", "Corn oil", "R", "late"),
                new TerminalSlotBoardSlot("S-501", "Barge dock", "08:00", "15:00", "Bunge", "Ethanol", "B", "loading"),
                new TerminalSlotBoardSlot("S-502", "Barge dock", "17:00", "21:30", "Gavilon", "DDGS", "B", "booked"),
                new TerminalSlotBoardSlot("S-601", "Pipeline", "06:00", "22:00", "Magellan", "Ethanol", "P", "loading")
            };

            this.terminalSlotBoard1.SlotSelected += terminalSlotBoard1_SlotSelected;
        }

        private void terminalSlotBoard1_SlotSelected(object sender, TerminalSlotBoardSlotEventArgs e)
        {
            this.labelEvents.Text = "SlotSelected: " + e.Id;
        }
    }
}
