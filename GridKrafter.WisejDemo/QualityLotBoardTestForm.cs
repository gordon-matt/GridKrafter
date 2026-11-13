using System;
using System.Collections.Generic;
using Wisej.Web;

namespace GridKrafter.WisejDemo
{
    /// <summary>
    /// A form to try QualityLotBoard on: open it in the designer to see the component, or show it
    /// from your application to try it live.
    /// </summary>
    public partial class QualityLotBoardTestForm : Form
    {
        public QualityLotBoardTestForm()
        {
            InitializeComponent();

            this.qualityLotBoard1.Specs = new List<QualitySpec>
            {
                new QualitySpec { Test = "Proof", Unit = "proof", Min = 188, Max = 202 },
                new QualitySpec { Test = "Water", Unit = "% w/w", Max = 6 },
                new QualitySpec { Test = "Methanol", Unit = "mg/L", Max = 200 },
                new QualitySpec { Test = "Acidity", Unit = "mg/L", Max = 30 },
                new QualitySpec { Test = "Appearance", Unit = "APHA", Max = 10 }
            };

            this.qualityLotBoard1.Lots = new List<QualityLot>
            {
                Lot("PE-2611-016", "Council Bluffs", "Beverage 190", "Tank T-09", new DateTime(2026, 11, 13, 7, 50, 0), "Pending", false, ""),
                Lot("PE-2611-015", "Atchison", "Industrial 200", "Railcar TILX 30551", new DateTime(2026, 11, 13, 6, 30, 0), "Pending", false, "proof=199.8;water=0.2"),
                Lot("PE-2611-014", "Council Bluffs", "USP 200", "Tank T-12", new DateTime(2026, 11, 12, 14, 20, 0), "Released", true, "proof=199.9;water=0.08;methanol=42;acidity=8;appearance=2"),
                Lot("PE-2611-013", "Atchison", "Beverage 190", "Tank T-07", new DateTime(2026, 11, 12, 9, 5, 0), "Released", true, "proof=190.1;water=4.8;methanol=95;acidity=12;appearance=3"),
                Lot("PE-2611-012", "Council Bluffs", "Industrial 200", "Railcar UTLX 84213", new DateTime(2026, 11, 11, 16, 40, 0), "Released", false, "proof=199.8;water=0.15;methanol=118;acidity=19;appearance=4"),
                Lot("PE-2611-011", "Albion", "USP 200", "Tank T-03", new DateTime(2026, 11, 11, 10, 30, 0), "Hold", false, "proof=199.6;water=0.35;methanol=186;acidity=22;appearance=5"),
                Lot("PE-2611-010", "Atchison", "Beverage 190", "Tank T-08", new DateTime(2026, 11, 10, 15, 15, 0), "Fail", false, "proof=188.2;water=6.4;methanol=71;acidity=14;appearance=6"),
                Lot("PE-2611-009", "Council Bluffs", "USP 200", "Railcar GATX 60917", new DateTime(2026, 11, 10, 8, 50, 0), "Released", true, "proof=200.0;water=0.05;methanol=31;acidity=6;appearance=1"),
                Lot("PE-2611-008", "Albion", "Industrial 200", "Tank T-05", new DateTime(2026, 11, 9, 13, 0, 0), "Released", false, "proof=199.7;water=0.22;methanol=140;acidity=27;appearance=7"),
                Lot("PE-2611-007", "Atchison", "Beverage 190", "Tank T-07", new DateTime(2026, 11, 9, 7, 45, 0), "Released", true, "proof=189.6;water=5.4;methanol=60;acidity=9;appearance=2"),
                Lot("PE-2611-006", "Council Bluffs", "USP 200", "Tank T-12", new DateTime(2026, 11, 8, 11, 10, 0), "Fail", false, "proof=199.5;water=0.4;methanol=214;acidity=17;appearance=4"),
                Lot("PE-2611-005", "Albion", "USP 190", "Tank T-01", new DateTime(2026, 11, 8, 9, 0, 0), "Released", true, "proof=190.3;water=4.7;methanol=48;acidity=11;appearance=3")
            };

            this.qualityLotBoard1.LotSelected += new QualityLotBoardLotSelectedEventHandler(this.qualityLotBoard1_LotSelected);
            this.qualityLotBoard1.StatusFilterChanged += new QualityLotBoardStatusFilterChangedEventHandler(this.qualityLotBoard1_StatusFilterChanged);
        }

        private static QualityLot Lot(string lot, string plant, string grade, string vessel, DateTime sampled, string status, bool coa, string results)
        {
            return new QualityLot { Lot = lot, Plant = plant, Grade = grade, Vessel = vessel, Sampled = sampled, Status = status, CoaIssued = coa, Results = results };
        }

        private void qualityLotBoard1_LotSelected(object sender, QualityLotBoardLotSelectedEventArgs e)
        {
            this.labelEvents.Text = e.Lot.Length == 0 ? "LotSelected: selection cleared" : "LotSelected: " + e.Lot;
        }

        private void qualityLotBoard1_StatusFilterChanged(object sender, QualityLotBoardStatusFilterChangedEventArgs e)
        {
            this.labelEvents.Text = "StatusFilterChanged: " + e.Status;
        }
    }
}
