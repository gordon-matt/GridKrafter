/**
 * gridkrafter.wisejdemo.WidgetClock
 *
 * The client half of GridKrafter.WisejDemo.Controls.WidgetClock: the browser's local time in large
 * tabular figures above a muted date line ("Thursday, 12 November 2026"), centred. It ticks here, once a
 * second, with a browser timer: the server only sends the format.
 */
qx.Class.define("gridkrafter.wisejdemo.WidgetClock", {

	extend: wisej.web.Control,

	/**
	 * Creates the time and the date and starts the timer.
	 */
	construct: function () {

		this.base(arguments, new qx.ui.layout.VBox(4).set({ alignX: "center", alignY: "middle" }));

		this.setFocusable(false);

		this._createChildControl("time");
		this._createChildControl("date");

		this.__timer = new qx.event.Timer(1000);
		this.__timer.addListener("interval", this._draw, this);
		this.__timer.start();

		this._draw();
	},

	statics: {

		/**
		 * The English day names, Sunday first, as Date.getDay counts them.
		 */
		DAYS: ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"],

		/**
		 * The English month names.
		 */
		MONTHS: ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"]
	},

	properties: {

		/**
		 * The theme appearance.
		 */
		appearance: { init: "widget-clock", refine: true },

		/**
		 * Whether the time is on a 12-hour clock with AM/PM.
		 */
		hour12: { init: false, check: "Boolean", apply: "_draw" },

		/**
		 * Whether the seconds are shown.
		 */
		showSeconds: { init: true, check: "Boolean", apply: "_draw" }
	},

	members: {

		/**
		 * The one-second timer. Disposed in destruct.
		 */
		__timer: null,

		/**
		 * Creates the parts.
		 */
		_createChildControlImpl: function (id, hash) {

			var control;
			switch (id) {

				case "time":
					control = new qx.ui.basic.Label().set({ rich: false, anonymous: true, alignX: "center" });
					control.getContentElement().setStyles({ fontVariantNumeric: "tabular-nums", letterSpacing: "-0.02em" });
					this._add(control);
					break;

				case "date":
					control = new qx.ui.basic.Label().set({ rich: false, anonymous: true, alignX: "center" });
					this._add(control);
					break;
			}

			return control || this.base(arguments, id, hash);
		},

		/**
		 * Shows the current time and date in the chosen format: HH:mm:ss, HH:mm, h:mm:ss tt or h:mm tt.
		 */
		_draw: function () {

			if (!this.hasChildControl("time") || !this.hasChildControl("date"))
				return;

			var pad = function (n) { return (n < 10 ? "0" : "") + n; };
			var d = new Date();
			var h = d.getHours();
			var seconds = this.getShowSeconds() ? ":" + pad(d.getSeconds()) : "";
			var time = this.getHour12()
				? (h % 12 || 12) + ":" + pad(d.getMinutes()) + seconds + " " + (h < 12 ? "AM" : "PM")
				: pad(h) + ":" + pad(d.getMinutes()) + seconds;

			var statics = gridkrafter.wisejdemo.WidgetClock;
			var date = statics.DAYS[d.getDay()] + ", " + d.getDate() + " " + statics.MONTHS[d.getMonth()] + " " + d.getFullYear();

			this.getChildControl("time").setValue(time);
			this.getChildControl("date").setValue(date);
		}
	},

	/**
	 * Stops and drops the timer.
	 */
	destruct: function () {

		if (this.__timer) {
			this.__timer.stop();
			this.__timer.dispose();
			this.__timer = null;
		}
	}
});
