/**
 * gridkrafter.wisejdemo.WidgetStopwatch
 *
 * The client half of GridKrafter.WisejDemo.Controls.WidgetStopwatch, as the GridKrafter React demo's
 * stopwatch: a large mm:ss.t time, a row of buttons (Start/Pause/Resume, Lap, Reset) and the last five
 * laps, newest first. All of its state is here, in the browser: it counts with a 100 ms browser timer and
 * never asks the server.
 */
qx.Class.define("gridkrafter.wisejdemo.WidgetStopwatch", {

	extend: wisej.web.Control,

	/**
	 * Creates the time, the buttons and the lap list, at zero.
	 */
	construct: function () {

		this.base(arguments, new qx.ui.layout.VBox(12).set({ alignX: "center", alignY: "middle" }));

		this.setFocusable(false);

		this.__laps = [];

		this._createChildControl("time");
		this._createChildControl("buttons");
		this._createChildControl("laps");

		this.__timer = new qx.event.Timer(100);
		this.__timer.addListener("interval", this._update, this);

		this._update();
	},

	statics: {

		/**
		 * How many laps are listed.
		 */
		MAX_LAPS: 5,

		/**
		 * The button icons: 24 x 24 strokes drawn in the button's text colour.
		 */
		ICONS: {
			play: '<path d="M7 4.5v15l12-7.5z"/>',
			pause: '<path d="M8 5v14M16 5v14"/>',
			flag: '<path d="M5 21V4M5 4h11l-2 4 2 4H5"/>',
			reset: '<path d="M4 11a8 8 0 1 1 2 6M4 5v6h6"/>'
		},

		/**
		 * Returns the markup of an icon followed by an optional text.
		 *
		 * @param {String} icon the icon's name.
		 * @param {String} text the literal text, or null for the icon alone.
		 * @return {String} the markup.
		 */
		iconText: function (icon, text) {

			var svg = '<svg viewBox="0 0 24 24" width="15" height="15" style="display:block;fill:none;stroke:currentColor;stroke-width:2;stroke-linecap:round;stroke-linejoin:round">' +
				gridkrafter.wisejdemo.WidgetStopwatch.ICONS[icon] + '</svg>';

			if (!text)
				return '<span style="display:inline-flex;align-items:center">' + svg + '</span>';

			return '<span style="display:inline-flex;align-items:center;gap:6px;white-space:nowrap">' + svg +
				'<span>' + qx.bom.String.escape(text) + '</span></span>';
		},

		/**
		 * Formats milliseconds as mm:ss.t.
		 *
		 * @param {Number} ms the time in milliseconds.
		 * @return {String} the text.
		 */
		format: function (ms) {

			var pad = function (n) { return (n < 10 ? "0" : "") + n; };
			var minutes = Math.floor(ms / 60000);
			var seconds = Math.floor(ms / 1000) % 60;
			var tenths = Math.floor(ms / 100) % 10;
			return pad(minutes) + ":" + pad(seconds) + "." + tenths;
		}
	},

	properties: {

		/**
		 * The theme appearance.
		 */
		appearance: { init: "widget-stopwatch", refine: true }
	},

	members: {

		/**
		 * The 100 ms timer; it runs only while the stopwatch runs.
		 */
		__timer: null,

		/**
		 * When the current run started (Date.now()), or null while stopped.
		 */
		__startedAt: null,

		/**
		 * The milliseconds of the runs before the current one.
		 */
		__banked: 0,

		/**
		 * The laps, newest first: { n: the lap's number, ms: its time }.
		 */
		__laps: null,

		/**
		 * How many laps were taken since the last reset.
		 */
		__lapCount: 0,

		/**
		 * Creates the parts.
		 */
		_createChildControlImpl: function (id, hash) {

			var control;
			switch (id) {

				case "time":
					control = new qx.ui.basic.Label().set({ rich: false, anonymous: true, allowGrowX: false });
					control.getContentElement().setStyles({ fontVariantNumeric: "tabular-nums", letterSpacing: "-0.02em" });
					this._add(control);
					break;

				case "buttons":
					control = new qx.ui.container.Composite(new qx.ui.layout.HBox(8).set({ alignX: "center" })).set({ allowGrowX: false });
					control.add(this.getChildControl("toggle"));
					control.add(this.getChildControl("lap"));
					control.add(this.getChildControl("reset"));
					this._add(control);
					break;

				case "toggle":
					control = new qx.ui.form.Button();
					control.getChildControl("label").setRich(true);
					control.addListener("execute", this._onToggle, this);
					break;

				case "lap":
					control = new qx.ui.form.Button(gridkrafter.wisejdemo.WidgetStopwatch.iconText("flag", "Lap"));
					control.getChildControl("label").setRich(true);
					control.addListener("execute", this._onLap, this);
					break;

				case "reset":
					control = new qx.ui.form.Button(gridkrafter.wisejdemo.WidgetStopwatch.iconText("reset", null));
					control.getChildControl("label").setRich(true);
					control.setToolTipText("Reset");
					control.getContentElement().setAttribute("aria-label", "Reset");
					control.addListener("execute", this._onReset, this);
					break;

				case "laps":
					control = new qx.ui.container.Composite(new qx.ui.layout.VBox(0)).set({ allowGrowX: true });
					this._add(control);
					break;
			}

			return control || this.base(arguments, id, hash);
		},

		/**
		 * The milliseconds counted so far.
		 */
		_elapsed: function () {

			return this.__banked + (this.__startedAt === null ? 0 : Date.now() - this.__startedAt);
		},

		/**
		 * Shows the time and the buttons' labels and states.
		 */
		_update: function () {

			var Stopwatch = gridkrafter.wisejdemo.WidgetStopwatch;
			var elapsed = this._elapsed();
			var running = this.__startedAt !== null;

			this.getChildControl("time").setValue(Stopwatch.format(elapsed));

			var toggle = Stopwatch.iconText(running ? "pause" : "play", running ? "Pause" : (elapsed ? "Resume" : "Start"));
			if (this.getChildControl("toggle").getLabel() !== toggle)
				this.getChildControl("toggle").setLabel(toggle);

			this.getChildControl("lap").setEnabled(running);
			this.getChildControl("reset").setEnabled(elapsed > 0);
		},

		/**
		 * Draws the lap list again: one row a lap, newest first.
		 */
		_updateLaps: function () {

			var Stopwatch = gridkrafter.wisejdemo.WidgetStopwatch;
			var list = this.getChildControl("laps");
			list.removeAll().forEach(function (row) { row.dispose(); });

			for (var i = 0; i < this.__laps.length; i++) {

				var lap = this.__laps[i];
				var row = new qx.ui.container.Composite(new qx.ui.layout.HBox(8).set({ alignY: "middle" })).set({
					appearance: "widget-stopwatch/lap-row", anonymous: true
				});

				var name = new qx.ui.basic.Label("Lap " + lap.n).set({ rich: false, anonymous: true, appearance: "widget-stopwatch/lap-name" });
				var time = new qx.ui.basic.Label(Stopwatch.format(lap.ms)).set({ rich: false, anonymous: true, appearance: "widget-stopwatch/lap-time" });
				time.getContentElement().setStyle("fontVariantNumeric", "tabular-nums");

				if (i === 0)
					row.addState("first");

				row.add(name);
				row.add(new qx.ui.core.Spacer(), { flex: 1 });
				row.add(time);
				list.add(row);
			}

			list.setVisibility(this.__laps.length ? "visible" : "excluded");
		},

		/**
		 * Starts, pauses or resumes.
		 */
		_onToggle: function () {

			if (this.__startedAt !== null) {
				this.__banked = this._elapsed();
				this.__startedAt = null;
				this.__timer.stop();
			}
			else {
				this.__startedAt = Date.now();
				this.__timer.start();
			}

			this._update();
		},

		/**
		 * Adds a lap at the current time, keeping the newest five.
		 */
		_onLap: function () {

			if (this.__startedAt === null)
				return;

			this.__lapCount++;
			this.__laps.unshift({ n: this.__lapCount, ms: this._elapsed() });
			this.__laps = this.__laps.slice(0, gridkrafter.wisejdemo.WidgetStopwatch.MAX_LAPS);
			this._updateLaps();
		},

		/**
		 * Stops and goes back to zero, without laps.
		 */
		_onReset: function () {

			this.__timer.stop();
			this.__startedAt = null;
			this.__banked = 0;
			this.__laps = [];
			this.__lapCount = 0;
			this._updateLaps();
			this._update();
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

		this.__laps = null;
	}
});
