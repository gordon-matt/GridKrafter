namespace GridKrafter.WisejDemo.Panels
{
	partial class PlantFilterBar
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Wisej Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.region = new Wisej.Web.ComboBox();
			this.plant = new Wisej.Web.ComboBox();
			this.mot = new Wisej.Web.ComboBox();
			this.ownership = new Wisej.Web.SegmentedButton();
			this.SuspendLayout();
			//
			// region
			//
			this.region.DropDownStyle = Wisej.Web.ComboBoxStyle.DropDownList;
			this.region.Label.Position = Wisej.Web.LabelPosition.Left;
			this.region.LabelText = "Region";
			this.region.Location = new System.Drawing.Point(0, 0);
			this.region.Name = "region";
			this.region.Size = new System.Drawing.Size(220, 30);
			this.region.TabIndex = 0;
			this.region.SelectedIndexChanged += new System.EventHandler(this.region_SelectedIndexChanged);
			//
			// plant
			//
			this.plant.DropDownStyle = Wisej.Web.ComboBoxStyle.DropDownList;
			this.plant.Label.Position = Wisej.Web.LabelPosition.Left;
			this.plant.LabelText = "Plant";
			this.plant.Location = new System.Drawing.Point(232, 0);
			this.plant.Name = "plant";
			this.plant.Size = new System.Drawing.Size(270, 30);
			this.plant.TabIndex = 1;
			this.plant.SelectedIndexChanged += new System.EventHandler(this.plant_SelectedIndexChanged);
			//
			// mot
			//
			this.mot.DropDownStyle = Wisej.Web.ComboBoxStyle.DropDownList;
			this.mot.Label.Position = Wisej.Web.LabelPosition.Left;
			this.mot.LabelText = "MOT";
			this.mot.Location = new System.Drawing.Point(514, 0);
			this.mot.Name = "mot";
			this.mot.Size = new System.Drawing.Size(180, 30);
			this.mot.TabIndex = 2;
			this.mot.SelectedIndexChanged += new System.EventHandler(this.mot_SelectedIndexChanged);
			//
			// ownership
			//
			this.ownership.Location = new System.Drawing.Point(706, 0);
			this.ownership.Name = "ownership";
			this.ownership.Size = new System.Drawing.Size(260, 30);
			this.ownership.Stretch = true;
			this.ownership.TabIndex = 3;
			this.ownership.SelectedIndexChanged += new System.EventHandler(this.ownership_SelectedIndexChanged);
			//
			// PlantFilterBar
			//
			this.Controls.Add(this.ownership);
			this.Controls.Add(this.mot);
			this.Controls.Add(this.plant);
			this.Controls.Add(this.region);
			this.Name = "PlantFilterBar";
			this.Size = new System.Drawing.Size(1080, 42);
			this.Resize += new System.EventHandler(this.PlantFilterBar_Resize);
			this.ResumeLayout(false);

		}

		#endregion

		private Wisej.Web.ComboBox region;
		private Wisej.Web.ComboBox plant;
		private Wisej.Web.ComboBox mot;
		private Wisej.Web.SegmentedButton ownership;
	}
}
