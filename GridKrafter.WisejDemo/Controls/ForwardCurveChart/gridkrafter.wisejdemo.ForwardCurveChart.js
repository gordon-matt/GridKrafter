/**
 * gridkrafter.wisejdemo.ForwardCurveChart
 *
 * The client half of GridKrafter.WisejDemo.ForwardCurveChart: a header with the title, the unit,
 * the front month, its price and its change against the prior curve; the forward curve drawn as
 * SVG in real pixels (price grid, hedged volume columns, the dashed prior curve, the curve with its
 * dots, the hedged price ticks and the month axis); a tooltip for the hovered month; and the legend.
 * The server owns every value; the browser only reports the month the user clicks.
 */
qx.Class.define("gridkrafter.wisejdemo.ForwardCurveChart", {

	extend: wisej.web.Control,

	/**
	 * Builds the widget tree once: the header, the chart with its tooltip, the legend.
	 */
	construct: function () {

		this.base(arguments, new qx.ui.layout.VBox(6));

		this.setFocusable(false);

		// header: title and unit on the left, the front month on the right
		var head = new qx.ui.container.Composite(new qx.ui.layout.HBox(6).set({ alignY: "bottom" }));
		this.__title = new qx.ui.basic.Label().set({ appearance: "forward-curve-chart/title", rich: false, minWidth: 20, marginBottom: 2 });
		this.__unit = new qx.ui.basic.Label().set({ appearance: "forward-curve-chart/unit", rich: false, marginBottom: 3 });
		this.__frontLabel = new qx.ui.basic.Label().set({ appearance: "forward-curve-chart/front-label", rich: false, marginBottom: 3 });
		this.__frontValue = new qx.ui.basic.Label().set({ appearance: "forward-curve-chart/front-value", rich: false });
		this.__change = new qx.ui.basic.Label().set({ appearance: "forward-curve-chart/change", rich: false, marginBottom: 3 });
		head.add(this.__title);
		head.add(this.__unit);
		head.add(new qx.ui.core.Spacer(), { flex: 1 });
		head.add(this.__frontLabel);
		head.add(this.__frontValue);
		head.add(this.__change);
		this.add(head);

		// the chart: only what no widget draws is markup; the tooltip floats over it
		var area = new qx.ui.container.Composite(new qx.ui.layout.Canvas()).set({ minHeight: 90 });
		this.__chart = new qx.ui.embed.Html().set({ appearance: "forward-curve-chart/chart", overflowX: "hidden", overflowY: "hidden" });
		area.add(this.__chart, { left: 0, top: 0, right: 0, bottom: 0 });
		this.__tip = new qx.ui.basic.Label().set({ appearance: "forward-curve-chart/tip", rich: true, anonymous: true, visibility: "excluded", zIndex: 20 });
		this.__tip.getContentElement().setStyle("pointerEvents", "none");
		area.add(this.__tip, { left: 0, top: 0 });
		this.__area = area;
		this.add(area, { flex: 1 });

		this.__legend = new qx.ui.basic.Label().set({ appearance: "forward-curve-chart/legend", rich: true });
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
		 * The user clicked a contract month: { month }. The server sets selected and renders it back.
		 */
		monthSelected: "qx.event.type.Data"
	},

	properties: {

		/**
		 * The theme appearance of the root widget.
		 */
		appearance: { init: "forward-curve-chart", refine: true },

		/**
		 * The curve's name, shown in the header.
		 */
		title: { init: "Henry Hub NG", check: "String", apply: "_applyHeader" },

		/**
		 * The price unit shown after the title.
		 */
		unit: { init: "$/MMBtu", check: "String", apply: "_applyHeader" },

		/**
		 * The unit of the hedged volume, shown in the legend and the tooltip.
		 */
		volumeUnit: { init: "MMBtu", check: "String", apply: "_applyAll" },

		/**
		 * The decimals of prices.
		 */
		decimals: { init: 3, check: "Integer", apply: "_applyAll" },

		/**
		 * Whether the prior curve shows as a dashed line.
		 */
		showPrior: { init: true, check: "Boolean", apply: "_applyAll" },

		/**
		 * The selected contract month.
		 */
		selected: { init: "", check: "String", apply: "_applyChart" },

		/**
		 * The curve by contract month, nearest first: [{ month, price, priorPrice, hedgedPrice, hedgedVolume }].
		 */
		points: { init: null, nullable: true, check: "Array", apply: "_applyAll" },

		/**
		 * The theme's colour of the curve, its dots, the volume columns and the selected month, set by the mixin.
		 */
		themeAccentColor: { init: "#6d4dff", check: "Color", themeable: true, apply: "_applyAll" },

		/**
		 * The theme's colour of the hedged price ticks and a rising curve, set by the mixin.
		 */
		themePositiveColor: { init: "#1fb57a", check: "Color", themeable: true, apply: "_applyAll" },

		/**
		 * The theme's colour of a falling curve, set by the mixin.
		 */
		themeNegativeColor: { init: "#e5484d", check: "Color", themeable: true, apply: "_applyAll" }
	},

	members: {

		__title: null,
		__unit: null,
		__frontLabel: null,
		__frontValue: null,
		__change: null,
		__area: null,
		__chart: null,
		__tip: null,
		__legend: null,

		/** The points drawn last, valid ones only. */
		__pts: null,

		/** The geometry of the last drawing: { x: fn, y: fn, W, H }. */
		__geo: null,

		/** The index of the hovered month, or -1. */
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
		 * True for a number that is set and not 0 (0 means "none" for prior and hedged values).
		 */
		__has: function (v) {

			return v != null && v !== "" && !isNaN(+v) && +v !== 0;
		},

		/**
		 * A price with the configured decimals, or a dash.
		 */
		__num: function (v) {

			return (v == null || isNaN(+v)) ? "—" : (+v).toFixed(this.getDecimals());
		},

		/**
		 * The valid points: a month and a numeric price.
		 */
		__validPoints: function () {

			var rows = this.getPoints() || [], pts = [];
			for (var i = 0; i < rows.length; i++) {
				var p = rows[i];
				if (p && p.month != null && p.month !== "" && p.price != null && !isNaN(+p.price))
					pts.push(p);
			}
			return pts;
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
		 * Shows the title, the unit, the front month, its price and the change badge.
		 */
		_applyHeader: function () {

			if (!this.__change)
				return;

			this.__title.setValue(this.getTitle() || "");
			this.__unit.setValue(this.getUnit() || "");

			var pts = this.__validPoints(), f = pts[0];
			if (!f) {
				this.__frontLabel.setValue("");
				this.__frontValue.setValue("");
				this.__change.setVisibility("excluded");
				return;
			}

			this.__frontLabel.setValue(String(f.month));
			this.__frontValue.setValue(this.__num(f.price));

			if (this.__has(f.priorPrice)) {
				var c = f.price - f.priorPrice;
				var color = c >= 0 ? this.__positive() : this.__negative();
				this.__change.set({
					value: (c >= 0 ? "▲ " : "▼ ") + Math.abs(c).toFixed(this.getDecimals()),
					textColor: color,
					backgroundColor: this.__tint(color, 0.14),
					visibility: "visible"
				});
			}
			else {
				this.__change.setVisibility("excluded");
			}
		},

		/**
		 * Draws the chart in real pixels at the chart's current size, and the legend under it.
		 */
		_applyChart: function () {

			if (!this.__legend)
				return;

			var esc = qx.bom.String.escape;
			var pts = this.__validPoints();
			var showPrior = this.getShowPrior();
			var volumeUnit = this.getVolumeUnit() || "";
			var gray = this.__css("grayText");

			this.__pts = pts;
			this.__geo = null;
			this.__hover = -1;
			this.__tip.setVisibility("excluded");

			if (pts.length < 2) {
				this.__chart.setHtml("<div style='padding:30px 0;text-align:center;color:" + gray + ";font-size:12px'>No curve</div>");
				this.__legend.setValue("");
				return;
			}

			// legend
			var accent = this.__accent(), positive = this.__positive();
			var key = "<span style='display:inline-flex;align-items:center;gap:5px;margin-right:14px;white-space:nowrap'>";
			this.__legend.setValue(
				key + "<i style='display:inline-block;width:14px;border-top:2px solid " + accent + "'></i>Curve</span>" +
				(showPrior ? key + "<i style='display:inline-block;width:14px;border-top:2px dashed " + gray + "'></i>Prior</span>" : "") +
				key + "<i style='display:inline-block;width:10px;border-top:3px solid " + positive + ";border-radius:2px'></i>Hedged price</span>" +
				key + "<i style='display:inline-block;width:9px;height:9px;border-radius:2px;background:" + this.__tint(accent, 0.22) + "'></i>Hedged volume" +
				(volumeUnit ? " (" + esc(volumeUnit) + ")" : "") + "</span>");

			var bounds = this.__chart.getBounds();
			if (!bounds || bounds.width < 10 || bounds.height < 10)
				return; // drawn again on resize

			var self = this, has = this.__has;
			var dec = this.getDecimals();
			var W = Math.max(200, bounds.width), H = Math.max(80, bounds.height);
			var L = 38, R = 8, T = 6, B = 18;
			var pw = W - L - R, ph = H - T - B;

			var vals = [];
			for (var i = 0; i < pts.length; i++) {
				var p = pts[i];
				vals.push(+p.price);
				if (showPrior && has(p.priorPrice)) vals.push(+p.priorPrice);
				if (has(p.hedgedPrice)) vals.push(+p.hedgedPrice);
			}
			var lo = Math.min.apply(null, vals), hi = Math.max.apply(null, vals);
			var span = (hi - lo) || 1;
			lo -= span * 0.12;
			hi += span * 0.08;

			var maxVol = 0;
			for (var i = 0; i < pts.length; i++)
				maxVol = Math.max(maxVol, +pts[i].hedgedVolume || 0);

			var step = pw / pts.length;
			var x = function (i) { return L + step * (i + 0.5); };
			var y = function (v) { return T + (hi - v) * ph / (hi - lo); };
			var f1 = function (v) { return v.toFixed(1); };
			this.__geo = { x: x, y: y, W: W, H: H };

			var border = this.__css("windowFrame"), windowColor = this.__css("window");
			var selected = this.getSelected();
			var s = "";

			// price grid: 4 lines with their labels
			for (var g = 0; g <= 3; g++) {
				var v = lo + (hi - lo) * g / 3, yy = y(v);
				s += "<line x1='" + L + "' x2='" + (W - R) + "' y1='" + f1(yy) + "' y2='" + f1(yy) + "' stroke='" + border + "' stroke-width='1'/>" +
					"<text x='" + (L - 6) + "' y='" + f1(yy + 4) + "' text-anchor='end' fill='" + gray + "' font-size='11'>" + v.toFixed(Math.min(dec, 2)) + "</text>";
			}

			// month bands: hover and selection, and the hit area of each month
			for (var i = 0; i < pts.length; i++) {
				var sel = pts[i].month === selected;
				s += "<rect data-i='" + i + "' x='" + f1(L + step * i) + "' y='" + T + "' width='" + f1(step) + "' height='" + f1(ph) + "' rx='4'" +
					" fill='" + (sel ? this.__tint(accent, 0.1) : "transparent") + "' data-fill='" + (sel ? this.__tint(accent, 0.1) : "transparent") + "' style='cursor:pointer'/>";
			}

			// hedged volume columns on their own scale, along the bottom
			var cw = Math.min(22, step * 0.55), volFill = this.__tint(accent, 0.14);
			for (var i = 0; i < pts.length; i++) {
				var h = maxVol > 0 ? (+pts[i].hedgedVolume || 0) / maxVol * ph * 0.38 : 0;
				if (h > 0)
					s += "<rect x='" + f1(x(i) - cw / 2) + "' y='" + f1(T + ph - h) + "' width='" + f1(cw) + "' height='" + f1(h) + "' rx='2' fill='" + volFill + "' pointer-events='none'/>";
			}

			// the area under the curve, the prior curve and the curve
			var path = function (key) {
				var d = "";
				for (var i = 0; i < pts.length; i++)
					d += (i ? " L" : "M") + f1(x(i)) + " " + f1(y(+pts[i][key]));
				return d;
			};
			var d = path("price");
			s += "<path d='" + d + " L" + f1(x(pts.length - 1)) + " " + f1(T + ph) + " L" + f1(x(0)) + " " + f1(T + ph) + "Z' fill='" + this.__tint(accent, 0.07) + "' pointer-events='none'/>";

			var allPrior = true;
			for (var i = 0; i < pts.length; i++)
				if (!has(pts[i].priorPrice)) { allPrior = false; break; }
			if (showPrior && allPrior)
				s += "<path d='" + path("priorPrice") + "' fill='none' stroke='" + gray + "' stroke-width='1.5' stroke-dasharray='4 3' opacity='.8' pointer-events='none'/>";

			s += "<path d='" + d + "' fill='none' stroke='" + accent + "' stroke-width='2.2' stroke-linejoin='round' stroke-linecap='round' pointer-events='none'/>";

			// hedged price ticks
			var hw = Math.min(16, step * 0.5);
			for (var i = 0; i < pts.length; i++) {
				if (has(pts[i].hedgedPrice)) {
					var hy = f1(y(+pts[i].hedgedPrice));
					s += "<line x1='" + f1(x(i) - hw / 2) + "' x2='" + f1(x(i) + hw / 2) + "' y1='" + hy + "' y2='" + hy + "' stroke='" + positive + "' stroke-width='3' stroke-linecap='round' pointer-events='none'/>";
				}
			}

			// dots and month labels, thinned when crowded
			var every = step < 34 ? 2 : 1;
			for (var i = 0; i < pts.length; i++) {
				var sel = pts[i].month === selected;
				s += "<circle cx='" + f1(x(i)) + "' cy='" + f1(y(+pts[i].price)) + "' r='" + (sel ? 5 : 3.2) + "' fill='" + (sel ? accent : windowColor) + "' stroke='" + accent + "' stroke-width='2' pointer-events='none'/>";
				if (i % every === 0 || sel)
					s += "<text x='" + f1(x(i)) + "' y='" + (H - 4) + "' text-anchor='middle' fill='" + gray + "' font-size='11' pointer-events='none'>" + esc(String(pts[i].month)) + "</text>";
			}

			this.__chart.setHtml(
				"<svg width='" + W + "' height='" + H + "' style='display:block;overflow:visible;font-family:inherit' role='img' aria-label='" + esc(this.getTitle() || "Forward curve") + "'>" + s + "</svg>");
		},

		/**
		 * The index of the month band under a pointer event, or -1.
		 */
		__indexOf: function (e) {

			var el = e.getOriginalTarget();
			if (!el || !el.getAttribute)
				return -1;
			var i = el.getAttribute("data-i");
			if (i == null || i === "")
				return -1;
			i = parseInt(i, 10);
			return this.__pts && i >= 0 && i < this.__pts.length ? i : -1;
		},

		/**
		 * Hovering a month band: highlights it and shows its tooltip.
		 */
		_onChartPointerOver: function (e) {

			var i = this.__indexOf(e);
			if (i < 0 || !this.__geo)
				return;

			var el = e.getOriginalTarget();
			el.setAttribute("fill", this.__tint(this.__css("menuHighlight"), 0.55));
			this.__hover = i;

			var esc = qx.bom.String.escape, has = this.__has;
			var p = this.__pts[i];
			var volumeUnit = this.getVolumeUnit() || "";
			var text = this.__css("windowText");
			var row = function (name, value) {
				return "<div style='display:flex;justify-content:space-between;gap:14px'><span>" + name + "</span><b style='font-weight:600;color:" + text + "'>" + value + "</b></div>";
			};
			this.__tip.setValue(
				"<div style='font-weight:600;margin-bottom:3px;color:" + text + "'>" + esc(String(p.month)) + "</div>" +
				row("Price", this.__num(p.price)) +
				(this.getShowPrior() ? row("Prior", has(p.priorPrice) ? this.__num(p.priorPrice) : "—") : "") +
				row("Hedged price", has(p.hedgedPrice) ? this.__num(p.hedgedPrice) : "—") +
				row("Hedged volume", has(p.hedgedVolume) ? Math.round(+p.hedgedVolume).toLocaleString("en-US") + (volumeUnit ? " " + esc(volumeUnit) : "") : "—"));
			this.__tip.setVisibility("visible");

			// beside the dot, flipped to the left near the right edge, kept inside the chart
			var geo = this.__geo;
			var hint = this.__tip.getSizeHint();
			var tw = hint.width, th = hint.height;
			var tx = geo.x(i) + 12;
			if (tx + tw > geo.W)
				tx = geo.x(i) - 12 - tw;
			var ty = geo.y(+p.price) - th / 2;
			ty = Math.max(0, Math.min(geo.H - th, ty));
			this.__tip.setLayoutProperties({ left: Math.max(0, Math.round(tx)), top: Math.round(ty) });
		},

		/**
		 * Leaving a month band: restores it and hides the tooltip.
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
		 * A month band was tapped: report its month; the server sets selected and renders it back.
		 */
		_onChartTap: function (e) {

			var i = this.__indexOf(e);
			if (i < 0 || !this.isEnabled())
				return;

			this.fireDataEvent("monthSelected", { month: String(this.__pts[i].month) });
		}
	},

	/**
	 * Drops the theme listener; the child widgets are disposed by the container.
	 */
	destruct: function () {

		qx.theme.manager.Color.getInstance().removeListener("changeTheme", this._applyAll, this);
		this.__title = this.__unit = this.__frontLabel = this.__frontValue = this.__change = null;
		this.__area = this.__chart = this.__tip = this.__legend = this.__pts = this.__geo = null;
	}
});
