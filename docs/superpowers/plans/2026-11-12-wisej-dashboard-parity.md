# Wisej Dashboard Parity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the GridKrafter.WisejDemo dashboard look and behave like the GridKrafter.Demo.Mvc dashboard, in view mode and in edit mode, with native Wisej controls styled by the Wisej theme.

**Architecture:** Fix theme following first, so every later visual check is trustworthy. Then extend the existing `GridBoard`/`GridWidget` component (server class, qooxdoo client script, mixin) with the MVC edit-mode toolbar, section row and widget chrome. Add two small native components, `ActivityList` and `ServerLoadList`, for the widget bodies. Finally rework the stat tiles and the dashboard page (`Panels/HomePanel`) to use them in a centred ~1080 px column.

**Tech Stack:** Wisej.NET 4.2.0-test.7 (local `packages` feed), .NET 10 (`net10.0-windows;net10.0`), C#, qooxdoo client classes, Wisej `.theme`/`.mixin.theme`, GridKrafter.Core (layout engine, `DashboardBoard` model, `DashboardJson`, `BoardMerge`).

**Spec:** `docs/superpowers/specs/2026-11-12-wisej-dashboard-parity-design.md`

## Global Constraints

- Shell unchanged: NavigationBar sidebar, top bar (title, description, status, theme switcher, "Reset the board"), Events and About pages.
- No changes to GridKrafter.Core, GridKrafter.Components, GridKrafter.Demo.Mvc or GridKrafter.Demo.Blazor.
- Every colour comes from a theme colour name. The components' mixins name their own colours, which map to the current theme's (success, warning, danger, highlight, info) with the GridKrafter hue as the fallback.
- No HTML strings, iframes, page script or Canvas standing in for a widget body. No panels and labels imitating a catalog control.
- Designer files (`*.Designer.cs`) hold only designer statements: no lambdas, `var`, control flow, interpolation or initializers.
- The board is a centred column at most 1080 px wide. Inside it, text is 13 px (titles, tabs, links, rows) and 12 px (footers, muted lines); the theme's base font is untouched.
- UI text comes from `GridKrafter.Core/Locales/en.json` where the original has it ("Done", "Add widget", "Add section", "Reset layout", "Collapse all", "Expand all", "Last updated {0}", "Title", "Close", confirmation texts).
- Palette names: `blue green orange violet amber sky red pink` (`GridWidget.PaletteNames`).
- Meter thresholds: green < 65, amber ≥ 65, red ≥ 85.
- The project builds with 0 errors and 0 warnings for `net10.0-windows`, and every changed designer file passes the designer check.
- Do not touch `Page1.resx` binary entries or the `resources.GetObject(...)` lines in `Page1.Designer.cs`.

## Review Focus

- **Switching themes at runtime** (Auto → Dark → Fluent Light → Bootstrap Dark) while the dashboard is open: every tile, list, meter and the sidebar must recolour without a reload. Pinned in Task 1, step 4, and re-checked in Task 6.
- **Deleting the last widget of a section, then the section itself:** no confirmation for an empty section, a confirmation for a non-empty one, and the selected tab moves to a neighbour. Pinned in Task 3.
- **Renaming a section or a widget to an empty title:** it falls back to the original title or the section id, never an empty tab. Pinned in Task 3 and Task 2.
- **Reset layout, then reload:** the page's own board comes back, `App_Data/board.json` is gone, and restored deleted widgets are not duplicated. Pinned in Task 3.
- **A board saved before this change** (no new fields) loads without errors. Pinned in Task 6.

---

### Task 1: Theme following

Fix why KpiPanel/MeterBar render light in dark themes and why the NavigationBar keeps its slate default.

**Files:**
- Modify: `GridKrafter.WisejDemo/Program.cs`, `GridKrafter.WisejDemo/Page1.cs`
- Modify (through Theme Studio only): `GridKrafter.WisejDemo/Themes/GridKrafter-Light.theme`, `GridKrafter-Dark.theme`
- Create if needed: `GridKrafter.WisejDemo/Themes/App.mixin.theme`

