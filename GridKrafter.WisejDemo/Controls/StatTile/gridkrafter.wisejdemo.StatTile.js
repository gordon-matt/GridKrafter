/**
 * gridkrafter.wisejdemo.StatTile
 *
 * The client half of GridKrafter.WisejDemo.Controls.StatTile: a muted label, a large bold value, and a
 * line with the change (green up, red down, the board's palette) followed by a muted caption, as the
 * GridKrafter MVC demo's stat widget draws it.
 */
qx.Class.define("gridkrafter.wisejdemo.StatTile", {

	extend: wisej.web.Control,

	/**
	 * Creates the label, the value and the change line.
	 */
	construct: function () {

		this.base(arguments, new qx.ui.layout.VBox(2));

		this._createChildControl("label");
		this._createChildControl("value");
		this._createChildControl("change-line");

		qx.theme.manager.Color.getInstance().addListener("changeTheme", this._applyChange, this);
	},

	properties: {

		/**
		 * The theme appearance.
		 */
		appearance: { init: "stat-tile", refine: true },

		/**
		 * The small label above the value.
		 */
		label: { init: "", check: "String", apply: "_applyTexts" },

		/**
		 * The value, already formatted.
		 */
		value: { init: "", check: "String", apply: "_applyTexts" },

		/**
		 * The change, already formatted.
		 */
		change: { init: "", check: "String", apply: "_applyChange" },

		/**
		 * The muted text after the change.
		 */
		caption: { init: "", check: "String", apply: "_applyChange" },

		/**
		 * Whether the change is down: red instead of green.
		 */
		down: { init: false, check: "Boolean", apply: "_applyChange" }
	},

	members: {

		/**
		 * Creates the parts.
		 */
		_createChildControlImpl: function (id, hash) {

			var control;
			switch (id) {

				case "label":
				case "value":
					control = new qx.ui.basic.Label().set({ rich: false, anonymous: true });
					this._add(control);
					break;

				case "change-line":
					control = new qx.ui.container.Composite(new qx.ui.layout.HBox(4).set({ alignY: "baseline" })).set({ anonymous: true });
					control.add(this.getChildControl("change"));
					control.add(this.getChildControl("caption"));
					this._add(control);
					break;

				case "change":
				case "caption":
					control = new qx.ui.basic.Label().set({ rich: false, anonymous: true });
					break;
			}

			return control || this.base(arguments, id, hash);
		},

		/**
		 * Shows the label and the value.
		 */
		_applyTexts: function () {

			this.getChildControl("label").setValue(this.getLabel());
			this.getChildControl("value").setValue(this.getValue());
		},

		/**
		 * Shows the change in green or red, and the caption.
		 */
		_applyChange: function () {

			var change = this.getChildControl("change");
			change.setValue(this.getChange());
			change.setTextColor(gridkrafter.wisejdemo.GridBoard.resolveColor(this.getDown() ? "red" : "green"));
			this.getChildControl("caption").setValue(this.getCaption());
		}
	},

	/**
	 * Drops the theme listener.
	 */
	destruct: function () {

		qx.theme.manager.Color.getInstance().removeListener("changeTheme", this._applyChange, this);
	}
});
