using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GridKrafter.Models;
using GridKrafter.WisejDemo.Controls;
using GridKrafter.WisejDemo.Services.Business;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Model.Boards.Business
{
	/// <summary>
	/// The Compliance &amp; Regulatory board: four headline tiles (RINs generated and retired, average CI score, filings due),
	/// the credit book, the filing deadlines as a timeline, the CI score of each plant and credits held against credits
	/// committed to sales. Ethanol earns RINs and the low-carbon fuel credits; CO2 shows its 45Q credit and filings only; the other
	/// commodities say they have no compliance credits. "Next 30 days" has no history, so RINs generated and retired say so; the
	/// credit book, deadlines, CI scores and coverage are positions of today and stay. The program pick keeps the filings and
	/// coverage of that program, highlights its credit in the book and sets the standard the plants' CI is read against.
	/// </summary>
	public class ComplianceBoard : IBusinessBoard
	{
		private const string KIND_CREDITS = "compliance-credits";
		private const string KIND_DEADLINES = "compliance-deadlines";
		private const string KIND_CI = "compliance-ci";
		private const string KIND_COVERAGE = "compliance-coverage";

		private const string TILE_RINS = "rins-generated";
		private const string TILE_RETIRED = "rins-retired";
		private const string TILE_CI = "avg-ci";
		private const string TILE_FILINGS = "filings-due";

		private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

		private readonly IComplianceData data;

		/// <summary>Creates the board over the sample data.</summary>
		public ComplianceBoard() : this(SampleComplianceData.Default)
		{
		}

		/// <summary>Creates the board over the data.</summary>
		public ComplianceBoard(IComplianceData data)
		{
			this.data = data ?? throw new ArgumentNullException(nameof(data));
		}

		/// <inheritdoc/>
		public string Key => "compliance";

		/// <inheritdoc/>
		public string PickLabel => "Program";

		/// <inheritdoc/>
		public string PickAllText => "All programs";

		/// <inheritdoc/>
		public IReadOnlyList<PickChoice> PickChoices { get; } = new[]
		{
			new PickChoice("rfs", "RFS (EPA)"),
			new PickChoice("lcfs", "California LCFS"),
			new PickChoice("ocfp", "Oregon CFP"),
			new PickChoice("wacfs", "Washington CFS"),
		};

		/// <inheritdoc/>
		public DashboardBoard Create() => new DashboardBoard
		{
			Sections = new List<DashboardSection>
			{
				new DashboardSection { Id = "overview", Title = "Overview", Icon = "home" },
				new DashboardSection { Id = "plants", Title = "Plants & coverage", Icon = "server" },
			},
			Items = new List<DashboardItem>
			{
				BusinessBoardKit.Tile(TILE_RINS, "RINs generated", "green", 0),
				BusinessBoardKit.Tile(TILE_RETIRED, "RINs retired", "blue", 3),
				BusinessBoardKit.Tile(TILE_CI, "Avg CI score", "violet", 6),
				BusinessBoardKit.Tile(TILE_FILINGS, "Filings due", "orange", 9),
				new DashboardItem
				{
					Id = "credits", Kind = KIND_CREDITS, Section = "overview", Title = "RIN & LCFS credits", Icon = "check", Color = "green",
					X = 0, Y = 2, W = 7, H = 7, MinW = 5, MinH = 5, Refresh = 60,
				},
				new DashboardItem
				{
					Id = "deadlines", Kind = KIND_DEADLINES, Section = "overview", Title = "Filing deadlines", Icon = "clock", Color = "orange",
					X = 7, Y = 2, W = 5, H = 7, MinW = 3, MinH = 4, Refresh = 60,
				},
				new DashboardItem
				{
					Id = "ci-scores", Kind = KIND_CI, Section = "plants", Title = "CI score by plant", Icon = "chart", Color = "violet",
					X = 0, Y = 0, W = 7, H = 8, MinW = 5, MinH = 4, NoPadding = true, Refresh = 60,
				},
				new DashboardItem
				{
					Id = "coverage", Kind = KIND_COVERAGE, Section = "plants", Title = "Credits held vs. committed sales", Icon = "server", Color = "blue",
					X = 7, Y = 0, W = 5, H = 8, MinW = 4, MinH = 4, Refresh = 60,
				},
			},
		};

		/// <inheritdoc/>
		public IEnumerable<GridWidgetKind> Catalog => new[]
		{
			BusinessBoardKit.Kind(KIND_CREDITS, "RIN & LCFS credits", "Credit prices, holdings and the uplift per gallon.", "check", "green", 7, 7, 5, 5),
			BusinessBoardKit.Kind(KIND_DEADLINES, "Filing deadlines", "EPA, CARB, Oregon and Washington filings, soonest first.", "clock", "orange", 5, 7, 3, 4),
			BusinessBoardKit.Kind(KIND_CI, "CI score by plant", "Carbon intensity of each plant against the standard.", "chart", "violet", 7, 8, 5, 4),
			BusinessBoardKit.Kind(KIND_COVERAGE, "Credits held vs. committed sales", "Credits held against credits promised to buyers, by program.", "server", "blue", 5, 8, 4, 4),
		};

		/// <inheritdoc/>
		public Control CreateBody(DashboardItem item, BusinessFilter filter)
		{
			filter ??= BusinessFilter.Default;
			var state = new State { Filter = filter };

			switch (item.Kind)
			{
				case KIND_CREDITS:
					return Made(state, BusinessBoardKit.Wrap(new CarbonCreditTracker(), state));

				case KIND_DEADLINES:
					return Made(state, BusinessBoardKit.Wrap(new Timeline
					{
						TimeFormat = "d MMM",
						TimePosition = TimelineTimePosition.Left,
						HasMoreItems = false,
					}, state));

				case KIND_CI:
					return Made(state, BusinessBoardKit.Wrap(CreateCiGrid(), state));

				case KIND_COVERAGE:
					return Made(state, BusinessBoardKit.Wrap(new LimitUtilization { Title = "Committed vs held", Unit = "$M", WarnAt = 0.85 }, state));

				default:
					// a headline tile: which figure it shows follows its id, which stays when people rename it
					state.Tile = item.Id switch
					{
						TILE_RETIRED => TILE_RETIRED,
						TILE_CI => TILE_CI,
						TILE_FILINGS => TILE_FILINGS,
						_ => TILE_RINS,
					};
					var tile = new StatTile { Label = item.Title, Tag = state };
					Fill(tile, state);
					return tile;
			}
		}

		/// <inheritdoc/>
		public void Refresh(Control body, BusinessFilter filter)
		{
			if (body?.Tag is not State state)
				return;

			filter ??= BusinessFilter.Default;

			// the same filter again is a refresh: the figures move a little; a new filter starts from its own figures
			if (!Equals(state.Filter, filter))
			{
				state.Filter = filter;
				state.Refreshes = 0;
			}
			else
			{
				state.Refreshes++;
			}

			Fill(body, state);
		}

		private Control Made(State state, Control body)
		{
			Fill(body, state);
			return body;
		}

		// Shows the figures for the state's filter in a tile or in a body made by BusinessBoardKit.Wrap.
		private void Fill(Control body, State state)
		{
			var filter = state.Filter;
			int refresh = state.Refreshes;
			var scope = SampleComplianceData.ScopeOf(filter);

			if (body is StatTile tile)
			{
				FillTile(tile, state.Tile, scope, filter, refresh);
				return;
			}

			var content = BusinessBoardKit.Content(body);
			if (content == null)
				return;

			if (scope == ComplianceScope.None)
			{
				BusinessBoardKit.ShowEmpty(body, NoneMessage(filter));
				return;
			}

			switch (content)
			{
				case CarbonCreditTracker tracker:
				{
					string message = scope == ComplianceScope.Carbon ? "CO2 earns 45Q credits, not RINs or fuel credits." : null;
					var credits = message == null ? this.data.Credits(filter, refresh) : null;
					message ??= credits.Count == 0 ? "No credits held." : null;
					BusinessBoardKit.ShowEmpty(body, message);
					if (message != null)
						break;

					tracker.Credits = credits;
					// the program's own credit is the one picked; all programs start from California's
					tracker.Selected = filter.Pick == null ? "lcfs" : (filter.Pick == "rfs" ? "" : SampleComplianceData.CreditOf(filter.Pick));
					break;
				}

				case Timeline timeline:
				{
					var deadlines = this.data.Deadlines(filter);
					BusinessBoardKit.ShowEmpty(body, deadlines.Count == 0 ? "No filings fall due." : null);
					timeline.Items.Clear();
					foreach (var d in deadlines)
					{
						timeline.Items.Add(new TimelineItem(d.Due, d.Title)
						{
							Description = d.Agency + " · " + DueText(d),
							Tone = ToneOf(d),
							Tag = d.Id,
						});
					}
					break;
				}

				case DataGridView grid:
				{
					string message = scope == ComplianceScope.Carbon ? "CI scores apply to fuel ethanol, not CO2." : null;
					var scores = message == null ? this.data.PlantScores(filter, refresh) : null;
					message ??= scores.Count == 0 ? "No plants to score." : null;
					BusinessBoardKit.ShowEmpty(body, message);
					if (message == null)
						FillCiGrid(grid, scores, filter);
					break;
				}

				case LimitUtilization limits:
				{
					var coverage = this.data.Coverage(filter, refresh);
					BusinessBoardKit.ShowEmpty(body, coverage.Count == 0 ? "No credits held for the filter." : null);
					if (coverage.Count > 0)
						limits.Rows = coverage.Select(c => new LimitUtilizationRow(c.Name, c.CommittedM, c.HeldM, c.Tag)).ToList();
					break;
				}
			}
		}

		// Why nothing is left for the filter, or null when something is.
		private static string NoneMessage(BusinessFilter filter)
		{
			string commodity = BusinessFilter.CommodityName(filter.Commodity);
			if (filter.Pick != null && string.Equals(filter.Commodity, "co2", StringComparison.OrdinalIgnoreCase))
			{
				string program = filter.Pick switch
				{
					"rfs" => "RFS",
					"lcfs" => "California LCFS",
					"ocfp" => "Oregon CFP",
					"wacfs" => "Washington CFS",
					_ => "program",
				};
				return "No " + program + " credits for " + commodity + ".";
			}

			return "No compliance credits for " + commodity + ".";
		}

		private void FillTile(StatTile tile, string metric, ComplianceScope scope, BusinessFilter filter, int refresh)
		{
			if (scope == ComplianceScope.None)
			{
				BusinessBoardKit.EmptyTile(tile, NoneMessage(filter));
				return;
			}

			var s = this.data.Summary(filter, refresh);
			if (s == null)
			{
				BusinessBoardKit.EmptyTile(tile, NoneMessage(filter));
				return;
			}

			switch (metric)
			{
				case TILE_RINS:
				case TILE_RETIRED:
				{
					// RINs are an RFS thing: not for CO2, and not for a low-carbon program's pick
					if (scope == ComplianceScope.Carbon)
					{
						BusinessBoardKit.EmptyTile(tile, "No RINs for CO2");
						return;
					}
					if (!filter.IncludesPick("rfs"))
					{
						BusinessBoardKit.EmptyTile(tile, "RINs belong to the RFS program");
						return;
					}
					if (filter.IsForward)
					{
						BusinessBoardKit.EmptyTile(tile, filter.NoHistoryMessage());
						return;
					}

					bool generated = metric == TILE_RINS;
					tile.Value = Millions(generated ? s.RinsGenerated : s.RinsRetired);
					tile.Change = Signed(generated ? s.RinsGeneratedChangePct : s.RinsRetiredChangePct, "%");
					tile.Caption = (generated ? "vs last period · " : "retired or separated · ") + BusinessBoardKit.Caption(filter);
					break;
				}

				case TILE_CI:
					if (scope == ComplianceScope.Carbon)
					{
						BusinessBoardKit.EmptyTile(tile, "No CI score for CO2");
						return;
					}

					tile.Value = s.AvgCi.ToString("0.0", Culture);
					tile.Change = Signed(s.AvgCiChange, "");
					tile.Caption = "gCO2e/MJ · vs " + SampleComplianceData.StandardTag(filter) + " " + SampleComplianceData.StandardOf(filter).ToString("0.0", Culture);
					break;

				default:
					tile.Value = s.FilingsDue.ToString(Culture);
					tile.Change = "";
					tile.Caption = s.FilingsDue == 0 ? "none in the next 30 days" : "next: " + s.NextFiling;
					break;
			}
		}

		private static string Millions(double value) => (value / 1_000_000).ToString("N1", Culture) + "M";

		private static string Signed(double value, string suffix) =>
			(value < 0 ? "-" : "+") + Math.Abs(value).ToString("0.0", Culture) + suffix;

		private static string DueText(ComplianceDeadline d)
		{
			if (d.Filed)
				return "filed";

			int days = (int)(d.Due - BusinessFilter.Today).TotalDays;
			return days switch
			{
				0 => "due today",
				1 => "due tomorrow",
				_ => "due in " + days.ToString(Culture) + " days",
			};
		}

		private static TimelineTone ToneOf(ComplianceDeadline d)
		{
			if (d.Filed)
				return TimelineTone.Success;

			int days = (int)(d.Due - BusinessFilter.Today).TotalDays;
			return days <= 3 ? TimelineTone.Danger : days <= 14 ? TimelineTone.Warning : TimelineTone.Primary;
		}

		private static DataGridView CreateCiGrid()
		{
			var grid = new DataGridView
			{
				Name = "CiScores",
				AllowUserToAddRows = false,
				AllowUserToDeleteRows = false,
				ReadOnly = true,
				RowHeadersVisible = false,
				SelectionMode = DataGridViewSelectionMode.FullRowSelect,
				BorderStyle = BorderStyle.None,
				CellBorderStyle = DataGridViewCellBorderStyle.Horizontal,
				ColumnHeadersHeight = 34,
				AllowUserToResizeRows = false,
			};
			grid.RowTemplate.Height = 36;
			grid.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.FromName("@grayText");
			grid.Columns.AddRange(
				new DataGridViewTextBoxColumn { Name = "Plant", HeaderText = "Plant", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 120 },
				new DataGridViewTextBoxColumn { Name = "Pathway", HeaderText = "Pathway", Width = 190 },
				Right("Ci", "CI g/MJ", 90),
				Right("Delta", "vs standard", 100),
				Right("Credit", "¢/gal", 80));
			return grid;
		}

		private static DataGridViewTextBoxColumn Right(string name, string header, int width)
		{
			var column = new DataGridViewTextBoxColumn { Name = name, Width = width, HeaderText = header };
			column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
			column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
			return column;
		}

		private static void FillCiGrid(DataGridView grid, IReadOnlyList<PlantCiScore> scores, BusinessFilter filter)
		{
			grid.Columns["Delta"].HeaderText = "vs " + SampleComplianceData.StandardTag(filter);
			grid.Rows.Clear();
			foreach (var s in scores)
			{
				double delta = s.Ci - s.Standard;
				grid.Rows.Add(
					s.Plant + ", " + s.Region,
					s.Pathway,
					s.Ci.ToString("0.0", Culture),
					Signed(delta, ""),
					s.CreditCents.ToString("0.0", Culture));
			}
		}

		// What a body keeps between fills: its filter, how often it was refreshed, and which figure a tile shows.
		private sealed class State
		{
			public BusinessFilter Filter;
			public int Refreshes;
			public string Tile = TILE_RINS;
		}
	}
}