**Interfaces:**
- Produces: the rule later tasks follow: "a component colour is a mixin colour whose value is a theme colour name". Record which way works (theme colour names in a project mixin vs values in the theme) in a comment at the top of `App.mixin.theme`, or in the task report if no mixin is needed.

- [ ] **Step 1: Reproduce.** Run the app with the browser in dark mode and the switcher on Auto, then switch to GridKrafter Dark, Fluent Dark and Bootstrap Dark. Note which controls stay light: KpiPanel, MeterBar track, NavigationBar.
- [ ] **Step 2: Find the cause.** Check (a) whether `Application.Browser.IsDarkMode` is known in `Program.Main` (if not, the first theme load happens after the page exists), and (b) whether extension or framework mixin colours (`navbar-*`, `kpipanel-*`, `meterbar-*`) override the theme's colours of the same name. Inspect the loaded theme in the browser with `Wisej.Platform` theme colours, or read the mixins' order of application.
- [ ] **Step 3: Fix.**
  - If (a): apply the browser's choice before the page is created, or recreate the shown panel after `Application.LoadTheme`.
  - If (b): add `Themes/App.mixin.theme`, with colours mapped to theme colour names (`navbar-background: window`, `navbar-text: grayText`, `navbar-background-selected: tint`, `navbar-text-selected: highlight`, `kpipanel-background: window`, `kpipanel-border: window`, `meterbar-track: buttonFace`, …), so it follows every theme.
- [ ] **Step 4: Verify.** Build. Re-run step 1: in all four dark themes nothing stays light, and in all light themes nothing stays dark. The sidebar is white in GridKrafter Light and #161B22 in GridKrafter Dark.
- [ ] **Step 5: Commit.** `git commit -m "fix(wisej-demo): controls follow the current theme"`

### Task 2: GridWidget edit-mode chrome

**Files:**
- Modify: `GridKrafter.WisejDemo/Controls/GridBoard/GridWidget.cs`, `gridkrafter.wisejdemo.GridBoard.js` (class `gridkrafter.wisejdemo.GridWidget`), `GridBoard.mixin.theme`

**Interfaces:**
- Consumes: `GridBoard.Changed` with kinds `moved`, `resized`, `color`, `removed`.
- Produces: `GridWidget.Title` updates live from the edit box. `Changed` gets the new kind `"renamed"` (`ItemId` set). `WidgetButtons.Edit` toggles the edit box client-side; there is no server round trip until the title changes.

