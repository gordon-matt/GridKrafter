namespace GridKrafter.WisejDemo
{
    partial class FreightLaneMatrixTestForm
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

        #region Wisej.NET Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.labelTitle = new Wisej.Web.Label();
            this.freightLaneMatrix1 = new GridKrafter.WisejDemo.FreightLaneMatrix();
            this.labelEvents = new Wisej.Web.Label();
            this.SuspendLayout();
            // 
            // labelTitle
            // 
            this.labelTitle.AutoSize = true;
            this.labelTitle.Location = new System.Drawing.Point(16, 16);
            this.labelTitle.Name = "labelTitle";
            this.labelTitle.Size = new System.Drawing.Size(200, 18);
            this.labelTitle.TabIndex = 0;
            this.labelTitle.Text = "FreightLaneMatrix";
            // 
            // freightLaneMatrix1
            // 
            this.freightLaneMatrix1.Location = new System.Drawing.Point(16, 48);
            this.freightLaneMatrix1.Name = "freightLaneMatrix1";
            this.freightLaneMatrix1.Size = new System.Drawing.Size(640, 420);
            this.freightLaneMatrix1.TabIndex = 1;
            // 
            // labelEvents
            // 
            this.labelEvents.Location = new System.Drawing.Point(16, 480);
            this.labelEvents.Name = "labelEvents";
            this.labelEvents.Size = new System.Drawing.Size(640, 22);
            this.labelEvents.TabIndex = 2;
            this.labelEvents.Text = "What the component fires is shown here.";
            // 
            // FreightLaneMatrixTestForm
            // 
            this.ClientSize = new System.Drawing.Size(672, 518);
            this.Controls.Add(this.labelEvents);
            this.Controls.Add(this.freightLaneMatrix1);
            this.Controls.Add(this.labelTitle);
            this.Name = "FreightLaneMatrixTestForm";
            this.Text = "FreightLaneMatrix test";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Wisej.Web.Label labelTitle;
        private GridKrafter.WisejDemo.FreightLaneMatrix freightLaneMatrix1;
        private Wisej.Web.Label labelEvents;
    }
}
