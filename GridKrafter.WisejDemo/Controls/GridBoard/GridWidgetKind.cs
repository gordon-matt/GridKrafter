using System;
using System.ComponentModel;
using System.Drawing;

namespace GridKrafter.WisejDemo.Controls
{
	/// <summary>
	/// One item of <see cref="GridBoard.Catalog"/>: The kinds of widget Add widget offers.
	/// </summary>
	/// <remarks>
	/// The designer edits it in the collection editor and writes it as a field of the form;
	/// a change of any property tells its collection, which tells the component.
	/// </remarks>
	[TypeConverter(typeof(ExpandableObjectConverter)), DefaultProperty(nameof(Kind))]
	public class GridWidgetKind : INotifyPropertyChanged
	{
		/// <summary>Occurs when a property of the item changes.</summary>
		public event PropertyChangedEventHandler PropertyChanged;

		private string name = "";
		private string kind = "kind";
		private string text = "";
		private string description = "";
		private string icon = "chart";
		private string color = "blue";
		private int w = 4;
		private int h = 3;
		private int minW = 1;
		private int minH = 1;
		private int maxW = 0;
		private int maxH = 0;
		private bool autoHeight = false;
		private int refresh = 0;

		/// <summary>Initializes an item with its defaults: the designer and code set the rest.</summary>
		public GridWidgetKind()
		{
		}

		/// <summary>The collection the item is in; null when it is in none.</summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public GridWidgetKindCollection Owner { get; internal set; }

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

		/// <summary>Gets or sets the item's Kind.</summary>
		[Category("Data"), DefaultValue("kind"), NotifyParentProperty(true), Localizable(true)]
		public string Kind
		{
			get => this.kind;
			set
			{
				if (value == null) value = "";
				if (this.kind == value) return;
				this.kind = value;
				OnPropertyChanged(nameof(Kind));
			}
		}

		/// <summary>Gets or sets the name shown in the Add widget catalog; empty shows <see cref="Kind"/>.</summary>
		/// <remarks><see cref="Name"/> is the designer's name of the item, so the shown name is this one.</remarks>
		[Category("Data"), DefaultValue(""), NotifyParentProperty(true), Localizable(true)]
		public string Text
		{
			get => this.text;
			set
			{
				if (value == null) value = "";
				if (this.text == value) return;
				this.text = value;
				OnPropertyChanged(nameof(Text));
			}
		}

		/// <summary>Gets the name the catalog shows: <see cref="Text"/>, or <see cref="Kind"/> when it is empty.</summary>
		[Browsable(false)]
		public string DisplayName => this.text.Length > 0 ? this.text : this.kind;

		/// <summary>Gets or sets the item's Description.</summary>
		[Category("Data"), DefaultValue(""), NotifyParentProperty(true), Localizable(true)]
		public string Description
		{
			get => this.description;
			set
			{
				if (value == null) value = "";
				if (this.description == value) return;
				this.description = value;
				OnPropertyChanged(nameof(Description));
			}
		}

		/// <summary>Gets or sets the item's Icon.</summary>
		[Category("Data"), DefaultValue("chart"), NotifyParentProperty(true), Localizable(true)]
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

		/// <summary>Gets or sets the item's Color.</summary>
		[Category("Data"), DefaultValue("blue"), NotifyParentProperty(true), Localizable(true)]
		public string Color
		{
			get => this.color;
			set
			{
				if (value == null) value = "";
				if (this.color == value) return;
				this.color = value;
				OnPropertyChanged(nameof(Color));
			}
		}

		/// <summary>Gets or sets the item's W.</summary>
		[Category("Data"), DefaultValue(4), NotifyParentProperty(true)]
		public int W
		{
			get => this.w;
			set
			{
				if (this.w == value) return;
				this.w = value;
				OnPropertyChanged(nameof(W));
			}
		}

		/// <summary>Gets or sets the item's H.</summary>
		[Category("Data"), DefaultValue(3), NotifyParentProperty(true)]
		public int H
		{
			get => this.h;
			set
			{
				if (this.h == value) return;
				this.h = value;
				OnPropertyChanged(nameof(H));
			}
		}

		/// <summary>Gets or sets the item's MinW.</summary>
		[Category("Data"), DefaultValue(1), NotifyParentProperty(true)]
		public int MinW
		{
			get => this.minW;
			set
			{
				if (this.minW == value) return;
				this.minW = value;
				OnPropertyChanged(nameof(MinW));
			}
		}

		/// <summary>Gets or sets the item's MinH.</summary>
		[Category("Data"), DefaultValue(1), NotifyParentProperty(true)]
		public int MinH
		{
			get => this.minH;
			set
			{
				if (this.minH == value) return;
				this.minH = value;
				OnPropertyChanged(nameof(MinH));
			}
		}

		/// <summary>Gets or sets the item's MaxW.</summary>
		[Category("Data"), DefaultValue(0), NotifyParentProperty(true)]
		public int MaxW
		{
			get => this.maxW;
			set
			{
				if (this.maxW == value) return;
				this.maxW = value;
				OnPropertyChanged(nameof(MaxW));
			}
		}

		/// <summary>Gets or sets the item's MaxH.</summary>
		[Category("Data"), DefaultValue(0), NotifyParentProperty(true)]
		public int MaxH
		{
			get => this.maxH;
			set
			{
				if (this.maxH == value) return;
				this.maxH = value;
				OnPropertyChanged(nameof(MaxH));
			}
		}

		/// <summary>Gets or sets the item's AutoHeight.</summary>
		[Category("Data"), DefaultValue(false), NotifyParentProperty(true)]
		public bool AutoHeight
		{
			get => this.autoHeight;
			set
			{
				if (this.autoHeight == value) return;
				this.autoHeight = value;
				OnPropertyChanged(nameof(AutoHeight));
			}
		}

		/// <summary>Gets or sets the item's Refresh.</summary>
		[Category("Data"), DefaultValue(0), NotifyParentProperty(true)]
		public int Refresh
		{
			get => this.refresh;
			set
			{
				if (this.refresh == value) return;
				this.refresh = value;
				OnPropertyChanged(nameof(Refresh));
			}
		}

		/// <summary>Raises <see cref="PropertyChanged"/>.</summary>
		protected virtual void OnPropertyChanged(string property)
		{
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
		}

		/// <summary>What the collection editor lists the item as.</summary>
		public override string ToString() => this.kind.Length > 0 ? this.kind : this.name.Length > 0 ? this.name : nameof(GridWidgetKind);
	}
}
