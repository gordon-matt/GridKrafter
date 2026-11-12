using System;
using System.ComponentModel;
using System.Drawing;

namespace GridKrafter.WisejDemo.Controls
{
	/// <summary>
	/// One item of <see cref="GridBoard.Sections"/>: The board's sections, shown as tabs.
	/// </summary>
	/// <remarks>
	/// The designer edits it in the collection editor and writes it as a field of the form;
	/// a change of any property tells its collection, which tells the component.
	/// </remarks>
	[TypeConverter(typeof(ExpandableObjectConverter)), DefaultProperty(nameof(Id))]
	public class GridBoardSection : INotifyPropertyChanged
	{
		/// <summary>Occurs when a property of the item changes.</summary>
		public event PropertyChangedEventHandler PropertyChanged;

		private string name = "";
		private string id = "section1";
		private string title = "Section";
		private string icon = "home";

		/// <summary>Initializes an item with its defaults: the designer and code set the rest.</summary>
		public GridBoardSection()
		{
		}

		/// <summary>The collection the item is in; null when it is in none.</summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public GridBoardSectionCollection Owner { get; internal set; }

		/// <summary>Gets or sets the item's name: the designer gives each item it adds one; code finds an item by it.</summary>
		[Category("Design"), DefaultValue(""), NotifyParentProperty(true)]
		public string Name
		{
			get => this.name;
			set
			{
				if (value == null) value = "";
				if (this.name == value) return;
				this.name = value;
				OnPropertyChanged(nameof(Name));
			}
		}

		/// <summary>Gets or sets the item's Id.</summary>
		[Category("Data"), DefaultValue("section1"), NotifyParentProperty(true), Localizable(true)]
		public string Id
		{
			get => this.id;
			set
			{
				if (value == null) value = "";
				if (this.id == value) return;
				this.id = value;
				OnPropertyChanged(nameof(Id));
			}
		}

		/// <summary>Gets or sets the item's Title.</summary>
		[Category("Data"), DefaultValue("Section"), NotifyParentProperty(true), Localizable(true)]
		public string Title
		{
			get => this.title;
			set
			{
				if (value == null) value = "";
				if (this.title == value) return;
				this.title = value;
				OnPropertyChanged(nameof(Title));
			}
		}

		/// <summary>Gets or sets the item's Icon.</summary>
		[Category("Data"), DefaultValue("home"), NotifyParentProperty(true), Localizable(true)]
		public string Icon
		{
			get => this.icon;
			set
			{
				if (value == null) value = "";
				if (this.icon == value) return;
				this.icon = value;
				OnPropertyChanged(nameof(Icon));
			}
		}

		/// <summary>Raises <see cref="PropertyChanged"/>.</summary>
		protected virtual void OnPropertyChanged(string property)
		{
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
		}

		/// <summary>What the collection editor lists the item as.</summary>
		public override string ToString() => this.id.Length > 0 ? this.id : this.name.Length > 0 ? this.name : nameof(GridBoardSection);
	}
}
