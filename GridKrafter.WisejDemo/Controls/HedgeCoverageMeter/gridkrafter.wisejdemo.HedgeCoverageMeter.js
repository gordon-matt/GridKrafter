/**
 * gridkrafter.wisejdemo.HedgeCoverageMeter
 *
 * The client half of GridKrafter.WisejDemo.HedgeCoverageMeter: a header (title, the weighted coverage of the
 * next 12 months, the count of months out of policy), a legend, and one column per month whose bar rises to the
 * hedged % of the exposure, split into futures (solid) and fixed-price physical (lighter), against a shaded
 * policy band. In policy is the accent, under-hedged the negative colour, over-hedged the amber. The server
 * owns every value; the browser only reports the month the user picks.
 */
qx.Class.define("gridkrafter.wisejdemo.HedgeCoverageMeter", {

	extend: wisej.web.Control,

	/**
	 * Builds the header, the legend and the chart frame once; the columns are drawn from the data.
	 */
	construct: function () {

		this.base(arguments, new qx.ui.layout.VBox(8));

		this.setFocusable(false);

		var head = new qx.ui.container.Composite(new qx.ui.layout.HBox(10));
		var headline = new qx.ui.container.Composite(new qx.ui.layout.VBox(0));
		this.__title = new qx.ui.basic.Label("").set({ appearance: "hedge-coverage-meter/title", rich: false });
		headline.add(this.__title);
		var valuerow = new qx.ui.container.Composite(new qx.ui.layout.HBox(8).set({ alignY: "bottom" }));
		this.__total = new qx.ui.basic.Label("0%").set({ appearance: "hedge-coverage-meter/total", rich: false });
		valuerow.add(this.__total);
		valuerow.add(new qx.ui.basic.Label("weighted, next 12 months").set({ appearance: "hedge-coverage-meter/caption", rich: false, paddingBottom: 5 }));
		headline.add(valuerow);
		head.add(headline, { flex: 1 });
		this.__flag = new qx.ui.basic.Label("").set({ appearance: "hedge-coverage-meter/flag", rich: false, alignY: "top", allowGrowY: false });
		head.add(this.__flag);
		this.add(head);

		this.__legend = new qx.ui.container.Composite(new qx.ui.layout.Flow(14, 4));
		this.add(this.__legend);

		var chart = new qx.ui.container.Composite(new qx.ui.layout.HBox(6));
		var left = new qx.ui.container.Composite(new qx.ui.layout.VBox(0)).set({ width: 34, allowGrowX: false });
		this.__axis = new qx.ui.container.Composite(new qx.ui.layout.Canvas()).set({ marginTop: 14 });
		left.add(this.__axis, { flex: 1 });
		left.add(new qx.ui.core.Spacer(10, 20));
		chart.add(left);

		var right = new qx.ui.container.Composite(new qx.ui.layout.VBox(0));
		this.__plot = new qx.ui.container.Composite(new qx.ui.layout.Canvas()).set({ marginTop: 14 });
		right.add(this.__plot, { flex: 1 });
		this.__months = new qx.ui.container.Composite(new qx.ui.layout.Canvas()).set({ height: 20, allowGrowY: false });
		right.add(this.__months);
		chart.add(right, { flex: 1 });
		this.add(chart, { flex: 1 });

		qx.theme.manager.Color.getInstance().addListener("changeTheme", this._applyMonths, this);

		this._applyMonths();
	},

	events: {

		/**
		 * The user selected a month: { month, selected }.
		 */
		monthSelected: "qx.event.type.Data"
	},

	properties: {

		/**
		 * The theme appearance of the root widget.
		 */
		appearance: { init: "hedge-coverage-meter", refine: true },

		/**
		 * The label above the weighted coverage.
		 */
		title: { init: "Hedge coverage", check: "String", apply: "_applyMonths" },

		/**
		 * The volume unit shown in the tooltips.
		 */
		unit: { init: "", check: "String", apply: "_applyMonths" },

		/**
		 * The lower edge of the policy band, in percent.
		 */
		policyMin: { init: 50, check: "Number", apply: "_applyMonths" },

		/**
		 * The upper edge of the policy band, in percent.
		 */
		policyMax: { init: 80, check: "Number", apply: "_applyMonths" },

		/**
		 * The selected month.
		 */
		selected: { init: "", check: "String", apply: "_applyMonths" },

		/**
		 * The months: [{ month, exposure, futures, physical }].
		 */
		months: { init: null, nullable: true, check: "Array", apply: "_applyMonths" },

		/**
		 * The theme's colours: in policy, under-hedged, over-hedged, all in policy. Set by the mixin.
		 */
		themeInColor: { init: "#6d4dff", check: "Color", themeable: true, apply: "_applyMonths" },
		themeBelowColor: { init: "#e5484d", check: "Color", themeable: true, apply: "_applyMonths" },
		themeAboveColor: { init: "#f2a516", check: "Color", themeable: true, apply: "_applyMonths" },
		themeOkColor: { init: "#1fb57a", check: "Color", themeable: true, apply: "_applyMonths" }
	},

	members: {

		__title: null,
		__total: null,
		__flag: null,
		__legend: null,
		__axis: null,
		__plot: null,
		__months: null,
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
		 * Every property draws the widget again, once per round of changes.
		 */
		_applyMonths: function () {

			if (!this.__plot || this.__pending)
				return;

			this.__pending = true;
			qx.event.Timer.once(function () {
				this.__pending = false;
				if (!this.isDisposed())
					this.__draw();
			}, this, 0);
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
		 * A number with thousands separators.
		 */
		__fmt: function (v) {

			return Math.round(v).toLocaleString();
		},

		/**
		 * The header, the legend, the axis and the columns.
		 */
		__draw: function () {

			var inColor = this.__css(this.getThemeInColor()), below = this.__css(this.getThemeBelowColor());
			var above = this.__css(this.getThemeAboveColor()), ok = this.__css(this.getThemeOkColor());
			var gray = this.__css("grayText");
			var pmin = this.getPolicyMin() || 0, pmax = this.getPolicyMax() || 0;
			var colors = { "in": inColor, below: below, above: above };

			var rows = [];
			var source = this.getMonths() || [];
			for (var i = 0; i < source.length; i++) {
				var m = source[i];
				var exp = m.exposure || 0, fut = m.futures || 0, phy = m.physical || 0;
				var hedged = fut + phy, pct = exp ? hedged / exp * 100 : 0;
				rows.push({
					month: m.month, exp: exp, fut: fut, phy: phy, hedged: hedged, pct: pct,
					status: pct < pmin ? "below" : pct > pmax ? "above" : "in"
				});
			}

			// header
			this.__title.setValue(this.getTitle() || "");
			var te = 0, th = 0, out = 0;
			for (var i = 0; i < rows.length; i++) {
				if (i < 12) {
					te += rows[i].exp;
					th += rows[i].hedged;
				}
				if (rows[i].status !== "in")
					out++;
			}
			this.__total.setValue((te ? Math.round(th / te * 100) : 0) + "%");
			this.__flag.setValue(out ? out + (out === 1 ? " month" : " months") + " out of policy" : "All months in policy");
			this.__flag.addState(out ? "bad" : "ok");
			this.__flag.removeState(out ? "ok" : "bad");

			// legend
			this.__clear(this.__legend);
			var entries = [
				[inColor, "In policy", 1], [below, "Under-hedged", 1], [above, "Over-hedged", 1],
				[gray, "Futures", 1], [gray, "Fixed-price physical", 0.45],
				[inColor, "Policy " + pmin + "–" + pmax + "%", 0.25]
			];
			for (var i = 0; i < entries.length; i++)
				this.__legend.add(this.__legendItem(entries[i][0], entries[i][1], entries[i][2]));

			// scale: 0 to 100 (or the highest bar rounded up), plus headroom for the label of the tallest bar
			var peak = 100;
			for (var i = 0; i < rows.length; i++)
				peak = Math.max(peak, Math.ceil(rows[i].pct / 10) * 10);
			var scale = peak + 12;
			var y = function (v) { return (v / scale * 100); };

			// axis
			this.__clear(this.__axis);
			var ticks = [0, 25, 50, 75, 100];
			for (var i = 0; i < ticks.length; i++) {
				if (ticks[i] > peak)
					continue;
				this.__axis.add(new qx.ui.basic.Label(ticks[i] + "%").set({ appearance: "hedge-coverage-meter/axis", rich: false, textAlign: "right", marginBottom: -8 }),
					{ right: 0, left: 0, bottom: y(ticks[i]) + "%" });
			}

			// plot
			this.__clear(this.__plot);
			this.__clear(this.__months);
			var n = rows.length;
			if (!n) {
				this.__plot.add(new qx.ui.basic.Label("No hedge data").set({ appearance: "hedge-coverage-meter/empty", rich: false, textAlign: "center", allowGrowX: true }), { left: 0, right: 0, top: 0 });
				return;
			}

			for (var i = 0; i < ticks.length; i++) {
				if (ticks[i] > peak)
					continue;
				var line = new qx.ui.core.Widget().set({ appearance: "hedge-coverage-meter/line", height: 1, anonymous: true });
				if (ticks[i] > 0) {
					line.setBackgroundColor(null);
					line.setOpacity(0.6);
					line.getContentElement().setStyle("borderTop", "1px dashed " + this.__css("windowFrame"));
				}
				this.__plot.add(line, { left: 0, right: 0, bottom: y(ticks[i]) + "%" });
			}

			if (pmax > pmin) {
				var band = new qx.ui.core.Widget().set({ appearance: "hedge-coverage-meter/band", backgroundColor: inColor, opacity: 0.12, anonymous: true });
				this.__plot.add(band, { left: 0, right: 0, bottom: y(pmin) + "%", height: y(pmax - pmin) + "%" });
				var edges = [pmin, pmax];
				for (var e = 0; e < edges.length; e++) {
					var edge = new qx.ui.core.Widget().set({ height: 1, opacity: 0.7, anonymous: true });
					edge.getContentElement().setStyle("borderTop", "1px dashed " + inColor);
					this.__plot.add(edge, { left: 0, right: 0, bottom: y(edges[e]) + "%" });
				}
			}

			for (var i = 0; i < n; i++)
				this.__createColumn(rows[i], i, n, y, colors);
		},

		/**
		 * A legend entry: a swatch and its text.
		 */
		__legendItem: function (color, text, opacity) {

			var item = new qx.ui.container.Composite(new qx.ui.layout.HBox(5).set({ alignY: "middle" }));
			var swatch = new qx.ui.core.Widget().set({ appearance: "hedge-coverage-meter/swatch", width: 10, height: 8, allowGrowY: false, backgroundColor: color, opacity: opacity });
			item.add(swatch);
			item.add(new qx.ui.basic.Label(text).set({ appearance: "hedge-coverage-meter/legend", rich: false }));
			return item;
		},

		/**
		 * One month: its hit area, its two-segment bar, its percent and its name.
		 */
		__createColumn: function (r, index, n, y, colors) {

			var esc = qx.bom.String.escape;
			var cell = 100 / n;
			var left = index * cell;
			var color = colors[r.status];
			var selected = r.month === this.getSelected();

			var column = new qx.ui.core.Widget().set({ appearance: "hedge-coverage-meter/column", cursor: "pointer" });
			column.setUserData("month", r.month);
			column.getContentElement().setAttribute("role", "button");
			column.getContentElement().setAttribute("aria-label", r.month + ": " + Math.round(r.pct) + "% hedged");
			if (selected)
				column.addState("selected");
			this.__hover(column);
			column.addListener("tap", this._onColumnTap, this);
			column.setToolTipText("<b>" + esc(r.month) + "</b> · " + Math.round(r.pct) + "% hedged<br/>Hedged " + this.__fmt(r.hedged) + " of " + this.__fmt(r.exp) + " " + esc(this.getUnit()) +
				"<br/>Futures " + this.__fmt(r.fut) + " · Physical " + this.__fmt(r.phy) +
				"<br/>Policy " + this.getPolicyMin() + "–" + this.getPolicyMax() + "%");
			this.__plot.add(column, { left: left + "%", width: cell + "%", top: 0, bottom: 0 });

			var barLeft = (left + cell * 0.18) + "%", barWidth = (cell * 0.64) + "%";
			var futH = y(r.exp ? Math.min(r.fut, r.hedged) / r.exp * 100 : 0);
			var phyH = y(r.exp ? r.phy / r.exp * 100 : 0);
			if (futH > 0)
				this.__plot.add(new qx.ui.core.Widget().set({ appearance: "hedge-coverage-meter/bar", backgroundColor: color, anonymous: true }),
					{ left: barLeft, width: barWidth, bottom: 0, height: futH + "%" });
			if (phyH > 0)
				this.__plot.add(new qx.ui.core.Widget().set({ appearance: "hedge-coverage-meter/bar", backgroundColor: color, opacity: 0.45, anonymous: true }),
					{ left: barLeft, width: barWidth, bottom: futH + "%", height: phyH + "%" });

			var pct = new qx.ui.basic.Label(Math.round(r.pct) + "%").set({ appearance: "hedge-coverage-meter/pct", rich: false, textAlign: "center", anonymous: true });
			if (r.status === "below")
				pct.setTextColor(colors.below);
			this.__plot.add(pct, { left: left + "%", width: cell + "%", bottom: y(r.pct) + "%" });

			var month = new qx.ui.basic.Label(r.month).set({ appearance: "hedge-coverage-meter/month", rich: false, textAlign: "center", cursor: "pointer", allowGrowX: true });
			month.setUserData("month", r.month);
			if (selected)
				month.addState("selected");
			month.addListener("tap", this._onColumnTap, this);
			this.__months.add(month, { left: left + "%", width: cell + "%", top: 3 });
		},

		/**
		 * A month: select it.
		 */
		_onColumnTap: function (e) {

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
		this.__title = this.__total = this.__flag = this.__legend = this.__axis = this.__plot = this.__months = null;
	}
});
