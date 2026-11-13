/**
 * gridkrafter.wisejdemo.TerminalSlotBoard
 *
 * The client half of GridKrafter.WisejDemo.TerminalSlotBoard: a terminal's loading slots through
 * the day. A header with the title, the count of slots per status and the free capacity; under
 * it an hour axis and one row per bay, each slot a bar placed by percent from its start to its
 * end, coloured by status, with its MOT letter, counterparty and product, and a vertical now-line.
 * The server owns every value; the browser only reports the slot the user picks.
 */
qx.Class.define("gridkrafter.wisejdemo.TerminalSlotBoard", {

	extend: wisej.web.Control,

	/**
	 * Builds the fixed parts once: the header, the scrolling board with its axis and bay list,
	 * and the tooltip the slots share.
	 */
	construct: function () {

		this.base(arguments, new qx.ui.layout.VBox(8));

		this.setFocusable(false);

		var Self = gridkrafter.wisejdemo.TerminalSlotBoard;

		// header: title, counts per status, free capacity
		var head = new qx.ui.container.Composite(new qx.ui.layout.HBox(12).set({ alignY: "middle" }));
		this.__title = new qx.ui.basic.Label("").set({ appearance: "terminal-slot-board/title", rich: false, allowShrinkX: true, minWidth: 0 });
		head.add(this.__title, { flex: 1 });

		var counts = new qx.ui.container.Composite(new qx.ui.layout.HBox(10).set({ alignY: "middle" }));
		this.__counts = {};
		for (var i = 0; i < Self.STATUS.length; i++) {
			var status = Self.STATUS[i];
			var item = new qx.ui.container.Composite(new qx.ui.layout.HBox(5).set({ alignY: "middle" }));
			var swatch = new qx.ui.core.Widget().set({ appearance: "terminal-slot-board/swatch", width: 8, height: 8, allowGrowX: false, allowGrowY: false });
			swatch.addState(status.key);
			item.add(swatch);
			item.add(new qx.ui.basic.Label(status.text).set({ appearance: "terminal-slot-board/count-text", rich: false }));
			var count = new qx.ui.basic.Label("0").set({ appearance: "terminal-slot-board/count-value", rich: false });
			item.add(count);
			this.__counts[status.key] = count;
			counts.add(item);
		}
		head.add(counts);

		var free = new qx.ui.container.Composite(new qx.ui.layout.HBox(4).set({ alignY: "middle" })).set({ appearance: "terminal-slot-board/free" });
		this.__free = new qx.ui.basic.Label("—").set({ appearance: "terminal-slot-board/free-value", rich: false });
		free.add(this.__free);
		free.add(new qx.ui.basic.Label("free").set({ appearance: "terminal-slot-board/free-label", rich: false }));
		free.setToolTipText("Share of bay-hours in the window not booked by any slot");
		head.add(free);
		this.add(head);

		// board: the hour axis and the bays, scrolling only when very narrow or short
		var board = new qx.ui.container.Composite(new qx.ui.layout.VBox(0)).set({ minWidth: Self.MIN_WIDTH });
		var axis = new qx.ui.container.Composite(new qx.ui.layout.HBox(0)).set({ height: 22, allowGrowY: false });
		axis.add(new qx.ui.core.Widget().set({ width: Self.BAY_WIDTH, allowGrowX: false }));
		this.__hours = new qx.ui.container.Composite(new qx.ui.layout.Canvas());
		axis.add(this.__hours, { flex: 1 });
		board.add(axis);

		this.__bays = new qx.ui.container.Composite(new qx.ui.layout.VBox(0));
		board.add(this.__bays, { flex: 1 });

		var scroll = new qx.ui.container.Scroll(board).set({ appearance: "terminal-slot-board/board" });
		this.add(scroll, { flex: 1 });

		// one rich tooltip for every slot; its text is set when the pointer enters a slot
		this.__tip = new qx.ui.tooltip.ToolTip().set({ rich: true });

		qx.theme.manager.Color.getInstance().addListener("changeTheme", this._applySlots, this);

		this._applySlots();
	},

	statics: {

		/**
		 * The statuses: their key and their text, in the header's order.
		 */
		STATUS: [
			{ key: "booked", text: "Booked" },
			{ key: "loading", text: "Loading" },
			{ key: "done", text: "Done" },
			{ key: "late", text: "Late" }
		],

		/**
		 * The modes of transport by their letter.
		 */
		MOTS: { R: "Rail", T: "Truck", B: "Barge", P: "Pipeline" },

		/**
		 * The width of the bay names, and the width under which the board scrolls.
		 */
		BAY_WIDTH: 92,
		MIN_WIDTH: 380,

		/**
		 * Minutes of the day of an "HH:mm" text; null when it is not one.
		 */
		minutes: function (text) {

			var m = /^(\d{1,2}):(\d{2})$/.exec(String(text || "").trim());
			if (!m || +m[1] > 24 || +m[2] > 59)
				return null;
			return +m[1] * 60 + +m[2];
		}
	},

	events: {

		/**
		 * The user picked a slot: { id, selected }.
		 */
		slotSelected: "qx.event.type.Data"
	},

	properties: {

		/**
		 * The theme appearance of the root widget.
		 */
		appearance: { init: "terminal-slot-board", refine: true },

		/**
		 * The board's title.
		 */
		title: { init: "", check: "String", apply: "_applySlots" },

		/**
		 * The first hour shown (0-23).
		 */
		startHour: { init: 6, check: "Integer", apply: "_applySlots" },

		/**
		 * The last hour shown (1-24), after startHour.
		 */
		endHour: { init: 22, check: "Integer", apply: "_applySlots" },

		/**
		 * The current time as "HH:mm", drawn as a vertical line; empty hides it.
		 */
		now: { init: "", check: "String", apply: "_applySlots" },

		/**
		 * The slots, as the server normalized them: { id, bay, start, end, counterparty, product, mot, status }.
		 */
		slots: { init: null, nullable: true, check: "Array", apply: "_applySlots" },

		/**
		 * The id of the selected slot.
		 */
		selected: { init: "", check: "String", apply: "_applySlots" }
	},

	members: {

		__title: null,
		__counts: null,
		__free: null,
		__hours: null,
		__bays: null,
		__tip: null,
		__pending: false,

		/**
		 * Every property draws the board again, once per round of changes.
		 */
		_applySlots: function () {

			if (!this.__bays || this.__pending)
				return;

			this.__pending = true;
			qx.event.Timer.once(function () {
				this.__pending = false;
				if (!this.isDisposed())
					this.__draw();
			}, this, 0);
		},

		/**
		 * The header, the hour axis and the bays with their slots.
		 */
		__draw: function () {

			var Self = gridkrafter.wisejdemo.TerminalSlotBoard;

			var start = Math.max(0, Math.min(23, this.getStartHour()));
			var end = Math.max(start + 1, Math.min(24, this.getEndHour()));
			var from = start * 60, to = end * 60, span = to - from;
			var pct = function (m) { return (Math.max(from, Math.min(to, m)) - from) / span * 100; };

			// the slots that can be placed, and the bays in order of first appearance
			var slots = [], bays = [], all = this.getSlots() || [];
			for (var i = 0; i < all.length; i++) {
				var s = all[i], a = Self.minutes(s.start), b = Self.minutes(s.end);
				if (!s.id || a == null || b == null || b <= a)
					continue;
				var bay = s.bay || "—";
				if (bays.indexOf(bay) < 0)
					bays.push(bay);
				slots.push({ s: s, bay: bay, a: a, b: b });
			}

			this.__title.setValue(this.getTitle() || "");

			// counts per status
			var counts = {};
			for (var i = 0; i < Self.STATUS.length; i++)
				counts[Self.STATUS[i].key] = 0;
			for (var i = 0; i < slots.length; i++)
				counts[slots[i].s.status] = (counts[slots[i].s.status] || 0) + 1;
			for (var key in this.__counts)
				this.__counts[key].setValue(String(counts[key] || 0));

			// free capacity: bay-hours in the window not covered by any slot, overlaps merged per bay
			var used = 0;
			for (var k = 0; k < bays.length; k++) {
				var iv = [];
				for (var i = 0; i < slots.length; i++) {
					if (slots[i].bay === bays[k] && Math.min(to, slots[i].b) > Math.max(from, slots[i].a))
						iv.push([Math.max(from, slots[i].a), Math.min(to, slots[i].b)]);
				}
				iv.sort(function (x, y) { return x[0] - y[0]; });
				var cs = -1, ce = -1;
				for (var i = 0; i < iv.length; i++) {
					if (iv[i][0] > ce) {
						if (ce > cs)
							used += ce - cs;
						cs = iv[i][0];
						ce = iv[i][1];
					}
					else
						ce = Math.max(ce, iv[i][1]);
				}
				if (ce > cs)
					used += ce - cs;
			}
			var total = bays.length * span;
			this.__free.setValue(total ? Math.round((1 - used / total) * 100) + "%" : "—");

			var nowM = Self.minutes(this.getNow());
			var showNow = nowM != null && nowM >= from && nowM <= to;

			this.__drawAxis(start, end, pct, showNow ? nowM : null);
			this.__drawBays(bays, slots, start, end, pct, showNow ? nowM : null, from, to);
		},

		/**
		 * The hour labels (every two hours over a 12-hour window) and the now label.
		 */
		__drawAxis: function (start, end, pct, nowM) {

			var old = this.__hours.removeAll();
			for (var i = 0; i < old.length; i++)
				old[i].dispose();

			var step = end - start > 12 ? 2 : 1;
			for (var h = start; h <= end; h++) {
				if ((h - start) % step && h !== end)
					continue;
				var label = new qx.ui.basic.Label((h < 10 ? "0" : "") + h + ":00").set({ appearance: "terminal-slot-board/hour", rich: false });
				var left = pct(h * 60);
				if (h === start)
					this.__hours.add(label, { left: "0%", top: 4 });
				else if (h === end)
					this.__hours.add(label, { right: 0, top: 4 });
				else {
					this.__hours.add(label, { left: left + "%", top: 4 });
					this.__center(label);
				}
			}

			if (nowM != null) {
				var now = new qx.ui.basic.Label(this.getNow()).set({ appearance: "terminal-slot-board/now-label", rich: false, zIndex: 2 });
				this.__hours.add(now, { left: pct(nowM) + "%", top: 3 });
				this.__center(now);
			}
		},

		/**
		 * Moves a label left by half its width once it knows its size, so it centres on its place.
		 */
		__center: function (label) {

			label.addListenerOnce("appear", function () {
				var hint = label.getSizeHint();
				label.setMarginLeft(-Math.round(hint.width / 2));
			});
		},

		/**
		 * One row per bay: its name, the hour gridlines, its slots and the now-line.
		 */
		__drawBays: function (bays, slots, start, end, pct, nowM, from, to) {

			var Self = gridkrafter.wisejdemo.TerminalSlotBoard;
			var old = this.__bays.removeAll();
			for (var i = 0; i < old.length; i++)
				old[i].destroy();

			if (!bays.length) {
				this.__bays.add(new qx.ui.basic.Label("No slots booked").set({ appearance: "terminal-slot-board/empty", rich: false, allowGrowX: true, textAlign: "center" }));
				return;
			}

			var selected = this.getSelected();
			for (var k = 0; k < bays.length; k++) {
				var row = new qx.ui.container.Composite(new qx.ui.layout.HBox(0).set({ alignY: "middle" })).set({ appearance: "terminal-slot-board/bay", minHeight: 28, maxHeight: 48 });
				row.add(new qx.ui.basic.Label(bays[k]).set({ appearance: "terminal-slot-board/bay-name", rich: false, width: Self.BAY_WIDTH, allowGrowX: false, allowGrowY: false, toolTipText: bays[k] }));

				var track = new qx.ui.container.Composite(new qx.ui.layout.Canvas());
				for (var h = start + 1; h < end; h++)
					track.add(new qx.ui.core.Widget().set({ appearance: "terminal-slot-board/grid", width: 1, anonymous: true }), { left: pct(h * 60) + "%", top: 0, bottom: 0 });

				for (var i = 0; i < slots.length; i++) {
					var x = slots[i];
					if (x.bay !== bays[k] || x.b <= from || x.a >= to)
						continue;
					var left = pct(x.a);
					track.add(this.__slot(x.s, x.s.id === selected), { left: left + "%", width: (pct(x.b) - left) + "%", top: 3, bottom: 3 });
				}

				if (nowM != null)
					track.add(new qx.ui.core.Widget().set({ appearance: "terminal-slot-board/now", width: 2, marginLeft: -1, zIndex: 3, anonymous: true }), { left: pct(nowM) + "%", top: 0, bottom: 0 });

				row.add(track, { flex: 1 });
				this.__bays.add(row, { flex: 1 });
			}
		},

		/**
		 * One slot bar: its MOT letter, counterparty and product, in the state of its status.
		 */
		__slot: function (s, selected) {

			var slot = new qx.ui.container.Composite(new qx.ui.layout.HBox(5).set({ alignY: "middle" })).set({
				appearance: "terminal-slot-board/slot", cursor: "pointer", marginLeft: 1, marginRight: 1, minWidth: 0, zIndex: 1
			});
			slot.setUserData("slot", s);

			var mot = new qx.ui.basic.Label(s.mot).set({ appearance: "terminal-slot-board/mot", rich: false, width: 15, height: 15, allowGrowY: false, textAlign: "center", anonymous: true });
			var cp = new qx.ui.basic.Label(s.counterparty).set({ appearance: "terminal-slot-board/counterparty", rich: false, minWidth: 0, anonymous: true });
			var product = new qx.ui.basic.Label(s.product).set({ appearance: "terminal-slot-board/product", rich: false, minWidth: 0, anonymous: true });
			slot.add(mot);
			slot.add(cp, { flex: 1 });
			slot.add(product, { flex: 3 });

			var parts = [slot, mot, cp, product];
			for (var i = 0; i < parts.length; i++) {
				parts[i].addState(s.status);
				if (selected)
					parts[i].addState("selected");
			}

			slot.getContentElement().setAttribute("role", "button");
			slot.getContentElement().setAttribute("aria-pressed", selected ? "true" : "false");
			slot.setToolTip(this.__tip);
			slot.addListener("pointerover", this._onSlotOver, this);
			slot.addListener("pointerout", function () { slot.removeState("hovered"); });
			slot.addListener("tap", this._onSlotTap, this);
			return slot;
		},

		/**
		 * The pointer entered a slot: hover it and give the shared tooltip its text.
		 */
		_onSlotOver: function (e) {

			var Self = gridkrafter.wisejdemo.TerminalSlotBoard;
			var slot = e.getCurrentTarget();
			var s = slot.getUserData("slot");
			var esc = qx.bom.String.escape;
			slot.addState("hovered");

			var status = s.status;
			for (var i = 0; i < Self.STATUS.length; i++) {
				if (Self.STATUS[i].key === s.status)
					status = Self.STATUS[i].text;
			}
			this.__tip.setLabel(
				"<b>" + esc(s.bay || "") + "</b> · " + esc(s.start) + "–" + esc(s.end) + "<br/>" +
				esc(s.counterparty || "") + (s.product ? " · " + esc(s.product) : "") + "<br/>" +
				esc(Self.MOTS[s.mot] || s.mot || "") + " · " + esc(status));
		},

		/**
		 * A slot: select it.
		 */
		_onSlotTap: function (e) {

			var s = e.getCurrentTarget().getUserData("slot");
			if (this.isEnabled() && s && s.id !== this.getSelected())
				this.fireDataEvent("slotSelected", { id: s.id, selected: s.id });
		}
	},

	/**
	 * Drops the theme listener and the shared tooltip; the child widgets are disposed by the container.
	 */
	destruct: function () {

		qx.theme.manager.Color.getInstance().removeListener("changeTheme", this._applySlots, this);
		this._disposeObjects("__tip");
		this.__title = this.__counts = this.__free = this.__hours = this.__bays = null;
	}
});
