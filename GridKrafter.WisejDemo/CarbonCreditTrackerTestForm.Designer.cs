namespace GridKrafter.WisejDemo
{
    partial class CarbonCreditTrackerTestForm
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
            this.carbonCreditTracker1 = new GridKrafter.WisejDemo.CarbonCreditTracker();
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
            this.labelTitle.Text = "CarbonCreditTracker";
            // 
            // carbonCreditTracker1
            // 
            this.carbonCreditTracker1.Location = new System.Drawing.Point(16, 48);
            this.carbonCreditTracker1.Name = "carbonCreditTracker1";
            this.carbonCreditTracker1.Size = new System.Drawing.Size(680, 340);
            this.carbonCreditTracker1.TabIndex = 1;
            // 
            // labelEvents
            // 
            this.labelEvents.Location = new System.Drawing.Point(16, 400);
            this.labelEvents.Name = "labelEvents";
            this.labelEvents.Size = new System.Drawing.Size(680, 22);
            this.labelEvents.TabIndex = 2;
            this.labelEvents.Text = "What the component fires is shown here.";
            // 
            // CarbonCreditTrackerTestForm
            // 
            this.ClientSize = new System.Drawing.Size(712, 438);
            this.Controls.Add(this.labelEvents);
            this.Controls.Add(this.carbonCreditTracker1);
            this.Controls.Add(this.labelTitle);
            this.Name = "CarbonCreditTrackerTestForm";
            this.Text = "CarbonCreditTracker test";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Wisej.Web.Label labelTitle;
        private GridKrafter.WisejDemo.CarbonCreditTracker carbonCreditTracker1;
        private Wisej.Web.Label labelEvents;
    }
}
