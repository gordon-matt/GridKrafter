/**
 * gridkrafter.wisejdemo.ServerLoadList
 *
 * The client half of GridKrafter.WisejDemo.Controls.ServerLoadList: rows of a muted icon, the name with
 * its percentage right-aligned, and a thin bar coloured by its tone (success green, warning amber,
 * danger red), with a thin line between rows, as the GridKrafter MVC demo's server load widget draws
 * them. Icons and colours come from gridkrafter.wisejdemo.GridBoard's icon set and palette.
 */
qx.Class.define("gridkrafter.wisejdemo.ServerLoadList", {

	extend: wisej.web.Control,

	/**
	 * Creates the column of rows.
	 */
	construct: function () {

		this.base(arguments, new qx.ui.layout.VBox(0));
		qx.theme.manager.Color.getInstance().addListener("changeTheme", this._applyItems, this);
	},

	properties: {

		/**
		 * The theme appearance.
		 */
		appearance: { init: "server-load-list", refine: true },

		/**
		 * The rows: [{ name, icon, value, tone }]; tone is success, warning or danger.
		 */
		items: { init: null, nullable: true, check: "Array", apply: "_applyItems" }
	},

	statics: {

		/**
		 * The palette colour of each tone.
		 */
		TONES: { success: "green", warning: "amber", danger: "red" }
	},

	members: {

		/**
		 * Draws the rows again: the list is short and refreshed whole.
		 */
		_applyItems: function () {

			var old = this.removeAll();
			for (var i = 0; i < old.length; i++)
				old[i].dispose();

			var GridBoard = gridkrafter.wisejdemo.GridBoard;
			var tones = gridkrafter.wisejdemo.ServerLoadList.TONES;
			var items = this.getItems() || [];
			for (var i = 0; i < items.length; i++) {

				var item = items[i];
				var value = Math.max(0, Math.min(100, parseInt(item.value, 10) || 0));

				var row = new qx.ui.container.Composite(new qx.ui.layout.HBox(10).set({ alignY: "middle" })).set({ appearance: "server-load-list/row" });
				if (i === items.length - 1)
					row.addState("last");

				var icon = new qx.ui.basic.Label().set({ appearance: "server-load-list/icon", rich: true, anonymous: true });
				icon.setValue(GridBoard.iconHtml(item.icon, 16));
				row.add(icon);

				var body = new qx.ui.container.Composite(new qx.ui.layout.VBox(5));
				var head = new qx.ui.container.Composite(new qx.ui.layout.HBox(8));
				head.add(new qx.ui.basic.Label(item.name || "").set({ appearance: "server-load-list/name", rich: false, allowGrowX: true }), { flex: 1 });
				head.add(new qx.ui.basic.Label(value + "%").set({ appearance: "server-load-list/value", rich: false }));
				body.add(head);

				var track = new qx.ui.container.Composite(new qx.ui.layout.Canvas()).set({ appearance: "server-load-list/track", allowGrowX: true });
				var fill = new qx.ui.core.Widget().set({ appearance: "server-load-list/fill", backgroundColor: GridBoard.resolveColor(tones[item.tone] || "green") });
				track.add(fill, { left: 0, top: 0, bottom: 0, width: value + "%" });
				track.getContentElement().setAttribute("role", "progressbar");
				track.getContentElement().setAttribute("aria-label", item.name || "");
				track.getContentElement().setAttribute("aria-valuenow", String(value));
				track.getContentElement().setAttribute("aria-valuemin", "0");
				track.getContentElement().setAttribute("aria-valuemax", "100");
				body.add(track);

				row.add(body, { flex: 1 });
				this.add(row);
			}
		}
	},

	/**
	 * Drops the theme listener.
	 */
	destruct: function () {

		qx.theme.manager.Color.getInstance().removeListener("changeTheme", this._applyItems, this);
	}
});
