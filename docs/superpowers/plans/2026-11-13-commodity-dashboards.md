# Commodity Dashboards Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an Enterprise Overview board and seven commodity boards (Ethanol, CO2, Purified Ethanol, Denaturant, Natural Gas, Grain, Freight) to the Wisej demo. Each is an editable GridBoard with a shared plant filter bar, fed by sample data services.

**Architecture:** One `CommodityBoardPanel` (the HomePanel board logic, parameterised) hosts any `ICommodityBoard`, a self-contained definition that holds the board layout, its widget bodies and its data service. The tiny per-board panel subclasses exist only so `Page1`'s navigation `Tag` → type lookup keeps working. Boards share a `PlantRegistry` and a `PlantFilter`.

**Tech Stack:** Wisej-4 (4.2.0-test.7), .NET 10 (`net10.0-windows;net10.0`), GridKrafter.Core board model, existing and new Wisej components in `GridKrafter.WisejDemo/Controls`.

**Spec:** `docs/superpowers/specs/2026-11-13-commodity-dashboards-design.md`

## Global Constraints

- Only `GridKrafter.WisejDemo` changes (plus these docs). No new packages.
- Look follows the GridKrafter-Light/Dark theme. No hard-coded colours in C#; widget colours use the board colour names already in use (green, blue, orange, violet, sky…).
- `*.Designer.cs`: plain designer statements only (no `var`, lambdas, loops, interpolation, initializers).
- Plants: 42 total, 35 owned, 7 third-party, in regions Iowa, Nebraska, Minnesota, Illinois, South Dakota, Indiana, Ohio.
- Sample data is seeded and deterministic per (board, filter), with a small jitter on Refresh. Magnitudes are realistic for the US ethanol industry (plants of 50–150 MMgy; ethanol $1.60–2.20/gal; NG $3–4.5/MMBtu; corn $4–4.8/bu; CO2 $40–120/ton).
- No unit tests: the test project references only Core and stays untouched (scope). Verification = build both targets, the designer check on changed designer files, and running the app.

## Review Focus

1. **Filter that empties a board** (a single 3rd-party plant on the Grain board): every widget shows an empty state, nothing throws. Each board task checks `PlantFilter.ForPlant(<a 3rd-party id>)`.
2. **Switching boards quickly or theme mid-board:** panels unsubscribe their filter handler on dispose; no handler leaks (the same pattern as `Page1.Unsubscribe`).
3. **Saved layout of one board must not leak into another:** each board saves to `App_Data/board-<key>.json`; Reset on Ethanol leaves Grain's layout alone. Task 1 verification.
4. **Narrow screens (<600 px):** the filter bar wraps its combos under each other, and the board keeps HomePanel's centring rules.
5. **Refresh button on a widget** re-reads the service with the current filter, not the default one.

---

### Task 1: Board foundation (one agent; blocks Tasks 3–10)

**Files:**
- Create: `GridKrafter.WisejDemo/Services/Commodities/PlantFilter.cs`, `PlantRegistry.cs`, `ICommodityBoard.cs`, `SampleRandom.cs`
- Create: `GridKrafter.WisejDemo/Panels/CommodityBoardPanel.cs` (+ `.Designer.cs`): GridBoard + `PlantFilterBar` docked top + save timer, as HomePanel
- Create: `GridKrafter.WisejDemo/Panels/PlantFilterBar.cs` (+ `.Designer.cs`)
- Modify: `GridKrafter.WisejDemo/Services/BoardStore.cs`: keyed overloads
- Modify: `GridKrafter.WisejDemo/Page1.Designer.cs` / `Page1.cs`: "Commodities" navigation group with 8 items, plus `Intros` entries
- Create: `GridKrafter.WisejDemo/Panels/CommodityPanels.cs`: 8 subclasses

