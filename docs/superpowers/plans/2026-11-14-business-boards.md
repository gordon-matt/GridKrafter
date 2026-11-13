# Business Boards Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add eight business boards to the Poet Biofuels Wisej demo: Customers, Carriers, Accounting, Contracts, Risk & Credit, Plant Operations, Scheduling & Nominations, and Compliance & Regulatory. They run on the same board page as the commodity boards, with a Commodity + Period + one-pick filter, shared customer/carrier/counterparty names, and five new components.

**Architecture:** The commodity board page's logic moves into `BoardPanel`, which works with any `IBoardSource`. A source is a board definition plus its filter bar. Commodity boards keep `ICommodityBoard` and `PlantFilterBar` unchanged, behind a `CommodityBoardSource`. Business boards implement `IBusinessBoard` with a `BusinessFilter` and `BusinessFilterBar`, behind a `BusinessBoardSource`.

**Tech Stack:** Wisej-4 4.2.0-test.7, Wisej-4-NavigationBar, Wisej-4-BootstrapIcons, .NET 10 (`net10.0-windows;net10.0`).

**Spec:** `docs/superpowers/specs/2026-11-14-business-boards-design.md`

## Global Constraints

- Only `GridKrafter.WisejDemo` and these docs change. No packages.
- Theme colours only, by name (`@highlight`, `windowFrame`…). No hard-coded colours in C#. Mixins follow GridKrafter-Light/Dark.
- `*.Designer.cs`: plain designer statements only. Run the designer check on every designer file changed. Never hand-write `.resx` binary values.
- Every Bootstrap icon name must exist in `~/.nuget/packages/wisej-4-bootstrapicons/4.1.8-preview.9/lib/net8.0/Wisej.Ext.BootstrapIcons.dll` (grep `-a "<name>.svg"`).
- Keep behaviours already shipped: page scrolling (`IFitsPage`/`BoardFit`), title-row actions (`IBoardPage`, `board.ShowActions = false`), save per key (`App_Data/board-<key>.json`), empty states, and the MOT filter on commodity boards.
- Commit only your own files, with explicit paths. Messages end with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

## Review Focus

1. **A business pick that matches nothing** (a carrier with no lanes for the commodity): every widget shows an empty state and nothing throws. Each board task checks it.
2. **Switching between a commodity board and a business board:** each keeps its own filter in the session, and neither overwrites the other's.
3. **Search and alerts navigate to business boards** with the pick set (a customer hit opens Customers filtered to that customer).
4. **The same customer name on Ethanol liftings, Customers, Contracts and Risk.** All draw from one registry.
5. **Period "Next 30 days"** on pages with only historic data shows forward-looking widgets (expiries, deadlines, nominations) and says "no history for the next 30 days" on the others. It never throws.

---

### Task 1: Board foundation (one agent; blocks Tasks 3–10)

**Files:**
- Create:
  - `Panels/BoardPanel.cs` (+ `.Designer.cs`): the logic of today's `CommodityBoardPanel`, moved and generalised.
  - `Panels/BoardSources.cs`
  - `Panels/BusinessFilterBar.cs` (+ `.Designer.cs`)
  - `Services/Business/BusinessFilter.cs`
  - `Services/Business/IBusinessBoard.cs`
  - `Services/Business/Registries.cs`
  - `Panels/BusinessPanels.cs`
  - `Model/Boards/Business/<X>Board.cs` stubs (8)
- Modify:
  - `Panels/CommodityBoardPanel.cs`: becomes a thin subclass of `BoardPanel`, keeping its public `Filter`, `ShowPlant` and `RememberPlant` and the static API that Page1 uses.
  - `Page1.cs` / `Page1.Designer.cs`: four navigation groups.
  - `Services/Shell/PageInfo.cs`: 8 entries.
  - `Services/Shell/ShellSearch.cs`: customers and carriers from the registries, opening the business boards.
  - The customer-name lists in `Services/Commodities/{Ethanol,Co2,Purified}Data.cs`, which switch to `CustomerRegistry` names. Values and seeds otherwise unchanged.

