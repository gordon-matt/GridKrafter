using System;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Panels
{
	/// <summary>
	/// How this page works, and useful links: the MVC demo's "How this page works" and Links widgets.
	/// </summary>
	public partial class AboutPanel : Wisej.Web.UserControl
	{
		public AboutPanel()
		{
			InitializeComponent();
		}

		private void linkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			Application.Navigate((string)((Control)sender).Tag, target: "_blank");
		}
	}
}
