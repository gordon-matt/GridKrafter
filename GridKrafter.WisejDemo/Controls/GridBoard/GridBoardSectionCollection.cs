using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace GridKrafter.WisejDemo.Controls
{
	/// <summary>
	/// The items of a component: each belongs to one collection at a time, and a change to the
	/// list or to an item raises <see cref="Changed"/>, which the component draws itself again on.
	/// </summary>
	public class GridBoardSectionCollection : Collection<GridBoardSection>
	{
		/// <summary>Occurs when an item is added, removed or replaced, or a property of one changes.</summary>
		public event EventHandler Changed;

		/// <summary>Adds the items in order: what a designer writes for several at once.</summary>
		public void AddRange(params GridBoardSection[] items)
		{
			if (items == null) throw new ArgumentNullException(nameof(items));
			foreach (var item in items) Add(item);
		}

		/// <summary>The item of that name; null when there is none.</summary>
		public GridBoardSection this[string name]
		{
			get
			{
				foreach (var item in this)
					if (string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase)) return item;
				return null;
			}
		}

		protected override void InsertItem(int index, GridBoardSection item)
		{
			Validate(item);
			base.InsertItem(index, item);
			Attach(item);
			OnChanged();
		}

		protected override void SetItem(int index, GridBoardSection item)
		{
			if (ReferenceEquals(this[index], item)) return;
			Validate(item);
			var old = this[index];
			base.SetItem(index, item);
			Detach(old);
			Attach(item);
			OnChanged();
		}

		protected override void RemoveItem(int index)
		{
			var old = this[index];
			base.RemoveItem(index);
			Detach(old);
			OnChanged();
		}

		protected override void ClearItems()
		{
			foreach (var item in this) Detach(item);
			base.ClearItems();
			OnChanged();
		}

		/// <summary>Raises <see cref="Changed"/>.</summary>
		protected virtual void OnChanged()
		{
			this.Changed?.Invoke(this, EventArgs.Empty);
		}

		private static void Validate(GridBoardSection item)
		{
			if (item == null) throw new ArgumentNullException(nameof(item));
			if (item.Owner != null) throw new ArgumentException("The item already belongs to a collection.", nameof(item));
		}

		private void Attach(GridBoardSection item)
		{
			item.Owner = this;
			item.PropertyChanged += ItemChanged;
		}

		private void Detach(GridBoardSection item)
		{
			item.PropertyChanged -= ItemChanged;
			item.Owner = null;
		}

		private void ItemChanged(object sender, PropertyChangedEventArgs e) => OnChanged();
	}
}
