using System;
using GridKrafter.WisejDemo.Model.Boards.Business;
using GridKrafter.WisejDemo.Services.Business;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Panels
{
	/// <summary>
	/// A business board: the board an <see cref="IBusinessBoard"/> defines under the business filter bar
	/// (commodity, period and the board's pick). <see cref="BoardPanel"/> shows it, saves its layout under its key and
	/// fills every widget again when the filter changes. Each business board keeps its own filter for the session.
	/// </summary>
	public abstract class BusinessBoardPanel : BoardPanel
	{
		/// <summary>Shows the board the definition makes, saved under its key.</summary>
		/// <param name="board">The board: its layout, catalog, pick, widget bodies and data.</param>
		protected BusinessBoardPanel(IBusinessBoard board)
			: this(new BusinessBoardSource(board ?? throw new ArgumentNullException(nameof(board))))
		{
		}

		private BusinessBoardPanel(BusinessBoardSource source) : base(source)
		{
			this.BusinessSource = source;
		}

		private BusinessBoardSource BusinessSource { get; }

		/// <summary>The board this panel shows.</summary>
		protected IBusinessBoard Definition => this.BusinessSource.Board;

		/// <summary>The board's key: customers, carriers…</summary>
		public string BoardKey => this.BusinessSource.Key;

		/// <summary>What the board shows now.</summary>
		public BusinessFilter Filter => this.BusinessSource.Filter;

		/// <summary>
		/// Shows only this pick (or all for <see langword="null"/>) on the board on show, for every commodity and
		/// the period already chosen.
		/// </summary>
		public void ShowPick(string pickId)
		{
			this.BusinessSource.Show(this.Filter with { Commodity = null, Pick = pickId });
		}

		/// <summary>
		/// Makes the business board with the key show only this pick (or all for <see langword="null"/>) when it
		/// opens next, for every commodity and the period it had.
		/// </summary>
		/// <param name="key">The board's key: customers, carriers…</param>
		/// <param name="pickId">The pick's id: a customer's, a carrier's…</param>
		public static void RememberPick(string key, string pickId)
		{
			string sessionKey = BusinessBoardSource.SessionKey(key);
			var current = Application.Session[sessionKey] as BusinessFilter ?? BusinessFilter.Default;
			Application.Session[sessionKey] = current with { Commodity = null, Pick = pickId };
		}
	}

	// One panel per business board, so the navigation bar's Tag (the panel's class name) finds it. Each only names
	// its board; BusinessBoardPanel does the rest.

	/// <summary>The Customers board.</summary>
	public class CustomersPanel : BusinessBoardPanel
	{
		public CustomersPanel() : base(new CustomersBoard()) { }
	}

	/// <summary>The Carriers board.</summary>
	public class CarriersPanel : BusinessBoardPanel
	{
		public CarriersPanel() : base(new CarriersBoard()) { }
	}

	/// <summary>The Accounting board.</summary>
	public class AccountingPanel : BusinessBoardPanel
	{
		public AccountingPanel() : base(new AccountingBoard()) { }
	}

	/// <summary>The Contracts board.</summary>
	public class ContractsPanel : BusinessBoardPanel
	{
		public ContractsPanel() : base(new ContractsBoard()) { }
	}

	/// <summary>The Risk &amp; Credit board.</summary>
	public class RiskPanel : BusinessBoardPanel
	{
		public RiskPanel() : base(new RiskBoard()) { }
	}

	/// <summary>The Plant Operations board.</summary>
	public class PlantOpsPanel : BusinessBoardPanel
	{
		public PlantOpsPanel() : base(new PlantOpsBoard()) { }
	}

	/// <summary>The Scheduling &amp; Nominations board.</summary>
	public class SchedulingPanel : BusinessBoardPanel
	{
		public SchedulingPanel() : base(new SchedulingBoard()) { }
	}

	/// <summary>The Compliance &amp; Regulatory board.</summary>
	public class CompliancePanel : BusinessBoardPanel
	{
		public CompliancePanel() : base(new ComplianceBoard()) { }
	}
}
