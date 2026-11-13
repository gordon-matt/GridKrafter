/**
 * gridkrafter.wisejdemo.FreightLaneMatrix
 *
 * The client half of GridKrafter.WisejDemo.FreightLaneMatrix: the All / Rail / Truck / Barge filter,
 * a matrix of lanes by mode (rate in $/gal, transit days and the trend against last month, the cheapest
 * mode of each lane highlighted) and a demurrage aging strip with the cars and dollars of each bucket.
 * The server owns every value; the browser only reports what the user picks.
 */
qx.Class.define("gridkrafter.wisejdemo.FreightLaneMatrix", {

	extend: wisej.web.Control,

	/**
	 * Builds the fixed parts once: the header with the mode filter, the matrix and the demurrage strip.
	 */
	construct: function () {

		this.base(arguments, new qx.ui.layout.VBox(8));

		this.setFocusable(false);

		var Self = gridkrafter.wisejdemo.FreightLaneMatrix;

		// header: title, the units, and the mode filter
		var head = new qx.ui.container.Composite(new qx.ui.layout.HBox(10).set({ alignY: "middle" }));
		head.add(new qx.ui.basic.Label("Freight lanes").set({ appearance: "freight-lane-matrix/title", rich: false }));
		this.__unit = new qx.ui.basic.Label("$/gal · transit days · vs last month").set({ appearance: "freight-lane-matrix/unit", rich: false, allowShrinkX: true });
		head.add(this.__unit, { flex: 1 });
		this.__modes = new qx.ui.container.Composite(new qx.ui.layout.HBox(2)).set({ appearance: "freight-lane-matrix/modes" });
		this.__modeButtons = {};
		for (var i = 0; i < Self.MODES.length; i++) {
			var button = new qx.ui.basic.Label(Self.MODES[i].text).set({ appearance: "freight-lane-matrix/mode-button", rich: false, cursor: "pointer" });
			button.setUserData("mode", Self.MODES[i].key);
			button.getContentElement().setAttribute("role", "button");
			button.addListener("tap", this._onModeTap, this);
			this.__hover(button);
			this.__modeButtons[Self.MODES[i].key] = button;
			this.__modes.add(button);
		}
		head.add(this.__modes);
		this.add(head);

		// matrix: the column header and the scrolling lanes
		var matrix = new qx.ui.container.Composite(new qx.ui.layout.VBox(0)).set({ appearance: "freight-lane-matrix/matrix" });
		this.__columns = new qx.ui.container.Composite(new qx.ui.layout.HBox(8).set({ alignY: "middle" })).set({ appearance: "freight-lane-matrix/columns" });
		matrix.add(this.__columns);
		this.__lanes = new qx.ui.container.Composite(new qx.ui.layout.VBox(0));
		var scroll = new qx.ui.container.Scroll(this.__lanes).set({ scrollbarX: "off" });
		matrix.add(scroll, { flex: 1 });
		this.add(matrix, { flex: 1 });

		// demurrage aging strip
		var strip = new qx.ui.container.Composite(new qx.ui.layout.VBox(5)).set({ appearance: "freight-lane-matrix/demurrage" });
		var title = new qx.ui.container.Composite(new qx.ui.layout.HBox(8));
		title.add(new qx.ui.basic.Label("Demurrage aging").set({ appearance: "freight-lane-matrix/demurrage-title", rich: false }), { flex: 1 });
		this.__demurrageTotal = new qx.ui.basic.Label("").set({ appearance: "freight-lane-matrix/demurrage-total", rich: false });
		title.add(this.__demurrageTotal);
		strip.add(title);
		this.__buckets = new qx.ui.container.Composite(new qx.ui.layout.HBox(6));
		strip.add(this.__buckets);
		this.add(strip);

		qx.theme.manager.Color.getInstance().addListener("changeTheme", this._applyLanes, this);
		this.addListener("resize", this._onResize, this);

		this._applyLanes();
	},

	statics: {

		/**
		 * The modes of the filter, in order; the last three are also the columns of the matrix.
		 */
		MODES: [
			{ key: "all", text: "All" },
			{ key: "rail", text: "Rail" },
			{ key: "truck", text: "Truck" },
			{ key: "barge", text: "Barge" }
		],

		/**
		 * Below this width the matrix stacks days over trend and hides the units.
		 */
		COMPACT_WIDTH: 480
	},

	events: {

		/**
		 * The user selected a lane: { lane, selected }.
		 */
		laneSelected: "qx.event.type.Data",

		/**
		 * The user picked a mode: { mode }.
		 */
		modeChanged: "qx.event.type.Data"
	},

	properties: {

		/**
		 * The theme appearance of the root widget.
		 */
		appearance: { init: "freight-lane-matrix", refine: true },

		/**
		 * The mode shown: "all" for every column, or one mode.
		 */
		mode: { init: "all", check: ["all", "rail", "truck", "barge"], apply: "_applyLanes" },

		/**
		 * The lane string of the selected lane.
		 */
		selected: { init: "", check: "String", apply: "_applyLanes" },

		/**
		 * The lanes, as the server normalized them.
		 */
		lanes: { init: null, nullable: true, check: "Array", apply: "_applyLanes" },

		/**
		 * The demurrage buckets, as the server normalized them.
		 */
		demurrage: { init: null, nullable: true, check: "Array", apply: "_applyLanes" },

		/**
		 * The theme's colour of a rate that fell (cheaper) and of the cheapest mode, set by the mixin.
		 */
		themePositiveColor: { init: "#1fb57a", check: "Color", themeable: true, apply: "_applyLanes" },

		/**
		 * The theme's colour of a rate that rose (dearer) and of the aged demurrage, set by the mixin.
		 */
		themeNegativeColor: { init: "#e5484d", check: "Color", themeable: true, apply: "_applyLanes" }
	},

	members: {

		__unit: null,
		__modes: null,
		__modeButtons: null,
		__columns: null,
		__lanes: null,
		__demurrageTotal: null,
		__buckets: null,
		__pending: false,
		__compact: false,

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
		 * Dollars, short above 10,000.
		 */
		__money: function (value) {

			if (value >= 10000)
				return "$" + (value / 1000).toFixed(1).replace(/\.0$/, "") + "k";
			return "$" + Math.round(value).toLocaleString();
		},

		/**
		 * Every property draws the whole widget again, once per round of changes.
		 */
		_applyLanes: function () {

			if (!this.__buckets || this.__pending)
				return;

			this.__pending = true;
			qx.event.Timer.once(function () {
				this.__pending = false;
				if (this.isDisposed())
					return;
				this.__drawHeader();
				this.__drawLanes();
				this.__drawDemurrage();
			}, this, 0);
		},

		/**
		 * The mode filter and the column header.
		 */
		__drawHeader: function () {

			var mode = this.getMode();
			for (var key in this.__modeButtons) {
				if (key === mode)
					this.__modeButtons[key].addState("selected");
				else
					this.__modeButtons[key].removeState("selected");
				this.__modeButtons[key].getContentElement().setAttribute("aria-pressed", key === mode ? "true" : "false");
			}

			this.__unit.setVisibility(this.__compact ? "excluded" : "visible");

			var old = this.__columns.removeAll();
			for (var i = 0; i < old.length; i++)
				old[i].dispose();

			var cols = this.__cols();
			this.__columns.getLayout().setSpacing(this.__compact ? 4 : 8);
			this.__columns.add(new qx.ui.basic.Label("LANE").set({ appearance: "freight-lane-matrix/column", rich: false, minWidth: 96, allowShrinkX: true }), { flex: 3 });
			for (var i = 0; i < cols.length; i++)
				this.__columns.add(new qx.ui.basic.Label(cols[i].toUpperCase()).set({ appearance: "freight-lane-matrix/column", rich: false, textAlign: "center", allowShrinkX: true }), { flex: 2 });
		},

		/**
		 * The mode columns shown.
		 */
		__cols: function () {

			var mode = this.getMode();
			return mode === "all" ? ["rail", "truck", "barge"] : [mode];
		},

		/**
		 * The lanes, one row each, with a cell per mode shown.
		 */
		__drawLanes: function () {

			var old = this.__lanes.removeAll();
			for (var i = 0; i < old.length; i++)
				old[i].dispose();

			var lanes = this.getLanes() || [];
			if (!lanes.length) {
				this.__lanes.add(new qx.ui.basic.Label("No lanes").set({ appearance: "freight-lane-matrix/empty", rich: false, allowGrowX: true, textAlign: "center" }));
				return;
			}

			var cols = this.__cols();
			for (var i = 0; i < lanes.length; i++) {
				var l = lanes[i];

				var best = "", rate = 0, modes = ["rail", "truck", "barge"];
				for (var m = 0; m < modes.length; m++) {
					var r = l[modes[m] + "Rate"] || 0;
					if (r > 0 && (!best || r < rate)) {
						best = modes[m];
						rate = r;
					}
				}

				var row = new qx.ui.container.Composite(new qx.ui.layout.HBox(this.__compact ? 4 : 8).set({ alignY: "middle" })).set({ appearance: "freight-lane-matrix/lane", cursor: "pointer", toolTipText: l.lane });
				row.setUserData("lane", l.lane);
				row.getContentElement().setAttribute("role", "button");
				if (l.lane === this.getSelected())
					row.addState("selected");
				this.__hover(row);
				row.addListener("tap", this._onLaneTap, this);

				row.add(new qx.ui.basic.Label((l.origin || "") + " → " + (l.destination || "")).set({ appearance: "freight-lane-matrix/lane-name", rich: false, anonymous: true, minWidth: 96, allowShrinkX: true }), { flex: 3 });
				for (var c = 0; c < cols.length; c++)
					row.add(this.__cell(l, cols[c], cols[c] === best), { flex: 2 });

				this.__lanes.add(row);
			}
		},

		/**
		 * One cell: the rate, and below it the transit days and the trend; a dash when the mode is not available.
		 */
		__cell: function (l, mode, best) {

			var rate = l[mode + "Rate"] || 0;
			var cell = new qx.ui.container.Composite(new qx.ui.layout.VBox(0).set({ alignX: "center" })).set({ appearance: "freight-lane-matrix/cell", anonymous: true, allowShrinkX: true });

			if (rate <= 0) {
				cell.addState("unavailable");
				cell.add(new qx.ui.basic.Label("—").set({ appearance: "freight-lane-matrix/cell-rate", rich: false, anonymous: true, textAlign: "center", allowGrowX: true }));
				return cell;
			}

			if (best) {
				cell.addState("best");
				cell.setToolTipText(mode.charAt(0).toUpperCase() + mode.slice(1) + " · cheapest");
			}

			var rateLabel = new qx.ui.basic.Label("$" + rate.toFixed(3)).set({ appearance: "freight-lane-matrix/cell-rate", rich: false, anonymous: true, textAlign: "center", allowGrowX: true });
			if (best)
				rateLabel.setTextColor(this.__css(this.getThemePositiveColor()));
			cell.add(rateLabel);

			var trend = l[mode + "Trend"] || 0;
			var sub = new qx.ui.container.Composite(this.__compact ? new qx.ui.layout.VBox(0).set({ alignX: "center" }) : new qx.ui.layout.HBox(5).set({ alignX: "center" })).set({ anonymous: true });
			sub.add(new qx.ui.basic.Label((l[mode + "Days"] || 0) + "d").set({ appearance: "freight-lane-matrix/cell-days", rich: false, anonymous: true }));
			var arrow = Math.abs(trend) < 0.05 ? "–" : (trend > 0 ? "▲" : "▼") + Math.abs(trend).toFixed(1) + "%";
			var trendLabel = new qx.ui.basic.Label(arrow).set({ appearance: "freight-lane-matrix/cell-trend", rich: false, anonymous: true });
			if (Math.abs(trend) >= 0.05)
				trendLabel.setTextColor(this.__css(trend > 0 ? this.getThemeNegativeColor() : this.getThemePositiveColor()));
			sub.add(trendLabel);
			cell.add(sub);

			return cell;
		},

		/**
		 * The demurrage strip: the totals, and a bucket per aging range tinted from neutral to the alert colour.
		 */
		__drawDemurrage: function () {

			var old = this.__buckets.removeAll();
			for (var i = 0; i < old.length; i++)
				old[i].dispose();

			var list = this.getDemurrage() || [];
			var cars = 0, amount = 0;
			for (var i = 0; i < list.length; i++) {
				cars += list[i].cars || 0;
				amount += list[i].amount || 0;
			}
			this.__demurrageTotal.setValue(cars + " cars · " + this.__money(amount) + " accrued");

			var alert = this.getThemeNegativeColor();
			for (var i = 0; i < list.length; i++) {
				var b = list[i];
				var t = list.length > 1 ? i / (list.length - 1) * 0.45 : 0;
				var hot = i >= list.length - 2 && list.length > 1;

				var bucket = new qx.ui.container.Composite(new qx.ui.layout.VBox(0)).set({ appearance: "freight-lane-matrix/bucket", allowShrinkX: true });
				bucket.addState("level" + Math.round(t / 0.45 * 3));
				bucket.add(new qx.ui.basic.Label((b.bucket || "") + " days").set({ appearance: "freight-lane-matrix/bucket-label", rich: false, anonymous: true }));
				var count = new qx.ui.basic.Label((b.cars || 0) + " cars").set({ appearance: "freight-lane-matrix/bucket-cars", rich: false, anonymous: true });
				var money = new qx.ui.basic.Label(this.__money(b.amount || 0)).set({ appearance: "freight-lane-matrix/bucket-amount", rich: false, anonymous: true });
				if (hot) {
					count.setTextColor(this.__css(alert));
					money.setTextColor(this.__css(alert));
				}
				bucket.add(count);
				bucket.add(money);
				this.__buckets.add(bucket, { flex: 1 });
			}
		},

		/**
		 * A mode button: ask for that mode.
		 */
		_onModeTap: function (e) {

			var key = e.getCurrentTarget().getUserData("mode");
			if (this.isEnabled() && key !== this.getMode())
				this.fireDataEvent("modeChanged", { mode: key });
		},

		/**
		 * A lane row: select it.
		 */
		_onLaneTap: function (e) {

			var lane = e.getCurrentTarget().getUserData("lane");
			if (this.isEnabled() && lane !== this.getSelected())
				this.fireDataEvent("laneSelected", { lane: lane, selected: lane });
		},

		/**
		 * Switches between the roomy and the compact layout when the width crosses the limit.
		 */
		_onResize: function (e) {

			var compact = e.getData().width < gridkrafter.wisejdemo.FreightLaneMatrix.COMPACT_WIDTH;
			if (compact !== this.__compact) {
				this.__compact = compact;
				this._applyLanes();
			}
		}
	},

	/**
	 * Drops the theme listener; the child widgets are disposed by the container.
	 */
	destruct: function () {

		qx.theme.manager.Color.getInstance().removeListener("changeTheme", this._applyLanes, this);

		this.__unit = this.__modes = this.__modeButtons = this.__columns = this.__lanes = this.__demurrageTotal = this.__buckets = null;
	}
});
