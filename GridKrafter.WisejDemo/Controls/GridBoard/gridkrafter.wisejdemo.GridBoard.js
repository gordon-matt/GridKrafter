/**
 * gridkrafter.wisejdemo.GridBoard
 *
 * The client half of GridKrafter.WisejDemo.Controls.GridBoard: a scrollable Wisej container with a
 * toolbar docked above the grid area, drawn as the GridKrafter demos draw it: the Edit toggle (and Add
 * widget with its catalog in edit mode), the underlined section tabs with their widget counts (and +
 * in edit mode), and the Collapse all / Expand all links. The
 * server places the GridWidget children; the browser only shows where a dragged or resized widget
 * would snap to and reports the cell it was dropped on. The server answers with the new layout.
 */
qx.Class.define("gridkrafter.wisejdemo.GridBoard", {

	extend: wisej.web.ScrollableControl,

	/**
	 * Creates the toolbar above the grid area.
	 */
	construct: function () {

		this.base(arguments);

		this._createChildControl("toolbar");
		this.getChildControl("pane").addListener("resize", this.__onAreaResize, this);
		this.getChildControl("pane").addListener("changeScrollbarYVisibility", this.__onAreaResize, this);

		qx.theme.manager.Color.getInstance().addListener("changeTheme", this._applyGrid, this);
		qx.theme.manager.Color.getInstance().addListener("changeTheme", this._applySections, this);

		this._applySections();
		this._applyEditMode(this.getEditMode());
		this._applyLinks();
	},

	events: {

		/**
		 * The user dropped a dragged widget: { id, x, y } in cells of the current layout.
		 */
		widgetMove: "qx.event.type.Data",

		/**
		 * The user resized a widget with its grip: { id, w, h } in cells of the current layout.
		 */
		widgetResize: "qx.event.type.Data",

		/**
		 * The user clicked a section tab; the data is the section's id.
		 */
		sectionSelect: "qx.event.type.Data",

		/**
		 * The user clicked the + tab.
		 */
		sectionAdd: "qx.event.type.Event",

		/**
		 * The user clicked Edit layout.
		 */
		editToggle: "qx.event.type.Event",

		/**
		 * The user picked a kind in the Add widget catalog; the data is the kind.
		 */
		widgetAdd: "qx.event.type.Data",

		/**
		 * The width of the grid area changed; the data is the width in pixels.
		 */
		areaResize: "qx.event.type.Data",

		/**
		 * The user clicked Collapse all.
		 */
		collapseAll: "qx.event.type.Event",

		/**
		 * The user clicked Expand all.
		 */
		expandAll: "qx.event.type.Event",

		/**
		 * The user clicked Reset layout.
		 */
		resetLayout: "qx.event.type.Event",

		/**
		 * The user typed a new title for the selected section: { id, title }.
		 */
		sectionRename: "qx.event.type.Data",

		/**
		 * A section action of edit mode on the selected section: { id, action }; action is up, down or delete.
		 */
		sectionAction: "qx.event.type.Data",

		/**
		 * The user picked a deleted widget in the catalog to bring back; the data is its id.
		 */
		widgetRestore: "qx.event.type.Data"
	},

	properties: {

		/**
		 * The theme appearance of the board.
		 */
		appearance: { init: "grid-board", refine: true },

		/**
		 * The sections, shown as tabs: [{ name, id, title, icon, count }].
		 */
		sections: { init: null, nullable: true, check: "Array", apply: "_applySections" },

		/**
		 * The kinds Add widget offers: [{ name, text, kind, description, icon, color, w, h, ... }].
		 */
		catalog: { init: null, nullable: true, check: "Array", apply: "_applyCatalog" },

		/**
		 * The deleted widgets the catalog can bring back: [{ id, title, icon, color }].
		 */
		removed: { init: null, nullable: true, check: "Array", apply: "_applyCatalog" },

		/**
		 * The id of the section whose widgets show.
		 */
		selectedSection: { init: "", check: "String", apply: "_applySections" },

		/**
		 * Whether widgets can be dragged and resized; shows the grid backdrop.
		 */
		editMode: { init: false, check: "Boolean", apply: "_applyEditMode" },

		/**
		 * Whether the toolbar shows.
		 */
		showToolbar: { init: true, check: "Boolean", apply: "_applyShowToolbar" },

		/**
		 * The height of the toolbar, in pixels: the server lays the grid out below it.
		 */
		toolbarHeight: { init: 128, check: "Integer", apply: "_applyShowToolbar" },

		/**
		 * Whether Collapse all has something to collapse.
		 */
		canCollapse: { init: true, check: "Boolean", apply: "_applyLinks" },

		/**
		 * Whether Expand all has something to expand.
		 */
		canExpand: { init: false, check: "Boolean", apply: "_applyLinks" },

		/**
		 * The number of columns of the current layout.
		 */
		columns: { init: 12, check: "Integer", apply: "_applyGrid" },

		/**
		 * The height of a row, in pixels.
		 */
		rowHeight: { init: 62, check: "Integer", apply: "_applyGrid" },

		/**
		 * The space between cells, in pixels.
		 */
		gap: { init: 12, check: "Integer", apply: "_applyGrid" },

		/**
		 * The width of a column as the server laid the widgets out, in pixels.
		 */
		cellWidth: { init: 0, check: "Number", apply: "_applyGrid" },

		/**
		 * The theme's accent: the drop placeholder's border.
		 */
		themeAccentColor: { init: "#6d4dff", check: "Color", themeable: true, apply: "_applyGrid" },

		/**
		 * The theme's fill of the drop placeholder.
		 */
		themePlaceholderColor: { init: "#e9e4ff", check: "Color", themeable: true, apply: "_applyGrid" },

		/**
		 * The theme's colour of the dotted grid backdrop in edit mode.
		 */
		themeGridColor: { init: "#d6ccff", check: "Color", themeable: true, apply: "_applyGrid" },

		/**
		 * The theme's corner radius of the drop placeholder, in pixels.
		 */
		themePlaceholderRadius: { init: 8, check: "Integer", themeable: true },

		/**
		 * The theme's fill of a tab's count badge.
		 */
		themeBadgeColor: { init: "#262c36", check: "Color", themeable: true, apply: "_applySections" },

		/**
		 * The theme's text of a tab's count badge.
		 */
		themeBadgeTextColor: { init: "#8d96a0", check: "Color", themeable: true, apply: "_applySections" }
	},

	statics: {

		/**
		 * The theme colours a palette colour falls back to, in order, in a theme that has no colour of
		 * that name (the shipped Fluent, Material and Bootstrap themes): the theme's own semantic colours.
		 */
		SEMANTIC: {
			green: ["success"],
			amber: ["warning"],
			red: ["danger"],
			sky: ["info"],
			blue: ["primary", "highlight"],
			violet: ["highlight"]
		},

		/**
		 * The built-in icons, as the inside of a 24 x 24 SVG drawn with the current text colour.
		 */
		ICONS: {
			home: '<path d="M3 11l9-7 9 7v9H3z"/>',
			cart: '<circle cx="9" cy="20" r="1.5"/><circle cx="18" cy="20" r="1.5"/><path d="M2 3h3l3 12h11l2-8H6"/>',
			activity: '<path d="M3 12h4l3-8 4 16 3-8h4"/>',
			server: '<rect x="3" y="4" width="18" height="7" rx="1.5"/><rect x="3" y="13" width="18" height="7" rx="1.5"/><path d="M7 7.5h.01M7 16.5h.01"/>',
			chart: '<path d="M4 20V10M10 20V4M16 20v-7M22 20H2"/>',
			table: '<rect x="3" y="4" width="18" height="16" rx="2"/><path d="M3 10h18M9 4v16"/>',
			users: '<circle cx="9" cy="8" r="4"/><path d="M2 21c0-4 3-6 7-6s7 2 7 6M17 4a4 4 0 0 1 0 8M22 21c0-3-1.5-5-4-5.6"/>',
			bell: '<path d="M6 16V11a6 6 0 0 1 12 0v5l2 2H4zM10 21h4"/>',
			clock: '<circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/>',
			star: '<path d="M12 3l2.8 5.8 6.2.9-4.5 4.4 1 6.2L12 17.4 6.5 20.3l1-6.2L3 9.7l6.2-.9z"/>',
			pie: '<path d="M12 3v9h9A9 9 0 1 1 12 3z"/><path d="M15 3.5A9 9 0 0 1 20.5 9H15z"/>',
			globe: '<circle cx="12" cy="12" r="9"/><path d="M3 12h18M12 3a14 14 0 0 1 0 18M12 3a14 14 0 0 0 0 18"/>',
			refresh: '<path d="M20 11a8 8 0 1 0-2 6M20 5v6h-6"/>',
			fullscreen: '<path d="M4 9V4h5M20 9V4h-5M4 15v5h5M20 15v5h-5"/>',
			exitFullscreen: '<path d="M9 4v5H4M15 4v5h5M9 20v-5H4M15 20v-5h5"/>',
			collapse: '<path d="M6 15l6-6 6 6"/>',
			expand: '<path d="M6 9l6 6 6-6"/>',
			"delete": '<path d="M6 6l12 12M18 6L6 18"/>',
			color: '<path d="M12 3a9 9 0 1 0 0 18c1.1 0 2-.9 2-2 0-.5-.2-1-.5-1.3-.3-.4-.5-.8-.5-1.3 0-1.1.9-2 2-2h2.4A4.6 4.6 0 0 0 21 9.8C21 6 17 3 12 3z"/><circle cx="7.5" cy="11" r="1"/><circle cx="10.5" cy="7.5" r="1"/><circle cx="15" cy="7.5" r="1"/>',
			collapseAll: '<path d="M7 11l5-5 5 5M7 18l5-5 5 5"/>',
			expandAll: '<path d="M7 6l5 5 5-5M7 13l5 5 5-5"/>',
			edit: '<path d="M4 20h4L19 9l-4-4L4 16z"/>',
			plus: '<path d="M12 5v14M5 12h14"/>',
			grip: '<circle cx="9" cy="6" r="1.2"/><circle cx="15" cy="6" r="1.2"/><circle cx="9" cy="12" r="1.2"/><circle cx="15" cy="12" r="1.2"/><circle cx="9" cy="18" r="1.2"/><circle cx="15" cy="18" r="1.2"/>',
			check: '<path d="M5 12l5 5 9-10"/>',
			layers: '<path d="M12 3l9 5-9 5-9-5z"/><path d="M3 13l9 5 9-5"/>',
			reset: '<path d="M4 11a8 8 0 1 1 2 6M4 5v6h6"/>',
			up: '<path d="M12 19V5M6 11l6-6 6 6"/>',
			down: '<path d="M12 5v14M6 13l6 6 6-6"/>',
			trash: '<path d="M4 7h16M10 11v6M14 11v6M6 7l1 13h10l1-13M9 7V4h6v3"/>',
			alert: '<circle cx="12" cy="12" r="9"/><path d="M12 8v5M12 16h.01"/>',
			message: '<path d="M21 12a8 8 0 0 1-11.6 7.1L4 20l1-4.6A8 8 0 1 1 21 12z"/>',
			zap: '<path d="M13 2L4 14h7l-1 8 9-12h-7z"/>',
			cpu: '<rect x="6" y="6" width="12" height="12" rx="1.5"/><rect x="9.5" y="9.5" width="5" height="5"/><path d="M9 2v4M15 2v4M9 18v4M15 18v4M2 9h4M2 15h4M18 9h4M18 15h4"/>',
			box: '<path d="M21 8l-9-5-9 5 9 5z"/><path d="M3 8v8l9 5 9-5V8M12 13v8"/>'
		},

		/**
		 * Returns the markup of an icon: a built-in name as an SVG in the current colour, an address as a picture.
		 *
		 * @param {String} name the icon's name or address.
		 * @param {Integer} size the size in pixels.
		 * @return {String} the markup; empty when there is no icon.
		 */
		iconHtml: function (name, size) {

			size = size || 15;
			if (!name)
				return "";

			var path = gridkrafter.wisejdemo.GridBoard.ICONS[name];
			if (!path) {
				if (/[\/\.:]/.test(name))
					return '<img src="' + qx.bom.String.escape(name) + '" style="width:' + size + 'px;height:' + size + 'px;vertical-align:middle" alt="">';

				path = gridkrafter.wisejdemo.GridBoard.ICONS.chart;
			}

			return '<svg viewBox="0 0 24 24" width="' + size + '" height="' + size + '" style="display:block;fill:none;stroke:currentColor;stroke-width:2;stroke-linecap:round;stroke-linejoin:round">' + path + '</svg>';
		},

		/**
		 * Returns the markup of an icon followed by a text, side by side.
		 *
		 * @param {String} icon the icon's name or address.
		 * @param {String} text the literal text; it is escaped.
		 * @return {String} the markup.
		 */
		iconTextHtml: function (icon, text, size, after) {

			var svg = gridkrafter.wisejdemo.GridBoard.iconHtml(icon, size || 15);
			return '<span style="display:inline-flex;align-items:center;gap:' + (after ? 8 : 6) + 'px;white-space:nowrap">' + svg +
				'<span>' + qx.bom.String.escape(text || "") + '</span>' + (after || "") + '</span>';
		},

		/**
		 * Resolves a widget colour (a palette name of the theme mixin, a theme colour or a CSS colour) to CSS.
		 *
		 * @param {String} color the colour.
		 * @return {String} a CSS colour.
		 */
		resolveColor: function (color) {

			// the theme's own palette first (GridKrafter-Light/Dark define blue, green...), then the mixin's
			var manager = qx.theme.manager.Color.getInstance();
			color = color || "blue";
			if (manager.isDynamic(color))
				return manager.resolve(color);
			var semantic = gridkrafter.wisejdemo.GridBoard.SEMANTIC[color];
			for (var i = 0; semantic && i < semantic.length; i++) {
				if (manager.isDynamic(semantic[i]))
					return manager.resolve(semantic[i]);
			}
			if (manager.isDynamic("grid-board-" + color + "-accent"))
				return manager.resolve("grid-board-" + color + "-accent");
			return color;
		},

		/**
		 * Creates a colour chip: a tinted square (the colour at the theme's chip-tint opacity) with an icon.
		 *
		 * @param {String} appearance the chip's appearance; its tint is appearance + "-tint".
		 * @return {qx.ui.container.Composite} the chip.
		 */
		createChip: function (appearance) {

			var chip = new qx.ui.container.Composite(new qx.ui.layout.Canvas()).set({ appearance: appearance, anonymous: true });
			var tint = new qx.ui.core.Widget().set({ appearance: appearance + "-tint", anonymous: true });
			var icon = new qx.ui.basic.Label().set({ rich: true, anonymous: true, appearance: "label", padding: 5 });
			chip.add(tint, { edge: 0 });
			chip.add(icon, { left: 0, top: 0, right: 0, bottom: 0 });
			chip.setUserData("tint", tint);
			chip.setUserData("icon", icon);
			return chip;
		},

		/**
		 * Shows an icon in a chip, in a colour.
		 *
		 * @param {qx.ui.container.Composite} chip a chip made by createChip.
		 * @param {String} icon the icon's name or address.
		 * @param {String} color the colour, as resolveColor answers it.
		 */
		setChip: function (chip, icon, color) {

			chip.getUserData("tint").setBackgroundColor(color);
			chip.getUserData("icon").set({
				value: gridkrafter.wisejdemo.GridBoard.iconHtml(icon, 15),
				textColor: color
			});
		}
	},

	members: {

		/**
		 * The widget the user selected last.
		 */
		__selected: null,

		/**
		 * The drag in progress: its state and native listeners.
		 */
		__drag: null,

		/**
		 * The timer that reports the width of the grid area once it settles.
		 */
		__areaTimer: null,

		/**
		 * The last width reported to the server.
		 */
		__areaWidth: 0,

		/**
		 * The timer that sends the section's new title once the user pauses typing.
		 */
		__renameTimer: null,

		/**
		 * The section a pending rename is for.
		 */
		__renameId: null,

		/**
		 * The section's action buttons, shown in edit mode.
		 */
		__sectionActions: null,

		/**
		 * Creates the toolbar, the tabs, the buttons and the catalog pop-up.
		 */
		_createChildControlImpl: function (id, hash) {

			var control;

			switch (id) {

				case "toolbar":
					// three rows: Edit (and Add widget), the tabs, the Collapse all / Expand all links
					// minWidth 0 on the toolbar and its rows: they never hold the page wider than the screen
					control = new qx.ui.container.Composite(new qx.ui.layout.VBox(8)).set({ minWidth: 0 });
					control.setHeight(this.getToolbarHeight());
					control.add(this.getChildControl("actions"));
					control.add(this.getChildControl("tabs"));
					control.add(this.getChildControl("links"));
					this._addAt(control, 0, { edge: "north" });
					break;

				case "actions":
					// view mode: Edit; edit mode: Done, Add widget, Add section ... Reset layout (as the original)
					control = new qx.ui.container.Composite(new qx.ui.layout.HBox(8).set({ alignY: "middle" })).set({ minWidth: 0 });
					control.add(this.getChildControl("edit-button"));
					control.add(this.getChildControl("add-button"));
					control.add(this.getChildControl("section-button"));
					control.add(new qx.ui.core.Spacer(), { flex: 1 });
					control.add(this.getChildControl("reset-button"));
					break;

				case "section-button":
					control = new qx.ui.form.Button().set({ rich: true });
					control.setLabel(gridkrafter.wisejdemo.GridBoard.iconTextHtml("layers", "Add section", 14));
					control.addListener("execute", function () { this.fireEvent("sectionAdd"); }, this);
					break;

				case "reset-button":
					control = new qx.ui.form.Button().set({ rich: true });
					control.setLabel(gridkrafter.wisejdemo.GridBoard.iconTextHtml("reset", "Reset layout", 14));
					control.addListener("execute", function () { this.fireEvent("resetLayout"); }, this);
					break;

				case "section-title":
					control = new qx.ui.form.TextField().set({ width: 280, minWidth: 90, allowShrinkX: true, placeholder: "Section title" });
					control.getContentElement().setAttribute("aria-label", "Section title");
					control.addListener("input", this.__onSectionTitleInput, this);
					break;

				case "section-add":
				case "section-up":
				case "section-down":
				case "section-delete":
					var action = id.substring(8);
					var names = { add: "Add widget", up: "Move up", down: "Move down", "delete": "Delete section" };
					var icons = { add: "plus", up: "up", down: "down", "delete": "trash" };
					control = new qx.ui.form.Button().set({ rich: true, appearance: "grid-board/icon-button", toolTipText: names[action] });
					control.setLabel(gridkrafter.wisejdemo.GridBoard.iconHtml(icons[action], 16));
					control.getContentElement().setAttribute("aria-label", names[action]);
					control.setUserData("action", action);
					control.addListener("execute", this.__onSectionAction, this);
					break;

				case "tabs":
					// tabs that don't fit scroll sideways, with arrows, instead of being cut off
					control = new qx.ui.container.SlideBar("horizontal").set({ minWidth: 0, appearance: "grid-board/tabs" });
					control.setLayout(new qx.ui.layout.HBox(2).set({ alignY: "bottom" }));
					control.getContentElement().setAttribute("role", "tablist");
					break;

				case "links":
					// the section's title (a field in edit mode) on the left; Collapse all, Expand all and,
					// in edit mode, the section's actions on the right
					control = new qx.ui.container.Composite(new qx.ui.layout.HBox(14).set({ alignY: "middle" })).set({ minWidth: 0 });
					control.add(this.getChildControl("section-title"), { flex: 1 });
					this.getChildControl("section-title").setMaxWidth(280);
					control.add(new qx.ui.core.Spacer(), { flex: 1 });
					control.add(this.getChildControl("collapse-link"));
					control.add(this.getChildControl("expand-link"));
					var actions = new qx.ui.container.Composite(new qx.ui.layout.HBox(2).set({ alignY: "middle" }));
					actions.add(this.getChildControl("section-add"));
					actions.add(this.getChildControl("section-up"));
					actions.add(this.getChildControl("section-down"));
					actions.add(this.getChildControl("section-delete"));
					control.add(actions);
					this.__sectionActions = actions;
					break;

				case "collapse-link":
				case "expand-link":
					control = this.__createLink(id === "collapse-link");
					break;

				case "edit-button":
					control = new qx.ui.form.Button();
					control.setRich(true);
					control.setLabel(gridkrafter.wisejdemo.GridBoard.iconTextHtml("edit", "Edit", 14));
					control.getContentElement().setAttribute("aria-pressed", "false");
					control.addListener("execute", function () {
						this.__flushRename();
						this.fireEvent("editToggle");
					}, this);
					break;

				case "add-button":
					control = new qx.ui.form.Button();
					control.setRich(true);
					control.setLabel(gridkrafter.wisejdemo.GridBoard.iconTextHtml("plus", "Add widget", 14));
					control.getContentElement().setAttribute("aria-haspopup", "true");
					control.addListener("execute", this.__onAddButton, this);
					break;

				case "catalog":
					control = new qx.ui.popup.Popup(new qx.ui.layout.VBox(2));
					control.setAppearance("grid-board/catalog");
					control.setPosition("bottom-right");
					break;
			}

			return control || this.base(arguments, id, hash);
		},

		/**
		 * Creates the Collapse all or the Expand all link.
		 *
		 * @param {Boolean} collapse true for Collapse all.
		 * @return {qx.ui.form.Button} the link.
		 */
		__createLink: function (collapse) {

			var link = new qx.ui.form.Button().set({ rich: true });
			link.setLabel(gridkrafter.wisejdemo.GridBoard.iconTextHtml(collapse ? "collapseAll" : "expandAll", collapse ? "Collapse all" : "Expand all", 13));
			link.addListener("execute", function () {
				this.fireEvent(collapse ? "collapseAll" : "expandAll");
			}, this);
			return link;
		},

		/**
		 * Draws the section tabs and the + tab.
		 */
		_applySections: function () {

			var tabs = this.getChildControl("tabs");
			var old = tabs.removeAll();
			for (var i = 0; i < old.length; i++)
				old[i].dispose();

			var sections = this.getSections() || [];
			var selected = this.getSelectedSection();
			var manager = qx.theme.manager.Color.getInstance();
			var badgeStyle = "padding:0 6px;border-radius:999px;font-size:11.5px;line-height:18px;background:" +
				manager.resolve(this.getThemeBadgeColor()) + ";color:" + manager.resolve(this.getThemeBadgeTextColor());
			for (var i = 0; i < sections.length; i++) {

				var section = sections[i];
				var tab = new qx.ui.form.Button().set({ appearance: "grid-board/tab", rich: true, focusable: true });
				var badge = '<span style="' + badgeStyle + '">' + (parseInt(section.count, 10) || 0) + '</span>';
				tab.setLabel(gridkrafter.wisejdemo.GridBoard.iconTextHtml(section.icon, section.title || section.id, 16, badge));
				tab.setUserData("id", section.id);
				tab.getContentElement().setAttribute("role", "tab");
				tab.getContentElement().setAttribute("aria-selected", section.id === selected ? "true" : "false");
				if (section.id === selected)
					tab.addState("selected");

				tab.addListener("execute", this.__onTab, this);
				tabs.add(tab);
			}

			// the selected section's title in its field (unless the user is typing in it), and its actions
			var current = null, index = -1;
			for (var i = 0; i < sections.length; i++) {
				if (sections[i].id === selected) { current = sections[i]; index = i; }
			}
			var field = this.getChildControl("section-title");
			if (!field.hasState("focused"))
				field.setValue(current ? (current.title || "") : "");
			this.getChildControl("section-up").setEnabled(index > 0);
			this.getChildControl("section-down").setEnabled(index >= 0 && index < sections.length - 1);
			this.getChildControl("section-delete").setEnabled(sections.length > 1);
		},

		/**
		 * The section's title field: tells the server once the user pauses typing.
		 */
		__onSectionTitleInput: function () {

			// the section the user is typing for, whatever is selected when the timer fires
			this.__renameId = this.getSelectedSection();
			if (!this.__renameTimer) {
				this.__renameTimer = new qx.event.Timer(350);
				this.__renameTimer.addListener("interval", this.__flushRename, this);
			}
			this.__renameTimer.restart();
		},

		/**
		 * Sends a pending section rename now: when its timer fires, and before a tab change or Done.
		 */
		__flushRename: function () {

			if (this.__renameTimer)
				this.__renameTimer.stop();

			var id = this.__renameId;
			this.__renameId = null;
			if (id && !this.isDisposed())
				this.fireDataEvent("sectionRename", { id: id, title: this.getChildControl("section-title").getValue() || "" });
		},

		/**
		 * A section action of edit mode: add opens the catalog for the section, the others go to the server.
		 */
		__onSectionAction: function (e) {

			var action = e.getTarget().getUserData("action");
			if (action === "add") {
				this.__openCatalog(e.getTarget());
				return;
			}

			this.fireDataEvent("sectionAction", { id: this.getSelectedSection(), action: action });
		},

		/**
		 * Shows Collapse all and Expand all as enabled when they have something to do.
		 */
		_applyLinks: function () {

			this.getChildControl("collapse-link").setEnabled(this.getCanCollapse());
			this.getChildControl("expand-link").setEnabled(this.getCanExpand());
		},

		/**
		 * Draws the catalog: one entry for each kind, with its icon, name and description.
		 */
		_applyCatalog: function () {

			var popup = this.getChildControl("catalog");
			var old = popup.removeAll();
			for (var i = 0; i < old.length; i++)
				old[i].dispose();

			var kinds = this.getCatalog() || [];
			for (var i = 0; i < kinds.length; i++) {

				var kind = kinds[i];
				var entry = new qx.ui.container.Composite(new qx.ui.layout.HBox(10).set({ alignY: "middle" }));
				entry.setAppearance("grid-board/catalog-item");
				entry.setUserData("kind", kind.kind);

				var color = gridkrafter.wisejdemo.GridBoard.resolveColor(kind.color);
				var icon = new qx.ui.basic.Label().set({ rich: true, anonymous: true, appearance: "label", textColor: color });
				icon.setValue(gridkrafter.wisejdemo.GridBoard.iconHtml(kind.icon, 16));
				entry.add(icon);

				var texts = new qx.ui.container.Composite(new qx.ui.layout.VBox(0)).set({ anonymous: true });
				texts.add(new qx.ui.basic.Label(kind.text || kind.kind).set({ appearance: "grid-board/catalog-name", rich: false, anonymous: true }));
				if (kind.description)
					texts.add(new qx.ui.basic.Label(kind.description).set({ appearance: "grid-board/catalog-description", rich: false, anonymous: true }));
				entry.add(texts, { flex: 1 });

				entry.addListener("pointerover", function () { this.addState("hovered"); }, entry);
				entry.addListener("pointerout", function () { this.removeState("hovered"); }, entry);
				entry.addListener("tap", this.__onCatalogEntry, this);
				popup.add(entry);
			}

			// deleted widgets come back from here, as in the original
			var removed = this.getRemoved() || [];
			if (removed.length > 0) {
				popup.add(new qx.ui.basic.Label("Hidden (" + removed.length + ")").set({ appearance: "grid-board/catalog-heading", anonymous: true }));
				for (var i = 0; i < removed.length; i++) {

					var item = removed[i];
					var entry = new qx.ui.container.Composite(new qx.ui.layout.HBox(10).set({ alignY: "middle" }));
					entry.setAppearance("grid-board/catalog-item");
					entry.setUserData("restore", item.id);

					var icon = new qx.ui.basic.Label().set({ rich: true, anonymous: true, appearance: "label", textColor: gridkrafter.wisejdemo.GridBoard.resolveColor(item.color) });
					icon.setValue(gridkrafter.wisejdemo.GridBoard.iconHtml(item.icon, 16));
					entry.add(icon);
					entry.add(new qx.ui.basic.Label(item.title || item.id).set({ appearance: "grid-board/catalog-name", rich: false, anonymous: true }), { flex: 1 });

					entry.addListener("pointerover", function () { this.addState("hovered"); }, entry);
					entry.addListener("pointerout", function () { this.removeState("hovered"); }, entry);
					entry.addListener("tap", this.__onCatalogEntry, this);
					popup.add(entry);
				}
			}
		},

		/**
		 * Shows the edit state on the toggle and the grid backdrop; tells the widgets.
		 */
		_applyEditMode: function (value) {

			var GridBoard = gridkrafter.wisejdemo.GridBoard;
			var button = this.getChildControl("edit-button");
			if (value)
				button.addState("checked");
			else
				button.removeState("checked");

			// Edit becomes Done, as in the original
			button.setLabel(value ? GridBoard.iconTextHtml("check", "Done", 14) : GridBoard.iconTextHtml("edit", "Edit", 14));
			button.getContentElement().setAttribute("aria-pressed", value ? "true" : "false");

			// Add widget, Add section, Reset layout, the section's title field and its actions: edit mode only
			var parts = ["add-button", "section-button", "reset-button", "section-title"];
			for (var i = 0; i < parts.length; i++) {
				if (value)
					this.getChildControl(parts[i]).show();
				else
					this.getChildControl(parts[i]).exclude();
			}
			if (value)
				this.__sectionActions.show();
			else
				this.__sectionActions.exclude();

			this._applySections();

			// the selection outline only shows while editing
			var selected = this.__selected;
			if (selected && !selected.isDisposed()) {
				if (value)
					selected.addState("selected");
				else
					selected.removeState("selected");
			}

			this._applyGrid();
		},

		/**
		 * Shows or hides the toolbar.
		 */
		_applyShowToolbar: function () {

			var toolbar = this.getChildControl("toolbar");
			toolbar.setHeight(this.getToolbarHeight());
			if (this.getShowToolbar())
				toolbar.show();
			else
				toolbar.exclude();
		},

		/**
		 * Draws the dotted grid backdrop of edit mode on the grid area.
		 */
		_applyGrid: function () {

			var container = this.getChildrenContainer && this.getChildrenContainer();
			if (!container)
				return;

			var el = container.getContentElement();
			var cell = this.getCellWidth();
			if (!this.getEditMode() || !(cell > 0)) {
				el.setStyles({ backgroundImage: null, backgroundSize: null, backgroundPosition: null });
				return;
			}

			// every free cell is a softly filled rounded block, as the original's edit-mode grid
			var manager = qx.theme.manager.Color.getInstance();
			var fill = manager.resolve(this.getThemeGridColor());
			var gap = this.getGap(), rowH = this.getRowHeight();
			var stepX = cell + gap, stepY = rowH + gap;
			var svg = '<svg xmlns="http://www.w3.org/2000/svg" width="' + stepX + '" height="' + stepY + '">' +
				'<rect x="0" y="0" width="' + cell + '" height="' + rowH + '" rx="4" ry="4" fill="' + fill + '"/></svg>';
			el.setStyles({
				backgroundImage: 'url("data:image/svg+xml,' + encodeURIComponent(svg) + '")',
				backgroundSize: stepX + "px " + stepY + "px",
				backgroundPosition: "0px 0px",
				backgroundRepeat: "repeat"
			});
		},

		/**
		 * Marks a widget as the selected one.
		 *
		 * @param {gridkrafter.wisejdemo.GridWidget} widget the widget.
		 */
		selectWidget: function (widget) {

			if (this.__selected === widget)
				return;

			if (this.__selected && !this.__selected.isDisposed())
				this.__selected.removeState("selected");

			this.__selected = widget;
			if (widget && this.getEditMode())
				widget.addState("selected");
		},

		/**
		 * Starts dragging a widget (or resizing it with its grip) in edit mode: a placeholder shows the
		 * cells it would take, and the drop is reported to the server in cells.
		 *
		 * @param {gridkrafter.wisejdemo.GridWidget} widget the widget.
		 * @param {Number} pageX the pointer's horizontal position in the document.
		 * @param {Number} pageY the pointer's vertical position in the document.
		 * @param {Boolean|String} resize false to move; "e" (width), "s" (height) or true / "se" (both) to resize.
		 */
		startDrag: function (widget, pageX, pageY, resize) {

			if (this.__drag || !this.getEditMode() || !this.isEnabled() || wisej.web.DesignMode)
				return;

			var mode = resize === true ? "se" : resize;

			var cell = this.getCellWidth();
			var bounds = widget.getBounds();
			if (!(cell > 0) || !bounds)
				return;

			var gap = this.getGap(), rowH = this.getRowHeight(), cols = this.getColumns();
			var stepX = cell + gap, stepY = rowH + gap;
			var start = {
				x: Math.round(bounds.left / stepX),
				y: Math.round(bounds.top / stepY),
				w: Math.max(1, Math.round((bounds.width + gap) / stepX)),
				h: Math.max(1, Math.round((bounds.height + gap) / stepY))
			};
			var rect = { x: start.x, y: start.y, w: start.w, h: start.h };

			var manager = qx.theme.manager.Color.getInstance();
			var accent = manager.resolve(this.getThemeAccentColor());
			var host = this.getChildrenContainer().getContentElement().getDomElement();
			if (!host)
				return;

			var place = function (div, r) {
				div.style.left = Math.round(r.x * stepX) + "px";
				div.style.top = (r.y * stepY) + "px";
				div.style.width = Math.round(r.w * cell + (r.w - 1) * gap) + "px";
				div.style.height = (r.h * rowH + (r.h - 1) * gap) + "px";
			};

			var placeholder = document.createElement("div");
			placeholder.setAttribute("aria-hidden", "true");
			placeholder.style.cssText = "position:absolute;box-sizing:border-box;pointer-events:none;z-index:1;" +
				"border:2px dashed " + accent + ";border-radius:" + this.getThemePlaceholderRadius() + "px;" +
				"background:" + manager.resolve(this.getThemePlaceholderColor());
			place(placeholder, rect);
			host.insertBefore(placeholder, host.firstChild);

			var outline = null;
			if (resize) {
				outline = document.createElement("div");
				outline.style.cssText = "position:absolute;box-sizing:border-box;pointer-events:none;z-index:100000;" +
					"border:1px solid " + accent + ";border-radius:" + this.getThemePlaceholderRadius() + "px;" +
					"left:" + bounds.left + "px;top:" + bounds.top + "px;width:" + bounds.width + "px;height:" + bounds.height + "px";
				host.appendChild(outline);
			}

			var el = widget.getContentElement();
			var oldZ = el.getStyle("zIndex");
			if (!resize)
				el.setStyle("zIndex", 100000, true);
			widget.addState("dragging");

			var me = this;
			var drag = this.__drag = { widget: widget };

			drag.move = function (ev) {

				ev.preventDefault();
				var dx = ev.pageX - pageX, dy = ev.pageY - pageY;
				if (resize) {
					var w = Math.max(stepX - gap, bounds.width + (mode === "s" ? 0 : dx)), h = Math.max(rowH, bounds.height + (mode === "e" ? 0 : dy));
					outline.style.width = w + "px";
					outline.style.height = h + "px";
					rect.w = Math.min(cols - start.x, Math.max(1, Math.round((w + gap) / stepX)));
					rect.h = Math.max(1, Math.round((h + gap) / stepY));
				}
				else {
					el.setStyle("transform", "translate(" + dx + "px," + dy + "px)", true);
					rect.x = Math.min(cols - start.w, Math.max(0, Math.round((bounds.left + dx) / stepX)));
					rect.y = Math.max(0, Math.round((bounds.top + dy) / stepY));
				}
				place(placeholder, rect);
			};

			drag.up = function (ev) {

				document.removeEventListener("pointermove", drag.move, true);
				document.removeEventListener("pointerup", drag.up, true);
				document.removeEventListener("pointercancel", drag.up, true);
				me.__drag = null;

				if (placeholder.parentNode) placeholder.parentNode.removeChild(placeholder);
				if (outline && outline.parentNode) outline.parentNode.removeChild(outline);
				if (widget.isDisposed())
					return;

				el.setStyle("transform", null, true);
				el.setStyle("zIndex", oldZ, true);
				widget.removeState("dragging");

				if (ev.type === "pointercancel")
					return;

				var id = widget.getItemId();
				if (resize && (rect.w !== start.w || rect.h !== start.h))
					me.fireDataEvent("widgetResize", { id: id, w: rect.w, h: rect.h });
				else if (!resize && (rect.x !== start.x || rect.y !== start.y))
					me.fireDataEvent("widgetMove", { id: id, x: rect.x, y: rect.y });
			};

			document.addEventListener("pointermove", drag.move, true);
			document.addEventListener("pointerup", drag.up, true);
			document.addEventListener("pointercancel", drag.up, true);
		},

		/**
		 * A section tab: tells the server which one, unless it is already selected.
		 */
		__onTab: function (e) {

			var id = e.getTarget().getUserData("id");
			this.__flushRename();
			if (id && id !== this.getSelectedSection())
				this.fireDataEvent("sectionSelect", id);
		},

		/**
		 * Opens the catalog under the Add widget button.
		 */
		__onAddButton: function (e) {

			this.__openCatalog(this.getChildControl("add-button"));
		},

		/**
		 * Opens the catalog under a button.
		 */
		__openCatalog: function (button) {

			var popup = this.getChildControl("catalog");
			if (!(this.getCatalog() || []).length && !(this.getRemoved() || []).length)
				return;

			popup.placeToWidget(button, true);
			popup.show();
		},

		/**
		 * A kind picked in the catalog: the server asks the application to create the widget.
		 */
		__onCatalogEntry: function (e) {

			var entry = e.getCurrentTarget();
			this.getChildControl("catalog").hide();
			entry.removeState("hovered");
			if (entry.getUserData("restore"))
				this.fireDataEvent("widgetRestore", entry.getUserData("restore"));
			else
				this.fireDataEvent("widgetAdd", entry.getUserData("kind"));
		},

		/**
		 * Reports the width of the grid area to the server once it settles: the columns are laid out on it.
		 */
		__onAreaResize: function () {

			if (this.__areaTimer)
				this.__areaTimer.stop();

			this.__areaTimer = qx.event.Timer.once(function () {

				this.__areaTimer = null;
				if (this.isDisposed())
					return;

				var size = wisej.utils.Widget.getClientSize(this.getChildControl("pane"));
				var container = this.getChildrenContainer();
				var width = size ? size.width - (container.getPaddingLeft() || 0) - (container.getPaddingRight() || 0) : 0;
				if (width > 0 && width !== this.__areaWidth) {
					this.__areaWidth = width;
					if (!wisej.web.DesignMode)
						this.fireDataEvent("areaResize", width);
				}
			}, this, 120);
		}
	},

	/**
	 * Drops the timer, the theme listener and a drag in progress.
	 */
	destruct: function () {

		if (this.__areaTimer)
			this.__areaTimer.stop();

		if (this.__drag) {
			document.removeEventListener("pointermove", this.__drag.move, true);
			document.removeEventListener("pointerup", this.__drag.up, true);
			document.removeEventListener("pointercancel", this.__drag.up, true);
		}

		qx.theme.manager.Color.getInstance().removeListener("changeTheme", this._applyGrid, this);
		qx.theme.manager.Color.getInstance().removeListener("changeTheme", this._applySections, this);
		this._disposeObjects("__areaTimer", "__renameTimer");
		this.__selected = this.__drag = this.__sectionActions = null;
	}
});


