# GridKrafter.WisejDemo: dashboard parity with the MVC demo

## Intent

`GridKrafter.WisejDemo` (Wisej.NET 4.2, .NET 10) ports `GridKrafter.Demo.Mvc` to Wisej. Its dashboard works, but it neither looks nor behaves like the MVC page, especially in edit mode. The developer wants the **dashboard area** to match the MVC page identically, in look and in function, while every part stays a native Wisej control styled by the Wisej theme.

Reference screenshots (dark theme, MVC demo):
- View mode: `C:\Users\larsont\AppData\Local\Wisej\Studio\chats\49d2f8a7591debff2507ee3e\images\i168.png`
- Edit mode: `...\images\i200.png`
- The Wisej app today, in edit mode: `...\images\i201.png`

The source of truth for behaviour and styling is the original GridKrafter code: `GridKrafter.Components` (`Dashboard.razor(.cs)`, `Dashboard.Actions.cs`, `Dashboard.Interaction.cs`, `Widget.razor`, `WidgetToolbar.razor`, `wwwroot/gridkrafter.css`, `wwwroot/gridkrafter.js`), plus the MVC demo's views and `wwwroot/site.css`. Its UI text is in `GridKrafter.Core/Locales/*.json`.

## Goals

1. **View mode** looks like `i168.png`:
   - **Stat tiles:** panel background, a 3 px left edge in the widget colour, 8 px radius, a small muted label, a large bold value, and one line under it: the trend coloured green or red ("+3.2%"), then muted "vs last week". No triangle arrows, no inner card border, no light background in a dark theme.
   - **Cards:** a 3 px top edge in the widget colour, a small icon and title, and compact header buttons on the right (refresh, colour, edit, fullscreen, collapse, close). The footer reads "Last updated h:mm:ss AM", bottom right.
   - **Activity body:** rows with an icon in a tinted circle (the widget colour of the entry), the text, and a right-aligned muted relative time ("9 minutes ago"), with 1 px dividers between rows.
   - **Server load body:** rows with a muted icon on the left, the name, its percentage right-aligned and muted, and a thin bar underneath (green, amber at ≥ 65, red at ≥ 85), with dividers between rows.
   - **Recent orders:** a grid styled like the MVC `gk-table`, with status badges.
   - **Tabs:** underlined, each with a count badge. "Collapse all" and "Expand all" sit on the right of the section row.
2. **Edit mode** looks and behaves like `i200.png` and the original code:
   - **Toolbar:** a primary "✓ Done" button that leaves edit mode, "+ Add widget" (opens the catalog), "Add section", and on the right "↺ Reset layout". Reset asks for confirmation, then restores the page's own board. In view mode the toolbar shows only the outlined "Edit" button.
   - **Section row:** the section title becomes a text box that renames the section. Next to "Collapse all" and "Expand all" are icon buttons: add a widget to this section, move the section up or down, and delete the section. Deleting asks for confirmation when the section has widgets, as the original does.
   - **Every widget:**
     - a ⋮⋮ drag handle (in the card header; on stat tiles, floating at the top right with the colour and close buttons),
     - a dashed outline,
     - blue resize handles at the middle of the right edge, the bottom edge, and the bottom-right corner.
     - Dragging and resizing snap to the grid, with the GridKrafter.Core engine resolving collisions as now.
   - **Pencil (edit) button:** opens an edit box inside the card with a "Title" field (renames the widget live) and a "Close" button, like `Widget.razor`'s `gk-widget-editbox`.
   - **Catalog:** lists the widget kinds and lets you bring back deleted widgets, as the original's catalog does.
   - **Saving and logging:** every change is saved to `App_Data/board.json` and written to the Events page, as now.
