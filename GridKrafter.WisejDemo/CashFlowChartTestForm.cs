using System;
using Wisej.Web;

namespace GridKrafter.WisejDemo
{
    /// <summary>
    /// A form to try CashFlowChart on: open it in the designer to see the component, or show it
    /// from your application to try it live.
    /// </summary>
    public partial class CashFlowChartTestForm : Form
    {
        public CashFlowChartTestForm()
        {
            InitializeComponent();

            this.cashFlowChart1.OpeningBalance = 2400000;
            this.cashFlowChart1.MinimumCash = 1000000;
            this.cashFlowChart1.Weeks = new[]
            {
                new CashFlowWeek("17 Nov", 1450000, 1220000),
                new CashFlowWeek("24 Nov", 1180000, 1390000),
                new CashFlowWeek("1 Dec", 1320000, 1510000),
                new CashFlowWeek("8 Dec", 1610000, 1780000),
                new CashFlowWeek("15 Dec", 980000, 1420000),
                new CashFlowWeek("22 Dec", 1040000, 1380000),
                new CashFlowWeek("29 Dec", 890000, 1260000),
                new CashFlowWeek("5 Jan", 1120000, 1310000),
                new CashFlowWeek("12 Jan", 1560000, 1190000),
                new CashFlowWeek("19 Jan", 1730000, 1240000),
                new CashFlowWeek("26 Jan", 1490000, 1360000),
                new CashFlowWeek("2 Feb", 1650000, 1420000),
                new CashFlowWeek("9 Feb", 1820000, 1290000),
            };
            this.cashFlowChart1.WeekSelected += (s, e) => this.labelEvents.Text = "WeekSelected: " + e.Week;
        }
    }
}
