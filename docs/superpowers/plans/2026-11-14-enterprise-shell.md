# Enterprise Shell Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the Wisej demo's main page into a "Poet Biofuels" enterprise shell. It gets a brand-green labelled sidebar with every icon showing, a top bar (breadcrumb, search, environment, alerts, user menu), a business page header with "Data as of", a full-width board, and a "Show demo notes" switch.

**Architecture:** Keep the NavigationBar extension and restyle it through a mixin that is identical in Light and Dark. Add one new native component, `AppTopBar`. Put the shell logic (alerts, search, subtitles, demo-notes state) in small services under `Services/Shell`, and have `Page1` wire them together.

**Tech Stack:** Wisej-4 4.2.0-test.7, Wisej-4-NavigationBar, Wisej-4-BootstrapIcons, .NET 10 (`net10.0-windows;net10.0`).

**Spec:** `docs/superpowers/specs/2026-11-14-enterprise-shell-design.md`

## Global Constraints

- Only `GridKrafter.WisejDemo` and these docs change. No new packages.
- Name "Poet Biofuels"; tab title "Poet Biofuels · Commodity Marketing"; monogram "P" placeholder logo (no trademark redraw).
- Sidebar colours in both themes: base `#0f1f17`, text `#e8f0ea`, selected `#1f3a2a` with a 3 px `#8bc34a` left bar. Define them once as tokens in the mixin.
- Content colours come from the GridKrafter-Light/Dark themes. No hard-coded colours in C#.
- `*.Designer.cs`: plain designer statements only. Never hand-write `.resx` binary values (responsive profiles are changed in the designer or in code).
- Icons must be real Bootstrap icon names. Check each against `Wisej.Ext.BootstrapIcons.dll`, as `activity` was missing.

## Review Focus

1. **Collapsed sidebar:** every item, the group items included, shows an icon and a tooltip, and the brand shows just the "P".
2. **Dark theme:** the sidebar stays green, and the top bar, menus and popups use dark surfaces with readable text.
3. **Demo notes off (the default):** no demo wording or Reset button shows anywhere. Turning it on brings back the intro, the status line and Reset, and the setting survives a page change.
4. **Search with no match, or with an empty string:** it says "No results" and doesn't navigate or throw.
5. **Narrow width (<600 px):** the top bar hides search behind an icon and keeps the bell and avatar visible, and the board keeps its 12 px gutters.

---

### Task 1: AppTopBar component (Component Studio, parallel)

**Files:** `Controls/AppTopBar/*` (created by Component Studio) + `AppTopBarTestForm`.

**Produces:**
```csharp
public class AppTopBar : Wisej.Web.Control {
  string[] Breadcrumb; string SearchPlaceholder; AppTopBarEnvironment Environment /* None, Dev, Uat, Prod */;
  int AlertCount; string UserName; string UserRole; string Initials; string AsOfText; bool AsOfStale;
  event AppTopBarSearchEventHandler SearchSubmitted;   // e.Text
  event EventHandler AlertsClicked; event EventHandler UserMenuClicked;
  event AppTopBarCrumbEventHandler CrumbClicked;        // e.Index
}
```
The control is 48 px high and docks to the top. Below 600 px wide, search collapses to an icon. `AsOfText`/`AsOfStale` draw "Data as of …" with a green dot, or amber when stale.

- [ ] Sketch, check the preview, create, write the control, add the icon, build, add the test form, commit `feat(wisej): AppTopBar component`.

### Task 2: Shell services (parallel)

**Files:** create `Services/Shell/DemoNotes.cs`, `PageInfo.cs`, `ShellAlerts.cs`, `ShellSearch.cs`.

