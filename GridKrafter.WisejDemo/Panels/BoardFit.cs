using System;
using System.Linq;
using GridKrafter.WisejDemo.Controls;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Panels
{
	/// <summary>
	/// A page whose content has a height of its own, so the page scrolls instead of the content: the boards,
	/// which grow to show every widget of the open tab.
	/// </summary>
	public interface IFitsPage
	{
		/// <summary>The height the content needs, in pixels.</summary>
		int ContentHeight { get; }

		/// <summary>The height of the page area people see; a widget in full screen fills it.</summary>
		int ViewportHeight { get; set; }

		/// <summary>Raised when <see cref="ContentHeight"/> changes.</summary>
		event EventHandler ContentHeightChanged;
	}

	/// <summary>
	/// A page that shows a board: the page header offers the board's Edit / Done, Add widget, Add section and
	/// Reset layout on the title row, so the board's own row of them is hidden.
	/// </summary>
	public interface IBoardPage
	{
		/// <summary>The board the page shows.</summary>
		GridBoard Board { get; }
	}

	/// <summary>
	/// Keeps a board as tall as the widgets of its open tab: its toolbar and tabs, the lowest widget and a little
	/// space under it. The board then never scrolls by itself; the page does. A widget in full screen fills the
	/// part of the page people see instead.
	/// </summary>
	internal sealed class BoardFit
	{
		// under the lowest widget: the board's own inner padding and some air
		private const int BOTTOM = 32;

		// the least a widget in full screen gets, on a very short window
		private const int MIN_FULLSCREEN = 320;

		private readonly Control host;
		private readonly GridBoard board;
		private readonly Func<int> above;
		private int viewport;
		private int lastContent = -1;

		/// <param name="host">The panel the board is in; its padding counts.</param>
		/// <param name="board">The board to fit; it is docked to the top.</param>
		/// <param name="above">The height of what is docked above the board (a filter bar), or 0.</param>
		public BoardFit(Control host, GridBoard board, Func<int> above)
		{
			this.host = host;
			this.board = board;
			this.above = above ?? (() => 0);

			board.Dock = DockStyle.Top;
			board.Layout += (s, e) => Update();
			board.Changed += (s, e) => Update();
			board.ControlAdded += (s, e) => Track(e.Control);
			board.ControlRemoved += (s, e) => Update();
			foreach (var widget in board.Widgets)
				Track(widget);

			host.Resize += (s, e) => Update();
		}

		/// <summary>Raised when the height the host needs changes.</summary>
		public event EventHandler Changed;

		/// <summary>The height the host needs: its padding, what is above the board, and the board.</summary>
		public int ContentHeight => this.host.Padding.Top + this.above() + this.board.Height + this.host.Padding.Bottom;

		/// <summary>The height of the page area people see.</summary>
		public int Viewport
		{
			get => this.viewport;
			set
			{
				if (this.viewport == value)
					return;
				this.viewport = value;
				Update();
			}
		}

		// a widget moving, growing or collapsing changes how low the tab reaches
		private void Track(Control control)
		{
			if (!(control is GridWidget))
				return;

			control.Move += (s, e) => Update();
			control.Resize += (s, e) => Update();
			Update();
		}

		/// <summary>Sizes the board for its open tab, and says so when the host's height changes.</summary>
		public void Update()
		{
			if (this.board.IsDisposed)
				return;

			int height = BoardHeight();
			if (height != this.board.Height)
				this.board.Height = height;

			int content = ContentHeight;
			if (content != this.lastContent)
			{
				this.lastContent = content;
				Changed?.Invoke(this.host, EventArgs.Empty);
			}
		}

		private int BoardHeight()
		{
			int toolbar = this.board.ShowToolbar ? this.board.EffectiveToolbarHeight : 0;

			// full screen: the widget fills what people see, under the page's own header when it is in view
			if (this.board.FullscreenWidget != null)
				return Math.Max(toolbar + MIN_FULLSCREEN, this.viewport - this.host.Padding.Vertical - this.above());

			string section = this.board.CurrentSectionId();
			int bottom = this.board.Widgets
				.Where(w => this.board.SectionOf(w) == section)
				.Select(w => w.Bottom)
				.DefaultIfEmpty(0)
				.Max();

			return toolbar + bottom + BOTTOM;
		}
	}
}