**Produces:**
```csharp
namespace GridKrafter.WisejDemo.Services.Business;
public enum BusinessPeriod { Mtd, Qtd, Ytd, Next30 }
public sealed record BusinessFilter(string Commodity /* null = all; board keys: ethanol, co2, purified, denaturant, natgas, grain */, BusinessPeriod Period, string Pick /* null = all */) {
  public static BusinessFilter Default { get; } // (null, Mtd, null)
  public string Describe();
}
public sealed record PickChoice(string Id, string Text);
public interface IBusinessBoard {
  string Key { get; }                  // "customers","carriers","accounting","contracts","risk","plantops","scheduling","compliance"
  string PickLabel { get; }            // "Customer", "Carrier", "Entity", "Counterparty", "Counterparty", "Plant", "Terminal", "Program"
  string PickAllText { get; }          // "All customers"…
  IReadOnlyList<PickChoice> PickChoices { get; }
  DashboardBoard Create();
  IEnumerable<GridWidgetKind> Catalog { get; }
  Control CreateBody(DashboardItem item, BusinessFilter filter);
  void Refresh(Control body, BusinessFilter filter);
}
public sealed record Customer(string Id, string Name, string Segment, string Region, double CreditLimit, string Rating);
public sealed record Carrier(string Id, string Name, TransportMode Mode);
public sealed record Counterparty(string Id, string Name, string Kind /* customer|supplier|broker */, double CreditLimit, string Rating);
public sealed record Terminal(string Id, string Name, string Location, int Bays);
public static class CustomerRegistry { IReadOnlyList<Customer> All; Customer Find(string id); }      // ~24, ids c01..
public static class CarrierRegistry { IReadOnlyList<Carrier> All; Carrier Find(string id); }        // ~12, ids k01..
public static class CounterpartyRegistry { IReadOnlyList<Counterparty> All; Counterparty Find(string id); } // customers + ~6 suppliers + 1 clearing broker
public static class TerminalRegistry { IReadOnlyList<Terminal> All; Terminal Find(string id); }     // ~6, ids t01..
// Panels
public interface IBoardSource { string Key {get;} DashboardBoard Create(); IEnumerable<GridWidgetKind> Catalog {get;} Control FilterBar {get;} event EventHandler FilterChanged; Control CreateBody(DashboardItem item); void Refresh(Control body); void Load(); /* restore filter from session */ string Describe(); }
public abstract class BoardPanel : UserControl, IFitsPage, IBoardPage { protected BoardPanel(IBoardSource source); public void ResetBoard(); }
public abstract class BusinessBoardPanel : BoardPanel { protected BusinessBoardPanel(IBusinessBoard board); public void ShowPick(string pickId); public static void RememberPick(string key, string pickId); }
// BusinessPanels.cs: CustomersPanel, CarriersPanel, AccountingPanel, ContractsPanel, RiskPanel, PlantOpsPanel, SchedulingPanel, CompliancePanel
```
Session keys: the commodity filter stays `gk.plantFilter`. Each business board uses `gk.bizFilter.<key>`.

- [ ] Move the panel logic into `BoardPanel` and make `CommodityBoardPanel` a subclass. Build. Then confirm by code reading that the 8 commodity boards behave exactly as before: filter, MOT, scrolling, title-row actions, save keys.
- [ ] Add the registries, `BusinessFilter`, `BusinessFilterBar` (Commodity, Period and pick ComboBoxes with labels on the left, wrapping like `PlantFilterBar`), the business panels, the 8 stub boards (one stat tile each) and the navigation groups:
  - Commercial: Customers, Contracts, Risk & Credit.
  - Logistics: Carriers, Scheduling.
  - Finance: Accounting.
  - Operations: Plant Operations, Compliance.
  Add the icons, and add `PageInfos` entries.
- [ ] Search: customer hits open Customers via `BusinessBoardPanel.RememberPick("customers", id)`; carrier hits open Carriers.
- [ ] Run the designer check on each changed designer file, build both targets, and commit `feat(wisej): business board foundation`.

### Task 2: Five components (Component Studio, one agent each; parallel with Task 1)

AgingBuckets, LimitUtilization, CashFlowChart, PnlWaterfall and TerminalSlotBoard, with the properties and events from the spec's table. Each must:
- dock to fill a widget;
- be fluid from about 300×200 to 1100×600;
- have no card chrome of its own;
- follow the theme.

Each agent also adds the test form and reports the API and row classes. Commit per component.

### Tasks 3–10: One business board each (parallel, after Tasks 1 and 2)

Each board task owns only `Model/Boards/Business/<X>Board.cs` and `Services/Business/<X>Data.cs` (`I<X>Data` + `Sample<X>Data`, seeded with `SampleRandom`-style FNV from the key, filter and refresh).
- Widgets: as the spec's table lists.
- Row 1: 4 stat tiles (Kind `stat`, Plain, Color|Delete, W3 H2).
- Empty states: Review Focus 1 and 5.
- Verify: build; reason through the filters All, one pick and Next 30 days.
- Commit `feat(wisej): <X> board`.

| Task | Board | Key |
|---|---|---|
| 3 | Customers | customers |
| 4 | Carriers | carriers |
| 5 | Accounting | accounting |
| 6 | Contracts | contracts |
| 7 | Risk & Credit | risk |
| 8 | Plant Operations | plantops |
| 9 | Scheduling & Nominations | scheduling |
| 10 | Compliance & Regulatory | compliance |

### Task 11: Integration check

- [ ] Build both targets. Write a throwaway check, outside the repo, that calls every public method of every `Sample*Data` (business and commodity) for representative filters, and confirms no exceptions.
- [ ] Walk Review Focus 2–4 by code reading.
- [ ] Commit any fixes.
