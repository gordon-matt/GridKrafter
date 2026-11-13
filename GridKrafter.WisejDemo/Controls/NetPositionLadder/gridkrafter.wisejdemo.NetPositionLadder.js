/**
 * gridkrafter.wisejdemo.NetPositionLadder
 *
 * The client half of GridKrafter.WisejDemo.NetPositionLadder: totals (net open position, physical net,
 * futures hedge, limit breaches), a Gross / Net switch, a legend, and one row per delivery month whose
 * bars diverge from a centre zero line (short to the left, long to the right) on one scale for every
 * month. Gross: purchases and sales on top, futures below, a tick at the net. Net: one bar per month
 * with dashed limit lines. The server owns every value; the browser only reports what the user picks.
 */
qx.Class.define("gridkrafter.wisejdemo.NetPositionLadder", {

	extend: wisej.web.Control,

	/**
	 * Builds the header, the legend and the ladder once; the rows are drawn from the data.
	 */
	construct: function () {

		this.base(arguments, new qx.ui.layout.VBox(8));

		this.setFocusable(false);

		var head = new qx.ui.container.Composite(new qx.ui.layout.HBox(8));
		var totals = new qx.ui.container.Composite(new qx.ui.layout.HBox(20));
		this.__totals = {};
		var list = [["net", "Net open position"], ["physical", "Physical net"], ["futures", "Futures hedge"], ["breaches", "Limit breaches"]];
		for (var i = 0; i < list.length; i++) {
			var box = new qx.ui.container.Composite(new qx.ui.layout.VBox(0));
			box.add(new qx.ui.basic.Label(list[i][1]).set({ appearance: "net-position-ladder/total-label", rich: false }));
			var value = new qx.ui.basic.Label("0").set({ appearance: "net-position-ladder/total-value", rich: true });
			box.add(value);
			this.__totals[list[i][0]] = value;
			totals.add(box);
		}
		head.add(totals, { flex: 1 });

		this.__views = new qx.ui.container.Composite(new qx.ui.layout.HBox(2)).set({ appearance: "net-position-ladder/views", alignY: "top" });
		var views = [["gross", "Gross"], ["net", "Net"]];
		for (var i = 0; i < views.length; i++) {
			var view = new qx.ui.basic.Label(views[i][1]).set({ appearance: "net-position-ladder/view", rich: false, cursor: "pointer" });
			view.setUserData("view", views[i][0]);
			view.getContentElement().setAttribute("role", "button");
			view.addListener("tap", this._onViewTap, this);
			this.__hover(view);
			this.__views.add(view);
		}
		head.add(this.__views);
		this.add(head);

		this.__legend = new qx.ui.container.Composite(new qx.ui.layout.HBox(14));
		this.add(this.__legend);

		var ladder = new qx.ui.container.Composite(new qx.ui.layout.VBox(0)).set({ appearance: "net-position-ladder/ladder" });
		var colhead = new qx.ui.container.Composite(new qx.ui.layout.HBox(10)).set({ appearance: "net-position-ladder/colhead" });
		colhead.add(new qx.ui.basic.Label("MONTH").set({ appearance: "net-position-ladder/column", rich: false, width: 58 }));
		colhead.add(new qx.ui.basic.Label("SHORT ← → LONG").set({ appearance: "net-position-ladder/column", rich: false, textAlign: "center", allowGrowX: true }), { flex: 1 });
		colhead.add(new qx.ui.basic.Label("NET").set({ appearance: "net-position-ladder/column", rich: false, width: 76, textAlign: "right" }));
		colhead.add(new qx.ui.basic.Label("HEDGED").set({ appearance: "net-position-ladder/column", rich: false, width: 52, textAlign: "right" }));
		ladder.add(colhead);

		this.__rows = new qx.ui.container.Composite(new qx.ui.layout.VBox(0));
		ladder.add(new qx.ui.container.Scroll(this.__rows).set({ scrollbarX: "off" }), { flex: 1 });
		this.add(ladder, { flex: 1 });

		qx.theme.manager.Color.getInstance().addListener("changeTheme", this._applyMonths, this);

		this._applyMonths();
	},

	events: {

		/**
		 * The user picked a view: { view: "gross" | "net" }.
		 */
		viewChanged: "qx.event.type.Data",

		/**
		 * The user selected a delivery month: { month, selected }.
		 */
		monthSelected: "qx.event.type.Data"
	},

	properties: {

		/**
		 * The theme appearance of the root widget.
		 */
		appearance: { init: "net-position-ladder", refine: true },

		/**
		 * The unit the volumes are in.
		 */
		unit: { init: "kgal", check: "String", apply: "_applyMonths" },

		/**
		 * The view: gross or net.
		 */
		view: { init: "gross", check: ["gross", "net"], apply: "_applyMonths" },

		/**
		 * The net position limit per month, either way; 0 for none.
		 */
		limit: { init: 1500, check: "Number", apply: "_applyMonths" },

		/**
		 * Whether the gross view shows the futures bar.
		 */
		showHedges: { init: true, check: "Boolean", apply: "_applyMonths" },

		/**
		 * The selected delivery month.
		 */
		selected: { init: "", check: "String", apply: "_applyMonths" },

		/**
		 * The months: [{ month, physicalLong, physicalShort, futures }].
		 */
		months: { init: null, nullable: true, check: "Array", apply: "_applyMonths" },

		/**
		 * The theme's colours: long (purchases, net long), short (sales, net short), futures, the net tick. Set by the mixin.
		 */
		themeLongColor: { init: "#1fb57a", check: "Color", themeable: true, apply: "_applyMonths" },
		themeShortColor: { init: "#e5484d", check: "Color", themeable: true, apply: "_applyMonths" },
		themeHedgeColor: { init: "#6d4dff", check: "Color", themeable: true, apply: "_applyMonths" },
		themeNetColor: { init: "#171a21", check: "Color", themeable: true, apply: "_applyMonths" }
	},

	members: {

		__totals: null,
		__views: null,
		__legend: null,
		__rows: null,
		__pending: false,

		/**
		 * Resolves a theme colour name or a CSS colour to CSS.
		 */
		__css: function (color) {

			var manager = qx.theme.manager.Color.getInstance();
			return manager.isDynamic(color) ? manager.resolve(color) : color;
		},

		/**
		 * Gives a widget the hovered state while the pointer is over it.
		 */
		__hover: function (widget) {

			widget.addListener("pointerover", function () { widget.addState("hovered"); });
			widget.addListener("pointerout", function () { widget.removeState("hovered"); });
		},

		/**
		 * A signed volume: +1,940, −440 or 0.
		 */
		__format: function (v) {

			return (v > 0 ? "+" : v < 0 ? "−" : "") + Math.abs(Math.round(v)).toLocaleString();
		},

		/**
		 * Every property draws the widget again, once per round of changes.
		 */
		_applyMonths: function () {

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
		 * The totals, the switch, the legend and the rows.
		 */
		__draw: function () {

			var long = this.__css(this.getThemeLongColor()), short = this.__css(this.getThemeShortColor());
			var hedge = this.__css(this.getThemeHedgeColor()), tick = this.__css(this.getThemeNetColor());
			var gray = this.__css("grayText");
			var view = this.getView(), limit = Math.abs(this.getLimit() || 0);
			var showHedges = this.getShowHedges();

			var rows = [];
			var source = this.getMonths() || [];
			for (var i = 0; i < source.length; i++) {
				var m = source[i];
				var L = m.physicalLong || 0, S = m.physicalShort || 0, F = m.futures || 0;
				rows.push({ month: m.month, L: L, S: S, F: F, net: L - S + F, ratio: Math.abs(L - S) ? Math.min(999, Math.abs(F) / Math.abs(L - S) * 100) : 0 });
			}

			// totals
			var net = 0, physical = 0, futures = 0, breaches = 0;
			for (var i = 0; i < rows.length; i++) {
				net += rows[i].net;
				physical += rows[i].L - rows[i].S;
				futures += rows[i].F;
				if (limit && Math.abs(rows[i].net) > limit)
					breaches++;
			}
			var esc = qx.bom.String.escape;
			this.__totals.net.setValue(this.__format(net) + "<span style='font-size:12px;font-weight:500;color:" + gray + ";margin-left:3px'>" + esc(this.getUnit()) + "</span>");
			this.__totals.net.setTextColor(net >= 0 ? long : short);
			this.__totals.physical.setValue(this.__format(physical));
			this.__totals.futures.setValue(this.__format(futures));
			this.__totals.breaches.setValue(String(breaches));
			this.__totals.breaches.setTextColor(breaches ? short : null);

			var views = this.__views.getChildren();
			for (var i = 0; i < views.length; i++) {
				var on = views[i].getUserData("view") === view;
				if (on)
					views[i].addState("selected");
				else
					views[i].removeState("selected");
				views[i].getContentElement().setAttribute("aria-pressed", on ? "true" : "false");
			}

			// legend
			var old = this.__legend.removeAll();
			for (var i = 0; i < old.length; i++)
				old[i].dispose();
			var entries = view === "gross"
				? [[long, "Purchases"], [short, "Sales"]].concat(showHedges ? [[hedge, "Futures"]] : []).concat([[tick, "Net", 3]])
				: [[long, "Net long"], [short, "Net short"]].concat(limit ? [[short, "Limit ±" + limit.toLocaleString(), 10, true]] : []);
			for (var i = 0; i < entries.length; i++)
				this.__legend.add(this.__legendItem(entries[i][0], entries[i][1], entries[i][2] || 10, entries[i][3]));

			// rows
			old = this.__rows.removeAll();
			for (var i = 0; i < old.length; i++)
				old[i].dispose();

			if (!rows.length) {
				this.__rows.add(new qx.ui.basic.Label("No positions").set({ appearance: "net-position-ladder/empty", rich: false, allowGrowX: true, textAlign: "center" }));
				return;
			}

			// one scale for every month: gross uses the largest leg, net the largest net or 115% of the limit
			var scale = 1;
			for (var i = 0; i < rows.length; i++)
				scale = Math.max(scale, view === "gross" ? Math.max(rows[i].L, rows[i].S, Math.abs(rows[i].F)) : Math.abs(rows[i].net));
			if (view === "net")
				scale = Math.max(scale, limit * 1.15);

			for (var i = 0; i < rows.length; i++)
				this.__rows.add(this.__createRow(rows[i], scale, limit, { long: long, short: short, hedge: hedge, tick: tick }));
		},

		/**
		 * A legend entry: a swatch (or a dashed line) and its text.
		 */
		__legendItem: function (color, text, width, dashed) {

			var item = new qx.ui.container.Composite(new qx.ui.layout.HBox(5).set({ alignY: "middle" }));
			var swatch = new qx.ui.core.Widget().set({ appearance: "net-position-ladder/swatch", width: width, height: dashed ? 2 : 8, allowGrowY: false });
			if (dashed)
				swatch.getContentElement().setStyle("borderTop", "1.5px dashed " + color);
			else
				swatch.setBackgroundColor(color);
			item.add(swatch);
			item.add(new qx.ui.basic.Label(text).set({ appearance: "net-position-ladder/legend", rich: false }));
			return item;
		},

		/**
		 * One month: its name, its bars from the centre line, its net and how much is hedged.
		 */
		__createRow: function (r, scale, limit, colors) {

			var view = this.getView();
			var pos = function (v) { return 50 + v / scale * 50; };
			var over = limit && Math.abs(r.net) > limit;

			var row = new qx.ui.container.Composite(new qx.ui.layout.HBox(10).set({ alignY: "middle" })).set({ appearance: "net-position-ladder/row", cursor: "pointer" });
			row.setUserData("month", r.month);
			row.getContentElement().setAttribute("role", "button");
			if (r.month === this.getSelected())
				row.addState("selected");
			this.__hover(row);
			row.addListener("tap", this._onRowTap, this);

			row.add(new qx.ui.basic.Label(r.month).set({ appearance: "net-position-ladder/month", rich: false, width: 58, anonymous: true }));

			var bars = new qx.ui.container.Composite(new qx.ui.layout.Canvas()).set({ height: 26, allowGrowY: false, anonymous: true });
			var span = function (a, b, top, height, color, opacity) {
				var left = Math.min(pos(a), pos(b)), width = Math.abs(pos(b) - pos(a));
				if (width <= 0)
					return;
				bars.add(new qx.ui.core.Widget().set({ appearance: "net-position-ladder/bar", backgroundColor: color, opacity: opacity || 1, anonymous: true }),
					{ left: left + "%", width: width + "%", top: top, height: height });
			};

			bars.add(new qx.ui.core.Widget().set({ appearance: "net-position-ladder/axis", anonymous: true }), { left: "50%", width: 1, top: 0, bottom: 0 });
			if (view === "gross") {
				span(0, r.L, 2, 9, colors.long);
				span(0, -r.S, 2, 9, colors.short);
				if (this.getShowHedges())
					span(0, r.F, 14, 7, colors.hedge, 0.8);
				bars.add(new qx.ui.core.Widget().set({ appearance: "net-position-ladder/tick", backgroundColor: colors.tick, marginLeft: -1, toolTipText: "Net " + this.__format(r.net), anonymous: true }),
					{ left: pos(r.net) + "%", width: 3, top: 0, bottom: 0 });
			}
			else {
				if (limit) {
					var marks = [limit, -limit];
					for (var m = 0; m < marks.length; m++) {
						var line = new qx.ui.core.Widget().set({ width: 2, opacity: 0.7, anonymous: true });
						line.getContentElement().setStyle("borderLeft", "1.5px dashed " + colors.short);
						bars.add(line, { left: pos(marks[m]) + "%", top: 0, bottom: 0 });
					}
				}
				span(0, r.net, 5, 16, r.net >= 0 ? colors.long : colors.short);
			}
			row.add(bars, { flex: 1 });

			var net = new qx.ui.basic.Label(this.__format(r.net)).set({ appearance: "net-position-ladder/net", textColor: r.net >= 0 ? colors.long : colors.short, rich: false, width: 76, textAlign: "right", anonymous: true });
			if (over) {
				net.getContentElement().setStyles({ textDecoration: "underline wavy " + colors.short, textUnderlineOffset: "3px" });
				net.setToolTipText("Over the limit of ±" + limit.toLocaleString());
			}
			row.add(net);
			row.add(new qx.ui.basic.Label(Math.round(r.ratio) + "%").set({ appearance: "net-position-ladder/ratio", rich: false, width: 52, textAlign: "right", anonymous: true }));

			return row;
		},

		/**
		 * A view button: report it; the server sets it and renders it back.
		 */
		_onViewTap: function (e) {

			var view = e.getCurrentTarget().getUserData("view");
			if (this.isEnabled() && view !== this.getView())
				this.fireDataEvent("viewChanged", { view: view });
		},

		/**
		 * A month row: select it.
		 */
		_onRowTap: function (e) {

			var month = e.getCurrentTarget().getUserData("month");
			if (this.isEnabled() && month !== this.getSelected())
				this.fireDataEvent("monthSelected", { month: month, selected: month });
		}
	},

	/**
	 * Drops the theme listener; the child widgets are disposed by the container.
	 */
	destruct: function () {

		qx.theme.manager.Color.getInstance().removeListener("changeTheme", this._applyMonths, this);
		this.__totals = this.__views = this.__legend = this.__rows = null;
	}
});