- [ ] **Step 1: Card header in edit mode.** A ⋮⋮ grip icon (16 px, muted) before the colour icon. A dashed 1 px outline in `windowFrame`. Resize handles: 4×24 px blue (`highlight`) bars at the middle of the right and bottom edges, plus the corner grip. Dragging starts only from the grip, matching `gk-grip` in `Widget.razor`.
- [ ] **Step 2: Plain tile in edit mode.** A floating box at the top right with grip, palette and close buttons (`i200.png`), visible while editing. The same dashed outline and resize handles.
- [ ] **Step 3: Title edit box.** The pencil toggles a box inside the card, under the header: a "Title" label, a text field bound to the title (renames on input; an empty value falls back to the widget's original title), the hint from `en.json` `widget.titleHint`, and a small "Close" button. The server raises `Changed("renamed", itemId, …)`.
- [ ] **Step 4: Verify.** Build, 0 warnings. Open the GridBoard test form in the designer. Run the app, enter edit mode, and compare with `i200.png`. Rename "Activity" to "" and check it reverts to "Activity".
- [ ] **Step 5: Commit.** `git commit -m "feat(grid-board): MVC edit-mode chrome and title editing"`

### Task 3: GridBoard toolbar and section row

**Files:**
- Modify: `GridBoard.cs`, `gridkrafter.wisejdemo.GridBoard.js` (class `gridkrafter.wisejdemo.GridBoard`), `GridBoard.mixin.theme`

**Interfaces:**
- Consumes: `GridKrafter.Models.DashboardBoard`, `GridEngine`.
- Produces:
  - `event EventHandler ResetRequested` (raised after the user confirms "Reset layout"; the page reloads the default board).
  - `public void RenameSection(string id, string title)`
  - `public void MoveSection(string id, int offset)`
  - `public void RemoveSection(string id)`
  - `public IReadOnlyList<string> RemovedItemIds { get; }` (deleted widgets, for the catalog's restore list).
  - `event GridWidgetKindEventHandler RestoreRequested` (`e.Item.Kind` = kind, `e.Item.Name` = item id).
  - `Changed` kinds: `section-renamed`, `section-moved`, `section-removed`, `reset`.

- [ ] **Step 1: Edit-mode toolbar.** A primary "✓ Done" button (`highlight` background, `highlightText` text), then outlined "+ Add widget" and "Add section", and on the right a text button "↺ Reset layout". View mode shows only the outlined "Edit" button. Sizes follow `gk-button` in `gridkrafter.css` (30 px high, 8 px radius, 13 px text).
- [ ] **Step 2: Section row.**
  - In edit mode the section title becomes a 280 px text field; renaming updates the tab live, and an empty title falls back to the section id.
  - On the right, after Collapse all / Expand all: icon buttons for add-to-section (opens the catalog for this section), move up, move down (disabled at the ends) and delete.
  - Delete with no widgets removes the section at once. With widgets, a Wisej `MessageBox` (Yes/No) using `en.json` `section.removeConfirm` is shown first. Afterwards the neighbouring tab is selected.
- [ ] **Step 3: Reset layout.** Confirm with `MessageBox` (`en.json` `dashboard.resetConfirm`), then raise `ResetRequested`.
- [ ] **Step 4: Catalog restore.** The Add widget pop-up lists deleted widgets under a "Restore" heading (from `RemovedItemIds`), each raising `RestoreRequested`.
- [ ] **Step 5: Verify.** Build, 0 warnings. In the running app:
  - Add a section, rename it to "", and check the tab shows its id.
  - Move it up and down.
  - Delete it while empty: there is no prompt.
  - Add a widget to it, then delete it: a prompt appears.
  - Delete a widget, then restore it from the catalog: it appears exactly once.
  - Reset layout: after a reload the default board shows.
- [ ] **Step 6: Commit.** `git commit -m "feat(grid-board): MVC edit toolbar, section actions, reset and restore"`

### Task 4: ActivityList and ServerLoadList components

**Files:**
- Create: `GridKrafter.WisejDemo/Controls/ActivityList/` and `Controls/ServerLoadList/` (each: server class, client class `gridkrafter.wisejdemo.<Name>.js`, `<Name>.mixin.theme`, 16×16 bmp, project items embedded as Component Studio writes them).
- Test form: one per component, written by Component Studio.

**Interfaces:**
- Produces:
  - `ActivityList : Wisej.Web.Control`, with `IList<ActivityListItem> Items` and `ActivityListItem { string Icon; string Color; string Text; DateTimeOffset At; string TimeText }`.
  - `ServerLoadList : Wisej.Web.Control`, with `IList<ServerLoadItem> Items` and `ServerLoadItem { string Name; string Icon; int Value }`. It has thresholds `WarningFrom = 65` and `DangerFrom = 85`.
  - Both have `void SetItems(IEnumerable<…>)`, which replaces the items and redraws once.

- [ ] **Step 1: Sketch and create each in Component Studio** (one sub-agent each, run in parallel) from the matching region of `i168.png` and the MVC partials `Views/Widgets/_Activity.cshtml` and `_ServerLoad.cshtml` with `site.css`:
  - **Activity:** a 30 px tinted circle (palette colour at 14 %) around a 15 px line icon, 13 px text, a right-aligned 12 px muted time, and 1 px `windowFrame` dividers.
  - **Server load:** a 16 px muted icon, the name, a right-aligned muted percentage, a 6 px bar in green, amber or red on a `buttonFace` track, and dividers.
- [ ] **Step 2: Verify.** Build, 0 warnings. Open each test form in the designer, then check it in GridKrafter Light and Dark and Bootstrap Dark.
- [ ] **Step 3: Commit.** `git commit -m "feat(wisej-demo): ActivityList and ServerLoadList components"`

### Task 5: Stat tiles

**Files:**
- Modify: `GridKrafter.WisejDemo/Model/WidgetBodies.cs` (`CreateStat`)
- Possibly modify: `GridKrafter.WisejDemo/Themes/App.mixin.theme` (KpiPanel appearance)

**Interfaces:**
- Produces: `WidgetBodies.Create("stat", id, title)`, which returns a control drawn as a 12 px muted label, a 26 px bold value, and a line with the 12 px coloured trend (`+3.2%` in `success`, `-2.4%` in `danger`) followed by muted " vs last week".

- [ ] **Step 1: Try KpiPanel.** Use `Visual = None` and `AccentPosition = None`, and put the trend and caption on one line with no arrow, through the mixin's `kpipanel` appearance (inspect it first). If KpiPanel cannot drop its arrow or put the trend inline, add a `StatTile` to Task 4's batch with the same look and use it instead.
- [ ] **Step 2: Verify.** Build. The four tiles match `i168.png` in GridKrafter Dark and have no light background in any dark theme.
- [ ] **Step 3: Commit.** `git commit -m "feat(wisej-demo): MVC stat tiles"`

### Task 6: Dashboard page

**Files:**
- Modify: `GridKrafter.WisejDemo/Panels/HomePanel.cs`, `HomePanel.Designer.cs`, `Model/WidgetBodies.cs`

**Interfaces:**
- Consumes: Tasks 2–5 (`ResetRequested`, `RestoreRequested`, `RemovedItemIds`, `ActivityList.SetItems`, `ServerLoadList.SetItems`, the stat tile).

- [ ] **Step 1: Layout.** In the designer file, the board sits in a `Panel` docked Fill whose child `board` has `MaximumSize = 1080, 0` and is centred: an anchored `Left|Right` panel inside a `FlexLayoutPanel` with `HorizontalAlign = Center`, or the equivalent. Keep `board` as the field name.
- [ ] **Step 2: Bodies.**
  - Activity → `ActivityList`; server-load → `ServerLoadList`; refresh fills them through `SetItems` from `DemoData`. Remove the Timeline and MeterBar code.
  - Recent orders: style the DataGridView like `gk-table` with no row headers, a muted 12 px header, horizontal 1 px dividers only and 36 px rows. Order numbers are monospace, totals right-aligned, and status uses `DataGridViewChipColumn` soft chips.
- [ ] **Step 3: Wire events.**
  - `ResetRequested`: `BoardStore.Reset()`, then reload the panel. Write "reset" to `BoardEvents`.
  - `RestoreRequested`: recreate the widget from the default board item with that id and call `board.AddWidget`.
  - The new `Changed` kinds log and save like the others.
- [ ] **Step 4: Verify.**
  - Build with 0 warnings; the designer check on `HomePanel` passes.
  - Delete `App_Data/board.json`, run, and compare view mode and edit mode with `i168.png` and `i200.png` side by side in GridKrafter Dark.
  - Switch the theme to Light, Fluent Light, Fluent Dark, Material, Material Dark, Bootstrap and Bootstrap Dark, checking for light/dark mismatches.
  - Put back a `board.json` saved before this change (copy it aside before deleting it) and check it loads.
- [ ] **Step 5: Commit.** `git commit -m "feat(wisej-demo): dashboard matches the MVC demo"`
