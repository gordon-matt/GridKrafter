/**
 * gridkrafter.wisejdemo.EthanolPriceBoard
 *
 * The client half of GridKrafter.WisejDemo.EthanolPriceBoard: a strip of benchmarks (name, price, unit,
 * change), a grid of hub tiles (hub, $/gal price, change badge, trend line, basis to the reference hub,
 * REF on the reference tile) and a footer with the time of the prices. The trend lines are small SVGs;
 * everything else is native widgets. The server owns every value; the browser only reports a selection.
 */
qx.Class.define("gridkrafter.wisejdemo.EthanolPriceBoard", {

	extend: wisej.web.Control,

	/**
	 * Builds the strip, the tile grid and the footer once; their content is drawn from the data.
	 */
	construct: function () {

		this.base(arguments, new qx.ui.layout.VBox(10));

		this.setFocusable(false);

		this.__strip = new qx.ui.container.Composite(new qx.ui.layout.HBox(6));
		this.add(this.__strip);

		this.__tiles = new qx.ui.container.Composite(new qx.ui.layout.Grid(8, 8));
		this.add(this.__tiles, { flex: 1 });

		var foot = new qx.ui.container.Composite(new qx.ui.layout.HBox(8));
		foot.add(new qx.ui.basic.Label("Spot, $/gal · trend").set({ appearance: "ethanol-price-board/foot", rich: false }), { flex: 1 });
		this.__asOf = new qx.ui.basic.Label("").set({ appearance: "ethanol-price-board/foot", rich: false });
		foot.add(this.__asOf);
		this.add(foot);

		qx.theme.manager.Color.getInstance().addListener("changeTheme", this._applyBoard, this);

		this._applyBoard();
	},

	events: {

		/**
		 * The user selected a hub: { id, selected }.
		 */
		hubSelected: "qx.event.type.Data"
	},

	properties: {

		/**
		 * The theme appearance of the root widget.
		 */
		appearance: { init: "ethanol-price-board", refine: true },

		/**
		 * How many tiles share a row.
		 */
		columns: { init: 3, check: "Integer", apply: "_applyBoard" },

		/**
		 * The id of the hub the basis is measured against.
		 */
		referenceHub: { init: "chi", check: "String", apply: "_applyBoard" },

		/**
		 * Whether tiles show their basis.
		 */
		showBasis: { init: true, check: "Boolean", apply: "_applyBoard" },

		/**
		 * Whether the benchmark strip shows.
		 */
		showBenchmarks: { init: true, check: "Boolean", apply: "_applyBoard" },

		/**
		 * When the prices were taken.
		 */
		asOf: { init: "", check: "String", apply: "_applyBoard" },

		/**
		 * The id of the selected hub.
		 */
		selected: { init: "", check: "String", apply: "_applyBoard" },

		/**
		 * The hubs: [{ id, hub, price, change, history }].
		 */
		hubs: { init: null, nullable: true, check: "Array", apply: "_applyBoard" },

		/**
		 * The benchmarks: [{ name, price, unit, change, decimals }].
		 */
		benchmarks: { init: null, nullable: true, check: "Array", apply: "_applyBoard" },

		/**
		 * The theme's colours of a rise and a fall. Set by the mixin.
		 */
		themeUpColor: { init: "#1fb57a", check: "Color", themeable: true, apply: "_applyBoard" },
		themeDownColor: { init: "#e5484d", check: "Color", themeable: true, apply: "_applyBoard" }
	},

	members: {

		__strip: null,
		__tiles: null,
		__asOf: null,
		__pending: false,

		/**
		 * Resolves a theme colour name or a CSS colour to CSS.
		 */
		__css: function (color) {

			var manager = qx.theme.manager.Color.getInstance();
			return manager.isDynamic(color) ? manager.resolve(color) : color;
		},

		/**
		 * The colour of a change: up, down or unchanged.
		 */
		__changeColor: function (v) {

			return v > 0 ? this.__css(this.getThemeUpColor()) : v < 0 ? this.__css(this.getThemeDownColor()) : this.__css("grayText");
		},

		/**
		 * The colour at the given opacity.
		 */
		__tint: function (color, alpha) {

			try {
				var rgb = qx.util.ColorUtil.stringToRgb(color);
				return qx.util.ColorUtil.rgbToRgbString([rgb[0], rgb[1], rgb[2], alpha]);
			}
			catch (ex) {
				return null;
			}
		},

		/**
		 * Every property draws the board again, once per round of changes.
		 */
		_applyBoard: function () {

			if (!this.__tiles || this.__pending)
				return;

			this.__pending = true;
			qx.event.Timer.once(function () {
				this.__pending = false;
				if (!this.isDisposed())
					this.__draw();
			}, this, 0);
		},

		/**
		 * The strip, the tiles and the footer.
		 */
		__draw: function () {

			var arrow = function (v) { return v > 0 ? "▲" : v < 0 ? "▼" : "■"; };

			// benchmarks
			var old = this.__strip.removeAll();
			for (var i = 0; i < old.length; i++)
				old[i].dispose();
			var bench = this.getBenchmarks() || [];
			this.__strip.setVisibility(this.getShowBenchmarks() && bench.length ? "visible" : "excluded");
			for (var i = 0; i < bench.length; i++) {
				var b = bench[i], d = b.decimals >= 0 ? b.decimals : 4, c = b.change || 0;
				var item = new qx.ui.container.Composite(new qx.ui.layout.HBox(6).set({ alignY: "bottom" })).set({ appearance: "ethanol-price-board/benchmark" });
				item.add(new qx.ui.basic.Label(b.name || "").set({ appearance: "ethanol-price-board/benchmark-name", rich: false }));
				item.add(new qx.ui.basic.Label((b.price || 0).toFixed(d)).set({ appearance: "ethanol-price-board/benchmark-price", rich: false }));
				item.add(new qx.ui.basic.Label(b.unit || "").set({ appearance: "ethanol-price-board/benchmark-unit", rich: false }));
				item.add(new qx.ui.core.Spacer(), { flex: 1 });
				item.add(new qx.ui.basic.Label(arrow(c) + " " + Math.abs(c).toFixed(d)).set({ appearance: "ethanol-price-board/benchmark-change", textColor: this.__changeColor(c), rich: false }));
				this.__strip.add(item, { flex: 1 });
			}

			// tiles
			old = this.__tiles.removeAll();
			for (var i = 0; i < old.length; i++)
				old[i].dispose();

			var layout = this.__tiles.getLayout();
			var hubs = this.getHubs() || [];
			var cols = Math.max(1, this.getColumns());
			for (var c = 0; c < 12; c++)
				layout.setColumnFlex(c, c < cols ? 1 : 0);

			if (!hubs.length) {
				this.__tiles.add(new qx.ui.basic.Label("No prices").set({ appearance: "ethanol-price-board/empty", rich: false, textAlign: "center", allowGrowX: true }), { row: 0, column: 0, colSpan: cols });
				layout.setRowFlex(0, 1);
			}
			else {
				var ref = hubs[0];
				for (var i = 0; i < hubs.length; i++) {
					if (hubs[i].id === this.getReferenceHub()) {
						ref = hubs[i];
						break;
					}
				}
				var rowCount = Math.ceil(hubs.length / cols);
				for (var r = 0; r < Math.max(rowCount, 12); r++)
					layout.setRowFlex(r, r < rowCount ? 1 : 0);
				for (var i = 0; i < hubs.length; i++)
					this.__tiles.add(this.__createTile(hubs[i], ref), { row: Math.floor(i / cols), column: i % cols });
			}

			this.__asOf.setValue(this.getAsOf() || "");
		},

		/**
		 * One hub tile: name (and REF), price with its change badge, trend line, basis.
		 */
		__createTile: function (h, ref) {

			var price = h.price || 0, change = h.change || 0;
			var isRef = h.id === ref.id, showBasis = this.getShowBasis();
			var arrow = change > 0 ? "▲" : change < 0 ? "▼" : "■";
			var changeColor = this.__changeColor(change);

			var tile = new qx.ui.container.Composite(new qx.ui.layout.VBox(2)).set({ appearance: "ethanol-price-board/tile", cursor: "pointer", minWidth: 0 });
			tile.setUserData("id", h.id);
			tile.getContentElement().setAttribute("role", "button");
			tile.getContentElement().setAttribute("aria-label", (h.hub || h.id) + " $" + price.toFixed(4));
			if (h.id === this.getSelected())
				tile.addState("selected");
			tile.addListener("pointerover", function () { tile.addState("hovered"); });
			tile.addListener("pointerout", function () { tile.removeState("hovered"); });
			tile.addListener("tap", this._onTileTap, this);

			var top = new qx.ui.container.Composite(new qx.ui.layout.HBox(6)).set({ anonymous: true });
			top.add(new qx.ui.basic.Label(h.hub || "").set({ appearance: "ethanol-price-board/hub", rich: false, allowShrinkX: true, anonymous: true }), { flex: 1 });
			if (isRef && showBasis)
				top.add(new qx.ui.basic.Label("REF").set({ appearance: "ethanol-price-board/ref", rich: false, anonymous: true }));
			tile.add(top);

			var line = new qx.ui.container.Composite(new qx.ui.layout.HBox(6).set({ alignY: "middle" })).set({ anonymous: true });
			line.add(new qx.ui.basic.Label("$" + price.toFixed(4)).set({ appearance: "ethanol-price-board/price", rich: false, anonymous: true }));
			line.add(new qx.ui.basic.Label(arrow + " " + Math.abs(change).toFixed(4)).set({
				appearance: "ethanol-price-board/change", textColor: changeColor,
				backgroundColor: change ? this.__tint(changeColor, 0.14) : null, rich: false, anonymous: true
			}));
			tile.add(line);

			// trend line: only what no widget draws is markup
			var history = h.history || [];
			var spark = new qx.ui.embed.Html().set({ minHeight: 22, marginTop: 4, marginBottom: 2, anonymous: true });
			if (history.length > 1) {
				var lo = Math.min.apply(null, history), hi = Math.max.apply(null, history), span = hi - lo || 1;
				var points = [];
				for (var i = 0; i < history.length; i++)
					points.push((i / (history.length - 1) * 100).toFixed(1) + "," + (28 - (history[i] - lo) / span * 26).toFixed(1));
				var color = this.__css(history[history.length - 1] >= history[0] ? this.getThemeUpColor() : this.getThemeDownColor());
				spark.setHtml("<svg viewBox='0 0 100 30' preserveAspectRatio='none' style='display:block;width:100%;height:100%'>" +
					"<polyline points='" + points.join(" ") + "' fill='none' stroke='" + color + "' stroke-width='1.6' stroke-linejoin='round' vector-effect='non-scaling-stroke'/></svg>");
			}
			tile.add(spark, { flex: 1 });

			if (showBasis) {
				var basis = price - (ref.price || 0);
				var foot = new qx.ui.container.Composite(new qx.ui.layout.HBox(6)).set({ anonymous: true });
				foot.add(new qx.ui.basic.Label("Basis to " + String(ref.hub || "").split(" ")[0]).set({ appearance: "ethanol-price-board/basis", rich: false, anonymous: true }), { flex: 1 });
				foot.add(new qx.ui.basic.Label(isRef ? "—" : (basis > 0 ? "+" : basis < 0 ? "−" : "") + Math.abs(basis).toFixed(4)).set({
					appearance: "ethanol-price-board/basis-value", textColor: isRef ? null : this.__changeColor(basis), rich: false, anonymous: true
				}));
				tile.add(foot);
			}

			return tile;
		},

		/**
		 * A tile: select its hub.
		 */
		_onTileTap: function (e) {

			var id = e.getCurrentTarget().getUserData("id");
			if (this.isEnabled() && id !== this.getSelected())
				this.fireDataEvent("hubSelected", { id: id, selected: id });
		}
	},

	/**
	 * Drops the theme listener; the child widgets are disposed by the container.
	 */
	destruct: function () {

		qx.theme.manager.Color.getInstance().removeListener("changeTheme", this._applyBoard, this);
		this.__strip = this.__tiles = this.__asOf = null;
	}
});
