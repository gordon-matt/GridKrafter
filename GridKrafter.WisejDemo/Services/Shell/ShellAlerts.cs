using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GridKrafter.WisejDemo.Controls;
using GridKrafter.WisejDemo.Services.Commodities;

namespace GridKrafter.WisejDemo.Services.Shell
{
	/// <summary>One thing that needs attention, shown in the top bar's alert list.</summary>
	/// <param name="Severity">"danger" or "warning".</param>
	/// <param name="Text">What is wrong, in one line.</param>
	/// <param name="Panel">The panel that shows it (EthanolPanel...).</param>
	/// <param name="PlantId">The plant to filter the board to, or <see langword="null"/>.</param>
	public sealed record ShellAlert(string Severity, string Text, string Panel, string PlantId);

	/// <summary>
	/// The alerts of the day, worked out from the sample data the boards show: hedge months out of policy (natural gas
	/// and grain), plants with less than 5 days of denaturant, and purified-ethanol lots on Hold or Fail.
	/// </summary>
	public static class ShellAlerts
	{
		private const double POLICY_MIN = 50, POLICY_MAX = 80, DENATURANT_DAYS = 5;

		/// <summary>The current alerts, most severe first.</summary>
		public static IReadOnlyList<ShellAlert> Current()
		{
			var list = new List<ShellAlert>();

			var gas = new SampleNaturalGasData().HedgeCoverage(PlantFilter.All, 0);
			AddHedge(list, "Natural gas", "NaturalGasPanel", gas);

			var grain = new SampleGrainData().Hedges(PlantFilter.All, 0);
			AddHedge(list, "Grain", "GrainPanel", grain);

			foreach (var s in new SampleDenaturantData().DaysOfSupply(PlantFilter.All, 0).Where(s => s.DaysOfSupply < DENATURANT_DAYS))
			{
				string days = s.DaysOfSupply.ToString("0.0", CultureInfo.InvariantCulture);
				list.Add(new ShellAlert(s.DaysOfSupply < 4 ? "danger" : "warning",
					$"Denaturant at {s.Plant}: {days} days of supply", "DenaturantPanel", s.PlantId));
			}

			foreach (var lot in new SamplePurifiedData().Lots(PlantFilter.All, 0).Where(l => l.Status == "Fail" || l.Status == "Hold"))
			{
				bool fail = lot.Status == "Fail";
				string plantId = PlantRegistry.All.FirstOrDefault(p => p.Name == lot.Plant)?.Id;
				list.Add(new ShellAlert(fail ? "danger" : "warning",
					$"Purified lot {lot.Lot} ({lot.Grade}, {lot.Plant}) is on {(fail ? "Fail" : "Hold")}", "PurifiedPanel", plantId));
			}

			return list.OrderBy(a => a.Severity == "danger" ? 0 : 1).ToList();
		}

		// One alert for a commodity whose hedge months fall outside the 50 to 80 percent policy.
		private static void AddHedge(List<ShellAlert> list, string name, string panel, IReadOnlyList<HedgeCoverageMonth> months)
		{
			var under = months.Where(m => m.Exposure > 0 && Pct(m) < POLICY_MIN).ToList();
			var over = months.Where(m => m.Exposure > 0 && Pct(m) > POLICY_MAX).ToList();
			if (under.Count + over.Count == 0)
				return;

			string detail = (under.Count > 0 ? $"{under.Count} under {POLICY_MIN:0}%" : "") +
				(under.Count > 0 && over.Count > 0 ? ", " : "") +
				(over.Count > 0 ? $"{over.Count} over {POLICY_MAX:0}%" : "");
			list.Add(new ShellAlert(under.Count >= 3 ? "danger" : "warning",
				$"{name} hedges: {under.Count + over.Count} months out of policy ({detail})", panel, null));
		}

		private static double Pct(HedgeCoverageMonth m) => 100 * (m.Futures + m.Physical) / m.Exposure;
	}
}