/**
 * gridkrafter.wisejdemo.GridWidget
 *
 * The client half of GridKrafter.WisejDemo.Controls.GridWidget: a scrollable Wisej container whose
 * child controls fill the body, drawn as the GridKrafter demos draw a widget. A card has a 3-pixel top
 * edge in its colour and a header (a small line icon in its colour, the title, compact line buttons)
 * docked above the body; a plain tile has a 3-pixel left edge in its colour and no header, and offers
 * its colours and delete in a pop-up in edit mode. The footer text shows right-aligned under the body.
 * In edit mode the header (the whole tile when plain) drags the widget and the grip resizes it,
 * through the board.
 */
qx.Class.define("gridkrafter.wisejdemo.GridWidget", {

	extend: wisej.web.ScrollableControl,

	/**
	 * Creates the header above the body, and the stripe, hover pop-up and grip of the tile.
	 */
	construct: function () {

		this.base(arguments);

		this._createChildControl("header");
		this._createChildControl("footer");

		// the coloured edge: across the top of a card, down the left of a tile (inside the 1 px border)
		var stripe = this.__stripe = new qx.html.Element("div", {
			position: "absolute", left: "0px", top: "0px", zIndex: 100000, pointerEvents: "none"
		}, { "aria-hidden": "true" });

		var hover = this.__hover = new qx.html.Element("div", {
			position: "absolute", top: "8px", right: "8px", zIndex: 100001, display: "none",
			alignItems: "center", gap: "5px", padding: "4px 6px", borderRadius: "8px", boxSizing: "border-box"
		}, { role: "toolbar" });
		hover.addListener("pointerdown", this.__onHoverDown, this);
		hover.addListener("tap", this.__onHoverTap, this);

		var grip = this.__grip = new qx.html.Element("div", {
			position: "absolute", right: "4px", bottom: "4px", width: "10px", height: "10px",
			boxSizing: "border-box", borderRadius: "0 0 3px 0", opacity: 0.5, cursor: "se-resize",
			zIndex: 100001, display: "none"
		}, { "aria-hidden": "true", "data-resize": "se" });
		grip.addListener("pointerdown", this.__onGripDown, this);

		// the blue handles in the middle of the right and bottom edges, as in the original
		var east = this.__east = new qx.html.Element("div", {
			position: "absolute", right: "-3px", top: "50%", width: "5px", height: "26px", marginTop: "-13px",
			borderRadius: "3px", cursor: "ew-resize", zIndex: 100001, display: "none"
		}, { "aria-hidden": "true", "data-resize": "e" });
		east.addListener("pointerdown", this.__onGripDown, this);

		var south = this.__south = new qx.html.Element("div", {
			position: "absolute", bottom: "-3px", left: "50%", width: "26px", height: "5px", marginLeft: "-13px",
			borderRadius: "3px", cursor: "ns-resize", zIndex: 100001, display: "none"
		}, { "aria-hidden": "true", "data-resize": "s" });
		south.addListener("pointerdown", this.__onGripDown, this);

		var content = this.getContentElement();
		content.add(stripe);
		content.add(hover);
		content.add(grip);
		content.add(east);
		content.add(south);

		this.addListener("pointerdown", this.__onPointerDown, this);
		this.addListener("pointerover", this.__onPointerOver, this);
		this.addListener("pointerout", this.__onPointerOut, this);
		this.addListener("appear", this._updateTimer, this);
		this.addListener("disappear", this._updateTimer, this);

		qx.theme.manager.Color.getInstance().addListener("changeTheme", this._applyLook, this);
		qx.theme.manager.Color.getInstance().addListener("changeTheme", this._applyChrome, this);

		this._applyLook();
		this._applyChrome();
	},

	events: {

		/**
		 * A header button or the hover pop-up: { action, color }; action is refresh, fullscreen,
		 * collapse, color or delete.
		 */
		widgetAction: "qx.event.type.Data",

		/**
		 * The automatic refresh is due.
		 */
		widgetTimer: "qx.event.type.Event"
	},

	properties: {

		/**
		 * The theme appearance of the widget.
		 */
		appearance: { init: "grid-board-widget", refine: true },

		/**
		 * The id the board knows the widget by.
		 */
		itemId: { init: "", check: "String" },

		/**
		 * The title in the header.
		 */
		title: { init: "", check: "String", apply: "_applyTitle" },

		/**
		 * The header's icon: a built-in name or an address.
		 */
		icon: { init: "chart", check: "String", apply: "_applyLook" },

		/**
		 * The widget's colour: a palette name of the theme or a CSS colour.
		 */
		color: { init: "blue", check: "String", apply: "_applyLook" },

		/**
		 * The colours the pickers offer: palette names of the theme.
		 */
		palette: { init: null, nullable: true, check: "Array", apply: "_applyLook" },

		/**
		 * card or plain.
		 */
		chrome: { init: "card", check: ["card", "plain"], apply: "_applyChrome" },

		/**
		 * The header buttons, in order: refresh, fullscreen, collapse, color, delete.
		 */
		buttons: { init: null, nullable: true, check: "Array", apply: "_applyButtons" },

		/**
		 * Whether only the header shows.
		 */
		collapsed: { init: false, check: "Boolean", apply: "_applyChrome" },

		/**
		 * Whether the board is in edit mode.
		 */
		editMode: { init: false, check: "Boolean", apply: "_applyChrome" },

		/**
		 * Whether the widget fills the board.
		 */
		fullscreen: { init: false, check: "Boolean", apply: "_applyChrome" },

		/**
		 * How often, in seconds, the widget asks to be refreshed while visible; 0 never.
		 */
		refreshInterval: { init: 0, check: "Integer", apply: "_updateTimer" },

		/**
		 * The height of the header, in pixels: the server places the body below it.
		 */
		headerHeight: { init: 34, check: "Integer", apply: "_applyChrome" },

		/**
		 * The small muted text right-aligned under the body; empty shows no footer.
		 */
		footerText: { init: "", check: "String", apply: "_applyFooter" },

		/**
		 * The height of the footer, in pixels: the server places the body above it.
		 */
		footerHeight: { init: 24, check: "Integer", apply: "_applyFooter" },

		/**
		 * The theme's corner radius of the widget, in pixels: the coloured edge follows it.
		 */
		themeRadius: { init: 8, check: "Integer", themeable: true, apply: "_applyChrome" },

		/**
		 * The theme's surface of the hover pop-up.
		 */
		themeSurfaceColor: { init: "#ffffff", check: "Color", themeable: true, apply: "_applyLook" },

		/**
		 * The theme's border of the hover pop-up.
		 */
		themeBorderColor: { init: "#e1e4ea", check: "Color", themeable: true, apply: "_applyLook" },

		/**
		 * The theme's muted text: the grip and the pop-up's delete.
		 */
		themeMutedColor: { init: "#5f6878", check: "Color", themeable: true, apply: "_applyLook" },

		/**
		 * The theme's accent: the ring around the current colour.
		 */
		themeAccentColor: { init: "#6d4dff", check: "Color", themeable: true, apply: "_applyLook" },

		/**
		 * The theme's shadow of the hover pop-up.
		 */
		themeShadowColor: { init: "#c8c8d4", check: "Color", themeable: true, apply: "_applyLook" }
	},

	members: {

		/** The tile's colour stripe. */
		__stripe: null,

		/** The tile's hover pop-up. */
		__hover: null,

		/** The resize grip. */
		__grip: null,

		/** The handles in the middle of the right and bottom edges. */
		__east: null,
		__south: null,

		/** The timer that sends the new title once the user pauses typing. */
		__titleTimer: null,

		/** The automatic refresh timer. */
		__timer: null,

		/**
		 * Creates the header and its parts, and the colour pop-up of the Color button.
		 */
		_createChildControlImpl: function (id, hash) {

			var control;

			switch (id) {

				case "header":
					control = new qx.ui.container.Composite(new qx.ui.layout.HBox(8).set({ alignY: "middle" }));
					control.setHeight(this.getHeaderHeight());
					control.add(this.getChildControl("grip"));
					control.add(this.getChildControl("icon"));
					control.add(this.getChildControl("title"), { flex: 1 });
					control.add(this.getChildControl("buttons"));
					this._addAt(control, 0, { edge: "north" });
					break;

				case "icon":
					control = new qx.ui.basic.Label().set({ rich: true, anonymous: true });
					break;

				case "grip":
					// drags the widget in edit mode
					control = new qx.ui.basic.Label().set({ rich: true, cursor: "move", toolTipText: "Move" });
					control.setValue(gridkrafter.wisejdemo.GridBoard.iconHtml("grip", 16));
					control.exclude();
					break;

				case "editbox":
					// the pencil's box: the title, renamed as the user types, as the original's
					control = new qx.ui.container.Composite(new qx.ui.layout.VBox(4));
					control.add(new qx.ui.basic.Label("Title").set({ appearance: "grid-board-widget/editbox-label" }));
					control.add(this.getChildControl("title-field"));
					control.add(new qx.ui.basic.Label("Changes are saved as you type.").set({ appearance: "grid-board-widget/editbox-hint" }));
					var close = new qx.ui.form.Button("Close").set({ appearance: "grid-board-widget/editbox-close", allowGrowX: false });
					close.addListener("execute", function () { this.__setEditing(false); }, this);
					control.add(close);
					control.exclude();
					this._addAt(control, 1, { edge: "north" });
					break;

				case "title-field":
					control = new qx.ui.form.TextField();
					control.getContentElement().setAttribute("aria-label", "Title");
					control.addListener("input", this.__onTitleInput, this);
					control.addListener("pointerdown", function (e) { e.stopPropagation(); });
					break;

				case "title":
					// grows to take the header's free width, so the buttons sit at the right edge
					control = new qx.ui.basic.Label().set({ rich: false, anonymous: true, allowGrowX: true, allowShrinkX: true, minWidth: 0 });
					break;

				case "buttons":
					control = new qx.ui.container.Composite(new qx.ui.layout.HBox(2).set({ alignY: "middle" }));
					break;

				case "footer":
					control = new qx.ui.basic.Label().set({ rich: true, anonymous: true, textAlign: "right", allowGrowX: true });
					control.setHeight(this.getFooterHeight());
					control.exclude();
					this._addAt(control, 1, { edge: "south" });
					break;

				case "palette":
					control = new qx.ui.popup.Popup(new qx.ui.layout.HBox(5).set({ alignY: "middle" }));
					control.setAppearance("grid-board-widget/palette");
					control.setPosition("bottom-right");
					break;
			}

			return control || this.base(arguments, id, hash);
		},

		/**
		 * Returns the board the widget is on, or null.
		 *
		 * @return {gridkrafter.wisejdemo.GridBoard} the board.
		 */
		getBoard: function () {

			var parent = this.getLayoutParent();
			while (parent && !(parent instanceof gridkrafter.wisejdemo.GridBoard))
				parent = parent.getLayoutParent();

			return parent || null;
		},

		/**
		 * Shows the title.
		 */
		_applyTitle: function (value) {

			this.getChildControl("title").setValue(value || "");
			this.getContentElement().setAttribute("aria-label", value || "");

			var field = this.getChildControl("title-field");
			if (!field.hasState("focused"))
				field.setValue(value || "");
		},

		/**
		 * Shows the footer text under the body, or no footer.
		 */
		_applyFooter: function () {

			var footer = this.getChildControl("footer");
			var text = this.getFooterText();
			footer.setValue(text ? '<span style="font-size:12px">' + qx.bom.String.escape(text) + '</span>' : "");
			footer.setHeight(this.getFooterHeight());
			if (text && !(this.getCollapsed() && this.getChrome() !== "plain"))
				footer.show();
			else
				footer.exclude();
		},

		/**
		 * Applies the colour to the icon, the coloured edge and the pop-ups, and the theme's colours to the overlays.
		 */
		_applyLook: function () {

			if (!this.__stripe)
				return;

			var GridBoard = gridkrafter.wisejdemo.GridBoard;
			var manager = qx.theme.manager.Color.getInstance();
			var color = GridBoard.resolveColor(this.getColor());

			this.getChildControl("icon").set({ value: GridBoard.iconHtml(this.getIcon(), 16), textColor: color });

			this.__stripe.setStyle("backgroundColor", color);
			var accentColor = manager.resolve(this.getThemeAccentColor());
			this.__east.setStyle("backgroundColor", accentColor);
			this.__south.setStyle("backgroundColor", accentColor);
			this.__grip.setStyles({
				borderRight: "2px solid " + manager.resolve(this.getThemeMutedColor()),
				borderBottom: "2px solid " + manager.resolve(this.getThemeMutedColor())
			});

			// the hover pop-up of a plain tile: four colours and delete
			var surface = manager.resolve(this.getThemeSurfaceColor());
			var accent = manager.resolve(this.getThemeAccentColor());
			this.__hover.setStyles({
				backgroundColor: surface,
				border: "1px solid " + manager.resolve(this.getThemeBorderColor()),
				boxShadow: "0 4px 12px " + manager.resolve(this.getThemeShadowColor())
			});

			// the tile's floating box in edit mode: move, colour and delete, as the original's
			var muted = manager.resolve(this.getThemeMutedColor());
			var tool = function (action, icon, label, cursor) {
				return '<span role="button" tabindex="-1" aria-label="' + label + '" title="' + label + '" data-action="' + action +
					'" style="display:inline-flex;padding:3px;border-radius:5px;cursor:' + cursor + ';color:' + muted + '">' +
					GridBoard.iconHtml(icon, 16).replace("<svg ", '<svg style="pointer-events:none" ') + '</span>';
			};
			this.__hover.setAttribute("html", tool("grip", "grip", "Move", "move") + tool("color", "color", "Colour", "pointer") + tool("delete", "delete", "Close", "pointer"));

			this.__drawPalette();
		},

		/**
		 * Shows the header of a card or the stripe of a tile, the body unless collapsed, and the grip in edit mode.
		 */
		_applyChrome: function () {

			if (!this.__stripe)
				return;

			var plain = this.getChrome() === "plain";
			var header = this.getChildControl("header");
			var pane = this.getChildControl("pane");

			header.setHeight(this.getHeaderHeight());
			if (plain)
				header.exclude();
			else
				header.show();

			var collapsed = this.getCollapsed() && !plain;
			if (collapsed) {
				pane.exclude();
				this.addState("collapsed");
				header.addState("collapsed");
			}
			else {
				pane.show();
				this.removeState("collapsed");
				header.removeState("collapsed");
			}

			var edit = this.getEditMode() && !this.getFullscreen();
			if (edit)
				this.addState("editing");
			else
				this.removeState("editing");

			header.setCursor(null);
			this.setCursor(null);
			if (edit && !plain)
				this.getChildControl("grip").show();
			else
				this.getChildControl("grip").exclude();

			// the dashed outline of a widget being edited
			var manager = qx.theme.manager.Color.getInstance();
			this.getContentElement().setStyles(edit ?
				{ outline: "1px dashed " + manager.resolve(this.getThemeBorderColor()), outlineOffset: "3px" } :
				{ outline: null, outlineOffset: null });

			// a card's edge runs across the top, a tile's down the left, inside the border and its rounding
			var radius = Math.max(0, this.getThemeRadius() - 1) + "px";
			this.__stripe.setStyles(plain ?
				{ right: null, bottom: "0px", width: "3px", height: null, borderRadius: radius + " 0 0 " + radius } :
				{ right: "0px", bottom: null, width: null, height: "3px", borderRadius: radius + " " + radius + " 0 0" });
			this.__grip.setStyle("display", edit && !collapsed ? "block" : "none");
			this.__east.setStyle("display", edit ? "block" : "none");
			this.__south.setStyle("display", edit && !collapsed ? "block" : "none");
			this.__hover.setStyle("display", plain && edit ? "flex" : "none");
			if (!edit || plain || collapsed)
				this.__setEditing(false);

			this._applyFooter();

			if (this.getFullscreen())
				this.addState("fullscreen");
			else
				this.removeState("fullscreen");

			this._applyButtons();
		},

		/**
		 * Draws the header buttons.
		 */
		_applyButtons: function () {

			var GridBoard = gridkrafter.wisejdemo.GridBoard;
			var container = this.getChildControl("buttons");
			var old = container.removeAll();
			for (var i = 0; i < old.length; i++)
				old[i].dispose();

			var names = { refresh: "Refresh", color: "Colour", edit: "Edit", fullscreen: "Full screen", collapse: "Collapse", "delete": "Close" };
			var buttons = this.getButtons() || [];
			for (var i = 0; i < buttons.length; i++) {

				var action = buttons[i];
				var icon = action;
				var label = names[action] || action;
				if (action === "collapse" && this.getCollapsed()) { icon = "expand"; label = "Expand"; }
				if (action === "fullscreen" && this.getFullscreen()) { icon = "exitFullscreen"; label = "Exit full screen"; }

				var button = new qx.ui.form.Button().set({ appearance: "grid-board-widget/button", rich: true, toolTipText: label });
				button.setLabel(GridBoard.iconHtml(icon, 16));
				button.setUserData("action", action);
				button.getContentElement().setAttribute("aria-label", label);
				button.addListener("execute", this.__onButton, this);
				container.add(button);
			}
		},

		/**
		 * Draws the swatches of the Color button's pop-up.
		 */
		__drawPalette: function () {

			var GridBoard = gridkrafter.wisejdemo.GridBoard;
			var popup = this.getChildControl("palette");
			var old = popup.removeAll();
			for (var i = 0; i < old.length; i++)
				old[i].dispose();

			var palette = this.getPalette() || [];
			for (var i = 0; i < palette.length; i++) {

				var swatch = new qx.ui.form.Button().set({ appearance: "grid-board-widget/swatch", toolTipText: palette[i] });
				swatch.setBackgroundColor(GridBoard.resolveColor(palette[i]));
				swatch.setUserData("color", palette[i]);
				if (palette[i] === this.getColor())
					swatch.addState("selected");

				swatch.addListener("execute", function (e) {
					this.getChildControl("palette").hide();
					this.fireDataEvent("widgetAction", { action: "color", color: e.getTarget().getUserData("color") });
				}, this);
				popup.add(swatch);
			}
		},

		/**
		 * A header button: the Color button opens the swatches, the others go to the server.
		 */
		__onButton: function (e) {

			var button = e.getTarget();
			var action = button.getUserData("action");
			if (action === "edit") {
				this.__setEditing(!this.getChildControl("editbox").isVisible());
				return;
			}
			if (action === "color") {
				var popup = this.getChildControl("palette");
				popup.placeToWidget(button, true);
				popup.show();
				return;
			}

			this.fireDataEvent("widgetAction", { action: action });
		},

		/**
		 * A swatch or the delete of the hover pop-up.
		 */
		__onHoverTap: function (e) {

			var target = e.getOriginalTarget();
			if (!target || !target.getAttribute)
				return;

			var action = target.getAttribute("data-action");
			if (action === "delete")
				this.fireDataEvent("widgetAction", { action: "delete" });
			else if (action === "color") {
				var popup = this.getChildControl("palette");
				popup.placeToWidget(this, true);
				popup.show();
			}
		},

		/**
		 * The floating box's grip starts a drag; its other tools are taps.
		 */
		__onHoverDown: function (e) {

			e.stopPropagation();
			var target = e.getOriginalTarget();
			var board = this.getBoard();
			if (!board || !target || !target.getAttribute || target.getAttribute("data-action") !== "grip")
				return;

			e.preventDefault();
			board.selectWidget(this);
			board.startDrag(this, e.getDocumentLeft(), e.getDocumentTop(), false);
		},

		/**
		 * Shows or hides the edit box under the header.
		 */
		__setEditing: function (value) {

			var box = this.getChildControl("editbox");
			if (value && this.getChrome() !== "plain" && !this.getCollapsed()) {
				this.getChildControl("title-field").setValue(this.getTitle() || "");
				box.show();
				this.getChildControl("title-field").focus();
			}
			else
				box.exclude();
		},

		/**
		 * The title field: tells the server once the user pauses typing.
		 */
		__onTitleInput: function () {

			if (!this.__titleTimer) {
				this.__titleTimer = new qx.event.Timer(350);
				this.__titleTimer.addListener("interval", function () {
					this.__titleTimer.stop();
					if (!this.isDisposed())
						this.fireDataEvent("widgetAction", { action: "rename", title: this.getChildControl("title-field").getValue() || "" });
				}, this);
			}
			this.__titleTimer.restart();
		},

		/**
		 * Selects the widget; in edit mode the header (the whole tile when plain) starts a drag.
		 */
		__onPointerDown: function (e) {

			var board = this.getBoard();
			if (!board)
				return;

			board.selectWidget(this);

			if (!this.getEditMode() || this.getFullscreen() || !e.isPrimary || !e.isPrimary())
				return;

			// a card moves by its grip, a tile by the grip of its floating box (handled there)
			var target = e.getTarget();
			if (this.getChrome() === "plain" || !qx.ui.core.Widget.contains(this.getChildControl("grip"), target))
				return;

			e.preventDefault();
			board.startDrag(this, e.getDocumentLeft(), e.getDocumentTop(), false);
		},

		/**
		 * The grip starts a resize.
		 */
		__onGripDown: function (e) {

			e.stopPropagation();
			e.preventDefault();

			var board = this.getBoard();
			var target = e.getCurrentTarget();
			var mode = (target && target.getAttribute && target.getAttribute("data-resize")) || "se";
			if (board) {
				board.selectWidget(this);
				board.startDrag(this, e.getDocumentLeft(), e.getDocumentTop(), mode);
			}
		},

		/**
		 * Shows the hover pop-up of a plain tile.
		 */
		__onPointerOver: function (e) {

			// the colours and delete of a tile are an edit-mode affordance, as in the original
			// the tile's floating box stays while editing (see _applyChrome)
		},

		/**
		 * Hides the hover pop-up when the pointer leaves the tile.
		 */
		__onPointerOut: function (e) {

			// nothing to hide: the floating box stays while editing
		},

		/**
		 * Runs the automatic refresh while the widget is visible.
		 */
		_updateTimer: function () {

			var seconds = this.getRefreshInterval();
			var run = seconds > 0 && !wisej.web.DesignMode && this.isSeeable();

			if (this.__timer && (!run || this.__timer.getInterval() !== seconds * 1000)) {
				this.__timer.stop();
				this.__timer.dispose();
				this.__timer = null;
			}

			if (run && !this.__timer) {
				this.__timer = new qx.event.Timer(seconds * 1000);
				this.__timer.addListener("interval", function () {
					if (this.isSeeable())
						this.fireEvent("widgetTimer");
				}, this);
				this.__timer.start();
			}
		}
	},

	/**
	 * Drops the timer and the theme listener.
	 */
	destruct: function () {

		qx.theme.manager.Color.getInstance().removeListener("changeTheme", this._applyLook, this);
		qx.theme.manager.Color.getInstance().removeListener("changeTheme", this._applyChrome, this);
		this._disposeObjects("__timer", "__titleTimer", "__stripe", "__hover", "__grip", "__east", "__south");
	}
});
