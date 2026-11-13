/**
 * gridkrafter.wisejdemo.CarbonCreditTracker
 *
 * The client half of GridKrafter.WisejDemo.CarbonCreditTracker: totals (credits held at market, uplift
 * per gallon, RVO coverage) and a table of credits: program chip and name, price with its change, a
 * trend line (a small SVG), credits held, their value and the cents a gallon earns. Everything except
 * the trend lines is native widgets. The server owns every value; the browser only reports a selection.
 */
qx.Class.define("gridkrafter.wisejdemo.CarbonCreditTracker", {

	extend: wisej.web.Control,

	/**
	 * Builds the totals and the table header once; the rows are drawn from the data.
	 */
	construct: function () {

		this.base(arguments, new qx.ui.layout.VBox(10));

		this.setFocusable(false);

		var totals = new qx.ui.container.Composite(new qx.ui.layout.HBox(22));
		this.__totals = {};
		this.__totalLabels = {};
		var list = [["value", "Credits held, at market"], ["uplift", "Uplift per gallon"], ["rvo", "RVO coverage"]];
		for (var i = 0; i < list.length; i++) {
			var box = new qx.ui.container.Composite(new qx.ui.layout.VBox(0));
			var label = new qx.ui.basic.Label(list[i][1]).set({ appearance: "carbon-credit-tracker/total-label", rich: false });
			box.add(label);
			var value = new qx.ui.basic.Label("0").set({ appearance: "carbon-credit-tracker/total-value", rich: true });
			box.add(value);
			this.__totals[list[i][0]] = value;
			this.__totalLabels[list[i][0]] = label;
			box.setUserData("key", list[i][0]);
			totals.add(box);
		}
		this.__totalBoxes = totals.getChildren();
		this.add(totals);

		var table = new qx.ui.container.Composite(new qx.ui.layout.VBox(0)).set({ appearance: "carbon-credit-tracker/table" });
		this.__colhead = new qx.ui.container.Composite(new qx.ui.layout.HBox(10)).set({ appearance: "carbon-credit-tracker/colhead" });
		var Self = gridkrafter.wisejdemo.CarbonCreditTracker;
		var heads = ["CREDIT", "PRICE", "30-DAY TREND", "HELD", "VALUE", "¢/GAL"];
		for (var i = 0; i < heads.length; i++) {
			var col = Self.COLUMNS[i];
			this.__colhead.add(new qx.ui.basic.Label(heads[i]).set({ appearance: "carbon-credit-tracker/column", rich: false, width: col.width || null, textAlign: col.right ? "right" : "left", allowGrowX: true }), col.flex ? { flex: col.flex } : null);
		}
		table.add(this.__colhead);

		this.__rows = new qx.ui.container.Composite(new qx.ui.layout.VBox(0));
		table.add(new qx.ui.container.Scroll(this.__rows).set({ scrollbarX: "off" }), { flex: 1 });
		this.add(table, { flex: 1 });

		qx.theme.manager.Color.getInstance().addListener("changeTheme", this._applyCredits, this);

		this._applyCredits();
	},

	statics: {

		/**
		 * The table's columns: credit, price, trend, held, value, cents a gallon.
		 */
		COLUMNS: [{ flex: 14 }, { width: 96, right: true }, { flex: 10 }, { width: 72, right: true }, { width: 80, right: true }, { width: 52, right: true }],

		/**
		 * The theme property that colours each program's chip.
		 */
		PROGRAMS: { RFS: "RfsColor", LCFS: "LcfsColor", CFP: "CfpColor", CFS: "CfsColor" }
	},

	events: {

		/**
		 * The user selected a credit: { id, selected }.
		 */
		creditSelected: "qx.event.type.Data"
	},

	properties: {

		/**
		 * The theme appearance of the root widget.
		 */
		appearance: { init: "carbon-credit-tracker", refine: true },

		/**
		 * The id of the selected credit.
		 */
		selected: { init: "", check: "String", apply: "_applyCredits" },

		/**
		 * The renewable volume obligation in RINs; 0 hides the coverage.
		 */
		rvo: { init: 0, check: "Number", apply: "_applyCredits" },

		/**
		 * The credits: [{ id, name, program, price, unit, change, held, perGallon, history }].
		 */
		credits: { init: null, nullable: true, check: "Array", apply: "_applyCredits" },

		/**
		 * The theme's colours: rise, fall, and each program's chip. Set by the mixin.
		 */
		themeUpColor: { init: "#1fb57a", check: "Color", themeable: true, apply: "_applyCredits" },
		themeDownColor: { init: "#e5484d", check: "Color", themeable: true, apply: "_applyCredits" },
		themeRfsColor: { init: "#6d4dff", check: "Color", themeable: true, apply: "_applyCredits" },
		themeLcfsColor: { init: "#0ea5e9", check: "Color", themeable: true, apply: "_applyCredits" },
		themeCfpColor: { init: "#1fb57a", check: "Color", themeable: true, apply: "_applyCredits" },
		themeCfsColor: { init: "#f2a516", check: "Color", themeable: true, apply: "_applyCredits" }
	},

	members: {

		__totals: null,
		__totalLabels: null,
		__totalBoxes: null,
		__colhead: null,
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
		 * The colour at the given opacity.
		 */
		__tint: function (color, alpha) {

			try {
				var rgb = qx.util.ColorUtil.stringToRgb(color);
				return qx.util.ColorUtil.rgbToRgbString([rgb[0], rgb[1], rgb[2], alpha]);
			}
			catch (ex) {
				return null;
			}
		},

		/**
		 * Dollars, short: $6.02M, $470k, $189.
		 */
		__money: function (v) {

			return v >= 1e6 ? "$" + (v / 1e6).toFixed(2) + "M" : v >= 1e3 ? "$" + Math.round(v / 1e3) + "k" : "$" + Math.round(v);
		},

		/**
		 * A count, short: 3.15M, 420k, 950.
		 */
		__quantity: function (v) {

			return v >= 1e6 ? (v / 1e6).toFixed(2) + "M" : v >= 1e3 ? Math.round(v / 1e3) + "k" : String(Math.round(v));
		},

		/**
		 * A credit's value at market, in dollars.
		 */
		__value: function (c) {

			return (c.held || 0) * (c.price || 0) / (c.unit === "¢" ? 100 : 1);
		},

		/**
		 * Every property draws the widget again, once per round of changes.
		 */
		_applyCredits: function () {

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
		 * The totals and the rows.
		 */
		__draw: function () {

			var credits = this.getCredits() || [];
			var gray = this.__css("grayText");
			var small = function (text) { return "<span style='font-size:12px;font-weight:500;color:" + gray + ";margin-left:3px'>" + text + "</span>"; };

			// totals
			var value = 0, rin = null, selected = null;
			for (var i = 0; i < credits.length; i++) {
				value += this.__value(credits[i]);
				if (credits[i].id === "d6")
					rin = credits[i];
				if (credits[i].id === this.getSelected())
					selected = credits[i];
			}
			var uplift = (rin ? rin.perGallon || 0 : 0) + (selected && selected !== rin ? selected.perGallon || 0 : 0);
			this.__totals.value.setValue(this.__money(value));
			this.__totalLabels.uplift.setValue("Uplift per gallon" + (selected && selected !== rin ? " (D6 + " + selected.name + ")" : ""));
			this.__totals.uplift.setValue(uplift.toFixed(1) + small("¢/gal"));
			this.__totals.uplift.setTextColor(this.__css(this.getThemeUpColor()));

			var rvo = this.getRvo();
			this.__totalBoxes[2].setVisibility(rvo > 0 ? "visible" : "excluded");
			if (rvo > 0)
				this.__totals.rvo.setValue(Math.round((rin ? rin.held || 0 : 0) / rvo * 100) + small("%"));

			// rows
			var old = this.__rows.removeAll();
			for (var i = 0; i < old.length; i++)
				old[i].dispose();

			this.__colhead.setVisibility(credits.length ? "visible" : "excluded");
			if (!credits.length) {
				this.__rows.add(new qx.ui.basic.Label("No credits").set({ appearance: "carbon-credit-tracker/empty", rich: false, allowGrowX: true, textAlign: "center" }));
				return;
			}
			for (var i = 0; i < credits.length; i++)
				this.__rows.add(this.__createRow(credits[i]));
		},

		/**
		 * One credit: chip and name, price and change, trend, held, value, cents a gallon.
		 */
		__createRow: function (c) {

			var Self = gridkrafter.wisejdemo.CarbonCreditTracker;
			var cols = Self.COLUMNS;
			var up = this.__css(this.getThemeUpColor()), down = this.__css(this.getThemeDownColor());
			var change = c.change || 0;
			var changeColor = change > 0 ? up : change < 0 ? down : this.__css("grayText");
			var property = Self.PROGRAMS[c.program];
			var chipColor = this.__css(property ? this["getTheme" + property]() : "highlight");

			var row = new qx.ui.container.Composite(new qx.ui.layout.HBox(10).set({ alignY: "middle" })).set({ appearance: "carbon-credit-tracker/row", cursor: "pointer" });
			row.setUserData("id", c.id);
			row.getContentElement().setAttribute("role", "button");
			if (c.id === this.getSelected())
				row.addState("selected");
			row.addListener("pointerover", function () { row.addState("hovered"); });
			row.addListener("pointerout", function () { row.removeState("hovered"); });
			row.addListener("tap", this._onRowTap, this);

			var name = new qx.ui.container.Composite(new qx.ui.layout.HBox(7).set({ alignY: "middle" })).set({ minWidth: 0, anonymous: true });
			if (c.program)
				name.add(new qx.ui.basic.Label(c.program).set({ appearance: "carbon-credit-tracker/program", textColor: chipColor, backgroundColor: this.__tint(chipColor, 0.14), rich: false, anonymous: true }));
			name.add(new qx.ui.basic.Label(c.name || c.id).set({ appearance: "carbon-credit-tracker/name", rich: false, allowShrinkX: true, anonymous: true }), { flex: 1 });
			row.add(name, { flex: cols[0].flex });

			var price = new qx.ui.container.Composite(new qx.ui.layout.VBox(0).set({ alignX: "right" })).set({ width: cols[1].width, anonymous: true });
			price.add(new qx.ui.basic.Label(c.unit === "¢" ? (c.price || 0).toFixed(2) + "¢" : "$" + (c.price || 0).toFixed(2)).set({ appearance: "carbon-credit-tracker/price", rich: false, anonymous: true }));
			price.add(new qx.ui.basic.Label((change > 0 ? "▲ " : change < 0 ? "▼ " : "■ ") + Math.abs(change).toFixed(2)).set({ appearance: "carbon-credit-tracker/change", textColor: changeColor, rich: false, anonymous: true }));
			row.add(price);

			// trend line: only what no widget draws is markup
			var spark = new qx.ui.embed.Html().set({ height: 24, allowGrowY: false, anonymous: true });
			var history = c.history || [];
			if (history.length > 1) {
				var lo = Math.min.apply(null, history), hi = Math.max.apply(null, history), span = hi - lo || 1;
				var points = [];
				for (var i = 0; i < history.length; i++)
					points.push((i / (history.length - 1) * 100).toFixed(1) + "," + (22 - (history[i] - lo) / span * 20).toFixed(1));
				spark.setHtml("<svg viewBox='0 0 100 24' preserveAspectRatio='none' style='display:block;width:100%;height:100%'>" +
					"<polyline points='" + points.join(" ") + "' fill='none' stroke='" + (history[history.length - 1] >= history[0] ? up : down) +
					"' stroke-width='1.6' stroke-linejoin='round' vector-effect='non-scaling-stroke'/></svg>");
			}
			row.add(spark, { flex: cols[2].flex });

			row.add(new qx.ui.basic.Label(this.__quantity(c.held || 0)).set({ appearance: "carbon-credit-tracker/held", rich: false, width: cols[3].width, textAlign: "right", anonymous: true }));
			row.add(new qx.ui.basic.Label(this.__money(this.__value(c))).set({ appearance: "carbon-credit-tracker/value", rich: false, width: cols[4].width, textAlign: "right", anonymous: true }));
			row.add(new qx.ui.basic.Label((c.perGallon || 0).toFixed(1)).set({ appearance: "carbon-credit-tracker/uplift", textColor: up, rich: false, width: cols[5].width, textAlign: "right", anonymous: true }));

			return row;
		},

		/**
		 * A row: select its credit.
		 */
		_onRowTap: function (e) {

			var id = e.getCurrentTarget().getUserData("id");
			if (this.isEnabled() && id !== this.getSelected())
				this.fireDataEvent("creditSelected", { id: id, selected: id });
		}
	},

	/**
	 * Drops the theme listener; the child widgets are disposed by the container.
	 */
	destruct: function () {

		qx.theme.manager.Color.getInstance().removeListener("changeTheme", this._applyCredits, this);
		this.__totals = this.__totalLabels = this.__totalBoxes = this.__colhead = this.__rows = null;
	}
});