3. **Theming:** it matches exactly in GridKrafter Light, GridKrafter Dark and Auto. In the shipped Fluent, Material and Bootstrap themes (light and dark), the layout and shapes are the same but the colours are that theme's own: a dark theme is fully dark, a light theme fully light. Every colour comes from theme colour names; no colour is hard-coded on a control.
4. **Theme colour bug fixes:**
   - The KPI tiles and meter tracks render light in dark themes.
   - The NavigationBar stays the extension's default slate blue instead of GridKrafter's colours.
   - Find the cause (suspected: framework or extension mixin colours overriding the theme's, or controls not refreshing when the theme is switched at runtime) and fix it so every control follows the current theme and the theme switcher.

## Non-goals

- The shell stays: the Wisej NavigationBar sidebar, the top bar (title, description, save status, theme switcher, "Reset the board"), and the separate Events and About pages. The MVC page's top bar and its inline Events log are **not** reproduced.
- No change to GridKrafter.Core, GridKrafter.Components or the MVC and Blazor demos.

## Anti-goals

- No HTML strings, iframes or page-level script standing in for a widget body. Every part is a Wisej control or a native Wisej component of the project.
- No panels and labels imitating a control the catalog offers, and no Canvas drawing from page code.
- Nothing that only works in GridKrafter Dark: a white tile in a dark theme, or dark text on a dark background, counts as a failure.

## Decided

- **Board:** the existing native component `GridBoard` / `GridWidget` (`GridKrafter.WisejDemo/Controls/GridBoard`) is extended. It is never replaced by a new component. Its public API stays; members may be added.
- **Two new native components** (developer's choice), in the project's `Controls` folder, themed by mixins that use theme colour names:
  - `ActivityList`: rows of icon, text and relative time, bindable to a list of entries with icon, colour, text and time.
  - `ServerLoadList`: rows of icon, name, percentage and threshold-coloured bar, bindable to a list of meters.
  - Both replace the Timeline and MeterBar bodies.
- **Stat tiles:** they may keep `KpiPanel` if it can be made to look like the reference (no arrow, inline trend + caption, no border, theme-correct colours). Otherwise the stat text is drawn by `GridWidget`'s plain chrome or by a small component. **Builder's call.**
- Data stays as in `Model/DemoData.cs` and `Model/DemoBoard.cs`. Saving stays in `Services/BoardStore.cs`, logging in `Services/BoardEvents.cs`.

## Calls made by the designer (check these)

- **Width (assumed; the developer did not answer):** the board sits in a centred column at most about 1080 px wide inside `HomePanel`, as on the MVC page, so its column count and widget widths match the screenshots. Inside the dashboard, text sizes match the MVC page (13 px titles, tabs and links; 12 px footers and muted lines); the theme's base font and the shell stay as they are.
- **Palette in shipped themes:** the widget colours (blue, green, orange, violet, amber, sky, red, pink), the trend colours, the meter thresholds, the activity tints and the status badges come from colours the components' mixins name. Each maps to the current theme's own colour where one exists (success, warning, danger, highlight, info), with the GridKrafter hue as the fallback for hues the theme lacks (violet, pink, sky). That counts as "theme colours only".
- **KpiPanel and MeterBar:** if they leave the dashboard, the cause of the light/dark mismatch is still fixed generically (controls refreshing when the theme switches at runtime; extension mixin colours such as the NavigationBar's following the theme). Those two controls are not separately verified in every theme.

## Left to the builder

- How edit-mode features are split between `GridBoard`'s server class, client script and mixin.
- The exact confirmation UI (a Wisej `MessageBox` is acceptable) and its wording, taken from `GridKrafter.Core/Locales/en.json`.
- The narrow-width behaviour ("more" menu, fewer columns) and keyboard nudging, matching the original where practical.
- Any further fixes needed in the GridKrafter-Light and GridKrafter-Dark themes, made through Theme Studio and kept as one family.

## Done when

- The project builds with no errors and no warnings, and every designer file passes the designer check.
- Side by side, the Wisej dashboard in GridKrafter Dark matches `i168.png` and `i200.png`, apart from the shell. It looks right in GridKrafter Light, and shows no light/dark mismatches in Fluent, Material and Bootstrap.
- Every edit-mode action of the MVC page works and is saved and logged: Done, Add widget, Add section, Reset layout, rename section, add to section, move section up/down, delete section, collapse/expand all, drag, resize, colour, rename widget, fullscreen, collapse, delete, restore.
