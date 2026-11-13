# Commodity dashboards for the Wisej.NET demo — design

Date: 2026-11-13 · Scope: `GridKrafter.WisejDemo` only

## Intent

Show GridKrafter as the dashboard layer of an enterprise ethanol-marketing company. The company buys and sells product from 40+ ethanol plants (35 owned, the rest 3rd-party) across these lines of business: Ethanol (fuel, E-85, 200 proof), CO2, Purified Ethanol, Denaturant, Natural Gas, Grain and Freight. Each line gets its own dashboard page, and an Enterprise Overview rolls them up.

## Decisions (developer)

- **Users: mixed and role-aware.** Each board opens with a row of key-figure tiles for leadership. Trading and operations widgets sit below.
- **Shared filter bar on every board:** All plants / Region / single Plant, plus Owned / 3rd-party / Both. Changing it refreshes every widget on that board.
- **Data:** realistic sample data behind one service interface per commodity, so real ERP/ETRM sources can replace it later without touching widgets.
- **Enterprise Overview board:** yes, the first item of the Commodities group.
- Pages and components are built in parallel.

## Goals / non-goals / anti-goals

- Goals: 8 new boards (Overview + 7 commodities), the same behaviour as the Dashboard (editable GridBoard, layout saved on the server), and a small set of reusable components.
- Non-goals: real data connections, authentication or per-user storage, and any change outside `GridKrafter.WisejDemo`.
- Anti-goals: one-off controls per page that only differ by data; panels-and-labels imitations of what a component should draw; a new look (everything follows the GridKrafter-Light/Dark theme and its mixins).

## Approaches considered

1. **Copy HomePanel once per commodity:** quick, but 8 copies of the board logic. Rejected.
2. **One generic `CommodityBoardPanel` driven by a board definition (picked).** It holds the board logic from HomePanel (load/merge/save, catalog, refresh), parameterised by a board key and a `DashboardBoard` definition, with the filter bar docked on top. Each page is a thin subclass or a definition entry.
3. **One page with a commodity switcher:** fewer menu items, but no deep link per commodity and harder to keep a layout per board. Rejected.

## Architecture

- **Navigation:** add a "Commodities" group to `Page1`'s navigation bar with Overview, Ethanol, CO2, Purified Ethanol, Denaturant, Natural Gas, Grain and Freight. Each item's `Tag` names its panel, so the existing `ShowPanel` reflection keeps working. Add an intro line per panel to `Intros`.
- **Board store per board:** `BoardStore` takes a board key (`board.json` stays for Home; commodities use `board-<key>.json`). Reset clears only that board.
- **Filter bar:** a `PlantFilterBar` user control (Wisej ComboBoxes / segmented RadioButtons) that raises `FilterChanged` with a `PlantFilter { Region, PlantId, Ownership }`. The panel refreshes every widget body with the filter.
- **Data:** `Services/Commodities/` holds `ICommodityDataService` per line (`IEthanolData`, `ICo2Data`, …) plus a `PlantRegistry` (42 plants: id, name, state, region, owned flag, nameplate capacity), with `Sample*Data` implementations that are seeded and deterministic, honour the filter and jitter slightly on refresh. They are resolved through one static `CommodityData` locator, which is easy to swap for DI later.
- **Widget bodies:** extend `WidgetBodies.Create`/`Refresh` (or a parallel `CommodityWidgetBodies`) with commodity kinds. Bodies take the active `PlantFilter`.
- **Board definitions:** `Model/CommodityBoards.cs` has one `DashboardBoard` per key. Row 1 is plain-chrome stat tiles (existing `StatTile`); widgets follow.

## New reusable components (Component Studio, one sub-agent each)

| Component | Shows | Key props / events |
|---|---|---|
| PlantStatusGrid | 40+ plant tiles: status (running/down/reduced), run rate vs. nameplate, owned vs. 3rd-party badge, region grouping | plants (rows), selected, groupBy; plantSelected |
| ForwardCurveChart | monthly forward price curve with the hedged volume/price overlay and today's marker | points (rows: month, price, hedgedPrice, hedgedVolume), unit; monthSelected |
| HedgeCoverageMeter | per-month % hedged as bars against a policy band (min/max) | months (rows), policyMin, policyMax; monthSelected |
| LiftingScheduleTracker | planned vs. actual (contracted vs. lifted, nominated vs. burned) per row with % and status | rows (label, planned, actual, unit), selected; rowSelected |
| QualityLotBoard | lot results (proof, water, methanol, sulfur, etc.) against spec, pass/fail/hold, CoA status | lots (rows), specs; lotSelected |
| FreightLaneMatrix | lanes × mode (rail/truck/barge) with rate, transit days and a demurrage-aging strip | lanes (rows), mode; laneSelected |

Existing components reused: EthanolPriceBoard, NetPositionLadder, CrushMarginGauge, TerminalInventoryTanks, CarbonCreditTracker, RailcarFleetTracker, StatTile.

## Boards

| Board | Tiles | Widgets |
|---|---|---|
| Overview | volume, gross margin, net position, plant run rate | per-commodity margin/volume grid, PlantStatusGrid, HedgeCoverageMeter (all lines), CarbonCreditTracker |
| Ethanol | volume, margin/gal, net position, inventory | product tabs Fuel / E-85 / 200 proof feeding: EthanolPriceBoard, NetPositionLadder, CrushMarginGauge, TerminalInventoryTanks, LiftingScheduleTracker, PlantStatusGrid, CarbonCreditTracker |
| CO2 | tons captured, tons sold, realized $/ton, storage % | LiftingScheduleTracker (customer contracts), TerminalInventoryTanks (CO2 storage), capture-by-plant grid, PlantStatusGrid |
| Purified Ethanol | volume, premium vs. fuel, lots on hold, open orders | QualityLotBoard, LiftingScheduleTracker, TerminalInventoryTanks |
| Denaturant | days of supply (min), inventory, receipts MTD, cost/gal | TerminalInventoryTanks, LiftingScheduleTracker (supplier deliveries), days-of-supply-by-plant grid |
| Natural Gas | burn MMBtu, price $/MMBtu, % hedged, imbalance | ForwardCurveChart (Henry Hub + basis), HedgeCoverageMeter, LiftingScheduleTracker (nominated vs. burned per plant), PlantStatusGrid |
| Grain | grind bu/day, receipts, avg basis, % hedged | ForwardCurveChart (corn), basis bids by plant grid, HedgeCoverageMeter, LiftingScheduleTracker (grind vs. receipts), PlantStatusGrid |
| Freight | cars in transit, avg transit days, demurrage $, cost/gal | RailcarFleetTracker, FreightLaneMatrix, mode mix (chart or tiles) |

## Left to the builder

- Exact grid positions and sizes, and the sample-data values (realistic US ethanol-industry magnitudes).
- The control for simple grids and the mode mix (DataGridView / existing chart widget), and the intro wording per board.
- Whether panels are subclasses or one panel class with a key. Navigation `Tag` must resolve either way.