**Interfaces (produces):**
```csharp
namespace GridKrafter.WisejDemo.Services.Commodities;
public enum PlantOwnership { All, Owned, ThirdParty }
public sealed record PlantFilter(string Region, string PlantId, PlantOwnership Ownership) {
  public static PlantFilter All { get; }                 // (null, null, All)
  public static PlantFilter ForPlant(string plantId);
  public bool Includes(Plant p);
  public string Describe();                               // "All plants", "Iowa · owned", "Plant: Mason City"
}
public sealed record Plant(string Id, string Name, string Region, bool Owned, double CapacityMmgy);
public static class PlantRegistry { public static IReadOnlyList<Plant> All { get; } public static IEnumerable<Plant> Where(PlantFilter f); public static IReadOnlyList<string> Regions { get; } }
public static class SampleRandom { public static Random For(string board, PlantFilter f, int refresh); } // stable seed from board+filter, refresh shifts it
public interface ICommodityBoard {
  string Key { get; }            // "overview","ethanol","co2","purified","denaturant","natgas","grain","freight"
  DashboardBoard Create();       // GridKrafter.Models; ids unique within the board
  IEnumerable<GridWidgetKind> Catalog { get; } // kinds people may add back
  Control CreateBody(DashboardItem item, PlantFilter filter);
  void Refresh(Control body, PlantFilter filter);
}
// BoardStore
public static DashboardBoard Load(string key); public static void Save(string key, DashboardBoard b); public static void Reset(string key);
// key "home" maps to the existing board.json; the old no-arg methods call these with "home".
// Panels
public abstract class CommodityBoardPanel : UserControl { protected CommodityBoardPanel(ICommodityBoard board); }
public partial class PlantFilterBar : UserControl { public PlantFilter Filter { get; set; } public event EventHandler FilterChanged; }
// CommodityPanels.cs: OverviewPanel, EthanolPanel, Co2Panel, PurifiedPanel, DenaturantPanel, NaturalGasPanel, GrainPanel, FreightPanel
//   each: public class XPanel : CommodityBoardPanel { public XPanel() : base(new XBoard()) {} }
```
Board classes are `GridKrafter.WisejDemo.Model.Boards.<X>Board`. Task 1 adds a stub file per board (`Model/Boards/<X>Board.cs`) that returns one stat tile, so everything builds. Tasks 3–10 replace each stub.

- [ ] **Step 1:** Write `PlantRegistry` (42 plants following the Global Constraints, ids `p01`..`p42`, `p36`–`p42` third-party), `PlantFilter` and `SampleRandom`.
- [ ] **Step 2:** Add the keyed `BoardStore` overloads (`board-<key>.json`; `"home"` → `board.json`).
- [ ] **Step 3:** Build `PlantFilterBar`: a Region ComboBox ("All regions" + regions), a Plant ComboBox (narrowed by region), and an Owned / 3rd-party / All segmented control. It raises `FilterChanged` once per user change and wraps on narrow widths.
- [ ] **Step 4:** Build `CommodityBoardPanel`, porting `HomePanel`'s load/merge/restore/add/refresh/save/reset with `board.Key`. It calls `board.CreateBody(item, filter)`, and on `FilterChanged` it calls `board.Refresh(body, filter)` for every widget body. It unsubscribes on dispose.
- [ ] **Step 5:** Add the 8 stub boards and the 8 panel subclasses. Add the "Commodities" group to the navigation bar (Overview first, `Tag` = panel class name), plus an `Intros` line each.
- [ ] **Step 6:** Run the designer check on `Page1.Designer.cs`, `CommodityBoardPanel.Designer.cs` and `PlantFilterBar.Designer.cs`, then build the project. Expected: both targets succeed.
- [ ] **Step 7:** Run the app. Each Commodities item shows its stub board. Move a tile on Ethanol, then open Grain and back: Ethanol keeps its move, and Grain is untouched (Review Focus 3).
- [ ] **Step 8:** Commit `feat(wisej): commodity board foundation`.

### Task 2: New components (in progress, one sub-agent each)

