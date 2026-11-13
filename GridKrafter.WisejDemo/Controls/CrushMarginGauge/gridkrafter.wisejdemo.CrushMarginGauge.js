/**
 * gridkrafter.wisejdemo.CrushMarginGauge
 *
 * The client half of GridKrafter.WisejDemo.CrushMarginGauge: the margin with its unit and change,
 * a 7D / 30D / 90D switch, the trend line inside its high-low band with a dashed average (drawn as
 * SVG), the High / Low / Avg legend, and the breakdown rows: revenues to the right of a centre line,
 * costs to the left. The server owns every value; the browser only reports what the user picks.
 */
qx.Class.define("gridkrafter.wisejdemo.CrushMarginGauge", {

	extend: wisej.web.Control,

	/**
	 * Builds the widget tree once: the header, the chart, the legend and the breakdown column.
	 */
	construct: function () {

		this.base(arguments, new qx.ui.layout.VBox(8));

		this.setFocusable(false);

		// header: the headline on the left, the range switch on the right
		var top = new qx.ui.container.Composite(new qx.ui.layout.HBox(8));
		var headline = new qx.ui.container.Composite(new qx.ui.layout.VBox(0));
		this.__title = new qx.ui.basic.Label().set({ appearance: "crush-margin-gauge/label", rich: false });
		headline.add(this.__title);

		var valueRow = new qx.ui.container.Composite(new qx.ui.layout.HBox(6).set({ alignY: "bottom" }));
		this.__value = new qx.ui.basic.Label().set({ appearance: "crush-margin-gauge/value", rich: false });
		this.__unit = new qx.ui.basic.Label().set({ appearance: "crush-margin-gauge/unit", rich: false, marginBottom: 6 });
		this.__change = new qx.ui.basic.Label().set({ appearance: "crush-margin-gauge/change", rich: false, marginBottom: 6 });
		valueRow.add(this.__value);
		valueRow.add(this.__unit);
		valueRow.add(this.__change);
		headline.add(valueRow);
		top.add(headline, { flex: 1 });

		this.__ranges = new qx.ui.container.Composite(new qx.ui.layout.HBox(2)).set({ appearance: "crush-margin-gauge/ranges", alignY: "top" });
		var ranges = gridkrafter.wisejdemo.CrushMarginGauge.RANGES;
		for (var i = 0; i < ranges.length; i++) {
			var range = new qx.ui.basic.Label(ranges[i]).set({ appearance: "crush-margin-gauge/range", rich: false, cursor: "pointer" });
			range.setUserData("range", ranges[i]);
			range.getContentElement().setAttribute("role", "button");
			range.addListener("tap", this._onRangeTap, this);
			range.addListener("pointerover", function (e) { e.getCurrentTarget().addState("hovered"); });
			range.addListener("pointerout", function (e) { e.getCurrentTarget().removeState("hovered"); });
			this.__ranges.add(range);
		}
		top.add(this.__ranges);
		this.add(top);

		// the trend: only what no widget draws is markup
		this.__chart = new qx.ui.embed.Html().set({ appearance: "crush-margin-gauge/chart", minHeight: 70, overflowX: "hidden", overflowY: "hidden" });
		this.add(this.__chart, { flex: 1 });

		this.__legend = new qx.ui.basic.Label().set({ appearance: "crush-margin-gauge/legend", rich: true });
		this.add(this.__legend);

		this.__breakdown = new qx.ui.container.Composite(new qx.ui.layout.VBox(3)).set({ appearance: "crush-margin-gauge/breakdown" });
		this.add(this.__breakdown);

		qx.theme.manager.Color.getInstance().addListener("changeTheme", this._applyAll, this);
		this.__chart.addListener("resize", this._applyChart, this);

		this._applyAll();
	},

	statics: {

		/**
		 * The ranges the switch offers, as the server names them.
		 */
		RANGES: ["7D", "30D", "90D"],

		/**
		 * The number of days of each range.
		 */
		DAYS: { "7D": 7, "30D": 30, "90D": 90 },

		/**
		 * Formats a margin as $0.00, with a true minus sign when it is negative.
		 *
		 * @param {Number} v the amount.
		 * @return {String} the text.
		 */
		money: function (v) {
			v = +v || 0;
			return (v < 0 ? "−$" : "$") + Math.abs(v).toFixed(2);
		}
	},

	events: {

		/**
		 * The user picked a range: { range: "7D" | "30D" | "90D" }. The server sets it and renders it back.
		 */
		rangeChanged: "qx.event.type.Data",

		/**
		 * The user clicked a breakdown row: { name }.
		 */
		componentClicked: "qx.event.type.Data"
	},

	properties: {

		/**
		 * The theme appearance of the root widget.
		 */
		appearance: { init: "crush-margin-gauge", refine: true },

		/**
		 * The label above the margin.
		 */
		title: { init: "Crush margin", check: "String", apply: "_applyHeadline" },

		/**
		 * Today's crush margin.
		 */
		value: { init: 0.67, check: "Number", apply: "_applyHeadline" },

		/**
		 * The change since the previous close; its sign picks the positive or negative colour.
		 */
		change: { init: 0.04, check: "Number", apply: "_applyHeadline" },

		/**
		 * The unit shown after the margin.
		 */
		unit: { init: "/gal", check: "String", apply: "_applyHeadline" },

		/**
		 * The days of history the trend shows.
		 */
		range: { init: "30D", check: ["7D", "30D", "90D"], apply: "_applyRange" },

		/**
		 * Whether the breakdown shows.
		 */
		showBreakdown: { init: true, check: "Boolean", apply: "_applyBreakdown" },

		/**
		 * The daily margins, oldest first: [{ date, value }].
		 */
		history: { init: null, nullable: true, check: "Array", apply: "_applyChart" },

		/**
		 * The breakdown: [{ name, value }], revenues positive, costs negative.
		 */
		breakdown: { init: null, nullable: true, check: "Array", apply: "_applyBreakdown" },

		/**
		 * A server override of the positive colour; null means the theme's.
		 */
		positiveColor: { init: null, nullable: true, check: "Color", apply: "_applyAll" },

		/**
		 * A server override of the negative colour; null means the theme's.
		 */
		negativeColor: { init: null, nullable: true, check: "Color", apply: "_applyAll" },

		/**
		 * The theme's positive colour (revenues, a rising margin), set by the mixin.
		 */
		themePositiveColor: { init: "#1fb57a", check: "Color", themeable: true, apply: "_applyAll" },

		/**
		 * The theme's negative colour (costs, a falling margin), set by the mixin.
		 */
		themeNegativeColor: { init: "#e5484d", check: "Color", themeable: true, apply: "_applyAll" },

		/**
		 * The theme's colour of the trend line, its band and the selected range, set by the mixin.
		 */
		themeAccentColor: { init: "#6d4dff", check: "Color", themeable: true, apply: "_applyAll" }
	},

	members: {

		__title: null,
		__value: null,
		__unit: null,
		__change: null,
		__ranges: null,
		__chart: null,
		__legend: null,
		__breakdown: null,

		/**
		 * Resolves a theme colour name or a CSS colour to CSS.
		 */
		__css: function (color) {

			var manager = qx.theme.manager.Color.getInstance();
			return manager.isDynamic(color) ? manager.resolve(color) : color;
		},

		/**
		 * The colour at the given opacity, as rgba.
		 */
		__tint: function (color, alpha) {

			try {
				var rgb = qx.util.ColorUtil.stringToRgb(this.__css(color));
				return qx.util.ColorUtil.rgbToRgbString([rgb[0], rgb[1], rgb[2], alpha]);
			}
			catch (ex) {
				return color;
			}
		},

		__positive: function () { return this.__css(this.getPositiveColor() || this.getThemePositiveColor()); },
		__negative: function () { return this.__css(this.getNegativeColor() || this.getThemeNegativeColor()); },
		__accent: function () { return this.__css(this.getThemeAccentColor()); },

		/**
		 * Draws everything that depends on the colours.
		 */
		_applyAll: function () {

			if (!this.__breakdown)
				return;

			this._applyHeadline();
			this._applyRange();
			this._applyBreakdown();
		},

		/**
		 * Shows the title, the margin, its unit and the change badge.
		 */
		_applyHeadline: function () {

			if (!this.__change)
				return;

			var money = gridkrafter.wisejdemo.CrushMarginGauge.money;
			this.__title.setValue(this.getTitle() || "Crush margin");
			this.__value.setValue(money(this.getValue()));
			this.__unit.setValue(this.getUnit() || "");

			var change = +this.getChange() || 0;
			var color = change >= 0 ? this.__positive() : this.__negative();
			this.__change.set({
				value: (change >= 0 ? "▲ " : "▼ ") + Math.abs(change).toFixed(2),
				textColor: color,
				backgroundColor: this.__tint(color, 0.14)
			});
		},

		/**
		 * Marks the selected range and draws the trend for it.
		 */
		_applyRange: function () {

			if (!this.__ranges)
				return;

			var selected = this.getRange();
			var children = this.__ranges.getChildren();
			for (var i = 0; i < children.length; i++) {
				var on = children[i].getUserData("range") === selected;
				if (on)
					children[i].addState("selected");
				else
					children[i].removeState("selected");
				children[i].setTextColor(on ? this.__accent() : null);
				children[i].getContentElement().setAttribute("aria-pressed", on ? "true" : "false");
			}

			this._applyChart();
		},

		/**
		 * Draws the trend line, its high-low band and its average, and the legend under it.
		 */
		_applyChart: function () {

			if (!this.__legend)
				return;

			var Self = gridkrafter.wisejdemo.CrushMarginGauge;
			var days = Self.DAYS[this.getRange()] || 30;
			var rows = this.getHistory() || [];
			var pts = [];
			for (var i = 0; i < rows.length; i++) {
				var v = parseFloat(rows[i] && rows[i].value);
				if (!isNaN(v))
					pts.push(v);
			}
			pts = pts.slice(-days);

			if (pts.length < 2) {
				this.__chart.setHtml("<div style='padding:20px 0;text-align:center;color:" + this.__css("grayText") + ";font-size:12px'>No history</div>");
				this.__legend.setValue("");
				return;
			}

			var accent = this.__accent();
			var W = 400, H = 100, pad = 6;
			var lo = Math.min.apply(null, pts), hi = Math.max.apply(null, pts);
			var avg = 0;
			for (var i = 0; i < pts.length; i++)
				avg += pts[i];
			avg /= pts.length;
			var min = lo - (hi - lo) * 0.15, max = hi + (hi - lo) * 0.15;
			if (max === min) { min -= 1; max += 1; }
			var x = function (i) { return pad + i * (W - 2 * pad) / (pts.length - 1); };
			var y = function (v) { return H - pad - (v - min) * (H - 2 * pad) / (max - min); };

			var d = "";
			for (var i = 0; i < pts.length; i++)
				d += (i ? " L" : "M") + x(i).toFixed(1) + " " + y(pts[i]).toFixed(1);
			var last = pts.length - 1;
			var gradient = "cmg" + this.$$hash;

			this.__chart.setHtml(
				"<svg viewBox='0 0 " + W + " " + H + "' preserveAspectRatio='none' style='display:block;width:100%;height:100%'>" +
				"<defs><linearGradient id='" + gradient + "' x1='0' y1='0' x2='0' y2='1'>" +
				"<stop offset='0' stop-color='" + accent + "' stop-opacity='.22'/><stop offset='1' stop-color='" + accent + "' stop-opacity='0'/></linearGradient></defs>" +
				"<rect x='0' y='" + y(hi).toFixed(1) + "' width='" + W + "' height='" + (y(lo) - y(hi)).toFixed(1) + "' fill='" + accent + "' fill-opacity='.1'/>" +
				"<line x1='0' x2='" + W + "' y1='" + y(avg).toFixed(1) + "' y2='" + y(avg).toFixed(1) + "' stroke='" + this.__css("grayText") + "' stroke-width='1' stroke-dasharray='3 3' opacity='.7' vector-effect='non-scaling-stroke'/>" +
				"<path d='" + d + " L" + x(last).toFixed(1) + " " + H + " L" + x(0).toFixed(1) + " " + H + "Z' fill='url(#" + gradient + ")'/>" +
				"<path d='" + d + "' fill='none' stroke='" + accent + "' stroke-width='2' stroke-linejoin='round' stroke-linecap='round' vector-effect='non-scaling-stroke'/>" +
				"<circle cx='" + x(last).toFixed(1) + "' cy='" + y(pts[last]).toFixed(1) + "' r='3.5' fill='" + this.__css("window") + "' stroke='" + accent + "' stroke-width='2' vector-effect='non-scaling-stroke'/>" +
				"</svg>");

			var strong = "<b style='color:" + this.__css("windowText") + ";font-weight:600'>";
			this.__legend.setValue(
				"High " + strong + Self.money(hi) + "</b>&nbsp;&nbsp;&nbsp;&nbsp;" +
				"Low " + strong + Self.money(lo) + "</b>&nbsp;&nbsp;&nbsp;&nbsp;" +
				"Avg " + strong + Self.money(avg) + "</b>");
		},

		/**
		 * Draws the breakdown rows: a name, a bar from the centre line (right for revenues, left for
		 * costs, scaled to the largest amount) and the amount.
		 */
		_applyBreakdown: function () {

			if (!this.__breakdown)
				return;

			var old = this.__breakdown.removeAll();
			for (var i = 0; i < old.length; i++)
				old[i].dispose();

			var items = this.getBreakdown() || [];
			var show = this.getShowBreakdown() && items.length > 0;
			this.__breakdown.setVisibility(show ? "visible" : "excluded");
			if (!show)
				return;

			var money = gridkrafter.wisejdemo.CrushMarginGauge.money;
			var positive = this.__positive(), negative = this.__negative();
			var peak = 0.01;
			for (var i = 0; i < items.length; i++)
				peak = Math.max(peak, Math.abs(parseFloat(items[i].value) || 0));

			for (var i = 0; i < items.length; i++) {

				var item = items[i];
				var v = parseFloat(item.value) || 0, cost = v < 0;
				var width = Math.round(Math.abs(v) / peak * 50 * 10) / 10;

				var row = new qx.ui.container.Composite(new qx.ui.layout.HBox(8).set({ alignY: "middle" })).set({ appearance: "crush-margin-gauge/item", cursor: "pointer" });
				row.setUserData("name", item.name || "");
				row.getContentElement().setAttribute("role", "button");

				row.add(new qx.ui.basic.Label(item.name || "").set({ appearance: "crush-margin-gauge/name", rich: false, width: 78, anonymous: true }));

				var track = new qx.ui.container.Composite(new qx.ui.layout.Canvas()).set({ height: 8, allowGrowY: false, anonymous: true });
				track.add(new qx.ui.core.Widget().set({ appearance: "crush-margin-gauge/axis", anonymous: true }), { left: "50%", top: 0, bottom: 0, width: 1 });
				var bar = new qx.ui.core.Widget().set({ appearance: "crush-margin-gauge/bar", backgroundColor: cost ? negative : positive, anonymous: true });
				track.add(bar, cost ? { right: "50%", top: 0, bottom: 0, width: width + "%" } : { left: "50%", top: 0, bottom: 0, width: width + "%" });
				row.add(track, { flex: 1 });

				var amount = new qx.ui.basic.Label(money(v)).set({ appearance: "crush-margin-gauge/amount", rich: false, width: 58, textAlign: "right", anonymous: true });
				if (cost)
					amount.setTextColor(negative);
				row.add(amount);

				row.addListener("tap", this._onItemTap, this);
				row.addListener("pointerover", function (e) { e.getCurrentTarget().addState("hovered"); });
				row.addListener("pointerout", function (e) { e.getCurrentTarget().removeState("hovered"); });
				this.__breakdown.add(row);
			}
		},

		/**
		 * A range was tapped: report it; the server sets it and renders it back.
		 */
		_onRangeTap: function (e) {

			var range = e.getCurrentTarget().getUserData("range");
			if (this.isEnabled() && range !== this.getRange())
				this.fireDataEvent("rangeChanged", { range: range });
		},

		/**
		 * A breakdown row was tapped: report its name.
		 */
		_onItemTap: function (e) {

			if (this.isEnabled())
				this.fireDataEvent("componentClicked", { name: e.getCurrentTarget().getUserData("name") });
		}
	},

	/**
	 * Drops the theme listener; the child widgets are disposed by the container.
	 */
	destruct: function () {

		qx.theme.manager.Color.getInstance().removeListener("changeTheme", this._applyAll, this);
		this.__title = this.__value = this.__unit = this.__change = this.__ranges = this.__chart = this.__legend = this.__breakdown = null;
	}
});
