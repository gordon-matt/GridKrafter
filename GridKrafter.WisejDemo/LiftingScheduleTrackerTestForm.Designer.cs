namespace GridKrafter.WisejDemo
{
    partial class LiftingScheduleTrackerTestForm
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
            this.liftingScheduleTracker1 = new GridKrafter.WisejDemo.LiftingScheduleTracker();
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
            this.labelTitle.Text = "LiftingScheduleTracker";
            // 
            // liftingScheduleTracker1
            // 
            this.liftingScheduleTracker1.Location = new System.Drawing.Point(16, 48);
            this.liftingScheduleTracker1.Name = "liftingScheduleTracker1";
            this.liftingScheduleTracker1.Size = new System.Drawing.Size(560, 360);
            this.liftingScheduleTracker1.TabIndex = 1;
            // 
            // labelEvents
            // 
            this.labelEvents.Location = new System.Drawing.Point(16, 420);
            this.labelEvents.Name = "labelEvents";
            this.labelEvents.Size = new System.Drawing.Size(560, 22);
            this.labelEvents.TabIndex = 2;
            this.labelEvents.Text = "What the component fires is shown here.";
            // 
            // LiftingScheduleTrackerTestForm
            // 
            this.ClientSize = new System.Drawing.Size(592, 458);
            this.Controls.Add(this.labelEvents);
            this.Controls.Add(this.liftingScheduleTracker1);
            this.Controls.Add(this.labelTitle);
            this.Name = "LiftingScheduleTrackerTestForm";
            this.Text = "LiftingScheduleTracker test";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Wisej.Web.Label labelTitle;
        private GridKrafter.WisejDemo.LiftingScheduleTracker liftingScheduleTracker1;
        private Wisej.Web.Label labelEvents;
    }
}
