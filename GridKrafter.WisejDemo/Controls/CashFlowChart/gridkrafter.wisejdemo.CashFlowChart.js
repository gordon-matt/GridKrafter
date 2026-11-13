/**
 * gridkrafter.wisejdemo.CashFlowChart
 *
 * The client half of GridKrafter.WisejDemo.CashFlowChart: a header with the title, the ending balance and
 * the lowest balance with its week; the 13-week forecast drawn as SVG in real pixels (inflow columns above
 * the zero line, outflow columns below, the net line, the running cash balance on a secondary scale, the
 * dashed minimum-cash line, weeks below the minimum highlighted); a tooltip for the hovered week; and the
 * legend. The server owns every value; the browser only reports the week the user clicks.
 */
qx.Class.define("gridkrafter.wisejdemo.CashFlowChart", {

	extend: wisej.web.Control,

	/**
	 * Builds the widget tree once: the header, the chart with its tooltip, the legend.
	 */
	construct: function () {

		this.base(arguments, new qx.ui.layout.VBox(6));

		this.setFocusable(false);

		// header: the title on the left, the ending and the lowest balance on the right
		var head = new qx.ui.container.Composite(new qx.ui.layout.HBox(18).set({ alignY: "bottom" }));
		this.__title = new qx.ui.basic.Label().set({ appearance: "cash-flow-chart/title", rich: false, minWidth: 20, marginBottom: 2 });
		head.add(this.__title);
		head.add(new qx.ui.core.Spacer(), { flex: 1 });

		var end = new qx.ui.container.Composite(new qx.ui.layout.VBox(0).set({ alignX: "right" }));
		this.__endLabel = new qx.ui.basic.Label("Ending").set({ appearance: "cash-flow-chart/stat-label", rich: false, alignX: "right" });
		this.__endValue = new qx.ui.basic.Label().set({ appearance: "cash-flow-chart/stat-value", rich: false, alignX: "right" });
		end.add(this.__endLabel);
		end.add(this.__endValue);
		head.add(end);

		var low = new qx.ui.container.Composite(new qx.ui.layout.VBox(0).set({ alignX: "right" }));
		this.__lowLabel = new qx.ui.basic.Label("Lowest").set({ appearance: "cash-flow-chart/stat-label", rich: false, alignX: "right" });
		this.__lowValue = new qx.ui.basic.Label().set({ appearance: "cash-flow-chart/stat-value", rich: false, alignX: "right" });
		low.add(this.__lowLabel);
		low.add(this.__lowValue);
		head.add(low);
		this.add(head);

		// the chart: only what no widget draws is markup; the tooltip floats over it
		var area = new qx.ui.container.Composite(new qx.ui.layout.Canvas()).set({ minHeight: 90 });
		this.__chart = new qx.ui.embed.Html().set({ appearance: "cash-flow-chart/chart", overflowX: "hidden", overflowY: "hidden" });
		area.add(this.__chart, { left: 0, top: 0, right: 0, bottom: 0 });
		this.__tip = new qx.ui.basic.Label().set({ appearance: "cash-flow-chart/tip", rich: true, anonymous: true, visibility: "excluded", zIndex: 20 });
		this.__tip.getContentElement().setStyle("pointerEvents", "none");
		area.add(this.__tip, { left: 0, top: 0 });
		this.__area = area;
		this.add(area, { flex: 1 });

		this.__legend = new qx.ui.basic.Label().set({ appearance: "cash-flow-chart/legend", rich: true });
		this.add(this.__legend);

		this.__chart.addListener("resize", this._applyChart, this);
		this.__chart.addListener("pointerover", this._onChartPointerOver, this);
		this.__chart.addListener("pointerout", this._onChartPointerOut, this);
		this.__chart.addListener("tap", this._onChartTap, this);
		qx.theme.manager.Color.getInstance().addListener("changeTheme", this._applyAll, this);

		this._applyAll();
	},

	events: {

		/**
		 * The user clicked a week: { week }. The server sets selected and renders it back.
		 */
		weekSelected: "qx.event.type.Data"
	},

	properties: {

		/**
		 * The theme appearance of the root widget.
		 */
		appearance: { init: "cash-flow-chart", refine: true },

		/**
		 * The chart's name, shown in the header.
		 */
		title: { init: "13-week cash forecast", check: "String", apply: "_applyHeader" },

		/**
		 * The currency prefix of every amount.
		 */
		unit: { init: "$", check: "String", apply: "_applyAll" },

		/**
		 * The cash balance at the start of the first week.
		 */
		openingBalance: { init: 0, check: "Number", apply: "_applyAll" },

		/**
		 * The minimum cash policy.
		 */
		minimumCash: { init: 0, check: "Number", apply: "_applyAll" },

		/**
		 * The selected week.
		 */
		selected: { init: "", check: "String", apply: "_applyChart" },

		/**
		 * The weeks in order: [{ week, inflow, outflow }].
		 */
		weeks: { init: null, nullable: true, check: "Array", apply: "_applyAll" },

		/**
		 * The theme's colour of the balance line and the selected week, set by the mixin.
		 */
		themeAccentColor: { init: "#6d4dff", check: "Color", themeable: true, apply: "_applyAll" },

		/**
		 * The theme's colour of the inflow columns, set by the mixin.
		 */
		themePositiveColor: { init: "#1fb57a", check: "Color", themeable: true, apply: "_applyAll" },

		/**
		 * The theme's colour of the outflow columns, the minimum line and the weeks below it, set by the mixin.
		 */
		themeNegativeColor: { init: "#e5484d", check: "Color", themeable: true, apply: "_applyAll" }
	},

	members: {

		__title: null,
		__endLabel: null,
		__endValue: null,
		__lowLabel: null,
		__lowValue: null,
		__area: null,
		__chart: null,
		__tip: null,
		__legend: null,

		/** The weeks drawn last, valid ones only, each with its running balance as bal. */
		__rows: null,

		/** The geometry of the last drawing: { x: fn, by: fn, W, H }. */
		__geo: null,

		/** The index of the hovered week, or -1. */
		__hover: -1,

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

		__accent: function () { return this.__css(this.getThemeAccentColor()); },
		__positive: function () { return this.__css(this.getThemePositiveColor()); },
		__negative: function () { return this.__css(this.getThemeNegativeColor()); },

		/**
		 * An amount with the unit prefix, compact: $1.25M, $720K, -$90.
		 */
		__money: function (v) {

			var a = Math.abs(v), s = v < 0 ? "-" : "", u = this.getUnit() || "";
			if (a >= 1e6) return s + u + (a / 1e6).toFixed(2) + "M";
			if (a >= 1e3) return s + u + Math.round(a / 1e3) + "K";
			return s + u + Math.round(a);
		},

		/**
		 * A "nice" step (1, 2 or 5 times a power of ten) of at least raw / count.
		 */
		__niceStep: function (raw) {

			if (!(raw > 0)) return 1;
			var p = Math.pow(10, Math.floor(Math.log(raw) / Math.LN10));
			var m = raw / p;
			return (m <= 1 ? 1 : m <= 2 ? 2 : m <= 5 ? 5 : 10) * p;
		},

		/**
		 * The valid weeks (a label) with the running balance of each.
		 */
		__validRows: function () {

			var rows = this.getWeeks() || [], out = [], bal = +this.getOpeningBalance() || 0;
			for (var i = 0; i < rows.length; i++) {
				var w = rows[i];
				if (!w || w.week == null || w.week === "")
					continue;
				var inflow = +w.inflow || 0, outflow = +w.outflow || 0;
				bal += inflow - outflow;
				out.push({ week: String(w.week), inflow: inflow, outflow: outflow, net: inflow - outflow, bal: bal });
			}
			return out;
		},

		/**
		 * Draws everything.
		 */
		_applyAll: function () {

			if (!this.__legend)
				return;

			this._applyHeader();
			this._applyChart();
		},

		/**
		 * Shows the title, the ending balance and the lowest balance with its week.
		 */
		_applyHeader: function () {

			if (!this.__lowValue)
				return;

			this.__title.setValue(this.getTitle() || "");

			var rows = this.__validRows();
			if (!rows.length) {
				this.__endLabel.setValue("");
				this.__endValue.setValue("");
				this.__lowLabel.setValue("");
				this.__lowValue.setValue("");
				return;
			}

			var low = 0;
			for (var i = 1; i < rows.length; i++)
				if (rows[i].bal < rows[low].bal) low = i;

			this.__endLabel.setValue("Ending");
			this.__endValue.setValue(this.__money(rows[rows.length - 1].bal));
			this.__lowLabel.setValue("Lowest · " + rows[low].week);
			this.__lowValue.set({
				value: this.__money(rows[low].bal),
				textColor: rows[low].bal < (+this.getMinimumCash() || 0) ? this.__negative() : this.__css("windowText")
			});
		},

		/**
		 * Draws the chart in real pixels at the chart's current size, and the legend under it.
		 */
		_applyChart: function () {

			if (!this.__legend)
				return;

			var esc = qx.bom.String.escape;
			var rows = this.__validRows();
			var gray = this.__css("grayText");
			var min = +this.getMinimumCash() || 0, open = +this.getOpeningBalance() || 0;

			this.__rows = rows;
			this.__geo = null;
			this.__hover = -1;
			this.__tip.setVisibility("excluded");

			if (!rows.length) {
				this.__chart.setHtml("<div style='padding:30px 0;text-align:center;color:" + gray + ";font-size:12px'>No forecast</div>");
				this.__legend.setValue("");
				return;
			}

			// legend
			var accent = this.__accent(), positive = this.__positive(), negative = this.__negative();
			var text = this.__css("windowText");
			var key = "<span style='display:inline-flex;align-items:center;gap:5px;margin-right:14px;white-space:nowrap'>";
			this.__legend.setValue(
				key + "<i style='display:inline-block;width:9px;height:9px;border-radius:2px;background:" + positive + "'></i>Inflow</span>" +
				key + "<i style='display:inline-block;width:9px;height:9px;border-radius:2px;background:" + negative + "'></i>Outflow</span>" +
				key + "<i style='display:inline-block;width:14px;border-top:1.6px solid " + text + "'></i>Net</span>" +
				key + "<i style='display:inline-block;width:14px;border-top:2.4px solid " + accent + "'></i>Cash balance</span>" +
				key + "<i style='display:inline-block;width:14px;border-top:1.5px dashed " + negative + "'></i>Minimum cash " + esc(this.__money(min)) + "</span>");

			var bounds = this.__chart.getBounds();
			if (!bounds || bounds.width < 10 || bounds.height < 10)
				return; // drawn again on resize

			var self = this, n = rows.length;
			var W = Math.max(200, bounds.width), H = Math.max(80, bounds.height);
			var L = 50, R = 50, T = 6, B = 18;
			var pw = W - L - R, ph = H - T - B;
			var step = pw / n;

			// the column scale: symmetric nice steps around the zero line
			var mi = 0, mo = 0, bals = [open, min];
			for (var i = 0; i < n; i++) {
				mi = Math.max(mi, rows[i].inflow);
				mo = Math.max(mo, rows[i].outflow);
				bals.push(rows[i].bal);
			}
			var ps = this.__niceStep(((mi + mo) || 1) * 1.05 / (ph > 160 ? 5 : 3));
			var pHi = Math.max(1, Math.ceil(mi * 1.02 / ps)) * ps, pLo = -Math.max(1, Math.ceil(mo * 1.02 / ps)) * ps;
			var py = function (v) { return T + (pHi - v) * ph / (pHi - pLo); };

			// the balance scale on the right, with its own nice ticks
			var bMin = Math.min.apply(null, bals), bMax = Math.max.apply(null, bals);
			var bs = this.__niceStep(((bMax - bMin) || 1) * 1.1 / (ph > 160 ? 5 : 3));
			var bLo = Math.floor(bMin / bs) * bs, bHi = Math.ceil(bMax / bs) * bs;
			if (bHi === bLo) bHi = bLo + bs;
			var by = function (v) { return T + (bHi - v) * ph / (bHi - bLo); };

			var x = function (i) { return L + step * (i + 0.5); };
			var f1 = function (v) { return v.toFixed(1); };
			this.__geo = { x: x, by: by, W: W, H: H };

			var border = this.__css("windowFrame"), windowColor = this.__css("window");
			var selected = this.getSelected();
			var s = "";

			// column grid at the nice steps, labelled on the left
			for (var v = pLo; v <= pHi + ps / 2; v += ps) {
				var yy = py(v);
				s += "<line x1='" + L + "' x2='" + (W - R) + "' y1='" + f1(yy) + "' y2='" + f1(yy) + "' stroke='" + border + "' stroke-width='1'/>" +
					"<text x='" + (L - 6) + "' y='" + f1(yy + 4) + "' text-anchor='end' fill='" + gray + "' font-size='11'>" + esc(this.__money(Math.abs(v) < ps / 2 ? 0 : v)) + "</text>";
			}

			// balance ticks on the right
			for (var v = bLo; v <= bHi + bs / 2; v += bs)
				s += "<text x='" + (W - R + 6) + "' y='" + f1(by(v) + 4) + "' fill='" + gray + "' font-size='11'>" + esc(this.__money(v)) + "</text>";

			// the zero line
			s += "<line x1='" + L + "' x2='" + (W - R) + "' y1='" + f1(py(0)) + "' y2='" + f1(py(0)) + "' stroke='" + gray + "' stroke-width='1.2'/>";

			// week bands: below minimum, selection, hover, and the hit area of each week
			var cw = Math.min(26, step * 0.5);
			for (var i = 0; i < n; i++) {
				var r = rows[i], below = r.bal < min, sel = r.week === selected;
				var fill = below ? this.__tint(negative, sel ? 0.2 : 0.1) : sel ? this.__tint(accent, 0.12) : "transparent";
				s += "<rect data-i='" + i + "' x='" + f1(L + step * i) + "' y='" + T + "' width='" + f1(step) + "' height='" + f1(ph) + "' rx='4'" +
					" fill='" + fill + "' data-fill='" + fill + "' style='cursor:pointer'/>";
			}

			// inflow columns above the zero line, outflow columns below
			var inflowFill = this.__tint(positive, 0.8), outflowFill = this.__tint(negative, 0.8);
			for (var i = 0; i < n; i++) {
				var r = rows[i], up = py(r.inflow), down = py(-r.outflow), zero = py(0);
				if (r.inflow > 0)
					s += "<rect x='" + f1(x(i) - cw / 2) + "' y='" + f1(up) + "' width='" + f1(cw) + "' height='" + f1(zero - up) + "' rx='2' fill='" + inflowFill + "' pointer-events='none'/>";
				if (r.outflow > 0)
					s += "<rect x='" + f1(x(i) - cw / 2) + "' y='" + f1(zero) + "' width='" + f1(cw) + "' height='" + f1(down - zero) + "' rx='2' fill='" + outflowFill + "' pointer-events='none'/>";
			}

			// the dashed minimum-cash line, the net line and the balance line
			s += "<line x1='" + L + "' x2='" + (W - R) + "' y1='" + f1(by(min)) + "' y2='" + f1(by(min)) + "' stroke='" + negative + "' stroke-width='1.5' stroke-dasharray='5 4' pointer-events='none'/>";

			var net = "", bal = "M" + f1(L) + " " + f1(by(open));
			for (var i = 0; i < n; i++) {
				net += (i ? " L" : "M") + f1(x(i)) + " " + f1(py(rows[i].net));
				bal += " L" + f1(x(i)) + " " + f1(by(rows[i].bal));
			}
			s += "<path d='" + net + "' fill='none' stroke='" + text + "' stroke-width='1.6' stroke-linejoin='round' pointer-events='none'/>";
			s += "<path d='" + bal + "' fill='none' stroke='" + accent + "' stroke-width='2.4' stroke-linejoin='round' stroke-linecap='round' pointer-events='none'/>";

			// dots and week labels, thinned when crowded
			var every = step < 34 ? 2 : 1;
			for (var i = 0; i < n; i++) {
				var r = rows[i], sel = r.week === selected, below = r.bal < min;
				s += "<circle cx='" + f1(x(i)) + "' cy='" + f1(py(r.net)) + "' r='2.6' fill='" + windowColor + "' stroke='" + text + "' stroke-width='1.5' pointer-events='none'/>" +
					"<circle cx='" + f1(x(i)) + "' cy='" + f1(by(r.bal)) + "' r='" + (sel ? 5 : 3.2) + "' fill='" + (below || sel ? (below ? negative : accent) : windowColor) + "' stroke='" + (below ? negative : accent) + "' stroke-width='2' pointer-events='none'/>";
				if (i % every === 0 || sel)
					s += "<text x='" + f1(x(i)) + "' y='" + (H - 4) + "' text-anchor='middle' fill='" + gray + "' font-size='11' pointer-events='none'>" + esc(r.week) + "</text>";
			}

			this.__chart.setHtml(
				"<svg width='" + W + "' height='" + H + "' style='display:block;overflow:visible;font-family:inherit' role='img' aria-label='" + esc(this.getTitle() || "Cash forecast") + "'>" + s + "</svg>");
		},

		/**
		 * The index of the week band under a pointer event, or -1.
		 */
		__indexOf: function (e) {

			var el = e.getOriginalTarget();
			if (!el || !el.getAttribute)
				return -1;
			var i = el.getAttribute("data-i");
			if (i == null || i === "")
				return -1;
			i = parseInt(i, 10);
			return this.__rows && i >= 0 && i < this.__rows.length ? i : -1;
		},

		/**
		 * Hovering a week band: highlights it and shows its tooltip.
		 */
		_onChartPointerOver: function (e) {

			var i = this.__indexOf(e);
			if (i < 0 || !this.__geo)
				return;

			var el = e.getOriginalTarget();
			el.setAttribute("fill", this.__tint(this.__css("menuHighlight"), 0.55));
			this.__hover = i;

			var esc = qx.bom.String.escape;
			var r = this.__rows[i];
			var text = this.__css("windowText");
			var row = function (name, value) {
				return "<div style='display:flex;justify-content:space-between;gap:14px'><span>" + name + "</span><b style='font-weight:600;color:" + text + "'>" + value + "</b></div>";
			};
			this.__tip.setValue(
				"<div style='font-weight:600;margin-bottom:3px;color:" + text + "'>" + esc(r.week) + "</div>" +
				row("Inflow", esc(this.__money(r.inflow))) +
				row("Outflow", esc(this.__money(r.outflow))) +
				row("Net", esc(this.__money(r.net))) +
				row("Balance", esc(this.__money(r.bal))));
			this.__tip.setVisibility("visible");

			// beside the balance dot, flipped to the left near the right edge, kept inside the chart
			var geo = this.__geo;
			var hint = this.__tip.getSizeHint();
			var tw = hint.width, th = hint.height;
			var tx = geo.x(i) + 12;
			if (tx + tw > geo.W)
				tx = geo.x(i) - 12 - tw;
			var ty = geo.by(r.bal) - th / 2;
			ty = Math.max(0, Math.min(geo.H - th, ty));
			this.__tip.setLayoutProperties({ left: Math.max(0, Math.round(tx)), top: Math.round(ty) });
		},

		/**
		 * Leaving a week band: restores it and hides the tooltip.
		 */
		_onChartPointerOut: function (e) {

			var i = this.__indexOf(e);
			if (i < 0)
				return;

			var el = e.getOriginalTarget();
			el.setAttribute("fill", el.getAttribute("data-fill") || "transparent");
			if (this.__hover === i) {
				this.__hover = -1;
				this.__tip.setVisibility("excluded");
			}
		},

		/**
		 * A week band was tapped: report its week; the server sets selected and renders it back.
		 */
		_onChartTap: function (e) {

			var i = this.__indexOf(e);
			if (i < 0 || !this.isEnabled())
				return;

			this.fireDataEvent("weekSelected", { week: this.__rows[i].week });
		}
	},

	/**
	 * Drops the theme listener; the child widgets are disposed by the container.
	 */
	destruct: function () {

		qx.theme.manager.Color.getInstance().removeListener("changeTheme", this._applyAll, this);
		this.__title = this.__endLabel = this.__endValue = this.__lowLabel = this.__lowValue = null;
		this.__area = this.__chart = this.__tip = this.__legend = this.__rows = this.__geo = null;
	}
});
