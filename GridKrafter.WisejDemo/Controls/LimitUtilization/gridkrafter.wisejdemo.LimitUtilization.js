/**
 * gridkrafter.wisejdemo.LimitUtilization
 *
 * The client half of GridKrafter.WisejDemo.LimitUtilization: a header (title, total exposure against total
 * limit, the count of breaches) and one scrolling row per counterparty with a rating chip, a bar of the
 * exposure against its limit with a marker at the warning threshold, the percent used and a status chip
 * (OK, Watch, Breach). Rows are sorted by percent used, highest first; a bar past its limit overflows in the
 * negative colour. The server owns every value; the browser only reports the row the user picks.
 */
qx.Class.define("gridkrafter.wisejdemo.LimitUtilization", {

	extend: wisej.web.Control,

	/**
	 * Builds the header and the scrolling list once; the rows are drawn from the data.
	 */
	construct: function () {

		this.base(arguments, new qx.ui.layout.VBox(6));

		this.setFocusable(false);

		var head = new qx.ui.container.Composite(new qx.ui.layout.HBox(10).set({ alignY: "middle" }));
		this.__title = new qx.ui.basic.Label("").set({ appearance: "limit-utilization/title", rich: false });
		this.__total = new qx.ui.basic.Label("").set({ appearance: "limit-utilization/total", rich: true });
		this.__flag = new qx.ui.basic.Label("").set({ appearance: "limit-utilization/flag", rich: false, allowGrowY: false });
		head.add(this.__title);
		head.add(this.__total, { flex: 1 });
		head.add(this.__flag);
		this.add(head);

		this.__rows = new qx.ui.container.Composite(new qx.ui.layout.VBox(1));
		this.add(new qx.ui.container.Scroll(this.__rows).set({ scrollbarX: "off" }), { flex: 1 });

		qx.theme.manager.Color.getInstance().addListener("changeTheme", this._applyRows, this);

		this._applyRows();
	},

	statics: {

		/**
		 * The statuses: their text and the theme property that colours them.
		 */
		STATUS: {
			ok: { text: "OK", color: "OkColor" },
			watch: { text: "Watch", color: "WatchColor" },
			breach: { text: "Breach", color: "BreachColor" }
		}
	},

	events: {

		/**
		 * The user selected a row: { name, selected }.
		 */
		rowSelected: "qx.event.type.Data"
	},

	properties: {

		/**
		 * The theme appearance of the root widget.
		 */
		appearance: { init: "limit-utilization", refine: true },

		/**
		 * The title in the header.
		 */
		title: { init: "Credit limits", check: "String", apply: "_applyRows" },

		/**
		 * The unit shown after the amounts.
		 */
		unit: { init: "$M", check: "String", apply: "_applyRows" },

		/**
		 * The share of the limit, 0 to 1, from which a row is on Watch.
		 */
		warnAt: { init: 0.8, check: "Number", apply: "_applyRows" },

		/**
		 * The name of the selected row.
		 */
		selected: { init: "", check: "String", apply: "_applyRows" },

		/**
		 * The rows, as the server normalized them: { name, exposure, limit, rating }.
		 */
		rows: { init: null, nullable: true, check: "Array", apply: "_applyRows" },

		/**
		 * The theme's colour of each status, set by the mixin.
		 */
		themeOkColor: { init: "#1fb57a", check: "Color", themeable: true, apply: "_applyRows" },
		themeWatchColor: { init: "#f2a516", check: "Color", themeable: true, apply: "_applyRows" },
		themeBreachColor: { init: "#e5484d", check: "Color", themeable: true, apply: "_applyRows" }
	},

	members: {

		__title: null,
		__total: null,
		__flag: null,
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
		 * The CSS colour of a status.
		 */
		__statusColor: function (key) {

			return this.__css(this["getTheme" + gridkrafter.wisejdemo.LimitUtilization.STATUS[key].color]());
		},

		/**
		 * Gives a widget the hovered state while the pointer is over it.
		 */
		__hover: function (widget) {

			widget.addListener("pointerover", function () { widget.addState("hovered"); });
			widget.addListener("pointerout", function () { widget.removeState("hovered"); });
		},

		/**
		 * A number with thousands separators and at most one decimal.
		 */
		__fmt: function (v) {

			return (Math.round(v * 10) / 10).toLocaleString();
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
				if (!this.isDisposed())
					this.__draw();
			}, this, 0);
		},

		/**
		 * The rows with their share and status, highest share first.
		 */
		__model: function () {

			var warn = this.getWarnAt() > 0 ? this.getWarnAt() : 0.8;
			var list = this.getRows() || [];
			var model = [];
			for (var i = 0; i < list.length; i++) {
				var r = list[i];
				var exposure = Math.max(0, r.exposure || 0), limit = Math.max(0, r.limit || 0);
				var ratio = limit > 0 ? exposure / limit : (exposure > 0 ? 9.99 : 0);
				model.push({
					name: r.name, rating: r.rating || "", exposure: exposure, limit: limit, ratio: ratio,
					status: ratio > 1 ? "breach" : ratio >= warn ? "watch" : "ok"
				});
			}
			model.sort(function (a, b) { return b.ratio - a.ratio; });
			return model;
		},

		/**
		 * The header and the rows.
		 */
		__draw: function () {

			var model = this.__model();
			var unit = this.getUnit();
			var te = 0, tl = 0, breaches = 0;
			for (var i = 0; i < model.length; i++) {
				te += model[i].exposure;
				tl += model[i].limit;
				if (model[i].status === "breach")
					breaches++;
			}

			this.__title.setValue(this.getTitle() || "");
			this.__total.setValue("<b>" + this.__fmt(te) + "</b> / " + this.__fmt(tl) + (unit ? " " + qx.bom.String.escape(unit) : ""));
			this.__flag.setValue(breaches ? breaches + (breaches === 1 ? " breach" : " breaches") : "No breaches");
			this.__flag.addState(breaches ? "bad" : "ok");
			this.__flag.removeState(breaches ? "ok" : "bad");

			var old = this.__rows.removeAll();
			for (var i = 0; i < old.length; i++)
				old[i].dispose();

			if (!model.length) {
				this.__rows.add(new qx.ui.basic.Label("No limits").set({ appearance: "limit-utilization/empty", rich: false, allowGrowX: true, textAlign: "center" }));
				return;
			}

			// shared scale: the longest bar (past its limit) sets it, so every limit sits at 1 / scale
			var scale = 1;
			for (var i = 0; i < model.length; i++)
				scale = Math.max(scale, model[i].ratio);

			for (var i = 0; i < model.length; i++)
				this.__addRow(model[i], scale);
		},

		/**
		 * One row: name and rating, the bar, the percent and the status chip.
		 */
		__addRow: function (m, scale) {

			var color = this.__statusColor(m.status);
			var unit = this.getUnit();
			var esc = qx.bom.String.escape;
			var pct = function (v) { return (v / scale * 100).toFixed(2) + "%"; };

			var row = new qx.ui.container.Composite(new qx.ui.layout.HBox(8).set({ alignY: "middle" })).set({ appearance: "limit-utilization/row", cursor: "pointer" });
			row.setUserData("name", m.name);
			row.getContentElement().setAttribute("role", "button");
			row.getContentElement().setAttribute("aria-label", m.name + ": " + Math.round(m.ratio * 100) + "% of limit used");
			row.setToolTipText("<b>" + esc(m.name) + "</b>" + (m.rating ? " · " + esc(m.rating) : "") +
				"<br/>Exposure " + this.__fmt(m.exposure) + " " + esc(unit) +
				"<br/>Limit " + this.__fmt(m.limit) + " " + esc(unit) +
				"<br/>" + (m.exposure > m.limit ? "Over by " : "Headroom ") + this.__fmt(Math.abs(m.limit - m.exposure)) + " " + esc(unit));
			if (m.name === this.getSelected())
				row.addState("selected");
			this.__hover(row);
			row.addListener("tap", this._onRowTap, this);

			var who = new qx.ui.container.Composite(new qx.ui.layout.HBox(6).set({ alignY: "middle" })).set({ width: 160, minWidth: 90, anonymous: true });
			who.add(new qx.ui.basic.Label(m.name).set({ appearance: "limit-utilization/name", rich: false, anonymous: true, allowShrinkX: true }), { flex: 1 });
			if (m.rating)
				who.add(new qx.ui.basic.Label(m.rating).set({ appearance: "limit-utilization/rating", rich: false, anonymous: true, allowGrowY: false }));
			row.add(who);

			// the track is the limit (1 / scale of the width); the fill runs past it when in breach
			var bar = new qx.ui.container.Composite(new qx.ui.layout.Canvas()).set({ height: 12, minWidth: 60, allowGrowY: false, anonymous: true });
			bar.add(new qx.ui.core.Widget().set({ appearance: "limit-utilization/track", anonymous: true }), { left: 0, top: 1, bottom: 1, width: pct(1) });
			if (m.ratio > 0)
				bar.add(new qx.ui.core.Widget().set({ appearance: "limit-utilization/fill", backgroundColor: color, anonymous: true }), { left: 0, top: 1, bottom: 1, width: pct(m.ratio) });
			bar.add(new qx.ui.core.Widget().set({ appearance: "limit-utilization/mark", width: 2, anonymous: true }), { left: pct(Math.min(this.getWarnAt() > 0 ? this.getWarnAt() : 0.8, 1)), top: 0, bottom: 0 });
			row.add(bar, { flex: 1 });

			row.add(new qx.ui.basic.Label(Math.round(m.ratio * 100) + "%").set({ appearance: "limit-utilization/percent", rich: false, width: 44, textAlign: "right", anonymous: true }));

			var chip = new qx.ui.basic.Label(gridkrafter.wisejdemo.LimitUtilization.STATUS[m.status].text).set({ appearance: "limit-utilization/chip", textColor: color, rich: false, textAlign: "center", anonymous: true });
			var holder = new qx.ui.container.Composite(new qx.ui.layout.HBox().set({ alignX: "center" })).set({ width: 60, anonymous: true });
			holder.add(chip);
			row.add(holder);

			this.__rows.add(row);
		},

		/**
		 * A row: select it.
		 */
		_onRowTap: function (e) {

			var name = e.getCurrentTarget().getUserData("name");
			if (this.isEnabled() && name !== this.getSelected())
				this.fireDataEvent("rowSelected", { name: name, selected: name });
		}
	},

	/**
	 * Drops the theme listener; the child widgets are disposed by the container.
	 */
	destruct: function () {

		qx.theme.manager.Color.getInstance().removeListener("changeTheme", this._applyRows, this);
		this.__title = this.__total = this.__flag = this.__rows = null;
	}
});
