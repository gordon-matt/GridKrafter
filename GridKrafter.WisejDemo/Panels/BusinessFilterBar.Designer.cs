namespace GridKrafter.WisejDemo.Panels
{
	partial class BusinessFilterBar
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
			this.commodity = new Wisej.Web.ComboBox();
			this.period = new Wisej.Web.ComboBox();
			this.pick = new Wisej.Web.ComboBox();
			this.SuspendLayout();
			//
			// commodity
			//
			this.commodity.DropDownStyle = Wisej.Web.ComboBoxStyle.DropDownList;
			this.commodity.Label.Position = Wisej.Web.LabelPosition.Left;
			this.commodity.LabelText = "Commodity";
			this.commodity.Location = new System.Drawing.Point(0, 0);
			this.commodity.Name = "commodity";
			this.commodity.Size = new System.Drawing.Size(270, 30);
			this.commodity.TabIndex = 0;
			this.commodity.SelectedIndexChanged += new System.EventHandler(this.commodity_SelectedIndexChanged);
			//
			// period
			//
			this.period.DropDownStyle = Wisej.Web.ComboBoxStyle.DropDownList;
			this.period.Label.Position = Wisej.Web.LabelPosition.Left;
			this.period.LabelText = "Period";
			this.period.Location = new System.Drawing.Point(282, 0);
			this.period.Name = "period";
			this.period.Size = new System.Drawing.Size(210, 30);
			this.period.TabIndex = 1;
			this.period.SelectedIndexChanged += new System.EventHandler(this.period_SelectedIndexChanged);
			//
			// pick
			//
			this.pick.DropDownStyle = Wisej.Web.ComboBoxStyle.DropDownList;
			this.pick.Label.Position = Wisej.Web.LabelPosition.Left;
			this.pick.LabelText = "Customer";
			this.pick.Location = new System.Drawing.Point(504, 0);
			this.pick.Name = "pick";
			this.pick.Size = new System.Drawing.Size(360, 30);
			this.pick.TabIndex = 2;
			this.pick.SelectedIndexChanged += new System.EventHandler(this.pick_SelectedIndexChanged);
			//
			// BusinessFilterBar
			//
			this.Controls.Add(this.pick);
			this.Controls.Add(this.period);
			this.Controls.Add(this.commodity);
			this.Name = "BusinessFilterBar";
			this.Size = new System.Drawing.Size(1080, 42);
			this.Resize += new System.EventHandler(this.BusinessFilterBar_Resize);
			this.ResumeLayout(false);

		}

		#endregion

		private Wisej.Web.ComboBox commodity;
		private Wisej.Web.ComboBox period;
		private Wisej.Web.ComboBox pick;
	}
}
