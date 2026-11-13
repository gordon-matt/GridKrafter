/**
 * gridkrafter.wisejdemo.AgingBuckets
 *
 * The client half of GridKrafter.WisejDemo.AgingBuckets: a title with the total and the past-due total
 * (every bucket but the first) with its percentage, column captions, and one clickable row for each bucket
 * with a bar scaled to the largest bucket, its amount, its count and its share of the total. The bars run
 * from neutral to warning to negative. The server owns every value; the browser only reports the click.
 */
qx.Class.define("gridkrafter.wisejdemo.AgingBuckets", {

	extend: wisej.web.Control,

	/**
	 * Builds the fixed parts once: header, captions and the rows box.
	 */
	construct: function () {

		this.base(arguments, new qx.ui.layout.VBox(6));

		this.setFocusable(false);

		var head = new qx.ui.container.Composite(new qx.ui.layout.HBox(10).set({ alignY: "bottom" }));
		this.__title = new qx.ui.basic.Label("").set({ appearance: "aging-buckets/title", rich: false });
		this.__total = new qx.ui.basic.Label("").set({ appearance: "aging-buckets/summary", rich: true });
		this.__past = new qx.ui.basic.Label("").set({ appearance: "aging-buckets/summary", rich: true });
		head.add(this.__title, { flex: 1 });
		head.add(this.__total);
		head.add(this.__past);
		this.add(head);

		this.__caption = new qx.ui.container.Composite(new qx.ui.layout.HBox(8).set({ alignY: "middle" })).set({ appearance: "aging-buckets/caption" });
		this.add(this.__caption);

		this.__rows = new qx.ui.container.Composite(new qx.ui.layout.VBox(2));
		this.add(this.__rows, { flex: 1 });

		this.addListener("resize", this._onResize, this);
		qx.theme.manager.Color.getInstance().addListener("changeTheme", this._applyRows, this);

		this._applyRows();
	},

	statics: {

		/**
		 * Below this width the count column is left out.
		 */
		NARROW: 420
	},

	events: {

		/**
		 * The user clicked a bucket: { bucket, selected }.
		 */
		bucketSelected: "qx.event.type.Data"
	},

	properties: {

		/**
		 * The theme appearance of the root widget.
		 */
		appearance: { init: "aging-buckets", refine: true },

		/**
		 * The title.
		 */
		title: { init: "Receivables aging", check: "String", apply: "_applyRows" },

		/**
		 * The currency or unit shown before the amounts.
		 */
		unit: { init: "$", check: "String", apply: "_applyRows" },

		/**
		 * The name of the selected bucket.
		 */
		selected: { init: "", check: "String", apply: "_applyRows" },

		/**
		 * The buckets, as the server normalized them: { bucket, amount, count }.
		 */
		buckets: { init: null, nullable: true, check: "Array", apply: "_applyRows" },

		/**
		 * The theme's colour of each bucket's bar, and of the past-due percentage, set by the mixin.
		 */
		themeBucket0Color: { init: "#8d95a3", check: "Color", themeable: true, apply: "_applyRows" },
		themeBucket1Color: { init: "#f2a516", check: "Color", themeable: true, apply: "_applyRows" },
		themeBucket2Color: { init: "#ff7a1a", check: "Color", themeable: true, apply: "_applyRows" },
		themeBucket3Color: { init: "#e5484d", check: "Color", themeable: true, apply: "_applyRows" },
		themeBucket4Color: { init: "#e5484d", check: "Color", themeable: true, apply: "_applyRows" },
		themePastDueColor: { init: "#e5484d", check: "Color", themeable: true, apply: "_applyRows" }
	},

	members: {

		__title: null,
		__total: null,
		__past: null,
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
		 * The CSS colour of the bar of a bucket (the fifth colour serves every bucket after it).
		 */
		__bucketColor: function (index) {

			return this.__css(this["getThemeBucket" + Math.min(index, 4) + "Color"]());
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
		 * An amount with the unit before it.
		 */
		__money: function (value) {

			return this.getUnit() + this.__compact(value);
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

			var narrow = (e.getData().width || 0) < gridkrafter.wisejdemo.AgingBuckets.NARROW;
			if (narrow !== this.__narrow) {
				this.__narrow = narrow;
				this._applyRows();
			}
		},

		/**
		 * Header, captions and rows.
		 */
		__draw: function () {

			var list = this.getBuckets() || [];
			var total = 0, past = 0;
			for (var i = 0; i < list.length; i++) {
				total += list[i].amount || 0;
				if (i > 0)
					past += list[i].amount || 0;
			}
			var pastPct = total > 0 ? Math.round(past / total * 100) : 0;
			var esc = qx.bom.String.escape;

			this.__title.setValue(this.getTitle());
			this.__total.setValue("Total <b>" + esc(this.__money(total)) + "</b>");
			this.__past.setValue("Past due <b>" + esc(this.__money(past)) + "</b> <b style='color:" + this.__css(this.getThemePastDueColor()) + "'>" + pastPct + "%</b>");

			this.__drawCaptions();
			this.__drawRows(list, total);
		},

		/**
		 * The captions of the columns.
		 */
		__drawCaptions: function () {

			var old = this.__caption.removeAll();
			for (var i = 0; i < old.length; i++)
				old[i].dispose();

			var narrow = this.__narrow;
			var make = function (self, text, props, layout) {
				self.__caption.add(new qx.ui.basic.Label(text).set({ appearance: "aging-buckets/caption-text", rich: false }).set(props), layout);
			};

			make(this, "Bucket", { width: narrow ? 54 : 62 }, {});
			make(this, "", {}, { flex: 1 });
			make(this, "Amount", { width: narrow ? 70 : 78, textAlign: "right" }, {});
			if (!narrow)
				make(this, "Count", { width: 52, textAlign: "right" }, {});
			make(this, narrow ? "%" : "% of total", { width: narrow ? 42 : 46, textAlign: "right" }, {});
		},

		/**
		 * One row for each bucket.
		 */
		__drawRows: function (list, total) {

			var old = this.__rows.removeAll();
			for (var i = 0; i < old.length; i++)
				old[i].dispose();

			if (!list.length) {
				this.__rows.add(new qx.ui.basic.Label("No balances").set({ appearance: "aging-buckets/empty", rich: false, allowGrowX: true, textAlign: "center" }));
				return;
			}

			var max = 1;
			for (var i = 0; i < list.length; i++)
				max = Math.max(max, list[i].amount || 0);

			for (var i = 0; i < list.length; i++)
				this.__addRow(list[i], i, max, total);
		},

		/**
		 * One row: bucket, bar, amount, count and share.
		 */
		__addRow: function (b, index, max, total) {

			var narrow = this.__narrow;
			var amount = b.amount || 0, count = b.count || 0;
			var pct = total > 0 ? amount / total * 100 : 0;

			var row = new qx.ui.container.Composite(new qx.ui.layout.HBox(8).set({ alignY: "middle" })).set({ appearance: "aging-buckets/row", cursor: "pointer", minHeight: 22, maxHeight: 44 });
			row.setUserData("bucket", b.bucket);
			row.setToolTipText(b.bucket + ": " + this.__money(amount) + ", " + count + " item" + (count === 1 ? "" : "s") + ", " + pct.toFixed(1) + "% of total");
			if (b.bucket === this.getSelected())
				row.addState("selected");
			this.__hover(row);
			row.addListener("tap", this._onRowTap, this);

			var last = index >= 4;
			var color = this.__bucketColor(index);

			row.add(new qx.ui.basic.Label(b.bucket).set({ appearance: "aging-buckets/bucket", rich: false, width: narrow ? 54 : 62, anonymous: true, allowShrinkX: true, textColor: last ? this.__css(this.getThemePastDueColor()) : null }));

			var track = new qx.ui.container.Composite(new qx.ui.layout.Canvas()).set({ height: 12, allowGrowY: false, anonymous: true });
			track.add(new qx.ui.core.Widget().set({ appearance: "aging-buckets/track", anonymous: true }), { left: 0, top: 0, bottom: 0, right: 0 });
			track.add(new qx.ui.core.Widget().set({ appearance: "aging-buckets/fill", backgroundColor: color, anonymous: true }), { left: 0, top: 0, bottom: 0, width: (amount / max * 100).toFixed(2) + "%" });
			row.add(track, { flex: 1 });

			row.add(new qx.ui.basic.Label(this.__money(amount)).set({ appearance: "aging-buckets/amount", rich: false, width: narrow ? 70 : 78, textAlign: "right", anonymous: true, textColor: last ? this.__css(this.getThemePastDueColor()) : null }));

			if (!narrow)
				row.add(new qx.ui.basic.Label(String(count)).set({ appearance: "aging-buckets/count", rich: false, width: 52, textAlign: "right", anonymous: true }));

			row.add(new qx.ui.basic.Label(Math.round(pct) + "%").set({ appearance: "aging-buckets/percent", rich: false, width: narrow ? 42 : 46, textAlign: "right", anonymous: true }));

			this.__rows.add(row, { flex: 1 });
		},

		/**
		 * A row: select its bucket.
		 */
		_onRowTap: function (e) {

			var bucket = e.getCurrentTarget().getUserData("bucket");
			if (this.isEnabled() && bucket !== this.getSelected())
				this.fireDataEvent("bucketSelected", { bucket: bucket, selected: bucket });
		}
	},

	/**
	 * Drops the theme listener; the child widgets are disposed by the container.
	 */
	destruct: function () {

		qx.theme.manager.Color.getInstance().removeListener("changeTheme", this._applyRows, this);
		this.__title = this.__total = this.__past = this.__caption = this.__rows = null;
	}
});
