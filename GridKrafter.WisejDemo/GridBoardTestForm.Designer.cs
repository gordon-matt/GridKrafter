namespace GridKrafter.WisejDemo.Controls
{
    partial class GridBoardTestForm
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
            this.labelTitle = new global::Wisej.Web.Label();
            this.gridBoard1 = new GridKrafter.WisejDemo.Controls.GridBoard();
            this.gridBoardSection1 = new GridKrafter.WisejDemo.Controls.GridBoardSection();
            this.gridBoardSection2 = new GridKrafter.WisejDemo.Controls.GridBoardSection();
            this.gridWidgetKind1 = new GridKrafter.WisejDemo.Controls.GridWidgetKind();
            this.gridWidgetKind2 = new GridKrafter.WisejDemo.Controls.GridWidgetKind();
            this.gridWidgetKind3 = new GridKrafter.WisejDemo.Controls.GridWidgetKind();
            this.widgetRevenue = new GridKrafter.WisejDemo.Controls.GridWidget();
            this.labelRevenue = new global::Wisej.Web.Label();
            this.widgetVisitors = new GridKrafter.WisejDemo.Controls.GridWidget();
            this.labelVisitors = new global::Wisej.Web.Label();
            this.widgetActivity = new GridKrafter.WisejDemo.Controls.GridWidget();
            this.labelActivity = new global::Wisej.Web.Label();
            this.widgetLoad = new GridKrafter.WisejDemo.Controls.GridWidget();
            this.labelLoad = new global::Wisej.Web.Label();
            this.labelEvents = new global::Wisej.Web.Label();
            this.gridBoard1.SuspendLayout();
            this.widgetRevenue.SuspendLayout();
            this.widgetVisitors.SuspendLayout();
            this.widgetActivity.SuspendLayout();
            this.widgetLoad.SuspendLayout();
            this.SuspendLayout();
            //
            // labelTitle
            //
            this.labelTitle.AutoSize = true;
            this.labelTitle.Location = new System.Drawing.Point(16, 16);
            this.labelTitle.Name = "labelTitle";
            this.labelTitle.Size = new System.Drawing.Size(200, 18);
            this.labelTitle.TabIndex = 0;
            this.labelTitle.Text = "GridBoard";
            //
            // gridBoard1
            //
            this.gridBoard1.sections.Add(this.gridBoardSection1);
            this.gridBoard1.sections.Add(this.gridBoardSection2);
            this.gridBoard1.catalog.Add(this.gridWidgetKind1);
            this.gridBoard1.catalog.Add(this.gridWidgetKind2);
            this.gridBoard1.catalog.Add(this.gridWidgetKind3);
            this.gridBoard1.Controls.Add(this.widgetRevenue);
            this.gridBoard1.Controls.Add(this.widgetVisitors);
            this.gridBoard1.Controls.Add(this.widgetActivity);
            this.gridBoard1.Controls.Add(this.widgetLoad);
            this.gridBoard1.EditMode = true;
            this.gridBoard1.Location = new System.Drawing.Point(16, 48);
            this.gridBoard1.Name = "gridBoard1";
            this.gridBoard1.SelectedSection = "overview";
            this.gridBoard1.Size = new System.Drawing.Size(960, 560);
            this.gridBoard1.TabIndex = 1;
            //
            // gridBoardSection1
            //
            this.gridBoardSection1.Name = "gridBoardSection1";
            this.gridBoardSection1.Id = "overview";
            this.gridBoardSection1.Title = "Overview";
            this.gridBoardSection1.Icon = "home";
            //
            // gridBoardSection2
            //
            this.gridBoardSection2.Name = "gridBoardSection2";
            this.gridBoardSection2.Id = "sales";
            this.gridBoardSection2.Title = "Sales";
            this.gridBoardSection2.Icon = "cart";
            //
            // gridWidgetKind1
            //
            this.gridWidgetKind1.Name = "gridWidgetKind1";
            this.gridWidgetKind1.Kind = "stat";
            this.gridWidgetKind1.Text = "Stat tile";
            this.gridWidgetKind1.Description = "A number with its trend";
            this.gridWidgetKind1.Color = "green";
            this.gridWidgetKind1.W = 3;
            this.gridWidgetKind1.H = 2;
            //
            // gridWidgetKind2
            //
            this.gridWidgetKind2.Name = "gridWidgetKind2";
            this.gridWidgetKind2.Kind = "activity";
            this.gridWidgetKind2.Text = "Activity";
            this.gridWidgetKind2.Description = "Latest events as a timeline";
            this.gridWidgetKind2.Icon = "activity";
            this.gridWidgetKind2.Color = "violet";
            this.gridWidgetKind2.W = 8;
            this.gridWidgetKind2.H = 4;
            //
            // gridWidgetKind3
            //
            this.gridWidgetKind3.Name = "gridWidgetKind3";
            this.gridWidgetKind3.Kind = "load";
            this.gridWidgetKind3.Text = "Server load";
            this.gridWidgetKind3.Description = "CPU, memory and disk meters";
            this.gridWidgetKind3.Icon = "server";
            this.gridWidgetKind3.Color = "green";
            this.gridWidgetKind3.W = 4;
            this.gridWidgetKind3.H = 4;
            //
            // widgetRevenue
            //
            this.widgetRevenue.Chrome = GridKrafter.WisejDemo.Controls.GridWidgetChrome.Plain;
            this.widgetRevenue.Color = "green";
            this.widgetRevenue.Controls.Add(this.labelRevenue);
            this.widgetRevenue.GridH = 2;
            this.widgetRevenue.GridW = 3;
            this.widgetRevenue.GridX = 0;
            this.widgetRevenue.GridY = 0;
            this.widgetRevenue.ItemId = "revenue";
            this.widgetRevenue.Kind = "stat";
            this.widgetRevenue.Name = "widgetRevenue";
            this.widgetRevenue.Section = "overview";
            this.widgetRevenue.Title = "Revenue";
            //
            // labelRevenue
            //
            this.labelRevenue.Dock = global::Wisej.Web.DockStyle.Fill;
            this.labelRevenue.Name = "labelRevenue";
            this.labelRevenue.Padding = new global::Wisej.Web.Padding(20, 14, 16, 0);
            this.labelRevenue.Text = "Revenue  $48,200";
            //
            // widgetVisitors
            //
            this.widgetVisitors.Chrome = GridKrafter.WisejDemo.Controls.GridWidgetChrome.Plain;
            this.widgetVisitors.Controls.Add(this.labelVisitors);
            this.widgetVisitors.GridH = 2;
            this.widgetVisitors.GridW = 3;
            this.widgetVisitors.GridX = 3;
            this.widgetVisitors.GridY = 0;
            this.widgetVisitors.ItemId = "visitors";
            this.widgetVisitors.Kind = "stat";
            this.widgetVisitors.Name = "widgetVisitors";
            this.widgetVisitors.Section = "overview";
            this.widgetVisitors.Title = "Visitors";
            //
            // labelVisitors
            //
            this.labelVisitors.Dock = global::Wisej.Web.DockStyle.Fill;
            this.labelVisitors.Name = "labelVisitors";
            this.labelVisitors.Padding = new global::Wisej.Web.Padding(20, 14, 16, 0);
            this.labelVisitors.Text = "Visitors  12,840";
            //
            // widgetActivity
            //
            this.widgetActivity.Buttons = ((GridKrafter.Models.WidgetButtons)(((GridKrafter.Models.WidgetButtons.Refresh | GridKrafter.Models.WidgetButtons.Fullscreen)
            | GridKrafter.Models.WidgetButtons.Collapse)));
            this.widgetActivity.Color = "violet";
            this.widgetActivity.Controls.Add(this.labelActivity);
            this.widgetActivity.GridH = 4;
            this.widgetActivity.GridW = 8;
            this.widgetActivity.GridX = 0;
            this.widgetActivity.GridY = 2;
            this.widgetActivity.Icon = "activity";
            this.widgetActivity.ItemId = "activity";
            this.widgetActivity.Kind = "activity";
            this.widgetActivity.Name = "widgetActivity";
            this.widgetActivity.RefreshInterval = 30;
            this.widgetActivity.Section = "overview";
            this.widgetActivity.Title = "Activity";
            //
            // labelActivity
            //
            this.labelActivity.Dock = global::Wisej.Web.DockStyle.Fill;
            this.labelActivity.Name = "labelActivity";
            this.labelActivity.Padding = new global::Wisej.Web.Padding(14, 12, 14, 12);
            this.labelActivity.Text = "A Timeline goes here.";
            //
            // widgetLoad
            //
            this.widgetLoad.Buttons = ((GridKrafter.Models.WidgetButtons)(((((GridKrafter.Models.WidgetButtons.Refresh | GridKrafter.Models.WidgetButtons.Color)
            | GridKrafter.Models.WidgetButtons.Fullscreen)
            | GridKrafter.Models.WidgetButtons.Collapse)
            | GridKrafter.Models.WidgetButtons.Delete)));
            this.widgetLoad.Color = "green";
            this.widgetLoad.Controls.Add(this.labelLoad);
            this.widgetLoad.GridH = 4;
            this.widgetLoad.GridW = 4;
            this.widgetLoad.GridX = 8;
            this.widgetLoad.GridY = 2;
            this.widgetLoad.Icon = "server";
            this.widgetLoad.ItemId = "load";
            this.widgetLoad.Kind = "load";
            this.widgetLoad.Name = "widgetLoad";
            this.widgetLoad.Section = "overview";
            this.widgetLoad.Title = "Server load";
            //
            // labelLoad
            //
            this.labelLoad.Dock = global::Wisej.Web.DockStyle.Fill;
            this.labelLoad.Name = "labelLoad";
            this.labelLoad.Padding = new global::Wisej.Web.Padding(14, 12, 14, 12);
            this.labelLoad.Text = "MeterBar controls go here.";
            //
            // labelEvents
            //
            this.labelEvents.Location = new System.Drawing.Point(16, 620);
            this.labelEvents.Name = "labelEvents";
            this.labelEvents.Size = new System.Drawing.Size(960, 22);
            this.labelEvents.TabIndex = 2;
            this.labelEvents.Text = "What the component fires is shown here.";
            //
            // GridBoardTestForm
            //
            this.ClientSize = new System.Drawing.Size(992, 658);
            this.Controls.Add(this.labelEvents);
            this.Controls.Add(this.gridBoard1);
            this.Controls.Add(this.labelTitle);
            this.Name = "GridBoardTestForm";
            this.Text = "GridBoard test";
            this.widgetLoad.ResumeLayout(false);
            this.widgetActivity.ResumeLayout(false);
            this.widgetVisitors.ResumeLayout(false);
            this.widgetRevenue.ResumeLayout(false);
            this.gridBoard1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private global::Wisej.Web.Label labelTitle;
        private GridKrafter.WisejDemo.Controls.GridBoard gridBoard1;
        private GridKrafter.WisejDemo.Controls.GridBoardSection gridBoardSection1;
        private GridKrafter.WisejDemo.Controls.GridBoardSection gridBoardSection2;
        private GridKrafter.WisejDemo.Controls.GridWidgetKind gridWidgetKind1;
        private GridKrafter.WisejDemo.Controls.GridWidgetKind gridWidgetKind2;
        private GridKrafter.WisejDemo.Controls.GridWidgetKind gridWidgetKind3;
        private GridKrafter.WisejDemo.Controls.GridWidget widgetRevenue;
        private global::Wisej.Web.Label labelRevenue;
        private GridKrafter.WisejDemo.Controls.GridWidget widgetVisitors;
        private global::Wisej.Web.Label labelVisitors;
        private GridKrafter.WisejDemo.Controls.GridWidget widgetActivity;
        private global::Wisej.Web.Label labelActivity;
        private GridKrafter.WisejDemo.Controls.GridWidget widgetLoad;
        private global::Wisej.Web.Label labelLoad;
        private global::Wisej.Web.Label labelEvents;
    }
}
