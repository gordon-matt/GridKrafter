# Enterprise shell for the Wisej demo main page — design

Date: 2026-11-14 · Scope: `GridKrafter.WisejDemo` (Page1, its theme and mixins, one new component)

## Intent

The main page should look like a real enterprise product for **Poet Biofuels**, an ethanol marketer, and still work as a GridKrafter demo. Today:

- The left menu is a 70 px icon strip. The Events icon is blank (`activity` is not a Bootstrap icon) and the Commodities group icon does not show.
- The menu stays white in the dark theme.
- The board sits off-centre, with an empty band on the left.
- The header text explains the demo instead of the business.

## Decisions (developer)

- **Audience: both.** The business look is the default. A **"Show demo notes"** switch brings back the demo explanations: the intro lines, the board status and the Reset button.
- **Navigation:** a labelled sidebar (~240 px) that collapses to an icon strip with tooltips. The choice is remembered for the session/browser. Commodities is shown expanded.
- **Header must show:** breadcrumb plus page title with a one-line subtitle; a "Data as of" time with a live/stale dot; an environment badge (PROD/UAT/DEV); global search (plants, customers, contracts); an alerts bell; and a signed-in user menu with avatar, role and the theme choice moved into it.
- **Name:** "Poet Biofuels". Use a simple monogram "P" placeholder; the developer can drop in the real logo file. Do not redraw a trademark.
- **Look: direction A, brand sidebar.** The sidebar is always deep green (`#0f1f17` base, selected `#1f3a2a` with a 3 px `#8bc34a` left bar) in both Light and Dark. The content area follows the chosen theme.
- **Approach: keep the existing NavigationBar extension**, restyled through the theme. No new sidebar component.

## Approaches considered

1. **Restyle the existing NavigationBar (picked).** Cheapest, and it keeps the extension's keyboard, responsive and phone behaviour. Risk: it may not draw the "COMMODITIES / SYSTEM" caption rows exactly as in the mockup; a group item standing in as the caption is acceptable.
2. **A new BrandSidebar component.** Pixel-exact, but it costs more and duplicates what the extension already does. Rejected.
3. **Accordion/TreeView as navigation.** Looks less like an app shell and loses compact mode. Rejected.

## Design

- **Sidebar**
  - Fix the icons: Events uses `activity`, which doesn't exist, so use e.g. `journal-text` or `clock-history`. Find out why the Commodities group icon is blank and fix it.
  - Add a "System" group holding Events and About. Commodities is expanded at start.
  - Brand: logo = monogram, Text = "Poet Biofuels".
  - Expanded by default on wide screens and compact under ~1024 px (adjust the existing responsive profiles); there is also a collapse toggle at the bottom.
  - The sidebar look goes in a mixin (`Themes/PoetShell.mixin.theme`, or the GridKrafter themes' own navigation-bar appearance) so it is identical in Light and Dark. Colours become theme tokens, not hard-coded per appearance.
- **Top bar: a new component `AppTopBar`** (Component Studio, a native control). It shows a breadcrumb (`Home › Commodities › Ethanol`), a search box with a placeholder, the environment badge, the alerts bell with a count, and the user avatar.
  - Properties: breadcrumb (string[]), searchPlaceholder, environment (enum Prod/Uat/Dev/None), alertCount, userName, userRole, initials.
  - Events: searchSubmitted {text}, alertsClicked, userMenuClicked, crumbClicked {index}.
  - It uses the app's theme colours. 48 px high.
- **User menu:** a ContextMenu/popup from the avatar with the name and role, Theme ▸ (the existing theme choices plus Auto), the "Show demo notes" toggle and "Reset board" (visible only when demo notes are on).
- **Alerts:** a popup list built from the boards' sample data: hedge months out of policy, plants under 5 days of denaturant, failed or held purified lots. The count goes on the bell. Clicking an alert opens that board.
- **Search:** a demo implementation that matches plant names (PlantRegistry), board names and customer names, and opens the matching board with the plant filter set when it's a plant.
- **Page header:** the title, a one-line business subtitle per page (replacing the demo `Intros`, which become the demo notes), and "Data as of h:mm tt CT · live" on the right. The time updates on each board refresh; the dot turns amber if older than 5 minutes.
- **Board area:** it fills the width with consistent 24 px gutters. Remove the off-centre band: check how HomePanel/CommodityBoardPanel compute the padding against the new sidebar width.
- **Theme selector:** removed from the header and moved into the user menu.
- **Browser tab title:** "Poet Biofuels · Commodity Marketing".

## Non-goals / anti-goals

- No real authentication, real search backend or real alerting.
- No change to the commodity boards' contents.
- No new look outside the brand sidebar: content keeps the GridKrafter-Light/Dark themes.
- Don't hard-code colours in C#.

## Left to the builder

- Exact icons and subtitle copy per page, and the alert rules' thresholds beyond those named.
- Whether the "Data as of" lives in AppTopBar or the page header.
- How the NavigationBar caption rows are approximated.
