/**
 * gridkrafter.wisejdemo.AppTopBar
 *
 * The client half of GridKrafter.WisejDemo.AppTopBar: the breadcrumb (earlier crumbs are links
 * separated by ›, the last one is the current page), "Data as of" with a live or stale dot, the
 * search box, the environment badge, the alerts bell with its count and the signed-in user.
 * Under 900 px the user's name and role hide; under 600 px "Data as of" hides and the search box
 * collapses to a magnifier button that opens it. The server owns every value; the browser only
 * reports what the user does.
 */
qx.Class.define("gridkrafter.wisejdemo.AppTopBar", {

	extend: wisej.web.Control,

	/**
	 * Builds the widget tree once, left to right.
	 */
	construct: function () {

		this.base(arguments, new qx.ui.layout.HBox(12).set({ alignY: "middle" }));

		this.setFocusable(false);
		var Self = gridkrafter.wisejdemo.AppTopBar;

		// breadcrumb
		this.__crumbs = new qx.ui.container.Composite(new qx.ui.layout.HBox(6).set({ alignY: "middle" })).set({ appearance: "app-top-bar/crumbs", allowShrinkX: true, minWidth: 0 });
		this.add(this.__crumbs, { flex: 0 });

		// data freshness
		this.__asOf = new qx.ui.container.Composite(new qx.ui.layout.HBox(6).set({ alignY: "middle" })).set({ allowShrinkX: false });
		this.__dot = new qx.ui.core.Widget().set({ appearance: "app-top-bar/dot", width: 8, height: 8, allowGrowX: false, allowGrowY: false });
		this.__asOfLabel = new qx.ui.basic.Label().set({ appearance: "app-top-bar/as-of", rich: false });
		this.__asOf.add(this.__dot);
		this.__asOf.add(this.__asOfLabel);
		this.add(this.__asOf);

		this.add(new qx.ui.core.Spacer(8), { flex: 1 });

		// search box
		this.__search = new qx.ui.container.Composite(new qx.ui.layout.HBox(6).set({ alignY: "middle" })).set({
			appearance: "app-top-bar/search", height: 32, allowGrowY: false, width: 320, maxWidth: 320, minWidth: 160, allowShrinkX: true
		});
		this.__search.add(this.__icon(Self.ICON_SEARCH, "app-top-bar/search-icon"));
		this.__input = new qx.ui.form.TextField().set({ appearance: "app-top-bar/search-input", liveUpdate: false });
		this.__input.getContentElement().setAttribute("aria-label", "Search");
		this.__input.getContentElement().setAttribute("spellcheck", "false");
		this.__input.addListener("keydown", this._onSearchKey, this);
		this.__input.addListener("focusin", function () { this.__search.addState("focused"); }, this);
		this.__input.addListener("focusout", this._onSearchBlur, this);
		this.__search.add(this.__input, { flex: 1 });
		this.add(this.__search, { flex: 1 });

		// the magnifier that opens the search box on a narrow bar
		this.__searchButton = this.__button("app-top-bar/icon-button", "Search", this._onSearchButton);
		this.__searchButton.setLayout(new qx.ui.layout.HBox().set({ alignX: "center", alignY: "middle" }));
		this.__searchButton.add(this.__icon(Self.ICON_SEARCH, "app-top-bar/icon"));
		this.__searchButton.setVisibility("excluded");
		this.add(this.__searchButton);

		// environment badge
		this.__env = new qx.ui.basic.Label().set({ appearance: "app-top-bar/env", rich: false, allowGrowY: false, alignY: "middle" });
		this.add(this.__env);

		// alerts bell with its count
		this.__bell = this.__button("app-top-bar/icon-button", "Alerts", this._onBellTap);
		this.__bell.setLayout(new qx.ui.layout.Canvas());
		this.__bell.add(this.__icon(Self.ICON_BELL, "app-top-bar/icon"), { left: 8, top: 8 });
		this.__badge = new qx.ui.basic.Label().set({ appearance: "app-top-bar/badge", rich: false, anonymous: true, textAlign: "center", minWidth: 16, height: 16 });
		this.__bell.add(this.__badge, { top: 1, right: 0 });
		this.add(this.__bell);

		// the signed-in user
		this.__user = this.__button("app-top-bar/user", "Account", this._onUserTap);
		this.__user.setLayout(new qx.ui.layout.HBox(8).set({ alignY: "middle" }));
		var avatar = new qx.ui.container.Composite(new qx.ui.layout.HBox().set({ alignX: "center", alignY: "middle" })).set({
			appearance: "app-top-bar/avatar", width: 30, height: 30, allowGrowX: false, allowGrowY: false, anonymous: true
		});
		this.__initials = new qx.ui.basic.Label().set({ appearance: "app-top-bar/initials", rich: false, anonymous: true });
		avatar.add(this.__initials);
		this.__user.add(avatar);
		this.__who = new qx.ui.container.Composite(new qx.ui.layout.VBox(0)).set({ anonymous: true });
		this.__name = new qx.ui.basic.Label().set({ appearance: "app-top-bar/name", rich: false, anonymous: true });
		this.__role = new qx.ui.basic.Label().set({ appearance: "app-top-bar/role", rich: false, anonymous: true });
		this.__who.add(this.__name);
		this.__who.add(this.__role);
		this.__user.add(this.__who);
		this.__user.add(this.__icon(Self.ICON_CHEVRON, "app-top-bar/chevron"));
		this.add(this.__user);

		this.addListener("resize", this._applyWidth, this);

		this._applyBreadcrumb();
		this._applySearchPlaceholder();
		this._applyEnvironment();
		this._applyAlertCount();
		this._applyUser();
		this._applyAsOf();
	},

	statics: {

		/**
		 * The magnifier, drawn with the current text colour.
		 */
		ICON_SEARCH: "<circle cx='7' cy='7' r='4.5'/><path d='M10.4 10.4 L14 14'/>",

		/**
		 * The bell.
		 */
		ICON_BELL: "<path d='M4 11.5V7.2a4 4 0 0 1 8 0v4.3l1.2 1.3H2.8z'/><path d='M6.6 14.2a1.5 1.5 0 0 0 2.8 0'/>",

		/**
		 * The chevron after the user.
		 */
		ICON_CHEVRON: "<path d='M4.5 6.5 L8 10 L11.5 6.5'/>",

		/**
		 * Below this width the name and role hide.
		 */
		WIDE: 900,

		/**
		 * Below this width "Data as of" hides and the search box collapses to a button.
		 */
		NARROW: 600
	},

	events: {

		/**
		 * The user pressed Enter in the search box: { text }.
		 */
		searchSubmitted: "qx.event.type.Data",

		/**
		 * The user clicked the alerts bell.
		 */
		alertsClicked: "qx.event.type.Event",

		/**
		 * The user clicked the avatar, the name or the chevron.
		 */
		userMenuClicked: "qx.event.type.Event",

		/**
		 * The user clicked an earlier crumb: { index }.
		 */
		crumbClicked: "qx.event.type.Data"
	},

	properties: {

		/**
		 * The theme appearance of the root widget.
		 */
		appearance: { init: "app-top-bar", refine: true },

		/**
		 * The path to the current page; the last crumb is the page itself.
		 */
		breadcrumb: { init: null, nullable: true, check: "Array", apply: "_applyBreadcrumb" },

		/**
		 * The hint in the empty search box.
		 */
		searchPlaceholder: { init: "Search plants, customers, contracts…", check: "String", apply: "_applySearchPlaceholder" },

		/**
		 * The environment badge: "Uat" amber, "Dev" blue, "Prod" red outline, "None" hidden.
		 */
		environment: { init: "Uat", check: ["None", "Dev", "Uat", "Prod"], apply: "_applyEnvironment" },

		/**
		 * The count on the bell; hidden at 0, 9+ above 9.
		 */
		alertCount: { init: 3, check: "Integer", apply: "_applyAlertCount" },

		/**
		 * The signed-in user's name.
		 */
		userName: { init: "Tim Larson", check: "String", apply: "_applyUser" },

		/**
		 * The signed-in user's role.
		 */
		userRole: { init: "Commodity Marketing", check: "String", apply: "_applyUser" },

		/**
		 * The initials in the avatar.
		 */
		initials: { init: "TL", check: "String", apply: "_applyUser" },

		/**
		 * The time the data is as of; empty hides "Data as of".
		 */
		asOfText: { init: "7:36 AM CT", check: "String", apply: "_applyAsOf" },

		/**
		 * Whether the data is stale: an amber dot and "· stale".
		 */
		asOfStale: { init: false, check: "Boolean", apply: "_applyAsOf" }
	},

	members: {

		__crumbs: null,
		__asOf: null,
		__dot: null,
		__asOfLabel: null,
		__search: null,
		__input: null,
		__searchButton: null,
		__env: null,
		__bell: null,
		__badge: null,
		__user: null,
		__initials: null,
		__who: null,
		__name: null,
		__role: null,

		/**
		 * True while the search box is open on a narrow bar.
		 */
		__searching: false,

		/**
		 * True while the bar is narrower than NARROW.
		 */
		__narrow: false,

		/**
		 * A 16 px line icon that takes the text colour of its appearance.
		 */
		__icon: function (paths, appearance) {

			return new qx.ui.embed.Html(
				"<svg viewBox='0 0 16 16' width='16' height='16' style='display:block;fill:none;stroke:currentColor;stroke-width:1.6;stroke-linecap:round;stroke-linejoin:round'>" + paths + "</svg>"
			).set({ appearance: appearance, width: 16, height: 16, allowGrowX: false, allowGrowY: false, anonymous: true });
		},

		/**
		 * A focusable box that acts as a button: tap, Enter or Space runs the handler.
		 */
		__button: function (appearance, label, handler) {

			var button = new qx.ui.container.Composite().set({ appearance: appearance, focusable: true, keepFocus: false, cursor: "pointer", allowGrowY: false });
			var el = button.getContentElement();
			el.setAttribute("role", "button");
			el.setAttribute("aria-label", label);
			el.setAttribute("title", label);
			button.addListener("tap", handler, this);
			button.addListener("keydown", function (e) {
				var key = e.getKeyIdentifier();
				if (key === "Enter" || key === "Space") {
					e.stop();
					handler.call(this, e);
				}
			}, this);
			button.addListener("pointerover", function () { button.addState("hovered"); });
			button.addListener("pointerout", function () { button.removeState("hovered"); });
			button.addListener("focusin", function () { button.addState("focused"); });
			button.addListener("focusout", function () { button.removeState("focused"); });
			return button;
		},

		/**
		 * Rebuilds the crumbs: earlier ones are links, the last one is the current page.
		 */
		_applyBreadcrumb: function () {

			if (!this.__crumbs)
				return;

			var old = this.__crumbs.removeAll();
			for (var i = 0; i < old.length; i++)
				old[i].dispose();

			var crumbs = this.getBreadcrumb() || [];
			for (var i = 0; i < crumbs.length; i++) {

				if (i > 0)
					this.__crumbs.add(new qx.ui.basic.Label("›").set({ appearance: "app-top-bar/separator", rich: false, anonymous: true }));

				var last = i === crumbs.length - 1;
				var crumb = new qx.ui.basic.Label(String(crumbs[i] == null ? "" : crumbs[i])).set({ appearance: "app-top-bar/crumb", rich: false });
				if (last) {
					crumb.addState("selected");
					crumb.getContentElement().setAttribute("aria-current", "page");
				}
				else {
					crumb.set({ cursor: "pointer", focusable: true });
					crumb.setUserData("index", i);
					crumb.getContentElement().setAttribute("role", "link");
					crumb.addListener("tap", this._onCrumbTap, this);
					crumb.addListener("keydown", function (e) {
						if (e.getKeyIdentifier() === "Enter") {
							e.stop();
							this._onCrumbTap(e);
						}
					}, this);
					crumb.addListener("pointerover", function (e) { e.getCurrentTarget().addState("hovered"); });
					crumb.addListener("pointerout", function (e) { e.getCurrentTarget().removeState("hovered"); });
				}
				this.__crumbs.add(crumb);
			}
		},

		/**
		 * Shows the hint in the empty search box.
		 */
		_applySearchPlaceholder: function () {

			if (this.__input)
				this.__input.setPlaceholder(this.getSearchPlaceholder() || "");
		},

		/**
		 * Shows the environment badge in the colours of its state.
		 */
		_applyEnvironment: function () {

			if (!this.__env)
				return;

			var env = this.getEnvironment() || "None";
			var states = ["dev", "uat", "prod"];
			for (var i = 0; i < states.length; i++)
				this.__env.removeState(states[i]);

			if (env === "None") {
				this.__env.setVisibility("excluded");
				return;
			}

			this.__env.addState(env.toLowerCase());
			this.__env.setValue(env.toUpperCase());
			this.__env.setVisibility("visible");
		},

		/**
		 * Shows the count on the bell: hidden at 0, 9+ above 9.
		 */
		_applyAlertCount: function () {

			if (!this.__badge)
				return;

			var n = Math.max(0, this.getAlertCount() | 0);
			this.__badge.setValue(n > 9 ? "9+" : String(n));
			this.__badge.setVisibility(n ? "visible" : "excluded");

			var label = n ? n + (n === 1 ? " alert" : " alerts") : "Alerts";
			this.__bell.getContentElement().setAttribute("aria-label", label);
			this.__bell.getContentElement().setAttribute("title", label);
		},

		/**
		 * Shows the initials, the name and the role.
		 */
		_applyUser: function () {

			if (!this.__role)
				return;

			this.__initials.setValue(this.getInitials() || "");
			this.__name.setValue(this.getUserName() || "");
			this.__role.setValue(this.getUserRole() || "");
			this.__role.setVisibility(this.getUserRole() ? "visible" : "excluded");
			this.__user.getContentElement().setAttribute("aria-label", "Account: " + (this.getUserName() || ""));
		},

		/**
		 * Shows "Data as of …" with a live or stale dot; hides it when there is no time.
		 */
		_applyAsOf: function () {

			if (!this.__asOfLabel)
				return;

			var text = this.getAsOfText() || "";
			var stale = this.getAsOfStale();
			this.__asOfLabel.setValue("Data as of " + text + (stale ? " · stale" : " · live"));
			if (stale)
				this.__dot.addState("stale");
			else
				this.__dot.removeState("stale");

			this._applyWidth();
		},

		/**
		 * Shows or hides the parts that depend on the bar's width.
		 */
		_applyWidth: function () {

			if (!this.__who)
				return;

			var Self = gridkrafter.wisejdemo.AppTopBar;
			var bounds = this.getBounds();
			var width = bounds ? bounds.width : 1100;

			this.__narrow = width < Self.NARROW;
			if (!this.__narrow)
				this.__searching = false;

			this.__who.setVisibility(width < Self.WIDE ? "excluded" : "visible");
			this.__asOf.setVisibility(!this.__narrow && this.getAsOfText() ? "visible" : "excluded");

			var showSearch = !this.__narrow || this.__searching;
			this.__search.setVisibility(showSearch ? "visible" : "excluded");
			this.__searchButton.setVisibility(showSearch ? "excluded" : "visible");
			this.__search.setMaxWidth(this.__narrow ? null : 320);
			this.__crumbs.setVisibility(this.__narrow && this.__searching ? "excluded" : "visible");
		},

		/**
		 * Enter submits the search, Esc clears it (and closes it on a narrow bar).
		 */
		_onSearchKey: function (e) {

			var key = e.getKeyIdentifier();
			if (key === "Enter") {
				e.stop();
				if (this.isEnabled())
					this.fireDataEvent("searchSubmitted", { text: this.__input.getValue() || "" });
			}
			else if (key === "Escape") {
				e.stop();
				this.__input.setValue("");
				if (this.__narrow) {
					this.__searching = false;
					this._applyWidth();
				}
			}
		},

		/**
		 * The search box lost the focus: on a narrow bar an empty box closes again.
		 */
		_onSearchBlur: function () {

			this.__search.removeState("focused");
			if (this.__narrow && this.__searching && !this.__input.getValue()) {
				this.__searching = false;
				this._applyWidth();
			}
		},

		/**
		 * The magnifier was pressed on a narrow bar: open the search box and focus it.
		 */
		_onSearchButton: function () {

			this.__searching = true;
			this._applyWidth();
			var input = this.__input;
			qx.event.Timer.once(function () { if (!input.isDisposed()) input.focus(); }, this, 0);
		},

		/**
		 * The bell was pressed.
		 */
		_onBellTap: function () {

			if (this.isEnabled())
				this.fireEvent("alertsClicked");
		},

		/**
		 * The user was pressed.
		 */
		_onUserTap: function () {

			if (this.isEnabled())
				this.fireEvent("userMenuClicked");
		},

		/**
		 * An earlier crumb was pressed: report its position.
		 */
		_onCrumbTap: function (e) {

			if (this.isEnabled())
				this.fireDataEvent("crumbClicked", { index: e.getCurrentTarget().getUserData("index") });
		}
	},

	/**
	 * The child widgets are disposed by the container; only the references are dropped.
	 */
	destruct: function () {

		this.__crumbs = this.__asOf = this.__dot = this.__asOfLabel = this.__search = this.__input = null;
		this.__searchButton = this.__env = this.__bell = this.__badge = this.__user = null;
		this.__initials = this.__who = this.__name = this.__role = null;
	}
});
