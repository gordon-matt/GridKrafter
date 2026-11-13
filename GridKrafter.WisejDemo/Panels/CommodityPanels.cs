using GridKrafter.WisejDemo.Model.Boards;

namespace GridKrafter.WisejDemo.Panels
{
	// One panel per commodity board, so the navigation bar's Tag (the panel's class name) finds it, as it
	// finds the Dashboard. Each only names its board; CommodityBoardPanel does the rest.

	/// <summary>The Enterprise Overview board.</summary>
	public class OverviewPanel : CommodityBoardPanel
	{
		public OverviewPanel() : base(new OverviewBoard()) { }
	}

	/// <summary>The Ethanol board: fuel, E-85 and 200 proof.</summary>
	public class EthanolPanel : CommodityBoardPanel
	{
		public EthanolPanel() : base(new EthanolBoard()) { }
	}

	/// <summary>The CO2 board.</summary>
	public class Co2Panel : CommodityBoardPanel
	{
		public Co2Panel() : base(new Co2Board()) { }
	}

	/// <summary>The Purified Ethanol board.</summary>
	public class PurifiedPanel : CommodityBoardPanel
	{
		public PurifiedPanel() : base(new PurifiedBoard()) { }
	}

	/// <summary>The Denaturant board.</summary>
	public class DenaturantPanel : CommodityBoardPanel
	{
		public DenaturantPanel() : base(new DenaturantBoard()) { }
	}

	/// <summary>The Natural Gas board.</summary>
	public class NaturalGasPanel : CommodityBoardPanel
	{
		public NaturalGasPanel() : base(new NaturalGasBoard()) { }
	}

	/// <summary>The Grain board.</summary>
	public class GrainPanel : CommodityBoardPanel
	{
		public GrainPanel() : base(new GrainBoard()) { }
	}

	/// <summary>The Freight board.</summary>
	public class FreightPanel : CommodityBoardPanel
	{
		public FreightPanel() : base(new FreightBoard()) { }
	}
}
