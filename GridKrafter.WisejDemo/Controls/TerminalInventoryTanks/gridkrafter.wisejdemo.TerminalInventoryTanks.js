/**
 * gridkrafter.wisejdemo.TerminalInventoryTanks
 *
 * The client half of GridKrafter.WisejDemo.TerminalInventoryTanks: totals (on hand, utilization, average
 * days of cover, alarms), a bbl / gal switch, and a row of tanks, each a vessel filled to its level with
 * its high and low alarm lines, a LOW / HIGH badge, the terminal, the quantity, days of cover and net flow.
 * Levels arrive in barrels. The server owns every value; the browser only reports what the user picks.
 */
qx.Class.define("gridkrafter.wisejdemo.TerminalInventoryTanks", {

	extend: wisej.web.Control,

	/**
	 * Builds the header once; the tanks are drawn from the data.
	 */
	construct: function () {

		this.base(arguments, new qx.ui.layout.VBox(12));

		this.setFocusable(false);

		var head = new qx.ui.container.Composite(new qx.ui.layout.HBox(8));
		var totals = new qx.ui.container.Composite(new qx.ui.layout.HBox(20));
		this.__totals = {};
		var list = [["onHand", "On hand"], ["utilization", "Utilization"], ["cover", "Avg days of cover"], ["alarms", "Alarms"]];
		for (var i = 0; i < list.length; i++) {
			var box = new qx.ui.container.Composite(new qx.ui.layout.VBox(0));
			box.add(new qx.ui.basic.Label(list[i][1]).set({ appearance: "terminal-inventory-tanks/total-label", rich: false }));
			var value = new qx.ui.basic.Label("0").set({ appearance: "terminal-inventory-tanks/total-value", rich: true });
			box.add(value);
			this.__totals[list[i][0]] = value;
			totals.add(box);
		}
		head.add(totals, { flex: 1 });

		this.__units = new qx.ui.container.Composite(new qx.ui.layout.HBox(2)).set({ appearance: "terminal-inventory-tanks/units", alignY: "top" });
		var units = ["bbl", "gal"];
		for (var i = 0; i < units.length; i++) {
			var unit = new qx.ui.basic.Label(units[i]).set({ appearance: "terminal-inventory-tanks/unit", rich: false, cursor: "pointer" });
			unit.setUserData("unit", units[i]);
			unit.getContentElement().setAttribute("role", "button");
			unit.addListener("tap", this._onUnitTap, this);
			this.__hover(unit);
			this.__units.add(unit);
		}
		head.add(this.__units);
		this.add(head);

		this.__tanks = new qx.ui.container.Composite(new qx.ui.layout.HBox(10));
		this.add(this.__tanks, { flex: 1 });

		qx.theme.manager.Color.getInstance().addListener("changeTheme", this._applyTanks, this);

		this._applyTanks();
	},

	events: {

		/**
		 * The user picked a unit: { unit: "bbl" | "gal" }.
		 */
		unitChanged: "qx.event.type.Data",

		/**
		 * The user selected a tank: { id, selected }.
		 */
		tankSelected: "qx.event.type.Data"
	},

	properties: {

		/**
		 * The theme appearance of the root widget.
		 */
		appearance: { init: "terminal-inventory-tanks", refine: true },

		/**
		 * The unit quantities show in; levels arrive in barrels.
		 */
		unit: { init: "bbl", check: ["bbl", "gal"], apply: "_applyTanks" },

		/**
		 * Whether each tank draws its alarm lines.
		 */
		showAlarms: { init: true, check: "Boolean", apply: "_applyTanks" },

		/**
		 * The id of the selected tank.
		 */
		selected: { init: "", check: "String", apply: "_applyTanks" },

		/**
		 * The tanks: [{ id, terminal, capacity, level, low, high, daysCover, netFlow }].
		 */
		tanks: { init: null, nullable: true, check: "Array", apply: "_applyTanks" },

		/**
		 * The theme's colours: a normal level, a high level, a low level, rising and falling flow. Set by the mixin.
		 */
		themeNormalColor: { init: "#0ea5e9", check: "Color", themeable: true, apply: "_applyTanks" },
		themeHighColor: { init: "#f2a516", check: "Color", themeable: true, apply: "_applyTanks" },
		themeLowColor: { init: "#e5484d", check: "Color", themeable: true, apply: "_applyTanks" },
		themeInflowColor: { init: "#1fb57a", check: "Color", themeable: true, apply: "_applyTanks" },
		themeOutflowColor: { init: "#f2a516", check: "Color", themeable: true, apply: "_applyTanks" }
	},

	members: {

		__totals: null,
		__units: null,
		__tanks: null,
		__pending: false,

		/**
		 * Resolves a theme colour name or a CSS colour to CSS.
		 */
		__css: function (color) {

			var manager = qx.theme.manager.Color.getInstance();
			return manager.isDynamic(color) ? manager.resolve(color) : color;
		},

		/**
		 * The colour mixed toward white by the given amount (0..1).
		 */
		__lighten: function (color, amount) {

			try {
				var rgb = qx.util.ColorUtil.stringToRgb(this.__css(color));
				return qx.util.ColorUtil.rgbToRgbString([
					Math.round(rgb[0] + (255 - rgb[0]) * amount),
					Math.round(rgb[1] + (255 - rgb[1]) * amount),
					Math.round(rgb[2] + (255 - rgb[2]) * amount)]);
			}
			catch (ex) {
				return this.__css(color);
			}
		},

		/**
		 * Gives a widget the hovered state while the pointer is over it.
		 */
		__hover: function (widget) {

			widget.addListener("pointerover", function () { widget.addState("hovered"); });
			widget.addListener("pointerout", function () { widget.removeState("hovered"); });
		},

		/**
		 * A quantity in barrels, in the current unit: 1.23M, 121k or 4,200.
		 */
		__format: function (barrels) {

			var v = barrels * (this.getUnit() === "gal" ? 42 : 1);
			if (v >= 1e6)
				return (v / 1e6).toFixed(2) + "M";
			if (v >= 1e4)
				return Math.round(v / 1e3) + "k";
			return Math.round(v).toLocaleString();
		},

		/**
		 * Every property draws the widget again, once per round of changes.
		 */
		_applyTanks: function () {

			if (!this.__tanks || this.__pending)
				return;

			this.__pending = true;
			qx.event.Timer.once(function () {
				this.__pending = false;
				if (!this.isDisposed())
					this.__draw();
			}, this, 0);
		},

		/**
		 * The totals, the unit switch and the tanks.
		 */
		__draw: function () {

			var tanks = this.getTanks() || [];
			var gray = this.__css("grayText");

			// totals
			var onHand = 0, capacity = 0, cover = 0, alarms = 0;
			for (var i = 0; i < tanks.length; i++) {
				var t = tanks[i];
				onHand += t.level || 0;
				capacity += t.capacity || 0;
				cover += t.daysCover || 0;
				var p = t.capacity ? t.level / t.capacity * 100 : 0;
				if (p <= (t.low || 0) || p >= (t.high == null ? 100 : t.high))
					alarms++;
			}
			var small = function (text) { return "<span style='font-size:12px;font-weight:500;color:" + gray + ";margin-left:3px'>" + text + "</span>"; };
			this.__totals.onHand.setValue(this.__format(onHand) + small(this.getUnit()));
			this.__totals.utilization.setValue((capacity ? Math.round(onHand / capacity * 100) : 0) + small("%"));
			this.__totals.cover.setValue(tanks.length ? (cover / tanks.length).toFixed(1) : "0");
			this.__totals.alarms.setValue(String(alarms));
			this.__totals.alarms.setTextColor(alarms ? this.__css(this.getThemeLowColor()) : null);

			var units = this.__units.getChildren();
			for (var i = 0; i < units.length; i++) {
				var on = units[i].getUserData("unit") === this.getUnit();
				if (on)
					units[i].addState("selected");
				else
					units[i].removeState("selected");
				units[i].getContentElement().setAttribute("aria-pressed", on ? "true" : "false");
			}

			// tanks
			var old = this.__tanks.removeAll();
			for (var i = 0; i < old.length; i++)
				old[i].dispose();

			if (!tanks.length) {
				this.__tanks.add(new qx.ui.basic.Label("No tanks").set({ appearance: "terminal-inventory-tanks/empty", rich: false, textAlign: "center", allowGrowX: true }), { flex: 1 });
				return;
			}
			for (var i = 0; i < tanks.length; i++)
				this.__tanks.add(this.__createTank(tanks[i]), { flex: 1 });
		},

		/**
		 * One tank: badge slot, vessel with liquid and alarm lines, terminal, quantity, cover and flow.
		 */
		__createTank: function (t) {

			var esc = qx.bom.String.escape;
			var p = Math.max(0, Math.min(100, t.capacity ? t.level / t.capacity * 100 : 0));
			var low = t.low || 0, high = t.high == null ? 100 : t.high;
			var state = p <= low ? "low" : p >= high ? "high" : "ok";
			var color = this.__css(state === "low" ? this.getThemeLowColor() : state === "high" ? this.getThemeHighColor() : this.getThemeNormalColor());
			var flow = t.netFlow || 0;

			var tank = new qx.ui.container.Composite(new qx.ui.layout.VBox(5).set({ alignX: "center" })).set({ appearance: "terminal-inventory-tanks/tank", cursor: "pointer", minWidth: 0 });
			tank.setUserData("id", t.id);
			tank.getContentElement().setAttribute("role", "button");
			tank.getContentElement().setAttribute("aria-label", (t.terminal || t.id) + ", " + Math.round(p) + "%");
			if (t.id === this.getSelected())
				tank.addState("selected");
			this.__hover(tank);
			tank.addListener("tap", this._onTankTap, this);

			// badge slot: always there so every vessel lines up
			var slot = new qx.ui.container.Composite(new qx.ui.layout.HBox().set({ alignX: "center", alignY: "middle" })).set({ height: 16, allowGrowY: false, anonymous: true });
			if (state !== "ok")
				slot.add(new qx.ui.basic.Label(state === "low" ? "LOW" : "HIGH").set({ appearance: "terminal-inventory-tanks/badge", backgroundColor: color, rich: false, anonymous: true }));
			tank.add(slot);

			// vessel
			var vessel = new qx.ui.container.Composite(new qx.ui.layout.Canvas()).set({ appearance: "terminal-inventory-tanks/vessel", width: 64, minHeight: 80, allowGrowX: false, anonymous: true });
			var liquid = new qx.ui.core.Widget().set({ appearance: "terminal-inventory-tanks/liquid", anonymous: true });
			liquid.getContentElement().setStyle("background", "linear-gradient(180deg," + this.__lighten(color, 0.3) + "," + color + ")");
			vessel.add(liquid, { left: 0, right: 0, bottom: 0, height: p + "%" });

			if (this.getShowAlarms()) {
				var marks = [[high, "H", this.getThemeHighColor()], [low, "L", this.getThemeLowColor()]];
				for (var m = 0; m < marks.length; m++) {
					var markColor = this.__css(marks[m][2]);
					var line = new qx.ui.core.Widget().set({ height: 0, anonymous: true });
					line.getContentElement().setStyle("borderTop", "1.5px dashed " + markColor);
					vessel.add(line, { left: 0, right: 0, bottom: marks[m][0] + "%" });
					vessel.add(new qx.ui.basic.Label(marks[m][1]).set({ appearance: "terminal-inventory-tanks/mark", textColor: markColor, rich: false, anonymous: true }),
						{ right: 3, bottom: marks[m][0] + "%" });
				}
			}

			var center = new qx.ui.container.Composite(new qx.ui.layout.VBox().set({ alignX: "center", alignY: "middle" })).set({ anonymous: true });
			center.add(new qx.ui.basic.Label(Math.round(p) + "%").set({ appearance: "terminal-inventory-tanks/percent", rich: false, anonymous: true }));
			vessel.add(center, { left: 0, right: 0, top: 0, bottom: 0 });
			tank.add(vessel, { flex: 1 });

			// texts
			tank.add(new qx.ui.basic.Label(t.terminal || "").set({ appearance: "terminal-inventory-tanks/name", rich: false, toolTipText: t.terminal || null, allowShrinkX: true, anonymous: true }));
			tank.add(new qx.ui.basic.Label("<b>" + esc(this.__format(t.level || 0)) + "</b> / " + esc(this.__format(t.capacity || 0))).set({ appearance: "terminal-inventory-tanks/quantity", rich: true, anonymous: true }));

			var meta = new qx.ui.container.Composite(new qx.ui.layout.HBox(6)).set({ anonymous: true });
			meta.add(new qx.ui.basic.Label((t.daysCover || 0).toFixed(1) + "d cover").set({ appearance: "terminal-inventory-tanks/meta", rich: false, anonymous: true }));
			meta.add(new qx.ui.basic.Label((flow >= 0 ? "▲" : "▼") + this.__format(Math.abs(flow))).set({
				appearance: "terminal-inventory-tanks/flow",
				textColor: this.__css(flow >= 0 ? this.getThemeInflowColor() : this.getThemeOutflowColor()),
				rich: false, anonymous: true
			}));
			tank.add(meta);

			return tank;
		},

		/**
		 * A unit button: report it; the server sets it and renders it back.
		 */
		_onUnitTap: function (e) {

			var unit = e.getCurrentTarget().getUserData("unit");
			if (this.isEnabled() && unit !== this.getUnit())
				this.fireDataEvent("unitChanged", { unit: unit });
		},

		/**
		 * A tank: select it.
		 */
		_onTankTap: function (e) {

			var id = e.getCurrentTarget().getUserData("id");
			if (this.isEnabled() && id !== this.getSelected())
				this.fireDataEvent("tankSelected", { id: id, selected: id });
		}
	},

	/**
	 * Drops the theme listener; the child widgets are disposed by the container.
	 */
	destruct: function () {

		qx.theme.manager.Color.getInstance().removeListener("changeTheme", this._applyTanks, this);
		this.__totals = this.__units = this.__tanks = null;
	}
});
