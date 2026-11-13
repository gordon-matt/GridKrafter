/**
 * gridkrafter.wisejdemo.LiftingScheduleTracker
 *
 * The client half of GridKrafter.WisejDemo.LiftingScheduleTracker: a title with its period and a total
 * line, sortable column captions, and one row per item with a planned track, the actual as its fill,
 * a pace marker shared by every row, the share done and a status chip (Ahead, On pace, Behind, Over).
 * The server owns every value; the browser only reports what the user picks.
 */
qx.Class.define("gridkrafter.wisejdemo.LiftingScheduleTracker", {

	extend: wisej.web.Control,

	/**
	 * Builds the fixed parts once: header, total line, column captions and the scrolling list.
	 */
	construct: function () {

		this.base(arguments, new qx.ui.layout.VBox(6));

		this.setFocusable(false);

		var head = new qx.ui.container.Composite(new qx.ui.layout.HBox(8).set({ alignY: "bottom" }));
		this.__title = new qx.ui.basic.Label("").set({ appearance: "lifting-schedule-tracker/title", rich: false });
		this.__period = new qx.ui.basic.Label("").set({ appearance: "lifting-schedule-tracker/period", rich: false });
		this.__total = new qx.ui.basic.Label("").set({ appearance: "lifting-schedule-tracker/total", rich: true });
		head.add(this.__title);
		head.add(this.__period, { flex: 1 });
		head.add(this.__total);
		this.add(head);

		this.__caption = new qx.ui.container.Composite(new qx.ui.layout.HBox(8).set({ alignY: "middle" })).set({ appearance: "lifting-schedule-tracker/caption" });
		this.add(this.__caption);

		this.__rows = new qx.ui.container.Composite(new qx.ui.layout.VBox(1));
		this.add(new qx.ui.container.Scroll(this.__rows).set({ scrollbarX: "off" }), { flex: 1 });

		this.addListener("resize", this._onResize, this);
		qx.theme.manager.Color.getInstance().addListener("changeTheme", this._applyRows, this);

		this._applyRows();
	},

	statics: {

		/**
		 * The statuses: their key, their text and the theme property that colours them.
		 */
		STATUS: {
			ahead: { text: "Ahead", color: "AheadColor" },
			pace: { text: "On pace", color: "PaceColor" },
			behind: { text: "Behind", color: "BehindColor" },
			over: { text: "Over", color: "OverColor" }
		},

		/**
		 * Below this width the amounts and the status chip are left out.
		 */
		NARROW: 460
	},

	events: {

		/**
		 * The user selected a row: { label, selected }.
		 */
		rowSelected: "qx.event.type.Data",

		/**
		 * The user clicked a sortable caption: { sortBy, sortDescending }.
		 */
		sortChanged: "qx.event.type.Data"
	},

	properties: {

		/**
		 * The theme appearance of the root widget.
		 */
		appearance: { init: "lifting-schedule-tracker", refine: true },

		/**
		 * The title.
		 */
		title: { init: "Schedule", check: "String", apply: "_applyRows" },

		/**
		 * The caption of the planned amount.
		 */
		plannedLabel: { init: "Planned", check: "String", apply: "_applyRows" },

		/**
		 * The caption of the actual amount.
		 */
		actualLabel: { init: "Actual", check: "String", apply: "_applyRows" },

		/**
		 * The period the amounts cover.
		 */
		period: { init: "", check: "String", apply: "_applyRows" },

		/**
		 * The unit of the amounts.
		 */
		unit: { init: "", check: "String", apply: "_applyRows" },

		/**
		 * How far along the period is, 0 to 1.
		 */
		paceRatio: { init: 0, check: "Number", apply: "_applyRows" },

		/**
		 * The share of the planned amount a row may be off pace before it is Ahead or Behind.
		 */
		tolerance: { init: 0.05, check: "Number", apply: "_applyRows" },

		/**
		 * The sorted column: none, label, percent or variance.
		 */
		sortBy: { init: "none", check: ["none", "label", "percent", "variance"], apply: "_applyRows" },

		/**
		 * Whether the sort is descending.
		 */
		sortDescending: { init: false, check: "Boolean", apply: "_applyRows" },

		/**
		 * The label of the selected row.
		 */
		selected: { init: "", check: "String", apply: "_applyRows" },

		/**
		 * The rows, as the server normalized them: { label, sublabel, planned, actual }.
		 */
		rows: { init: null, nullable: true, check: "Array", apply: "_applyRows" },

		/**
		 * The theme's colour of each status, set by the mixin.
		 */
		themeAheadColor: { init: "#1fb57a", check: "Color", themeable: true, apply: "_applyRows" },
		themePaceColor: { init: "#5f6878", check: "Color", themeable: true, apply: "_applyRows" },
		themeBehindColor: { init: "#e5484d", check: "Color", themeable: true, apply: "_applyRows" },
		themeOverColor: { init: "#6d4dff", check: "Color", themeable: true, apply: "_applyRows" }
	},

	members: {

		__title: null,
		__period: null,
		__total: null,
		__caption: null,
		__rows: null,
		__pending: false,
		__narrow: false,

		/**
		 * Resolves a theme colour name or a CSS colour to CSS.
		 */
		__css: function (color) {

			var manager = qx.theme.manager.Color.getInstance();
			return manager.isDynamic(color) ? manager.resolve(color) : color;
		},

		/**
		 * The CSS colour of a status.
		 */
		__statusColor: function (key) {

			return this.__css(this["getTheme" + gridkrafter.wisejdemo.LiftingScheduleTracker.STATUS[key].color]());
		},

		/**
		 * Gives a widget the hovered state while the pointer is over it.
		 */
		__hover: function (widget) {

			widget.addListener("pointerover", function () { widget.addState("hovered"); });
			widget.addListener("pointerout", function () { widget.removeState("hovered"); });
		},

		/**
		 * A number as a short text: 950, 4.2K, 1.2M, 3B.
		 */
		__compact: function (value) {

			var sign = value < 0 ? "-" : "";
			var a = Math.abs(value);
			var f = function (x, s) {
				var t = x >= 100 ? x.toFixed(0) : x >= 10 ? x.toFixed(1) : x.toFixed(2);
				if (t.indexOf(".") >= 0)
					t = t.replace(/0+$/, "").replace(/\.$/, "");
				return sign + t + s;
			};
			if (a >= 1e9) return f(a / 1e9, "B");
			if (a >= 1e6) return f(a / 1e6, "M");
			if (a >= 1e3) return f(a / 1e3, "K");
			return sign + String(Math.round(a));
		},

		/**
		 * The rows with their share, their distance from pace and their status.
		 */
		__model: function () {

			var pace = Math.max(0, Math.min(1, this.getPaceRatio()));
			var tol = Math.max(0, this.getTolerance());
			var list = this.getRows() || [];
			var model = [];
			for (var i = 0; i < list.length; i++) {
				var r = list[i];
				var planned = Math.max(0, r.planned || 0), actual = Math.max(0, r.actual || 0);
				var expected = pace * planned;
				model.push({
					label: r.label, sub: r.sublabel || "", planned: planned, actual: actual,
					ratio: planned > 0 ? actual / planned : 0,
					variance: actual - expected,
					status: actual > planned ? "over" : actual < expected - tol * planned ? "behind" : actual > expected + tol * planned ? "ahead" : "pace"
				});
			}

			var by = this.getSortBy();
			if (by !== "none") {
				var sign = this.getSortDescending() ? -1 : 1;
				var key = function (m) { return by === "label" ? String(m.label).toLowerCase() : by === "percent" ? m.ratio : m.variance; };
				model.sort(function (a, b) { var x = key(a), y = key(b); return (x < y ? -1 : x > y ? 1 : 0) * sign; });
			}
			return model;
		},

		/**
		 * Every property draws the widget again, once per round of changes.
		 */
		_applyRows: function () {

			if (!this.__rows || this.__pending)
				return;

			this.__pending = true;
			qx.event.Timer.once(function () {
				this.__pending = false;
				if (this.isDisposed())
					return;
				this.__draw();
			}, this, 0);
		},

		/**
		 * The width changed: draw again when it crosses the narrow limit.
		 */
		_onResize: function (e) {

			var narrow = (e.getData().width || 0) < gridkrafter.wisejdemo.LiftingScheduleTracker.NARROW;
			if (narrow !== this.__narrow) {
				this.__narrow = narrow;
				this._applyRows();
			}
		},

		/**
		 * Header, total line, captions and rows.
		 */
		__draw: function () {

			var model = this.__model();
			var unit = this.getUnit();
			var plannedTotal = 0, actualTotal = 0;
			for (var i = 0; i < model.length; i++) {
				plannedTotal += model[i].planned;
				actualTotal += model[i].actual;
			}

			this.__title.setValue(this.getTitle());
			this.__period.setValue(this.getPeriod());
			var accent = this.__css("highlight");
			this.__total.setValue("<b>" + this.__compact(actualTotal) + "</b> / " + this.__compact(plannedTotal) + (unit ? " " + qx.bom.String.escape(unit) : "") +
				" <b style='color:" + accent + "'>" + (plannedTotal > 0 ? Math.round(actualTotal / plannedTotal * 100) : 0) + "%</b>");

			this.__drawCaptions();
			this.__drawRows(model);
		},

		/**
		 * The captions; label, share and status sort when clicked.
		 */
		__drawCaptions: function () {

			var old = this.__caption.removeAll();
			for (var i = 0; i < old.length; i++)
				old[i].dispose();

			var narrow = this.__narrow;
			var by = this.getSortBy(), desc = this.getSortDescending();
			var arrow = function (key) { return by === key ? (desc ? " ▼" : " ▲") : ""; };

			var make = function (self, key, text, props, layout) {
				var label = new qx.ui.basic.Label(text + arrow(key)).set({ appearance: "lifting-schedule-tracker/caption-text", rich: false });
				label.set(props);
				if (key) {
					label.setCursor("pointer");
					label.setUserData("sort", key);
					if (by === key)
						label.addState("selected");
					label.getContentElement().setAttribute("role", "button");
					label.addListener("tap", self._onCaptionTap, self);
					self.__hover(label);
				}
				self.__caption.add(label, layout);
			};

			make(this, "label", "Name", { width: 120 }, {});
			make(this, null, "Pace ┃", { textAlign: "left" }, { flex: 1 });
			if (!narrow)
				make(this, null, this.getActualLabel() + " / " + this.getPlannedLabel(), { width: 128, textAlign: "right" }, {});
			make(this, "percent", "%", { width: 40, textAlign: "right" }, {});
			if (!narrow)
				make(this, "variance", "Status", { width: 64, textAlign: "center" }, {});
		},

		/**
		 * One row for each item.
		 */
		__drawRows: function (model) {

			var old = this.__rows.removeAll();
			for (var i = 0; i < old.length; i++)
				old[i].dispose();

			if (!model.length) {
				this.__rows.add(new qx.ui.basic.Label("Nothing scheduled").set({ appearance: "lifting-schedule-tracker/empty", rich: false, allowGrowX: true, textAlign: "center" }));
				return;
			}

			var scale = 1;
			for (var i = 0; i < model.length; i++)
				scale = Math.max(scale, model[i].ratio);

			for (var i = 0; i < model.length; i++)
				this.__addRow(model[i], scale);
		},

		/**
		 * One row: label, track with fill and pace marker, amounts, share and status chip.
		 */
		__addRow: function (m, scale) {

			var narrow = this.__narrow;
			var color = this.__statusColor(m.status);
			var pace = Math.max(0, Math.min(1, this.getPaceRatio()));
			var unit = this.getUnit();

			var row = new qx.ui.container.Composite(new qx.ui.layout.HBox(8).set({ alignY: "middle" })).set({ appearance: "lifting-schedule-tracker/row", cursor: "pointer" });
			row.setUserData("label", m.label);
			row.setToolTipText(m.label + ": " + this.__compact(m.actual) + " of " + this.__compact(m.planned) + (unit ? " " + unit : "") +
				" (" + (m.variance >= 0 ? "+" : "-") + this.__compact(Math.abs(m.variance)) + " vs pace)");
			if (m.label === this.getSelected())
				row.addState("selected");
			this.__hover(row);
			row.addListener("tap", this._onRowTap, this);

			var info = new qx.ui.container.Composite(new qx.ui.layout.VBox(0)).set({ width: 120, anonymous: true });
			info.add(new qx.ui.basic.Label(m.label).set({ appearance: "lifting-schedule-tracker/name", rich: false, anonymous: true, allowShrinkX: true }));
			if (m.sub)
				info.add(new qx.ui.basic.Label(m.sub).set({ appearance: "lifting-schedule-tracker/sub", rich: false, anonymous: true, allowShrinkX: true }));
			row.add(info);

			var track = new qx.ui.container.Composite(new qx.ui.layout.Canvas()).set({ height: 12, allowGrowY: false, anonymous: true });
			var pct = function (v) { return (v / scale * 100).toFixed(2) + "%"; };
			track.add(new qx.ui.core.Widget().set({ appearance: "lifting-schedule-tracker/planned", anonymous: true }), { left: 0, top: 1, bottom: 1, width: pct(1) });
			track.add(new qx.ui.core.Widget().set({ appearance: "lifting-schedule-tracker/fill", backgroundColor: color, anonymous: true }), { left: 0, top: 1, bottom: 1, width: pct(Math.min(m.ratio, scale)) });
			track.add(new qx.ui.core.Widget().set({ appearance: "lifting-schedule-tracker/mark", width: 2, anonymous: true }), { left: pct(pace), top: 0, bottom: 0 });
			row.add(track, { flex: 1 });

			if (!narrow)
				row.add(new qx.ui.basic.Label("<b>" + this.__compact(m.actual) + "</b> / " + this.__compact(m.planned)).set({ appearance: "lifting-schedule-tracker/amount", rich: true, width: 128, textAlign: "right", anonymous: true }));

			row.add(new qx.ui.basic.Label(Math.round(m.ratio * 100) + "%").set({ appearance: "lifting-schedule-tracker/percent", rich: false, width: 40, textAlign: "right", anonymous: true }));

			if (!narrow) {
				var chip = new qx.ui.basic.Label(gridkrafter.wisejdemo.LiftingScheduleTracker.STATUS[m.status].text).set({ appearance: "lifting-schedule-tracker/chip", textColor: color, rich: false, textAlign: "center", anonymous: true });
				var holder = new qx.ui.container.Composite(new qx.ui.layout.HBox().set({ alignX: "center" })).set({ width: 64, anonymous: true });
				holder.add(chip);
				row.add(holder);
			}

			this.__rows.add(row);
		},

		/**
		 * A sortable caption: sort by it, or flip the direction when it is the current one.
		 */
		_onCaptionTap: function (e) {

			if (!this.isEnabled())
				return;

			var key = e.getCurrentTarget().getUserData("sort");
			var on = key === this.getSortBy();
			this.fireDataEvent("sortChanged", { sortBy: key, sortDescending: on ? !this.getSortDescending() : false });
		},

		/**
		 * A row: select it.
		 */
		_onRowTap: function (e) {

			var label = e.getCurrentTarget().getUserData("label");
			if (this.isEnabled() && label !== this.getSelected())
				this.fireDataEvent("rowSelected", { label: label, selected: label });
		}
	},

	/**
	 * Drops the theme listener; the child widgets are disposed by the container.
	 */
	destruct: function () {

		qx.theme.manager.Color.getInstance().removeListener("changeTheme", this._applyRows, this);
		this.__title = this.__period = this.__total = this.__caption = this.__rows = null;
	}
});
