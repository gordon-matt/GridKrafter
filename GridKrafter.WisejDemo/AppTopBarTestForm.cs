using System;
using Wisej.Web;

namespace GridKrafter.WisejDemo
{
    /// <summary>
    /// A form to try AppTopBar on: open it in the designer to see the component, or show it
    /// from your application to try it live.
    /// </summary>
    public partial class AppTopBarTestForm : Form
    {
        public AppTopBarTestForm()
        {
            InitializeComponent();

            this.appTopBar1.Breadcrumb = new[] { "Home", "Commodities", "Ethanol" };
            this.appTopBar1.Environment = AppTopBarEnvironment.Uat;
            this.appTopBar1.AlertCount = 3;
            this.appTopBar1.UserName = "Tim Larson";
            this.appTopBar1.UserRole = "Commodity Marketing";
            this.appTopBar1.Initials = "TL";
            this.appTopBar1.AsOfText = DateTime.Now.ToString("h:mm tt") + " CT";

            this.appTopBar1.SearchSubmitted += (s, e) => ShowEvent("SearchSubmitted: \"" + e.Text + "\"");
            this.appTopBar1.AlertsClicked += (s, e) => ShowEvent("AlertsClicked");
            this.appTopBar1.UserMenuClicked += (s, e) => ShowEvent("UserMenuClicked");
            this.appTopBar1.CrumbClicked += (s, e) => ShowEvent("CrumbClicked: " + e.Index + " (" + this.appTopBar1.Breadcrumb[e.Index] + ")");
        }

        private void ShowEvent(string text)
        {
            this.labelEvents.Text = text;
        }
    }
}
