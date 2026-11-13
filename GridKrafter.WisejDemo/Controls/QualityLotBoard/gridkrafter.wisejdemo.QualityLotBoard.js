/**
 * gridkrafter.wisejdemo.QualityLotBoard
 *
 * The client half of GridKrafter.WisejDemo.QualityLotBoard: a summary strip (released, on hold, failed, awaiting
 * CoA), filter chips by status, and a list of lots. Each lot shows its number and where it is, one cell per test
 * coloured against its spec (red beyond a limit, amber within 10% of one), a status chip and a CoA flag; the
 * selected lot opens a detail row with each test's value against its limits. Below 560 px the test cells give way
 * to one coloured dot per test. The server owns every value; the browser only reports what the user picks.
 */
qx.Class.define("gridkrafter.wisejdemo.QualityLotBoard", {

	extend: wisej.web.Control,

	/**
	 * Builds the summary, the chips and the list once; the rows are drawn from the data.
	 */
	construct: function () {

		this.base(arguments, new qx.ui.layout.VBox(8));

		this.setFocusable(false);

		this.__summary = new qx.ui.container.Composite(new qx.ui.layout.HBox(8));
		this.add(this.__summary);

		this.__chips = new qx.ui.container.Composite(new qx.ui.layout.Flow(4, 4));
		this.add(this.__chips);

		this.__head = new qx.ui.container.Composite(new qx.ui.layout.HBox(8).set({ alignY: "middle" })).set({ appearance: "quality-lot-board/head" });
		this.add(this.__head);

		this.__rows = new qx.ui.container.Composite(new qx.ui.layout.VBox(0));
		var scroll = new qx.ui.container.Scroll().set({ scrollbarX: "off", scrollbarY: "auto" });
		scroll.add(this.__rows);
		this.add(scroll, { flex: 1 });

		this.addListener("resize", this._onResize, this);
		qx.theme.manager.Color.getInstance().addListener("changeTheme", this._applyBoard, this);

		this._applyBoard();
	},

	events: {

		/**
		 * The user selected a lot, or cleared the selection: { lot, selected }.
		 */
		lotSelected: "qx.event.type.Data",

		/**
		 * The user picked a status filter: { status, statusFilter }.
		 */
		statusFilterChanged: "qx.event.type.Data"
	},

	properties: {

		/**
		 * The theme appearance of the root widget.
		 */
		appearance: { init: "quality-lot-board", refine: true },

		/**
		 * The status the list shows: all, released, hold, fail or pending.
		 */
		statusFilter: { init: "all", check: "String", apply: "_applyBoard" },

		/**
		 * The lot number of the selected lot.
		 */
		selected: { init: "", check: "String", apply: "_applyBoard" },

		/**
		 * The lots: [{ lot, plant, grade, vessel, sampled, status, coaIssued, results }].
		 */
		lots: { init: null, nullable: true, check: "Array", apply: "_applyBoard" },

		/**
		 * The test specs: [{ test, unit, min, max }].
		 */
		specs: { init: null, nullable: true, check: "Array", apply: "_applyBoard" },

		/**
		 * The theme's colours: in spec, out of spec, near a limit, the accent. Set by the mixin.
		 */
		themePositiveColor: { init: "#1fb57a", check: "Color", themeable: true, apply: "_applyBoard" },
		themeNegativeColor: { init: "#e5484d", check: "Color", themeable: true, apply: "_applyBoard" },
		themeWarningColor: { init: "#e0a100", check: "Color", themeable: true, apply: "_applyBoard" },
		themeAccentColor: { init: "#6d4dff", check: "Color", themeable: true, apply: "_applyBoard" }
	},

	members: {

		__summary: null,
		__chips: null,
		__head: null,
		__rows: null,
		__pending: false,
		__narrow: false,
		__tight: false,

		/**
		 * Resolves a theme colour name or a CSS colour to CSS.
		 */
		__css: function (color) {

			var manager = qx.theme.manager.Color.getInstance();
			return manager.isDynamic(color) ? manager.resolve(color) : color;
		},

		/**
		 * The colour mixed into the theme's window colour by the given amount (0..1): a tint that follows the theme.
		 */
		__alpha: function (color, alpha) {

			try {
				var rgb = qx.util.ColorUtil.stringToRgb(this.__css(color));
				var back = qx.util.ColorUtil.stringToRgb(this.__css("window"));
				return qx.util.ColorUtil.rgbToRgbString([
					Math.round(back[0] + (rgb[0] - back[0]) * alpha),
					Math.round(back[1] + (rgb[1] - back[1]) * alpha),
					Math.round(back[2] + (rgb[2] - back[2]) * alpha)]);
			}
			catch (ex) {
				return this.__css(color);
			}
		},

		/**
		 * The state colour of a test: ok, warn, bad or none.
		 */
		__stateColor: function (state) {

			return state === "bad" ? this.getThemeNegativeColor()
				: state === "warn" ? this.getThemeWarningColor()
				: state === "ok" ? this.getThemePositiveColor()
				: "grayText";
		},

		/**
		 * Gives a label a tinted pill background of the colour.
		 */
		__pill: function (widget, color, alpha, radius) {

			var el = widget.getContentElement();
			el.setStyle("backgroundColor", alpha ? this.__alpha(color, alpha) : "transparent");
			el.setStyle("borderRadius", radius + "px");
		},

		/**
		 * A number as short text: up to one decimal from 100, two from 1, three below.
		 */
		__format: function (v) {

			var s = Math.abs(v) >= 100 ? v.toFixed(1) : Math.abs(v) >= 1 ? v.toFixed(2) : v.toFixed(3);
			if (s.indexOf(".") >= 0)
				s = s.replace(/0+$/, "").replace(/\.$/, "");
			return s;
		},

		/**
		 * A limit that may be missing as a number, or null.
		 */
		__limit: function (v) {

			return v === null || v === undefined || v === "" || isNaN(+v) ? null : +v;
		},

		/**
		 * "test=value;test=value" as { test: value } with lower case names.
		 */
		__parse: function (text) {

			var out = {}, parts = String(text || "").split(";");
			for (var i = 0; i < parts.length; i++) {
				var at = parts[i].indexOf("=");
				if (at > 0) {
					var v = this.__limit(parts[i].substring(at + 1).trim());
					if (v !== null)
						out[parts[i].substring(0, at).trim().toLowerCase()] = v;
				}
			}
			return out;
		},

		/**
		 * A value against its spec: ok, warn (within 10% of a limit), bad or none.
		 */
		__state: function (spec, v) {

			if (v === undefined || v === null)
				return "none";

			var lo = this.__limit(spec.min), hi = this.__limit(spec.max);
			if ((lo !== null && v < lo) || (hi !== null && v > hi))
				return "bad";

			var m = lo !== null && hi !== null ? (hi - lo) * 0.1 : 0.1 * Math.abs(lo !== null ? lo : hi !== null ? hi : 0);
			if ((lo !== null && v - lo <= m) || (hi !== null && hi - v <= m))
				return "warn";

			return "ok";
		},

		/**
		 * The limits of a spec as text.
		 */
		__specText: function (spec) {

			var lo = this.__limit(spec.min), hi = this.__limit(spec.max);
			return lo !== null && hi !== null ? lo + " – " + hi : hi !== null ? "≤ " + hi : lo !== null ? "≥ " + lo : "";
		},

		/**
		 * The short column title of a test.
		 */
		__short: function (spec) {

			var names = { proof: "Proof", water: "Water", methanol: "MeOH", acidity: "Acid", appearance: "Appear" };
			return names[String(spec.test).toLowerCase()] || spec.test;
		},

		/**
		 * Gives a widget the hovered state while the pointer is over it.
		 */
		__hover: function (widget) {

			widget.addListener("pointerover", function () { widget.addState("hovered"); });
			widget.addListener("pointerout", function () { widget.removeState("hovered"); });
		},

		/**
		 * A label with a themed appearance.
		 */
		__label: function (text, appearance, props) {

			return new qx.ui.basic.Label(text).set(qx.lang.Object.mergeWith({ appearance: "quality-lot-board/" + appearance, rich: false, anonymous: true }, props || {}, true));
		},

		/**
		 * Every property draws the widget again, once per round of changes.
		 */
		_applyBoard: function () {

			if (!this.__rows || this.__pending)
				return;

			this.__pending = true;
			qx.event.Timer.once(function () {
				this.__pending = false;
				if (!this.isDisposed())
					this.__draw();
			}, this, 0);
		},

		/**
		 * The narrow layouts change with the width.
		 */
		_onResize: function (e) {

			var width = e.getData().width;
			var narrow = width < 560, tight = width < 420;
			if (narrow !== this.__narrow || tight !== this.__tight) {
				this.__narrow = narrow;
				this.__tight = tight;
				this._applyBoard();
			}
		},

		/**
		 * The summary, the chips, the header and the rows.
		 */
		__draw: function () {

			var lots = (this.getLots() || []).filter(function (l) { return l && l.lot; });
			var specs = (this.getSpecs() || []).filter(function (s) { return s && s.test; });
			var filter = (this.getStatusFilter() || "all").toLowerCase();
			var selected = this.getSelected();
			var keys = ["released", "hold", "fail", "pending"];
			var labels = { released: "Released", hold: "Hold", fail: "Fail", pending: "Pending" };

			var count = function (status) { return lots.filter(function (l) { return l.status === status; }).length; };
			var awaiting = lots.filter(function (l) { return l.status === "released" && !l.coaIssued; }).length;

			// summary
			this.__clear(this.__summary);
			var stats = [
				[count("released"), "Released", this.getThemePositiveColor()],
				[count("hold"), "On hold", this.getThemeWarningColor()],
				[count("fail"), "Failed", this.getThemeNegativeColor()],
				[awaiting, "Awaiting CoA", this.getThemeAccentColor()]
			];
			for (var i = 0; i < stats.length; i++) {
				var box = new qx.ui.container.Composite(new qx.ui.layout.HBox(6).set({ alignY: "middle" })).set({ appearance: "quality-lot-board/stat", minWidth: 0, allowShrinkX: true });
				box.add(this.__label(String(stats[i][0]), "stat-value", { textColor: this.__css(stats[i][2]) }));
				box.add(this.__label(stats[i][1], "stat-label", { wrap: false, allowShrinkX: true, minWidth: 0 }), { flex: 1 });
				this.__summary.add(box, { flex: 1 });
			}

			// chips
			this.__clear(this.__chips);
			var all = ["all"].concat(keys);
			for (var i = 0; i < all.length; i++) {
				var status = all[i];
				var chip = new qx.ui.basic.Label((status === "all" ? "All" : labels[status]) + "  " + (status === "all" ? lots.length : count(status))).set({
					appearance: "quality-lot-board/chip", rich: false, cursor: "pointer", wrap: false
				});
				chip.setUserData("status", status);
				chip.getContentElement().setAttribute("role", "button");
				chip.getContentElement().setAttribute("aria-pressed", status === filter ? "true" : "false");
				if (status === filter)
					chip.addState("selected");
				this.__hover(chip);
				chip.addListener("tap", this._onChipTap, this);
				this.__chips.add(chip);
			}

			// header
			this.__clear(this.__head);
			this.__head.add(this.__label("Lot", "head-label"), { flex: 1 });
			if (!this.__narrow) {
				var titles = new qx.ui.container.Composite(new qx.ui.layout.HBox(4)).set({ anonymous: true });
				for (var i = 0; i < specs.length; i++)
					titles.add(this.__label(this.__short(specs[i]), "head-label", { width: 54, allowGrowX: false, textAlign: "center", toolTipText: specs[i].test + (specs[i].unit ? " (" + specs[i].unit + ")" : "") }));
				this.__head.add(titles);
			}
			this.__head.add(this.__label("Status", "head-label", { width: 70, allowGrowX: false, textAlign: "center" }));
			if (!this.__tight)
				this.__head.add(this.__label("CoA", "head-label", { width: 20, allowGrowX: false, textAlign: "center" }));

			// rows
			this.__clear(this.__rows);
			var shown = lots.filter(function (l) { return filter === "all" || l.status === filter; });
			if (!shown.length) {
				this.__rows.add(this.__label("No lots", "empty", { textAlign: "center", allowGrowX: true }));
				return;
			}
			for (var i = 0; i < shown.length; i++) {
				this.__rows.add(this.__createRow(shown[i], specs, shown[i].lot === selected));
				if (shown[i].lot === selected)
					this.__rows.add(this.__createDetail(shown[i], specs));
			}
		},

		/**
		 * Removes and disposes every child of a container.
		 */
		__clear: function (container) {

			var old = container.removeAll();
			for (var i = 0; i < old.length; i++)
				old[i].dispose();
		},

		/**
		 * The cells of a lot: the value of every spec and its state.
		 */
		__cells: function (lot, specs) {

			var results = this.__parse(lot.results), cells = [];
			for (var i = 0; i < specs.length; i++) {
				var v = results[String(specs[i].test).toLowerCase()];
				cells.push({ spec: specs[i], value: v, state: this.__state(specs[i], v) });
			}
			return cells;
		},

		/**
		 * One lot: number and meta line, test cells (or dots), status chip and CoA flag.
		 */
		__createRow: function (lot, specs, isSelected) {

			var cells = this.__cells(lot, specs);
			var row = new qx.ui.container.Composite(new qx.ui.layout.HBox(8).set({ alignY: "middle" })).set({ appearance: "quality-lot-board/row", cursor: "pointer" });
			row.setUserData("lot", lot.lot);
			row.getContentElement().setAttribute("role", "button");
			row.getContentElement().setAttribute("aria-label", lot.lot + ", " + lot.status);
			if (isSelected)
				row.addState("selected");
			this.__hover(row);
			row.addListener("tap", this._onRowTap, this);

			var sampled = String(lot.sampled || "").replace("T", " ").substring(5, 16);
			var meta = [lot.plant, lot.grade, lot.vessel, sampled].filter(function (s) { return !!s; }).join(" · ");
			var id = new qx.ui.container.Composite(new qx.ui.layout.VBox(1)).set({ minWidth: 0, allowShrinkX: true, anonymous: true });
			id.add(this.__label(lot.lot, "lot", { wrap: false }));
			id.add(this.__label(meta, "meta", { wrap: false, allowShrinkX: true, minWidth: 0, toolTipText: meta }));
			if (this.__narrow) {
				var dots = new qx.ui.container.Composite(new qx.ui.layout.HBox(3)).set({ anonymous: true, marginTop: 2 });
				for (var i = 0; i < cells.length; i++) {
					var dot = new qx.ui.core.Widget().set({ width: 8, height: 8, allowGrowX: false, allowGrowY: false, anonymous: true });
					this.__pill(dot, this.__stateColor(cells[i].state), 1, 4);
					dots.add(dot);
				}
				id.add(dots);
			}
			row.add(id, { flex: 1 });

			if (!this.__narrow) {
				var tests = new qx.ui.container.Composite(new qx.ui.layout.HBox(4)).set({ anonymous: true });
				for (var i = 0; i < cells.length; i++) {
					var c = cells[i];
					var cell = this.__label(c.value === undefined ? "—" : this.__format(c.value), "cell", {
						width: 54, allowGrowX: false, textAlign: "center", textColor: this.__css(this.__stateColor(c.state))
					});
					this.__pill(cell, this.__stateColor(c.state), c.state === "none" ? 0 : c.state === "warn" ? 0.16 : 0.13, 5);
					tests.add(cell);
				}
				row.add(tests);
			}

			var tone = lot.status === "released" ? this.getThemePositiveColor()
				: lot.status === "hold" ? this.getThemeWarningColor()
				: lot.status === "fail" ? this.getThemeNegativeColor()
				: "grayText";
			var status = this.__label(({ released: "Released", hold: "Hold", fail: "Fail", pending: "Pending" })[lot.status] || "Pending", "status", {
				width: 70, allowGrowX: false, textAlign: "center", textColor: this.__css(tone)
			});
			this.__pill(status, tone, 0.14, 10);
			row.add(status);

			if (!this.__tight)
				row.add(this.__coa(lot.coaIssued));

			return row;
		},

		/**
		 * The CoA flag: a document with a tick when issued, a dashed one when not.
		 */
		__coa: function (issued) {

			var color = this.__css(issued ? this.getThemeAccentColor() : "windowFrame");
			var path = issued
				? "<path d='M4 1.75h5.5L13 5.25v9H4z'/><path d='M9.5 1.75v3.5H13'/><path d='M6 10l1.6 1.6L10.8 8.2'/>"
				: "<path d='M4 1.75h5.5L13 5.25v9H4z' stroke-dasharray='2 2'/>";
			var svg = "<svg xmlns='http://www.w3.org/2000/svg' width='16' height='16' viewBox='0 0 16 16' fill='none' stroke='" + color + "' stroke-width='1.5' stroke-linecap='round' stroke-linejoin='round'>" + path + "</svg>";
			return new qx.ui.basic.Label(svg).set({
				rich: true, width: 20, allowGrowX: false, textAlign: "center", anonymous: true, toolTipText: issued ? "CoA issued" : "CoA not issued"
			});
		},

		/**
		 * The detail of the selected lot: each test's value against its limits.
		 */
		__createDetail: function (lot, specs) {

			var cells = this.__cells(lot, specs);
			var detail = new qx.ui.container.Composite(new qx.ui.layout.VBox(3)).set({ appearance: "quality-lot-board/detail" });

			var head = new qx.ui.container.Composite(new qx.ui.layout.HBox(8)).set({ anonymous: true });
			head.add(this.__label(lot.lot + "  ·  " + [lot.grade, lot.vessel].filter(function (s) { return !!s; }).join(" · "), "detail-title", { wrap: false, allowShrinkX: true, minWidth: 0 }), { flex: 1 });
			head.add(this.__label(lot.coaIssued ? "CoA issued" : "CoA not issued", "detail-meta"));
			detail.add(head);

			var grid = new qx.ui.container.Composite(new qx.ui.layout.Grid(8, 3)).set({ anonymous: true });
			grid.getLayout().setColumnFlex(0, 1);
			grid.getLayout().setColumnMinWidth(1, this.__tight ? 80 : 90);
			grid.getLayout().setColumnMinWidth(2, this.__tight ? 100 : 120);
			for (var i = 0; i < cells.length; i++) {
				var c = cells[i];
				var color = this.__css(this.__stateColor(c.state));
				grid.add(this.__label(c.spec.test, "detail-meta"), { row: i, column: 0 });
				grid.add(this.__label(c.value === undefined ? "not tested" : this.__format(c.value) + " " + (c.spec.unit || ""), "detail-value", { textColor: color }), { row: i, column: 1 });
				grid.add(this.__label(this.__specText(c.spec), "detail-meta"), { row: i, column: 2 });
				var mark = new qx.ui.core.Widget().set({ width: 8, height: 8, allowGrowX: false, allowGrowY: false, alignY: "middle", anonymous: true });
				this.__pill(mark, this.__stateColor(c.state), 1, 4);
				grid.add(mark, { row: i, column: 3 });
			}
			detail.add(grid);

			return detail;
		},

		/**
		 * A chip: report it; the server sets the filter and renders it back.
		 */
		_onChipTap: function (e) {

			var status = e.getCurrentTarget().getUserData("status");
			if (this.isEnabled() && status !== this.getStatusFilter())
				this.fireDataEvent("statusFilterChanged", { status: status, statusFilter: status });
		},

		/**
		 * A lot: select it, or clear the selection when it already is.
		 */
		_onRowTap: function (e) {

			if (!this.isEnabled())
				return;

			var lot = e.getCurrentTarget().getUserData("lot");
			var next = lot === this.getSelected() ? "" : lot;
			this.fireDataEvent("lotSelected", { lot: next, selected: next });
		}
	},

	/**
	 * Drops the theme listener; the child widgets are disposed by the container.
	 */
	destruct: function () {

		qx.theme.manager.Color.getInstance().removeListener("changeTheme", this._applyBoard, this);
		this.__summary = this.__chips = this.__head = this.__rows = null;
	}
});
