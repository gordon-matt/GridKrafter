# Business boards: Customers, Carriers, Accounting, Contracts, Risk & Credit, Plant Operations, Scheduling, Compliance — design

Date: 2026-11-14 · Scope: `GridKrafter.WisejDemo` only

## Intent

Round out the Poet Biofuels demo past commodities: give the people who run customers, freight, money, contracts, risk, plants, scheduling and compliance a board each. They should look and behave like the commodity boards, and they should share one set of customer, carrier and counterparty names, so the whole app tells one story.

## Decisions (developer)

- **Pages:** Customers, Carriers, Accounting, Contracts, Risk & Credit, Plant Operations, Scheduling & Nominations, Compliance & Regulatory. (Market Intelligence was not chosen.)
- **Filter bar on every new page:** Commodity (All, Ethanol, CO2, Purified, Denaturant, Natural Gas, Grain) + Period (MTD, QTD, YTD, Next 30 days), plus one page-specific pick:

| Page | Its own pick |
|---|---|
| Customers | Customer |
| Carriers | Carrier |
| Accounting | Entity |
| Contracts | Counterparty (customer or supplier) |
| Risk & Credit | Counterparty |
| Plant Operations | Plant |
| Scheduling | Terminal |
| Compliance | Program (RFS, LCFS, Oregon CFP, WA CFS) |

- **Approach A (picked):** generalise the commodity board page so its filter bar is pluggable. There is one board page type for all 16 boards, so scrolling, the title-row actions, saving and refresh are shared.

## Approaches considered

- **A. Pluggable filter bar on the existing board page (picked).** Everything already fixed stays fixed, and there is one code path.
- **B. A separate business-board page type.** Duplicates the save/refresh/fit logic, so two places need every fix. Rejected.
- **C. Plain forms with grids.** Not editable dashboards, and a different look. Rejected.

## Architecture

- **Board filter abstraction.**
  - `ICommodityBoard` keeps its name. Its filter parameter becomes a `BoardFilter` base record; `PlantFilter` derives from it, and a new `BusinessFilter(Commodity, Period, Pick)` does too.
  - Each board says which filter bar it uses (`IBoardFilterBar` factory on the board).
  - The `PlantFilterBar` stays as it is. A new `BusinessFilterBar` holds Commodity, Period and one pick (label and choices supplied by the board).
  - Session memory: the plant filter is shared across commodity boards as today; each business page remembers its own filter.
- **Shared names:** `Services/Business/Registries.cs` holds `CustomerRegistry` (about 24 customers), `CarrierRegistry` (about 12 rail/truck/barge carriers), `CounterpartyRegistry` (customers + suppliers + clearing broker) and `TerminalRegistry`.
  - The existing sample data that names customers (Ethanol, CO2, Purified) is switched to these names.
  - Shell search indexes them.
- **Navigation:** new groups after Commodities:
  - Commercial: Customers, Contracts, Risk & Credit.
  - Logistics: Carriers, Scheduling.
  - Finance: Accounting.
  - Operations: Plant Operations, Compliance.
  - Each page gets a breadcrumb, a subtitle and a demo intro in `PageInfos`.
- **Data:** `I<X>Data` + `Sample<X>Data` per page, seeded by `SampleRandom` from the board key and the filter, as the commodity boards are.

## Widgets per page

Row 1 on every page: 4 StatTiles.

| Page | Tiles | Widgets |
|---|---|---|
| Customers | Active customers, Volume, Margin, AR past due | Top customers (DataGridView: volume, margin, Δ); LiftingScheduleTracker (contracted vs lifted); **AgingBuckets** (AR for the pick); **LimitUtilization** (credit used vs limit); open orders (DataGridView) |
| Carriers | Carriers active, On-time %, Claims open, Demurrage $ | Carrier scorecard (DataGridView with MeterBar-style %); FreightLaneMatrix (rates for the carrier); LiftingScheduleTracker (committed vs used capacity); Timeline (insurance and compliance expiring) |
| Accounting | AR, AP, Cash, Gross margin | **AgingBuckets** ×2 (AR, AP); **CashFlowChart** (13-week in/out/net); **PnlWaterfall** (revenue → costs → margin by commodity); invoices to send or approve (DataGridView); month-end close checklist (CheckedListBox or Timeline) |
| Contracts | Open contracts, Volume under contract, Expiring 30d, % index-priced | Contract book (DataGridView: number, counterparty, product, fixed/index, volume, delivered %, end date); LiftingScheduleTracker (delivered vs contracted); Timeline (renewals and expiries); nominations due (DataGridView) |
| Risk & Credit | Total exposure, Limit breaches, MTM P&L, VaR (95%) | **LimitUtilization** (exposure vs limit by counterparty); NetPositionLadder (book); HedgeCoverageMeter (all lines); **AgingBuckets** (exposure by tenor) |
| Plant Operations | Run rate, Production vs plan, Plants down, Yield gal/bu | PlantStatusGrid; LiftingScheduleTracker (plan vs actual production per plant); Timeline (outages and maintenance); yields by plant (DataGridView) |
| Scheduling & Nominations | Nominations today, Loads scheduled, Slots free, Late loads | **TerminalSlotBoard** (loading slots per bay through the day); nominations (DataGridView: counterparty, product, volume, MOT, status); RailcarFleetTracker (cars arriving) |
| Compliance & Regulatory | RINs generated, RINs retired, Avg CI score, Filings due | CarbonCreditTracker; Timeline (EPA and state filing deadlines); CI score by plant (DataGridView); **LimitUtilization** (RVO coverage per obligation) |

Catalog controls to use where they fit: Timeline, DataGridView, StatTile (project), MeterBar/ChipLabel if they serve a column.

## New components (Component Studio, one sub-agent each)

| Component | Shows | Props / events |
|---|---|---|
| AgingBuckets | stacked or grouped bars for Current, 1–30, 31–60, 61–90, 90+ with $ and %, total, past-due highlight | title, unit, buckets (rows: bucket, amount, count); bucketSelected |
| LimitUtilization | one row per counterparty: exposure bar against limit, % used, breach chip, sorted by % | title, unit, rows (name, exposure, limit, rating); rowSelected |
| CashFlowChart | 13 weekly columns: inflows up, outflows down, net line and running balance, minimum-cash line | title, unit, weeks (rows: week, inflow, outflow), openingBalance, minimumCash; weekSelected |
| PnlWaterfall | waterfall from revenue through cost lines to gross margin, by commodity or line | title, unit, steps (rows: label, value, kind = total/up/down); stepSelected |
| TerminalSlotBoard | bays × hours of the day; booked, loading, done and late slots coloured by MOT; a now-line | title, startHour, endHour, slots (rows: bay, start, end, counterparty, product, mot, status); slotSelected |

## Non-goals / anti-goals

- No real ERP or ETRM connections, posting or approvals: read-only sample data.
- No change to the commodity boards' widgets, apart from switching their customer names to the shared registry.
- Don't build panels and labels standing in for a component. Don't hard-code colours: use the theme names, as the existing mixins do.

## Left to the builder

- Exact grid positions, intro and subtitle copy, and sample magnitudes (realistic for a 40-plant ethanol marketer).
- Bootstrap icon names: each must be checked against the icon assembly.
- How the business filter bar wraps on a narrow screen (same rules as the plant bar).
