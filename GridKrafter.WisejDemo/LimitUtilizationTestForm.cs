using System;
using Wisej.Web;

namespace GridKrafter.WisejDemo
{
    /// <summary>
    /// A form to try LimitUtilization on: open it in the designer to see the component, or show it
    /// from your application to try it live.
    /// </summary>
    public partial class LimitUtilizationTestForm : Form
    {
        public LimitUtilizationTestForm()
        {
            InitializeComponent();

            this.limitUtilization1.Rows = new[]
            {
                new LimitUtilizationRow("Valero Marketing", 42, 50, "BBB"),
                new LimitUtilizationRow("Cargill Ethanol", 38, 80, "A"),
                new LimitUtilizationRow("Gunvor USA", 31, 30, "BBB-"),
                new LimitUtilizationRow("Phillips 66", 27, 60, "A-"),
                new LimitUtilizationRow("BP Products", 18, 75, "A"),
                new LimitUtilizationRow("Trafigura", 24, 28, "BBB+"),
                new LimitUtilizationRow("Marathon Petroleum", 9, 45, "BBB")
            };
            this.limitUtilization1.RowSelected += this.limitUtilization1_RowSelected;
        }

        // What the component fires is shown in labelEvents.
        private void limitUtilization1_RowSelected(object sender, LimitUtilizationRowSelectedEventArgs e)
        {
            this.labelEvents.Text = "RowSelected: " + e.Name;
        }
    }
}