**Produces:**
```csharp
public static class DemoNotes { public static bool Enabled { get; set; } /* Application.Session["gk.demoNotes"], default false */ public static event EventHandler Changed; }
public sealed record PageInfo(string Panel, string Title, string[] Breadcrumb, string Subtitle, string DemoIntro);
public static class PageInfos { public static PageInfo For(string panelName); } // all 11 panels; DemoIntro = today's Page1.Intros text
public sealed record ShellAlert(string Severity /* "danger"|"warning" */, string Text, string Panel, string PlantId);
public static class ShellAlerts { public static IReadOnlyList<ShellAlert> Current(); } // hedge months out of policy (NatGas, Grain), denaturant < 5 days, purified lots Fail/Hold
public sealed record SearchHit(string Kind /* "plant"|"board"|"customer" */, string Text, string Panel, string PlantId);
public static class ShellSearch { public static IReadOnlyList<SearchHit> Find(string text, int max = 8); } // empty/whitespace → empty list; case-insensitive contains
```
The services read the existing sample-data services (`Sample*Data`) and `PlantRegistry`, and must not change them.

- [ ] Write the four files. Build. Verify by reasoning through `Find("")` (empty), `Find("mason")` (a plant hit) and `Current()` (it returns at least one of each rule with today's sample data). Commit `feat(wisej): shell services`.

### Task 3: Brand sidebar (parallel)

**Files:**
- Modify `Page1.Designer.cs`: the icons, the System group with Events and About, `Text = "Poet Biofuels"`, the logo.
- Add `Images/poet-monogram.svg`.
- Add `Themes/PoetShell.mixin.theme` (or an entry in the project's mixins).
- Modify `Page1.cs`: the compact toggle, remembered in `Application.Session["gk.navCompact"]` and a cookie.

- [ ] Fix the Events icon (`journal-text`). Diagnose the blank Commodities group icon (compact-mode group rendering, or the icon's colour on white), fix it, and write the cause in the commit body.
- [ ] Set the sidebar expanded at 240 px on wide screens and compact under 1024 px, with a collapse toggle at the bottom.
- [ ] Apply the brand colours through the mixin, then render it in Theme Studio in both GridKrafter-Light and GridKrafter-Dark.
- [ ] Run the designer check on `Page1.Designer.cs`, build, and commit `feat(wisej): brand sidebar`.

### Task 4: Page1 integration (after 1–3)

**Files:** modify `Page1.cs` and `Page1.Designer.cs`; modify `Panels/HomePanel.cs` and `Panels/CommodityBoardPanel.cs` (padding only).

- [ ] Replace the header with: AppTopBar docked at the top (environment `Uat`, user "Tim Larson", role "Commodity Marketing", initials "TL"), then the title row (title + `PageInfo.Subtitle`), then the demo-notes row (DemoIntro + status + Reset), which is visible only when `DemoNotes.Enabled`.
- [ ] Add a user menu (ContextMenu from `UserMenuClicked`) with the name and role, Theme ▸ (today's `Themes` + Auto, checked item), "Show demo notes" (checked) and "Reset board" (only when notes are on). Remove `comboBoxTheme`.
- [ ] Alerts: on `AlertsClicked`, show a popup list of `ShellAlerts.Current()` with severity icons. A click navigates to `Panel` and sets the plant filter when a `PlantId` is given. `AlertCount` is the count.
- [ ] Search: on `SearchSubmitted`, show a popup of `ShellSearch.Find`, or "No results". A click navigates the same way.
- [ ] Update `AsOfText` on every panel show and every board refresh/save. Set `AsOfStale` once it is more than 5 minutes old (timer).
- [ ] Board width: drop the 1080 px centred column. Use 24 px gutters (12 px under 600 px) in HomePanel and CommodityBoardPanel.
- [ ] Set the page `Text` to "Poet Biofuels · Commodity Marketing".
- [ ] Run the designer check, build both targets, and commit `feat(wisej): enterprise shell page`.

### Task 5: Final check

- [ ] Walk the Review Focus list (by code reading where no browser is available, and say so). Run the theme review of the mixin in Light and Dark. Fix what's found and commit.
