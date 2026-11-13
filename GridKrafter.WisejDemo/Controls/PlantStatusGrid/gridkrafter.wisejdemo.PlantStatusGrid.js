/**
 * gridkrafter.wisejdemo.PlantStatusGrid
 *
 * The client half of GridKrafter.WisejDemo.PlantStatusGrid: a summary line (plants running, reduced,
 * down, in turnaround, and the fleet's run rate against nameplate) over a scrolling list of groups
 * (by region, by ownership or none), each a header and a grid of plant tiles. A tile shows a status
 * dot, the plant's name, a 3P badge for 3rd-party plants, a thin run-rate bar and the rate; hovering it
 * shows capacity, today's production and status. The server owns every value; the browser only
 * reports the plant the user clicks.
 */
qx.Class.define("gridkrafter.wisejdemo.PlantStatusGrid", {

	extend: wisej.web.Control,

	/**
	 * Builds the summary, the scrolling group list and the shared tooltip once.
	 */
	construct: function () {

		this.base(arguments, new qx.ui.layout.VBox(8));

		this.setFocusable(false);

		// summary: the counts on the left (they wrap when narrow), the fleet total on the right
		var summary = new qx.ui.container.Composite(new qx.ui.layout.HBox(12).set({ alignY: "middle" })).set({ appearance: "plant-status-grid/summary" });
		this.__counts = new qx.ui.basic.Label().set({ appearance: "plant-status-grid/counts", rich: true, wrap: true });
		summary.add(this.__counts, { flex: 1 });
		this.__total = new qx.ui.basic.Label().set({ appearance: "plant-status-grid/total", rich: true, wrap: false, alignY: "middle" });
		summary.add(this.__total);
		this.add(summary);

		// the groups, in a vertical scroller
		this.__groups = new qx.ui.container.Composite(new qx.ui.layout.VBox(8));
		this.__scroll = new qx.ui.container.Scroll(this.__groups).set({ scrollbarX: "off", minHeight: 0 });
		this.add(this.__scroll, { flex: 1 });

		this.__tip = new qx.ui.tooltip.ToolTip().set({ rich: true });
		this.__tiles = [];

		qx.theme.manager.Color.getInstance().addListener("changeTheme", this._applyPlants, this);
		this.__scroll.addListener("resize", this._onScrollResize, this);

		this._applyPlants();
	},

	statics: {

		/**
		 * The statuses, in the order the summary counts them.
		 */
		STATUSES: ["running", "reduced", "down", "turnaround"],

		/**
		 * The text of each status.
		 */
		TEXTS: { running: "Running", reduced: "Reduced", down: "Down", turnaround: "Turnaround" },

		/**
		 * The narrowest a tile gets before the grid drops a column, and the gap between tiles.
		 */
		TILE_WIDTH: 112,
		GAP: 4,

		/**
		 * Formats a number with thousands separators.
		 */
		fmt: function (v) {
			v = +v || 0;
			return Math.round(v).toLocaleString("en-US");
		}
	},

	events: {

		/**
		 * The user clicked a plant's tile: { plantId, selected }. The server sets it and renders it back.
		 */
		plantSelected: "qx.event.type.Data"
	},

	properties: {

		/**
		 * The theme appearance of the root widget.
		 */
		appearance: { init: "plant-status-grid", refine: true },

		/**
		 * The plants: [{ id, name, region, owned, status, capacityMmgy, runRatePct, productionKgal }].
		 */
		plants: { init: null, nullable: true, check: "Array", apply: "_applyPlants" },

		/**
		 * The id of the selected plant.
		 */
		selected: { init: "", check: "String", apply: "_applySelected" },

		/**
		 * How the tiles are grouped.
		 */
		groupBy: { init: "region", check: ["region", "ownership", "none"], apply: "_applyPlants" },

		/**
		 * The theme's colours of each status, set by the mixin.
		 */
		themeRunningColor: { init: "#1fb57a", check: "Color", themeable: true, apply: "_applyPlants" },
		themeReducedColor: { init: "#f5a524", check: "Color", themeable: true, apply: "_applyPlants" },
		themeDownColor: { init: "#e5484d", check: "Color", themeable: true, apply: "_applyPlants" },
		themeTurnaroundColor: { init: "#6d4dff", check: "Color", themeable: true, apply: "_applyPlants" }
	},

	members: {

		__counts: null,
		__total: null,
		__groups: null,
		__scroll: null,
		__tip: null,
		__tiles: null,
		__columns: 0,

		/**
		 * Resolves a theme colour name or a CSS colour to CSS.
		 */
		__css: function (color) {

			var manager = qx.theme.manager.Color.getInstance();
			return manager.isDynamic(color) ? manager.resolve(color) : color;
		},

		/**
		 * The colour of a status.
		 */
		__statusColor: function (status) {

			switch (status) {
				case "reduced": return this.__css(this.getThemeReducedColor());
				case "down": return this.__css(this.getThemeDownColor());
				case "turnaround": return this.__css(this.getThemeTurnaroundColor());
				default: return this.__css(this.getThemeRunningColor());
			}
		},

		/**
		 * A plant's status, one of STATUSES; running when unknown.
		 */
		__status: function (plant) {

			var s = plant && plant.status;
			return gridkrafter.wisejdemo.PlantStatusGrid.TEXTS[s] ? s : "running";
		},

		/**
		 * How many tile columns fit the scroller's width.
		 */
		__columnCount: function () {

			var Self = gridkrafter.wisejdemo.PlantStatusGrid;
			var bounds = this.__scroll.getBounds();
			var width = bounds ? bounds.width - 12 : 600;
			return Math.max(1, Math.floor((width + Self.GAP) / (Self.TILE_WIDTH + Self.GAP)));
		},

		/**
		 * The scroller changed width: lay the tiles out again when the number of columns changed.
		 */
		_onScrollResize: function () {

			if (this.__columnCount() !== this.__columns)
				this._applyPlants();
		},

		/**
		 * Draws the summary and rebuilds the groups and their tiles.
		 */
		_applyPlants: function () {

			if (!this.__groups)
				return;

			var Self = gridkrafter.wisejdemo.PlantStatusGrid;
			var esc = qx.bom.String.escape;
			var fmt = Self.fmt;
			var plants = [];
			var all = this.getPlants() || [];
			for (var i = 0; i < all.length; i++)
				if (all[i] && all[i].id)
					plants.push(all[i]);

			// summary
			var counts = { running: 0, reduced: 0, down: 0, turnaround: 0 };
			var cap = 0, run = 0;
			for (var i = 0; i < plants.length; i++) {
				var c = +plants[i].capacityMmgy || 0;
				counts[this.__status(plants[i])]++;
				cap += c;
				run += c * (+plants[i].runRatePct || 0) / 100;
			}
			var strong = "color:" + this.__css("windowText") + ";font-weight:600";
			var parts = [];
			for (var s = 0; s < Self.STATUSES.length; s++) {
				var status = Self.STATUSES[s];
				if (status === "turnaround" && !counts[status])
					continue;
				parts.push("<span style='white-space:nowrap'><span style='display:inline-block;width:8px;height:8px;border-radius:50%;margin-right:5px;background:" +
					this.__statusColor(status) + "'></span><b style='" + strong + "'>" + counts[status] + "</b> " + Self.TEXTS[status].toLowerCase() + "</span>");
			}
			this.__counts.setValue(parts.join("&nbsp;&nbsp;&nbsp; "));
			this.__total.setValue(
				"<b style='color:" + this.__css("windowText") + ";font-weight:700;font-size:14px'>" + fmt(run) + "</b> / " + fmt(cap) + " MMgy " +
				"<span style='color:" + this.__css("highlight") + ";font-weight:600'>" + (cap ? Math.round(run / cap * 100) : 0) + "%</span>");

			// groups
			var old = this.__groups.removeAll();
			for (var i = 0; i < old.length; i++)
				old[i].dispose();
			this.__tiles = [];

			if (!plants.length) {
				this.__groups.add(new qx.ui.basic.Label("No plants").set({ appearance: "plant-status-grid/empty", rich: false, alignX: "center" }));
				return;
			}

			var by = this.getGroupBy();
			var groups = [], index = {};
			for (var i = 0; i < plants.length; i++) {
				var p = plants[i];
				var key = by === "none" ? "" : by === "ownership" ? (p.owned === false ? "3rd-party" : "Owned") : (p.region || "Other");
				if (!index.hasOwnProperty(key)) {
					index[key] = groups.length;
					groups.push({ key: key, plants: [] });
				}
				groups[index[key]].plants.push(p);
			}
			if (by === "ownership")
				groups.sort(function (a, b) { return a.key === "Owned" ? -1 : b.key === "Owned" ? 1 : 0; });

			var columns = this.__columns = this.__columnCount();
			for (var g = 0; g < groups.length; g++) {

				var group = groups[g];
				var box = new qx.ui.container.Composite(new qx.ui.layout.VBox(4));

				if (group.key) {
					var gcap = 0, grun = 0;
					for (var i = 0; i < group.plants.length; i++) {
						var c = +group.plants[i].capacityMmgy || 0;
						gcap += c;
						grun += c * (+group.plants[i].runRatePct || 0) / 100;
					}
					box.add(new qx.ui.basic.Label(
						"<span style='text-transform:uppercase;letter-spacing:.04em;font-weight:600'>" + esc(group.key) + "</span>&nbsp; " +
						group.plants.length + " plants · " + (gcap ? Math.round(grun / gcap * 100) : 0) + "%").set({ appearance: "plant-status-grid/header", rich: true }));
				}

				var layout = new qx.ui.layout.Grid(Self.GAP, Self.GAP);
				for (var col = 0; col < columns; col++)
					layout.setColumnFlex(col, 1);
				var grid = new qx.ui.container.Composite(layout);
				for (var i = 0; i < group.plants.length; i++)
					grid.add(this.__createTile(group.plants[i]), { row: Math.floor(i / columns), column: i % columns });
				box.add(grid);

				this.__groups.add(box);
			}

			this._applySelected();
		},

		/**
		 * One plant's tile: status dot, name, 3P badge, run-rate bar and rate.
		 */
		__createTile: function (plant) {

			var Self = gridkrafter.wisejdemo.PlantStatusGrid;
			var status = this.__status(plant);
			var color = this.__statusColor(status);
			var rate = Math.max(0, +plant.runRatePct || 0);

			var tile = new qx.ui.container.Composite(new qx.ui.layout.VBox(3)).set({ appearance: "plant-status-grid/tile", cursor: "pointer", minWidth: 60 });
			tile.setUserData("id", plant.id);
			tile.setUserData("plant", plant);
			tile.getContentElement().setAttribute("role", "button");

			var line1 = new qx.ui.container.Composite(new qx.ui.layout.HBox(5).set({ alignY: "middle" })).set({ anonymous: true });
			line1.add(new qx.ui.core.Widget().set({ appearance: "plant-status-grid/dot", backgroundColor: color, width: 8, height: 8, allowGrowX: false, allowGrowY: false, anonymous: true }));
			line1.add(new qx.ui.basic.Label(plant.name || plant.id).set({ appearance: "plant-status-grid/name", rich: false, minWidth: 0, anonymous: true }), { flex: 1 });
			if (plant.owned === false)
				line1.add(new qx.ui.basic.Label("3P").set({ appearance: "plant-status-grid/badge", rich: false, anonymous: true }));
			tile.add(line1);

			var line2 = new qx.ui.container.Composite(new qx.ui.layout.HBox(6).set({ alignY: "middle" })).set({ anonymous: true });
			var track = new qx.ui.container.Composite(new qx.ui.layout.Canvas()).set({ appearance: "plant-status-grid/track", height: 4, allowGrowY: false, anonymous: true });
			if (status === "running" || status === "reduced")
				track.add(new qx.ui.core.Widget().set({ appearance: "plant-status-grid/bar", backgroundColor: color, anonymous: true }), { left: 0, top: 0, bottom: 0, width: Math.min(100, Math.round(rate)) + "%" });
			line2.add(track, { flex: 1 });
			var text = status === "down" ? "Down" : status === "turnaround" ? "TA" : rate.toFixed(0) + "%";
			var rateLabel = new qx.ui.basic.Label(text).set({ appearance: "plant-status-grid/rate", rich: false, width: 34, textAlign: "right", anonymous: true });
			if (status === "down" || status === "turnaround")
				rateLabel.setTextColor(color);
			line2.add(rateLabel);
			tile.add(line2);

			tile.setToolTip(this.__tip);
			tile.addListener("tap", this._onTileTap, this);
			tile.addListener("pointerover", this._onTileOver, this);
			tile.addListener("pointerout", function (e) { e.getCurrentTarget().removeState("hovered"); });

			this.__tiles.push(tile);
			return tile;
		},

		/**
		 * Marks the selected tile.
		 */
		_applySelected: function () {

			if (!this.__tiles)
				return;

			var selected = this.getSelected();
			for (var i = 0; i < this.__tiles.length; i++) {
				var tile = this.__tiles[i];
				var on = !!selected && tile.getUserData("id") === selected;
				if (on)
					tile.addState("selected");
				else
					tile.removeState("selected");
				tile.getContentElement().setAttribute("aria-pressed", on ? "true" : "false");
			}
		},

		/**
		 * A tile is hovered: mark it and fill the shared tooltip with its plant.
		 */
		_onTileOver: function (e) {

			var Self = gridkrafter.wisejdemo.PlantStatusGrid;
			var esc = qx.bom.String.escape;
			var tile = e.getCurrentTarget();
			var p = tile.getUserData("plant");
			tile.addState("hovered");
			if (!p)
				return;

			this.__tip.setLabel(
				"<b>" + esc(p.name || p.id) + "</b> · " + esc(p.region || "") + (p.owned === false ? " · 3rd-party" : "") + "<br/>" +
				"Status: <b>" + Self.TEXTS[this.__status(p)] + "</b><br/>" +
				"Capacity: <b>" + Self.fmt(p.capacityMmgy) + " MMgy</b><br/>" +
				"Run rate: <b>" + Self.fmt(p.runRatePct) + "%</b> · Today: <b>" + Self.fmt(p.productionKgal) + " kgal</b>");
		},

		/**
		 * A tile was tapped: report its plant; the server sets it and renders it back.
		 */
		_onTileTap: function (e) {

			if (!this.isEnabled())
				return;

			var id = e.getCurrentTarget().getUserData("id");
			this.fireDataEvent("plantSelected", { plantId: id, selected: id });
		}
	},

	/**
	 * Drops the theme listener and the shared tooltip; the child widgets are disposed by the container.
	 */
	destruct: function () {

		qx.theme.manager.Color.getInstance().removeListener("changeTheme", this._applyPlants, this);
		this._disposeObjects("__tip");
		this.__counts = this.__total = this.__groups = this.__scroll = this.__tiles = null;
	}
});
