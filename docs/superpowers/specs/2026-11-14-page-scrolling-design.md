# Page-level scrolling for boards — design (small change)

Date: 2026-11-14 · Scope: `GridKrafter.WisejDemo` Page1, HomePanel, CommodityBoardPanel

**Why:** each board tab scrolls inside itself (a second scrollbar under the window's), so the developer can't see the whole tab. They want the tab to be as tall as its widgets, with the page scrolling instead.

**Decided (developer):**
- The page header (title, subtitle, demo notes), the filter bar and the board scroll together under the fixed top bar. The sidebar and top bar stay put.
- The board's height fits every widget of the open tab, so there is no inner scrollbar. Full-screen on a widget still fills the visible area.
- This applies to the Dashboard and all eight commodity boards. The GridBoard component itself is not changed.

**Approaches:**
1. **Size the board from the panels (picked).** After each layout, set the board's height to its toolbar plus the lowest visible widget, and let the page area scroll. Needs no component change.
2. **Add a fit-to-content mode inside GridBoard.** Cleaner, but it changes a shared component through its sketch, which is more work.
3. **Scale row height to fit the window.** Rejected: the developer chose growing and page scrolling.

**How:**
- In Page1, `panelTop` moves into the scrolling area, and `ShowPanel` replaces only the board panel.
- The board panels dock top, with their height = padding + filter bar + board.
- The board height = 128 (toolbar) + the lowest visible widget's bottom + 32, recalculated on the board's Layout/Changed/LayoutChanged events, on tab switch and on resize.
- In full-screen, the board height = the visible page height.

**Builder's calls:** how the scroll container is built (a Panel with AutoScroll, or panelMain itself) and the exact padding.
