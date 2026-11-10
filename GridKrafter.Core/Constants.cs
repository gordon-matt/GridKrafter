namespace GridKrafter;

/// <summary>Names shared by the components, the browser and hosts.</summary>
public static class Constants
{
    /// <summary>The section a board gets when it doesn't define any.</summary>
    public const string DefaultSection = "main";

    /// <summary>Prefix for browser storage keys, so a dashboard's saved state doesn't collide with the host's own.</summary>
    public const string StoragePrefix = "gridkrafter:";

    /// <summary>Values for a dashboard's or widget's theme.</summary>
    public static class Themes
    {
        /// <summary>Light colours.</summary>
        public const string Light = "light";

        /// <summary>Dark colours.</summary>
        public const string Dark = "dark";

        /// <summary>Follows the operating system's light or dark setting.</summary>
        public const string Auto = "auto";

        /// <summary>Takes its colours, fonts and corners from Bootstrap's CSS variables, including <c>data-bs-theme</c>.</summary>
        public const string Bootstrap = "bootstrap";
    }

    /// <summary>The DOM events the custom element raises. Each bubbles and carries its data as <c>event.detail</c>.</summary>
    public static class DomEvents
    {
        /// <summary>The board changed: <c>{ kind, itemId, sectionId, layout, board }</c>.</summary>
        public const string Changed = "gk-changed";

        /// <summary>The breakpoint changed: <c>{ id, columns, minWidth }</c>.</summary>
        public const string Layout = "gk-layout";

        /// <summary>The set of empty widget bodies changed, so a host can render into them: <c>{ mounts: [{ id, kind, section }] }</c>.</summary>
        public const string Mounts = "gk-mounts";

        /// <summary>A widget without its own content was refreshed: <c>{ id, kind }</c>.</summary>
        public const string Refresh = "gk-refresh";

        /// <summary>Edit mode was switched on or off from the toolbar: <c>{ editing }</c>.</summary>
        public const string EditMode = "gk-edit-mode";

        /// <summary>The dashboard has measured itself and restored any saved state: <c>{ layout, board }</c>.</summary>
        public const string Ready = "gk-ready";

        /// <summary>
        /// Sent the other way: content that isn't a Blazor component dispatches it from inside a widget's
        /// body, bubbling, to save one of the widget's settings with the board: <c>{ key, value }</c>.
        /// A missing or <see langword="null"/> value removes the setting.
        /// </summary>
        public const string Option = "gk-option";
    }

    /// <summary>Sizes used when nothing else says otherwise.</summary>
    public static class Defaults
    {
        /// <summary>The height of a grid row, in CSS pixels.</summary>
        public const int RowHeight = 60;

        /// <summary>The space between widgets, in CSS pixels.</summary>
        public const int Gap = 12;

        /// <summary>The width of a new widget, in columns of the largest layout.</summary>
        public const int Width = 4;

        /// <summary>The height of a new widget, in rows.</summary>
        public const int Height = 3;
    }
}
