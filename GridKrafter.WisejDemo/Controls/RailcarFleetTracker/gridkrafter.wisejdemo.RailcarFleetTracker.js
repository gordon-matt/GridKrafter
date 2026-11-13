/**
 * gridkrafter.wisejdemo.RailcarFleetTracker
 *
 * The client half of GridKrafter.WisejDemo.RailcarFleetTracker: fleet totals, the fleet split by status
 * in a bar and a legend that filter the list, the All / Terminal / Plant switch, the shipment rows
 * (route, progress track, status, days out, cars, ETA) with their car numbers when expanded, and a
 * route map drawn as SVG (a simplified lower 48, an arc per shipment, the train on its arc).
 * The server owns every value; the browser only reports what the user picks.
 */
qx.Class.define("gridkrafter.wisejdemo.RailcarFleetTracker", {

	extend: wisej.web.Control,

	/**
	 * Builds the fixed parts once: totals, status bar, legend, list header, list and map.
	 */
	construct: function () {

		this.base(arguments, new qx.ui.layout.VBox(10));

		this.setFocusable(false);

		var Self = gridkrafter.wisejdemo.RailcarFleetTracker;

		// summary: totals, the status bar and its legend
		var summary = new qx.ui.container.Composite(new qx.ui.layout.VBox(6));
		this.__kpis = new qx.ui.container.Composite(new qx.ui.layout.HBox(18)).set({ marginBottom: 2 });
		this.__kpiValues = {};
		var kpis = [["fleet", "Fleet"], ["transit", "In transit"], ["overdue", "Overdue cars"], ["shipments", "Shipments"]];
		for (var i = 0; i < kpis.length; i++) {
			var kpi = new qx.ui.container.Composite(new qx.ui.layout.VBox(0));
			kpi.add(new qx.ui.basic.Label(kpis[i][1]).set({ appearance: "railcar-fleet-tracker/kpi-label", rich: false }));
			var value = new qx.ui.basic.Label("0").set({ appearance: "railcar-fleet-tracker/kpi-value", rich: false });
			kpi.add(value);
			this.__kpiValues[kpis[i][0]] = value;
			this.__kpis.add(kpi);
		}
		summary.add(this.__kpis);

		this.__bar = new qx.ui.container.Composite(new qx.ui.layout.HBox(2)).set({ appearance: "railcar-fleet-tracker/statusbar", height: 10, allowGrowY: false });
		this.__legend = new qx.ui.container.Composite(new qx.ui.layout.Flow(12, 4));
		this.__segments = {};
		this.__legendItems = {};
		for (var i = 0; i < Self.STATUS.length; i++) {
			var status = Self.STATUS[i];

			var seg = new qx.ui.core.Widget().set({ appearance: "railcar-fleet-tracker/segment", cursor: "pointer", toolTipText: status.text });
			seg.setUserData("status", status.key);
			seg.addListener("tap", this._onStatusTap, this);
			this.__segments[status.key] = seg;

			var item = new qx.ui.container.Composite(new qx.ui.layout.HBox(5).set({ alignY: "middle" })).set({ appearance: "railcar-fleet-tracker/legend-item", cursor: "pointer" });
			item.setUserData("status", status.key);
			item.add(new qx.ui.core.Widget().set({ appearance: "railcar-fleet-tracker/swatch", width: 8, height: 8, allowGrowY: false, anonymous: true }));
			item.add(new qx.ui.basic.Label(status.text).set({ appearance: "railcar-fleet-tracker/legend-text", rich: false, anonymous: true }));
			item.add(new qx.ui.basic.Label("0").set({ appearance: "railcar-fleet-tracker/legend-count", rich: false, anonymous: true }));
			item.getContentElement().setAttribute("role", "button");
			item.addListener("tap", this._onStatusTap, this);
			this.__hover(item);
			this.__legendItems[status.key] = item;
			this.__legend.add(item);
		}
		summary.add(this.__bar);
		summary.add(this.__legend);
		this.add(summary);

		// main: the list pane and the map
		var main = new qx.ui.container.Composite(new qx.ui.layout.HBox(12));
		var pane = new qx.ui.container.Composite(new qx.ui.layout.VBox(0)).set({ appearance: "railcar-fleet-tracker/listpane" });

		var head = new qx.ui.container.Composite(new qx.ui.layout.HBox(8).set({ alignY: "middle" })).set({ padding: [6, 2] });
		head.add(new qx.ui.basic.Label("Shipments").set({ appearance: "railcar-fleet-tracker/list-title", rich: false }), { flex: 1 });
		this.__groups = new qx.ui.container.Composite(new qx.ui.layout.HBox(2)).set({ appearance: "railcar-fleet-tracker/groupby" });
		for (var i = 0; i < Self.GROUPS.length; i++) {
			var gb = new qx.ui.basic.Label(Self.GROUPS[i].text).set({ appearance: "railcar-fleet-tracker/group-button", rich: false, cursor: "pointer" });
			gb.setUserData("group", Self.GROUPS[i].key);
			gb.getContentElement().setAttribute("role", "button");
			gb.addListener("tap", this._onGroupTap, this);
			this.__hover(gb);
			this.__groups.add(gb);
		}
		head.add(this.__groups);
		pane.add(head);

		this.__rows = new qx.ui.container.Composite(new qx.ui.layout.VBox(0));
		var scroll = new qx.ui.container.Scroll(this.__rows).set({ scrollbarX: "off" });
		pane.add(scroll, { flex: 1 });
		main.add(pane, { flex: 1 });

		this.__map = new qx.ui.embed.Html().set({ appearance: "railcar-fleet-tracker/map", width: 280, allowGrowX: false, overflowX: "hidden", overflowY: "hidden" });
		this.__map.addListener("tap", this._onMapTap, this);
		main.add(this.__map);
		this.add(main, { flex: 1 });

		qx.theme.manager.Color.getInstance().addListener("changeTheme", this._applyShipments, this);

		this._applyShipments();
	},

	statics: {

		/**
		 * The statuses: their key, their text and the theme property that colours them.
		 */
		STATUS: [
			{ key: "loading", text: "Loading", color: "LoadingColor" },
			{ key: "transit", text: "In transit", color: "TransitColor" },
			{ key: "destination", text: "At destination", color: "DestinationColor" },
			{ key: "empty", text: "Empty return", color: "EmptyColor" },
			{ key: "badorder", text: "Bad order", color: "BadOrderColor" }
		],

		/**
		 * The groupings of the list.
		 */
		GROUPS: [{ key: "none", text: "All" }, { key: "destination", text: "Terminal" }, { key: "origin", text: "Plant" }],

		/**
		 * The lower 48 as [lon, lat] points, simplified.
		 */
		US: [[-124.7,48.4],[-124.1,46.2],[-124.4,42],[-123.8,39.8],[-122.4,37.2],[-120.6,34.6],[-117.1,32.5],[-114.7,32.7],[-111,31.3],[-108.2,31.3],[-106.5,31.8],[-104.5,29.6],[-103,29],[-101.4,29.8],[-99.5,27.5],[-97.4,25.9],[-97.2,27.7],[-94,29.6],[-90,29.1],[-89.4,30.2],[-85,29.7],[-83,29],[-82.6,27.5],[-81,25.2],[-80.1,26.5],[-80.6,28.5],[-81.4,30.7],[-79,33.5],[-75.5,35.2],[-76,37],[-74,39.5],[-71,41.5],[-70,41.8],[-70.7,43],[-67,44.8],[-67.8,47.1],[-69.2,47.4],[-71.5,45],[-74.9,45],[-76.5,44],[-79.2,43.4],[-82.5,41.7],[-82.7,45],[-84.6,46.5],[-88,48],[-95.2,49],[-123,49]]
	},

	events: {

		/**
		 * The user filtered by a status: { statusFilter }, "all" to clear.
		 */
		statusFilterChanged: "qx.event.type.Data",

		/**
		 * The user picked a grouping: { groupBy }.
		 */
		groupByChanged: "qx.event.type.Data",

		/**
		 * The user selected a shipment: { id, selected }.
		 */
		shipmentSelected: "qx.event.type.Data",

		/**
		 * The user expanded or collapsed a shipment: { id, expanded }, expanded empty when collapsed.
		 */
		shipmentExpanded: "qx.event.type.Data"
	},

	properties: {

		/**
		 * The theme appearance of the root widget.
		 */
		appearance: { init: "railcar-fleet-tracker", refine: true },

		/**
		 * The status whose shipments show; "all" shows every shipment.
		 */
		statusFilter: { init: "all", check: ["all", "loading", "transit", "destination", "empty", "badorder"], apply: "_applyShipments" },

		/**
		 * How the list is grouped.
		 */
		groupBy: { init: "destination", check: ["none", "destination", "origin"], apply: "_applyShipments" },

		/**
		 * The id of the selected shipment.
		 */
		selected: { init: "", check: "String", apply: "_applyShipments" },

		/**
		 * The id of the shipment whose car numbers show.
		 */
		expanded: { init: "", check: "String", apply: "_applyShipments" },

		/**
		 * Whether the route map shows.
		 */
		showMap: { init: true, check: "Boolean", apply: "_applyShipments" },

		/**
		 * How many car numbers an expanded shipment lists before "+N more".
		 */
		carNumbersShown: { init: 12, check: "Integer", apply: "_applyShipments" },

		/**
		 * The shipments, as the server normalized them.
		 */
		shipments: { init: null, nullable: true, check: "Array", apply: "_applyShipments" },

		/**
		 * The theme's colour of each status, set by the mixin.
		 */
		themeLoadingColor: { init: "#f2a516", check: "Color", themeable: true, apply: "_applyShipments" },
		themeTransitColor: { init: "#6d4dff", check: "Color", themeable: true, apply: "_applyShipments" },
		themeDestinationColor: { init: "#1fb57a", check: "Color", themeable: true, apply: "_applyShipments" },
		themeEmptyColor: { init: "#0ea5e9", check: "Color", themeable: true, apply: "_applyShipments" },
		themeBadOrderColor: { init: "#e5484d", check: "Color", themeable: true, apply: "_applyShipments" },

		/**
		 * The theme's colour of an overdue count, set by the mixin.
		 */
		themeAlertColor: { init: "#e5484d", check: "Color", themeable: true, apply: "_applyShipments" }
	},

	members: {

		__kpis: null,
		__kpiValues: null,
		__bar: null,
		__segments: null,
		__legend: null,
		__legendItems: null,
		__groups: null,
		__rows: null,
		__map: null,
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

			var list = gridkrafter.wisejdemo.RailcarFleetTracker.STATUS;
			for (var i = 0; i < list.length; i++) {
				if (list[i].key === key)
					return this.__css(this["getTheme" + list[i].color]());
			}
			return this.__css(this.getThemeTransitColor());
		},

		/**
		 * The status text of a key.
		 */
		__statusText: function (key) {

			var list = gridkrafter.wisejdemo.RailcarFleetTracker.STATUS;
			for (var i = 0; i < list.length; i++) {
				if (list[i].key === key)
					return list[i].text;
			}
			return key;
		},

		/**
		 * Gives a widget the hovered state while the pointer is over it.
		 */
		__hover: function (widget) {

			widget.addListener("pointerover", function () { widget.addState("hovered"); });
			widget.addListener("pointerout", function () { widget.removeState("hovered"); });
		},

		/**
		 * The shipments of the filtered status.
		 */
		__shown: function () {

			var filter = this.getStatusFilter();
			var all = this.getShipments() || [];
			if (filter === "all")
				return all;

			var shown = [];
			for (var i = 0; i < all.length; i++) {
				if (all[i].status === filter)
					shown.push(all[i]);
			}
			return shown;
		},

		/**
		 * Every property draws the whole widget again, once per round of changes.
		 */
		_applyShipments: function () {

			if (!this.__map || this.__pending)
				return;

			this.__pending = true;
			qx.event.Timer.once(function () {
				this.__pending = false;
				if (this.isDisposed())
					return;
				this.__drawSummary();
				this.__drawList();
				this.__drawMap();
			}, this, 0);
		},

		/**
		 * Totals, the status bar and the legend.
		 */
		__drawSummary: function () {

			var Self = gridkrafter.wisejdemo.RailcarFleetTracker;
			var ships = this.getShipments() || [];
			var filter = this.getStatusFilter();
			var counts = {}, total = 0, late = 0;
			for (var i = 0; i < Self.STATUS.length; i++)
				counts[Self.STATUS[i].key] = 0;
			for (var i = 0; i < ships.length; i++) {
				var n = ships[i].cars || 0;
				counts[ships[i].status] = (counts[ships[i].status] || 0) + n;
				total += n;
				if ((ships[i].daysOut || 0) > (ships[i].expectedDays || 0))
					late += n;
			}

			this.__kpiValues.fleet.setValue(total.toLocaleString());
			this.__kpiValues.transit.setValue((counts.transit || 0).toLocaleString());
			this.__kpiValues.overdue.setValue(late.toLocaleString());
			this.__kpiValues.overdue.setTextColor(late ? this.__css(this.getThemeAlertColor()) : null);
			this.__kpiValues.shipments.setValue(String(ships.length));

			this.__bar.removeAll();
			for (var i = 0; i < Self.STATUS.length; i++) {
				var key = Self.STATUS[i].key;
				var color = this.__statusColor(key);
				var on = filter === "all" || filter === key;

				var seg = this.__segments[key];
				seg.set({ backgroundColor: color, opacity: on ? 1 : 0.3 });
				if (counts[key])
					this.__bar.add(seg, { flex: counts[key] });

				var item = this.__legendItems[key];
				item.getChildren()[0].setBackgroundColor(color);
				item.getChildren()[2].setValue(String(counts[key]));
				if (filter === key)
					item.addState("selected");
				else
					item.removeState("selected");
				item.getContentElement().setAttribute("aria-pressed", filter === key ? "true" : "false");
			}

			var groups = this.__groups.getChildren();
			for (var i = 0; i < groups.length; i++) {
				if (groups[i].getUserData("group") === this.getGroupBy())
					groups[i].addState("selected");
				else
					groups[i].removeState("selected");
			}
		},

		/**
		 * The list: group headers and shipment rows, and the car numbers of the expanded one.
		 */
		__drawList: function () {

			var old = this.__rows.removeAll();
			for (var i = 0; i < old.length; i++)
				old[i].dispose();

			var shown = this.__shown();
			if (!shown.length) {
				this.__rows.add(new qx.ui.basic.Label("No shipments").set({ appearance: "railcar-fleet-tracker/empty", rich: false, allowGrowX: true, textAlign: "center" }));
				return;
			}

			var groupBy = this.getGroupBy();
			if (groupBy === "none") {
				for (var i = 0; i < shown.length; i++)
					this.__addRow(shown[i]);
				return;
			}

			var keys = [], by = {};
			for (var i = 0; i < shown.length; i++) {
				var k = shown[i][groupBy] || "—";
				if (!by[k]) {
					by[k] = [];
					keys.push(k);
				}
				by[k].push(shown[i]);
			}
			for (var g = 0; g < keys.length; g++) {
				var list = by[keys[g]], cars = 0;
				for (var i = 0; i < list.length; i++)
					cars += list[i].cars || 0;

				var header = new qx.ui.container.Composite(new qx.ui.layout.HBox(8)).set({ appearance: "railcar-fleet-tracker/group" });
				header.add(new qx.ui.basic.Label(keys[g].toUpperCase()).set({ appearance: "railcar-fleet-tracker/group-title", rich: false }), { flex: 1 });
				header.add(new qx.ui.basic.Label(list.length + " shipment" + (list.length > 1 ? "s" : "") + " · " + cars + " cars").set({ appearance: "railcar-fleet-tracker/group-total", rich: false }));
				this.__rows.add(header);

				for (var i = 0; i < list.length; i++)
					this.__addRow(list[i]);
			}
		},

		/**
		 * One shipment row, and its car numbers when it is expanded.
		 */
		__addRow: function (s) {

			var color = this.__statusColor(s.status);
			var over = (s.daysOut || 0) - (s.expectedDays || 0);
			var open = s.id === this.getExpanded();
			var progress = Math.max(0, Math.min(100, s.progress || 0));

			var row = new qx.ui.container.Composite(new qx.ui.layout.HBox(10).set({ alignY: "middle" })).set({ appearance: "railcar-fleet-tracker/row", cursor: "pointer" });
			row.setUserData("id", s.id);
			if (s.id === this.getSelected())
				row.addState("selected");
			this.__hover(row);
			row.addListener("tap", this._onRowTap, this);

			var chev = new qx.ui.basic.Label(open ? "▾" : "▸").set({ appearance: "railcar-fleet-tracker/chevron", rich: false, width: 16, textAlign: "center", cursor: "pointer" });
			chev.setUserData("id", s.id);
			chev.getContentElement().setAttribute("role", "button");
			chev.getContentElement().setAttribute("aria-expanded", open ? "true" : "false");
			chev.addListener("tap", this._onChevronTap, this);
			this.__hover(chev);
			row.add(chev);

			var info = new qx.ui.container.Composite(new qx.ui.layout.VBox(1)).set({ anonymous: true });
			var idLine = new qx.ui.container.Composite(new qx.ui.layout.HBox(6).set({ alignY: "middle" })).set({ anonymous: true });
			idLine.add(new qx.ui.basic.Label(s.id).set({ appearance: "railcar-fleet-tracker/id", rich: false, anonymous: true }));
			idLine.add(new qx.ui.basic.Label(s.unitTrain ? "Unit train" : "Manifest").set({ appearance: "railcar-fleet-tracker/kind", rich: false, anonymous: true }));
			info.add(idLine);
			info.add(new qx.ui.basic.Label(s.origin + " → " + s.destination).set({ appearance: "railcar-fleet-tracker/route", rich: false, anonymous: true, allowShrinkX: true }));
			row.add(info, { flex: 1 });

			var prog = new qx.ui.container.Composite(new qx.ui.layout.VBox(4)).set({ width: 116, anonymous: true });
			var track = new qx.ui.container.Composite(new qx.ui.layout.Canvas()).set({ appearance: "railcar-fleet-tracker/track", height: 10, anonymous: true });
			track.add(new qx.ui.core.Widget().set({ appearance: "railcar-fleet-tracker/fill", backgroundColor: color, anonymous: true }), { left: 0, top: 2, bottom: 2, width: progress + "%" });
			var dot = new qx.ui.core.Widget().set({ appearance: "railcar-fleet-tracker/train", width: 10, height: 10, marginLeft: -5, anonymous: true });
			dot.getContentElement().setStyles({ border: "2px solid " + color, borderRadius: "50%", boxSizing: "border-box" });
			track.add(dot, { left: progress + "%", top: 0 });
			prog.add(track);
			prog.add(new qx.ui.basic.Label(this.__statusText(s.status)).set({ appearance: "railcar-fleet-tracker/status", textColor: color, rich: false, anonymous: true }));
			row.add(prog);

			var days = new qx.ui.container.Composite(new qx.ui.layout.VBox(0).set({ alignX: "center" })).set({ width: 64, anonymous: true });
			var day = new qx.ui.basic.Label("Day " + (s.daysOut || 0)).set({ appearance: "railcar-fleet-tracker/days", rich: false, anonymous: true });
			if (over > 0)
				day.setTextColor(this.__css(this.getThemeAlertColor()));
			days.add(day);
			days.add(new qx.ui.basic.Label("of " + (s.expectedDays || 0)).set({ appearance: "railcar-fleet-tracker/days-of", rich: false, anonymous: true }));
			row.add(days);

			var right = new qx.ui.container.Composite(new qx.ui.layout.VBox(0).set({ alignX: "right" })).set({ width: 66, anonymous: true });
			right.add(new qx.ui.basic.Label((s.cars || 0) + " cars").set({ appearance: "railcar-fleet-tracker/cars", rich: false, anonymous: true }));
			var eta = new qx.ui.basic.Label(over > 0 ? "+" + over + "d late" : "ETA " + (s.eta || "—")).set({ appearance: "railcar-fleet-tracker/eta", rich: false, anonymous: true });
			if (over > 0)
				eta.set({ textColor: this.__css(this.getThemeAlertColor()), font: "railcar-fleet-tracker-strong" });
			right.add(eta);
			row.add(right);

			this.__rows.add(row);

			if (open) {
				var numbers = s.carNumbers || [];
				var max = this.getCarNumbersShown();
				var listed = Math.min(numbers.length, max);
				var rest = Math.max(0, (s.cars || 0) - listed);
				var cars = new qx.ui.container.Composite(new qx.ui.layout.Flow(4, 4)).set({ appearance: "railcar-fleet-tracker/carlist" });
				for (var i = 0; i < listed; i++)
					cars.add(new qx.ui.basic.Label(numbers[i]).set({ appearance: "railcar-fleet-tracker/car", rich: false }));
				if (rest)
					cars.add(new qx.ui.basic.Label("+" + rest + " more").set({ appearance: "railcar-fleet-tracker/car-more", rich: false }));
				this.__rows.add(cars);
			}
		},

		/**
		 * The route map: the lower 48, an arc per shipment of the filtered status, the train on it,
		 * plants as hollow pins and terminals as filled, labelled pins.
		 */
		__drawMap: function () {

			this.__map.setVisibility(this.getShowMap() ? "visible" : "excluded");
			if (!this.getShowMap())
				return;

			var Self = gridkrafter.wisejdemo.RailcarFleetTracker;
			var esc = qx.bom.String.escape;
			var W = 280, H = 190, L = -125.5, R = -66, T = 50, B = 24;
			var px = function (lon) { return (lon - L) / (R - L) * W; };
			var py = function (lat) { return (T - lat) / (T - B) * H; };

			var windowColor = this.__css("window"), frame = this.__css("windowFrame"), gray = this.__css("grayText"), text = this.__css("windowText");

			var land = "";
			for (var i = 0; i < Self.US.length; i++)
				land += (i ? " L" : "M") + px(Self.US[i][0]).toFixed(1) + " " + py(Self.US[i][1]).toFixed(1);
			land += "Z";

			var arcs = "", trains = "", places = {}, order = [];
			var shown = this.__shown();
			for (var i = 0; i < shown.length; i++) {
				var s = shown[i];
				if (s.originLon == null || s.originLat == null || s.destLon == null || s.destLat == null)
					continue;

				var color = this.__statusColor(s.status), sel = s.id === this.getSelected();
				var x1 = px(s.originLon), y1 = py(s.originLat), x2 = px(s.destLon), y2 = py(s.destLat);
				var mx = (x1 + x2) / 2, my = (y1 + y2) / 2 - Math.sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1)) * 0.18;
				var t = Math.max(0, Math.min(100, s.progress || 0)) / 100;
				var tx = (1 - t) * (1 - t) * x1 + 2 * (1 - t) * t * mx + t * t * x2;
				var ty = (1 - t) * (1 - t) * y1 + 2 * (1 - t) * t * my + t * t * y2;
				var id = esc(s.id);

				arcs += "<path data-id='" + id + "' d='M" + x1.toFixed(1) + " " + y1.toFixed(1) + " Q" + mx.toFixed(1) + " " + my.toFixed(1) + " " + x2.toFixed(1) + " " + y2.toFixed(1) +
					"' fill='none' stroke='" + color + "' stroke-width='" + (sel ? 2.6 : 1.6) + "' opacity='" + (sel ? 1 : t >= 1 ? 0.9 : 0.45) + "'" +
					(t >= 1 ? "" : " stroke-dasharray='4 3'") + " style='cursor:pointer'/>";
				trains += "<circle data-id='" + id + "' cx='" + tx.toFixed(1) + "' cy='" + ty.toFixed(1) + "' r='" + (sel ? 5 : 4) + "' fill='" + color +
					"' stroke='" + (sel ? text : windowColor) + "' stroke-width='1.6' style='cursor:pointer'><title>" + id + "</title></circle>";

				if (!places[s.origin]) {
					places[s.origin] = { x: x1, y: y1, term: false };
					order.push(s.origin);
				}
				if (!places[s.destination])
					order.push(s.destination);
				places[s.destination] = { x: x2, y: y2, term: true };
			}

			var pins = "";
			for (var i = 0; i < order.length; i++) {
				var p = places[order[i]];
				pins += "<circle cx='" + p.x.toFixed(1) + "' cy='" + p.y.toFixed(1) + "' r='2.6' fill='" + (p.term ? text : windowColor) + "'" +
					(p.term ? "" : " stroke='" + gray + "' stroke-width='1.4'") + "><title>" + esc(order[i]) + "</title></circle>";
				if (p.term)
					pins += "<text x='" + (p.x + 4).toFixed(1) + "' y='" + (p.y + 3).toFixed(1) + "' font-size='9' fill='" + gray + "'>" + esc(order[i].split(",")[0]) + "</text>";
			}

			this.__map.setHtml(
				"<div style='position:absolute;left:10px;top:8px;font-size:11.5px;font-weight:600;color:" + gray + "'>Routes</div>" +
				"<svg viewBox='-6 -24 " + (W + 12) + " " + (H + 34) + "' preserveAspectRatio='xMidYMid meet' style='display:block;width:100%;height:100%'>" +
				"<path d='" + land + "' fill='" + windowColor + "' stroke='" + frame + "' stroke-width='1'/>" + arcs + pins + trains + "</svg>");
		},

		/**
		 * A status segment or legend entry: filter by it, or clear the filter when it is the current one.
		 */
		_onStatusTap: function (e) {

			if (!this.isEnabled())
				return;

			var key = e.getCurrentTarget().getUserData("status");
			this.fireDataEvent("statusFilterChanged", { statusFilter: key === this.getStatusFilter() ? "all" : key });
		},

		/**
		 * A grouping button.
		 */
		_onGroupTap: function (e) {

			var key = e.getCurrentTarget().getUserData("group");
			if (this.isEnabled() && key !== this.getGroupBy())
				this.fireDataEvent("groupByChanged", { groupBy: key });
		},

		/**
		 * A shipment row: select it.
		 */
		_onRowTap: function (e) {

			var id = e.getCurrentTarget().getUserData("id");
			if (this.isEnabled() && id !== this.getSelected())
				this.fireDataEvent("shipmentSelected", { id: id, selected: id });
		},

		/**
		 * A chevron: expand the shipment, or collapse it when it is the expanded one.
		 */
		_onChevronTap: function (e) {

			e.stopPropagation();
			if (!this.isEnabled())
				return;

			var id = e.getCurrentTarget().getUserData("id");
			this.fireDataEvent("shipmentExpanded", { id: id, expanded: id === this.getExpanded() ? "" : id });
		},

		/**
		 * A route or a train on the map: select its shipment.
		 */
		_onMapTap: function (e) {

			if (!this.isEnabled())
				return;

			var target = e.getOriginalTarget();
			var id = target && target.getAttribute ? target.getAttribute("data-id") : null;
			if (id && id !== this.getSelected())
				this.fireDataEvent("shipmentSelected", { id: id, selected: id });
		}
	},

	/**
	 * Drops the theme listener; the child widgets are disposed by the container.
	 */
	destruct: function () {

		qx.theme.manager.Color.getInstance().removeListener("changeTheme", this._applyShipments, this);

		var segments = this.__segments;
		for (var key in segments) {
			if (segments[key] && !segments[key].getLayoutParent())
				segments[key].dispose();
		}
		this.__kpis = this.__kpiValues = this.__bar = this.__segments = this.__legend = this.__legendItems = this.__groups = this.__rows = this.__map = null;
	}
});
