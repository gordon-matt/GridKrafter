/**
 * gridkrafter.wisejdemo.ActivityList
 *
 * The client half of GridKrafter.WisejDemo.Controls.ActivityList: rows of a tinted icon circle, the text
 * and the time right-aligned, with a thin line between rows, as the GridKrafter MVC demo's activity
 * widget draws them. Icons and colours come from gridkrafter.wisejdemo.GridBoard's icon set and palette.
 */
qx.Class.define("gridkrafter.wisejdemo.ActivityList", {

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
		appearance: { init: "activity-list", refine: true },

		/**
		 * The rows: [{ icon, color, text, time, at }].
		 */
		items: { init: null, nullable: true, check: "Array", apply: "_applyItems" },

		/**
		 * The theme's opacity of an icon's circle, from 0 to 1.
		 */
		themeTintOpacity: { init: 0.14, check: "Number", themeable: true, apply: "_applyItems" }
	},

	statics: {

		/**
		 * Returns a colour at an opacity, as CSS.
		 *
		 * @param {String} color a CSS colour.
		 * @param {Number} opacity from 0 to 1.
		 * @return {String} the rgba() colour.
		 */
		tint: function (color, opacity) {

			try {
				var rgb = qx.util.ColorUtil.stringToRgb(color);
				return "rgba(" + rgb[0] + "," + rgb[1] + "," + rgb[2] + "," + opacity + ")";
			}
			catch (ex) {
				return "transparent";
			}
		}
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
			var items = this.getItems() || [];
			for (var i = 0; i < items.length; i++) {

				var item = items[i];
				var color = GridBoard.resolveColor(item.color);

				var row = new qx.ui.container.Composite(new qx.ui.layout.HBox(12).set({ alignY: "middle" })).set({ appearance: "activity-list/row" });
				if (i === items.length - 1)
					row.addState("last");

				var circle = new qx.ui.basic.Label().set({
					appearance: "activity-list/circle", rich: true, anonymous: true,
					width: 30, height: 30, textAlign: "center", allowGrowX: false, allowGrowY: false,
					textColor: color, backgroundColor: gridkrafter.wisejdemo.ActivityList.tint(color, this.getThemeTintOpacity())
				});
				circle.setValue('<span style="display:flex;width:30px;height:30px;align-items:center;justify-content:center">' + GridBoard.iconHtml(item.icon, 15) + '</span>');
				row.add(circle);

				row.add(new qx.ui.basic.Label(item.text || "").set({ appearance: "activity-list/text", rich: false, allowGrowX: true, minWidth: 0 }), { flex: 1 });

				var time = new qx.ui.basic.Label(item.time || "").set({ appearance: "activity-list/time", rich: false });
				if (item.at)
					time.setToolTipText(new Date(item.at).toLocaleString());
				row.add(time);

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
