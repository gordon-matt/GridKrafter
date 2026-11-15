using System;
using GridKrafter.WisejDemo.Dialogs;
using GridKrafter.WisejDemo.Services.Api;
using GridKrafter.WisejDemo.Services.Security;
using Wisej.Web;

namespace GridKrafter.WisejDemo
{
	/// <summary>
	/// The sign-in page shown before anything else. The data API checks the user name and password, locks an account
	/// after five failed attempts in a row, and writes every attempt into the audit trail.
	/// </summary>
	public partial class LoginPage : Page
	{
		public LoginPage()
		{
			InitializeComponent();
		}

		private void LoginPage_Load(object sender, EventArgs e)
		{
			CenterCard();
			this.userNameBox.Focus();
		}

		private void LoginPage_Resize(object sender, EventArgs e)
		{
			CenterCard();
		}

		private void CenterCard()
		{
			this.card.Location = new System.Drawing.Point(
				Math.Max(0, (this.ClientSize.Width - this.card.Width) / 2),
				Math.Max(0, (this.ClientSize.Height - this.card.Height) / 2));
		}

		private void signInButton_Click(object sender, EventArgs e)
		{
			this.errorLabel.Visible = false;
			if (string.IsNullOrWhiteSpace(this.userNameBox.Text) || string.IsNullOrEmpty(this.passwordBox.Text))
			{
				ShowError("Enter your user name and password.");
				return;
			}

			TrySignIn(() => CurrentUser.SignIn(this.userNameBox.Text.Trim(), this.passwordBox.Text));
		}

		private void TrySignIn(Func<Demo.Api.UserSession> signIn)
		{
			Demo.Api.UserSession user;
			try
			{
				user = signIn();
			}
			catch (DataApiException ex)
			{
				this.passwordBox.Text = "";
				ShowError(ex.Message);
				this.passwordBox.Focus();
				return;
			}

			if (user.MustChangePassword)
			{
				// a password an administrator set is replaced before going on
				PasswordDialog.ForOwnPassword(required: true).ShowDialog((dialog, result) =>
				{
					if (result == DialogResult.OK)
						OpenApplication();
				});
				return;
			}

			OpenApplication();
		}

		// the application proper replaces this page
		private void OpenApplication()
		{
			Application.MainPage = new Page1();
			Dispose();
		}

		private void ShowError(string text)
		{
			this.errorLabel.Text = text;
			this.errorLabel.Visible = true;
		}
	}
}
