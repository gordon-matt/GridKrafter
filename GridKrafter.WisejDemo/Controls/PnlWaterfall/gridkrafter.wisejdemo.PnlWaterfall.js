/**
 * gridkrafter.wisejdemo.PnlWaterfall
 *
 * The client half of GridKrafter.WisejDemo.PnlWaterfall: a header with the title and the final total,
 * and a P&L waterfall drawn as SVG in real pixels: "total" bars standing from zero, "up" steps floating
 * upward from the running total, "down" steps hanging downward, thin connectors, compact value labels
 * and x labels that wrap or rotate when the steps are narrow; a tooltip for the hovered step.
 * The server owns every value; the browser only reports the step the user clicks.
 */
qx.Class.define("gridkrafter.wisejdemo.PnlWaterfall", {

	extend: wisej.web.Control,

	/**
	 * Builds the widget tree once: the header, the chart with its tooltip.
	 */
	construct: function () {

		this.base(arguments, new qx.ui.layout.VBox(6));

		this.setFocusable(false);

		// header: title on the left, the final total on the right
		var head = new qx.ui.container.Composite(new qx.ui.layout.HBox(6).set({ alignY: "bottom" }));
		this.__title = new qx.ui.basic.Label().set({ appearance: "pnl-waterfall/title", rich: false, minWidth: 20, marginBottom: 2 });
		this.__totalLabel = new qx.ui.basic.Label().set({ appearance: "pnl-waterfall/total-label", rich: false, marginBottom: 3 });
		this.__totalValue = new qx.ui.basic.Label().set({ appearance: "pnl-waterfall/total-value", rich: false });
		head.add(this.__title);
		head.add(new qx.ui.core.Spacer(), { flex: 1 });
		head.add(this.__totalLabel);
		head.add(this.__totalValue);
		this.add(head);

		// the chart: only what no widget draws is markup; the tooltip floats over it
		var area = new qx.ui.container.Composite(new qx.ui.layout.Canvas()).set({ minHeight: 90 });
		this.__chart = new qx.ui.embed.Html().set({ appearance: "pnl-waterfall/chart", overflowX: "hidden", overflowY: "hidden" });
		area.add(this.__chart, { left: 0, top: 0, right: 0, bottom: 0 });
		this.__tip = new qx.ui.basic.Label().set({ appearance: "pnl-waterfall/tip", rich: true, anonymous: true, visibility: "excluded", zIndex: 20 });
		this.__tip.getContentElement().setStyle("pointerEvents", "none");
		area.add(this.__tip, { left: 0, top: 0 });
		this.add(area, { flex: 1 });

		this.__chart.addListener("resize", this._applyChart, this);
		this.__chart.addListener("pointerover", this._onChartPointerOver, this);
		this.__chart.addListener("pointerout", this._onChartPointerOut, this);
		this.__chart.addListener("tap", this._onChartTap, this);
		qx.theme.manager.Color.getInstance().addListener("changeTheme", this._applyAll, this);

		this._applyAll();
	},

	events: {

		/**
		 * The user clicked a step: { label }. The server sets selected and renders it back.
		 */
		stepSelected: "qx.event.type.Data"
	},

	properties: {

		/**
		 * The theme appearance of the root widget.
		 */
		appearance: { init: "pnl-waterfall", refine: true },

		/**
		 * The waterfall's name, shown in the header.
		 */
		title: { init: "Gross margin", check: "String", apply: "_applyHeader" },

		/**
		 * The currency prefix of the value labels.
		 */
		unit: { init: "$", check: "String", apply: "_applyAll" },

		/**
		 * The label of the selected step.
		 */
		selected: { init: "", check: "String", apply: "_applyChart" },

		/**
		 * The steps left to right: [{ label, value, kind }], kind total, up or down.
		 */
		steps: { init: null, nullable: true, check: "Array", apply: "_applyAll" },

		/**
		 * The theme's colour of the total bars and the selected step, set by the mixin.
		 */
		themeAccentColor: { init: "#6d4dff", check: "Color", themeable: true, apply: "_applyAll" },

		/**
		 * The theme's colour of the up steps, set by the mixin.
		 */
		themePositiveColor: { init: "#1fb57a", check: "Color", themeable: true, apply: "_applyAll" },

		/**
		 * The theme's colour of the down steps, set by the mixin.
		 */
		themeNegativeColor: { init: "#e5484d", check: "Color", themeable: true, apply: "_applyAll" }
	},

	members: {

		__title: null,
		__totalLabel: null,
		__totalValue: null,
		__chart: null,
		__tip: null,

		/** The bars drawn last: { step, value, from, to, run }. */
		__bars: null,

		/** The geometry of the last drawing: { cx: fn, W, H, T }. */
		__geo: null,

		/** The index of the hovered step, or -1. */
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

		/**
		 * A value in compact form with the unit: $1.2M, -$31.4M, $850K.
		 */
		__fmt: function (v) {

			var a = Math.abs(+v), unit = this.getUnit() || "", t;
			if (a >= 1e9) t = (a / 1e9).toFixed(1) + "B";
			else if (a >= 1e6) t = (a / 1e6).toFixed(1) + "M";
			else if (a >= 1e3) t = (a / 1e3).toFixed(a >= 1e5 ? 0 : 1) + "K";
			else t = a.toFixed(a < 10 && a !== Math.floor(a) ? 1 : 0);
			t = t.replace(/\.0(?=[KMB]?$)/, "");
			return ((+v) < 0 ? "-" : "") + unit + t;
		},

		/**
		 * The valid steps with their running totals: a label and a numeric value.
		 */
		__build: function () {

			var rows = this.getSteps() || [], bars = [], run = 0;
			for (var i = 0; i < rows.length; i++) {
				var s = rows[i];
				if (!s || s.label == null || s.label === "" || s.value == null || isNaN(+s.value))
					continue;

				var v = +s.value, from, to;
				if (s.kind === "total") { from = 0; to = v; run = v; }
				else { from = run; run += v; to = run; }
				bars.push({ step: s, value: v, from: from, to: to, run: run });
			}
			return bars;
		},

		/**
		 * Draws everything.
		 */
		_applyAll: function () {

			if (!this.__tip)
				return;

			this._applyHeader();
			this._applyChart();
		},

		/**
		 * Shows the title and the final total.
		 */
		_applyHeader: function () {

			if (!this.__totalValue)
				return;

			this.__title.setValue(this.getTitle() || "");
			var bars = this.__build();
			this.__totalLabel.setValue(bars.length ? "Total" : "");
			this.__totalValue.setValue(bars.length ? this.__fmt(bars[bars.length - 1].run) : "");
		},

		/**
		 * Draws the waterfall in real pixels at the chart's current size.
		 */
		_applyChart: function () {

			if (!this.__tip)
				return;

			var esc = qx.bom.String.escape;
			var bars = this.__build();
			var gray = this.__css("grayText");

			this.__bars = bars;
			this.__geo = null;
			this.__hover = -1;
			this.__tip.setVisibility("excluded");

			if (!bars.length) {
				this.__chart.setHtml("<div style='padding:30px 0;text-align:center;color:" + gray + ";font-size:12px'>No data</div>");
				return;
			}

			var bounds = this.__chart.getBounds();
			if (!bounds || bounds.width < 10 || bounds.height < 10)
				return; // drawn again on resize

			var accent = this.__css(this.getThemeAccentColor());
			var positive = this.__css(this.getThemePositiveColor());
			var negative = this.__css(this.getThemeNegativeColor());
			var border = this.__css("windowFrame"), text = this.__css("windowText");
			var f1 = function (v) { return v.toFixed(1); };

			var W = Math.max(200, bounds.width), H = Math.max(80, bounds.height);
			var L = 6, R = 6, T = 16;
			var n = bars.length, step = (W - L - R) / n;
			var bw = Math.min(64, step * 0.64);

			// x labels: as they are, wrapped on two lines, or rotated when even that is too wide
			var cw = 6, maxLen = 0, i;
			for (i = 0; i < n; i++)
				maxLen = Math.max(maxLen, String(bars[i].step.label).length);
			var fits = function (len) { return len * cw <= step - 4; };
			var mode = "flat", lines = [];
			var wrap = function (label) {
				label = String(label);
				if (fits(label.length)) return [label];
				var sp = label.indexOf(" ", 1), best = -1;
				for (var k = 0; k < label.length; k++)
					if (label.charAt(k) === " " && Math.abs(k - label.length / 2) < Math.abs(best - label.length / 2)) best = k;
				if (best < 1) return null;
				var a = label.substring(0, best), b = label.substring(best + 1);
				return fits(a.length) && fits(b.length) ? [a, b] : null;
			};
			var allFlat = true, allWrap = true;
			for (i = 0; i < n; i++) {
				var w = wrap(bars[i].step.label);
				lines.push(w);
				if (!w) allWrap = false;
				else if (w.length > 1) allFlat = false;
			}
			var B;
			if (allFlat) { mode = "flat"; B = 20; }
			else if (allWrap) { mode = "wrap"; B = 34; }
			else { mode = "rotate"; B = Math.min(80, 14 + Math.min(maxLen, 14) * 5.2); }

			var ph = H - T - B;
			var lo = 0, hi = 0;
			for (i = 0; i < n; i++) {
				lo = Math.min(lo, bars[i].from, bars[i].to);
				hi = Math.max(hi, bars[i].from, bars[i].to);
			}
			var span = (hi - lo) || 1;
			hi += span * 0.04;
			lo -= lo < 0 ? span * 0.08 : 0;
			var y = function (v) { return T + (hi - v) * ph / (hi - lo); };
			this.__geo = { cx: function (i) { return L + step * (i + 0.5); }, y: y, W: W, H: H };

			var selected = this.getSelected();
			var hoverFill = this.__tint(this.__css("menuHighlight"), 0.55);
			var s = "";

			// the zero line
			s += "<line x1='" + L + "' x2='" + (W - R) + "' y1='" + f1(y(0)) + "' y2='" + f1(y(0)) + "' stroke='" + border + "' stroke-width='1'/>";

			// step bands: hover and selection, and the hit area of each step
			for (i = 0; i < n; i++) {
				var sel = bars[i].step.label === selected;
				var fill = sel ? this.__tint(accent, 0.1) : "transparent";
				s += "<rect data-i='" + i + "' data-fill='" + fill + "' x='" + f1(L + step * i) + "' y='" + T + "' width='" + f1(step) + "' height='" + f1(ph) + "' rx='4' fill='" + fill + "' style='cursor:pointer'/>";
			}

			// connectors, bars, value labels, x labels
			for (i = 0; i < n; i++) {
				var b = bars[i], cx = this.__geo.cx(i);
				var yTop = y(Math.max(b.from, b.to)), h = Math.max(1.5, Math.abs(y(b.from) - y(b.to)));

				if (i < n - 1)
					s += "<line x1='" + f1(cx + bw / 2) + "' x2='" + f1(cx + step - bw / 2) + "' y1='" + f1(y(b.run)) + "' y2='" + f1(y(b.run)) + "' stroke='" + gray + "' stroke-width='1' stroke-dasharray='2 2' opacity='.8' pointer-events='none'/>";

				var color = b.step.kind === "total" ? accent : (b.value >= 0 ? positive : negative);
				s += "<rect x='" + f1(cx - bw / 2) + "' y='" + f1(yTop) + "' width='" + f1(bw) + "' height='" + f1(h) + "' rx='2' fill='" + color + "' pointer-events='none'/>";

				var below = b.value < 0 && b.step.kind !== "total" || (b.step.kind === "total" && b.value < 0);
				s += "<text x='" + f1(cx) + "' y='" + f1(below ? yTop + h + 12 : yTop - 4) + "' text-anchor='middle' fill='" + text + "' font-size='11' font-weight='600' pointer-events='none'>" + esc(this.__fmt(b.value)) + "</text>";

				var label = String(b.step.label);
				if (mode === "rotate") {
					s += "<text transform='translate(" + f1(cx + 3) + "," + (H - B + 12) + ") rotate(-35)' text-anchor='end' fill='" + gray + "' font-size='11' pointer-events='none'>" + esc(label) + "</text>";
				}
				else {
					var ls = mode === "wrap" ? lines[i] : [label];
					for (var k = 0; k < ls.length; k++)
						s += "<text x='" + f1(cx) + "' y='" + (H - B + 14 + k * 13) + "' text-anchor='middle' fill='" + gray + "' font-size='11' pointer-events='none'>" + esc(ls[k]) + "</text>";
				}
			}

			this.__hoverFill = hoverFill;
			this.__chart.setHtml(
				"<svg width='" + W + "' height='" + H + "' style='display:block;overflow:visible;font-family:inherit' role='img' aria-label='" + esc(this.getTitle() || "P&L waterfall") + "'>" + s + "</svg>");
		},

		__hoverFill: null,

		/**
		 * The index of the step band under a pointer event, or -1.
		 */
		__indexOf: function (e) {

			var el = e.getOriginalTarget();
			if (!el || !el.getAttribute)
				return -1;
			var i = el.getAttribute("data-i");
			if (i == null || i === "")
				return -1;
			i = parseInt(i, 10);
			return this.__bars && i >= 0 && i < this.__bars.length ? i : -1;
		},

		/**
		 * Hovering a step band: highlights it and shows its tooltip.
		 */
		_onChartPointerOver: function (e) {

			var i = this.__indexOf(e);
			if (i < 0 || !this.__geo)
				return;

			e.getOriginalTarget().setAttribute("fill", this.__hoverFill);
			this.__hover = i;

			var esc = qx.bom.String.escape;
			var b = this.__bars[i];
			var text = this.__css("windowText");
			var row = function (name, value) {
				return "<div style='display:flex;justify-content:space-between;gap:14px'><span>" + name + "</span><b style='font-weight:600;color:" + text + "'>" + value + "</b></div>";
			};
			this.__tip.setValue(
				"<div style='font-weight:600;margin-bottom:3px;color:" + text + "'>" + esc(String(b.step.label)) + "</div>" +
				row("Value", esc(this.__fmt(b.value))) +
				row("Running total", esc(this.__fmt(b.run))));
			this.__tip.setVisibility("visible");

			// beside the bar, flipped to the left near the right edge, kept inside the chart
			var geo = this.__geo;
			var hint = this.__tip.getSizeHint();
			var tw = hint.width, th = hint.height;
			var half = Math.min(64, (geo.W - 12) / this.__bars.length * 0.64) / 2;
			var tx = geo.cx(i) + half + 8;
			if (tx + tw > geo.W)
				tx = geo.cx(i) - half - 8 - tw;
			var ty = Math.max(Math.min(geo.y(b.from), geo.y(b.to)) - th - 4, 0);
			ty = Math.max(0, Math.min(geo.H - th, ty));
			this.__tip.setLayoutProperties({ left: Math.max(0, Math.round(tx)), top: Math.round(ty) });
		},

		/**
		 * Leaving a step band: restores it and hides the tooltip.
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
		 * A step band was tapped: report its label; the server sets selected and renders it back.
		 */
		_onChartTap: function (e) {

			var i = this.__indexOf(e);
			if (i < 0 || !this.isEnabled())
				return;

			this.fireDataEvent("stepSelected", { label: String(this.__bars[i].step.label) });
		}
	},

	/**
	 * Drops the theme listener; the child widgets are disposed by the container.
	 */
	destruct: function () {

		qx.theme.manager.Color.getInstance().removeListener("changeTheme", this._applyAll, this);
		this.__title = this.__totalLabel = this.__totalValue = this.__chart = this.__tip = null;
		this.__bars = this.__geo = null;
	}
});