PlantStatusGrid, ForwardCurveChart, HedgeCoverageMeter, LiftingScheduleTracker, QualityLotBoard and FreightLaneMatrix, created in Component Studio. Each report gives the component's public properties and row classes. Tasks 3–10 consume them.

### Tasks 3–10: One board each (parallel, after Tasks 1 and 2)

Each board task owns exactly: `Model/Boards/<X>Board.cs` (replacing the stub) and `Services/Commodities/<X>Data.cs` (interface `I<X>Data` + `Sample<X>Data`, taking `PlantFilter` and a refresh counter). It touches no shared file.

Every board, all its steps:
- [ ] **A:** Define `I<X>Data` with one method per widget, returning plain records. Write `Sample<X>Data` using `PlantRegistry.Where(filter)` and `SampleRandom`.
- [ ] **B:** Write `Create()`: row 1 is 4 plain stat tiles (W=3, H=2, `WidgetChrome.Plain`), with the widgets below in sections as listed. `Catalog` holds every widget kind of the board.
- [ ] **C:** Write `CreateBody`/`Refresh`: map records to the component properties, and show an empty state when the filter leaves nothing (Review Focus 1).
- [ ] **D:** Build, run, switch filters (All → a region → `p38` third-party), and press Refresh on a widget (Review Focus 5).
- [ ] **E:** Commit `feat(wisej): <X> board`.

| Task | Board (key) | Tiles | Widgets (component) |
|---|---|---|---|
| 3 | Overview (`overview`) | Volume (gal MTD), Gross margin $, Net position (gal), Plant run rate % | Line-of-business summary (DataGridView: line, volume, margin, Δ), PlantStatusGrid, HedgeCoverageMeter (all lines, weighted), CarbonCreditTracker |
| 4 | Ethanol (`ethanol`) | Volume, Margin $/gal, Net position, Inventory | a product switch Fuel/E-85/200 proof (TabControl or RadioButtons in a plain widget, which re-feeds the others), EthanolPriceBoard, NetPositionLadder, CrushMarginGauge, TerminalInventoryTanks, LiftingScheduleTracker (Contracted/Lifted per customer), PlantStatusGrid, CarbonCreditTracker |
| 5 | CO2 (`co2`) | Tons captured, Tons sold, $/ton, Storage % | LiftingScheduleTracker (contracts), TerminalInventoryTanks (CO2 storage, tons), capture by plant (DataGridView), PlantStatusGrid |
| 6 | Purified (`purified`) | Volume, Premium vs fuel ¢/gal, Lots on hold, Open orders | QualityLotBoard, LiftingScheduleTracker, TerminalInventoryTanks |
| 7 | Denaturant (`denaturant`) | Min days of supply, Inventory gal, Receipts MTD, Cost $/gal | TerminalInventoryTanks, LiftingScheduleTracker (Scheduled/Received per supplier), days of supply by plant (DataGridView, red under 5 days) |
| 8 | Natural Gas (`natgas`) | Burn MMBtu/d, Price $/MMBtu, % hedged, Imbalance | ForwardCurveChart (Henry Hub), HedgeCoverageMeter, LiftingScheduleTracker (Nominated/Burned per plant), PlantStatusGrid |
| 9 | Grain (`grain`) | Grind bu/d, Receipts bu, Avg basis ¢, % hedged | ForwardCurveChart (CBOT corn), basis bids by plant (DataGridView), HedgeCoverageMeter, LiftingScheduleTracker (Grind/Receipts), PlantStatusGrid |
| 10 | Freight (`freight`) | Cars in transit, Avg transit days, Demurrage $, Cost ¢/gal | RailcarFleetTracker, FreightLaneMatrix, mode mix (3 StatTiles or a DataGridView) |

### Task 11: Integration check (after all)

- [ ] Build both targets. Run the designer check on every changed designer file.
- [ ] Walk all 8 boards in the Light and Dark themes and at 1400 px and 500 px widths. Fix any overflow.
- [ ] Commit `chore(wisej): commodity boards polish`.
