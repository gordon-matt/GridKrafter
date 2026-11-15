using System;
using GridKrafter.WisejDemo.Services.Api;
using GridKrafter.WisejDemo.Services.Security;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Dialogs
{
	/// <summary>
	/// Changes a password through the data API: one's own (with the current one), or, for an administrator, another
	/// user's. The API checks the rules (length, kinds of characters) and answers why one is refused.
	/// </summary>
	public partial class PasswordDialog : Form
	{
		private int resetUserId;

		public PasswordDialog()
		{
			InitializeComponent();
		}

		/// <summary>A dialog that changes the signed-in person's own password.</summary>
		/// <param name="required">Whether they must change it before going on (it can't be cancelled).</param>
		public static PasswordDialog ForOwnPassword(bool required)
		{
			var dialog = new PasswordDialog();
			dialog.Text = "Change password";
			dialog.introLabel.Text = required
				? "Choose a new password before you go on. It needs at least 10 characters, with three of: capital letters, small letters, digits and symbols."
				: "It needs at least 10 characters, with three of: capital letters, small letters, digits and symbols.";
			dialog.mustChangeCheck.Visible = false;
			if (required)
			{
				dialog.cancelButton.Visible = false;
				dialog.CloseBox = false;
			}
			return dialog;
		}

		/// <summary>A dialog that sets another user's password, unlocks them and ends their sessions.</summary>
		public static PasswordDialog ForReset(int userId, string userName)
		{
			var dialog = new PasswordDialog();
			dialog.resetUserId = userId;
			dialog.Text = "Reset password";
			dialog.introLabel.Text = "Set a new password for " + userName + ". They are signed out everywhere, and unlocked.";
			dialog.currentBox.Visible = false;
			return dialog;
		}

		private void okButton_Click(object sender, EventArgs e)
		{
			this.errorLabel.Text = "";
			if (this.newBox.Text != this.confirmBox.Text)
			{
				ShowError("The two new passwords don't match.");
				this.confirmBox.Focus();
				return;
			}

			try
			{
				if (this.resetUserId > 0)
				{
					DataApi.ResetPassword(this.resetUserId, this.newBox.Text, this.mustChangeCheck.Checked);
				}
				else
				{
					DataApi.ChangePassword(this.currentBox.Text, this.newBox.Text);
					CurrentUser.PasswordChanged();
				}

				this.DialogResult = DialogResult.OK;
				Close();
			}
			catch (DataApiException ex)
			{
				ShowError(ex.Message);
			}
		}

		private void ShowError(string text)
		{
			this.errorLabel.Text = text;
			this.errorLabel.Visible = true;
		}

		private void PasswordDialog_Load(object sender, EventArgs e)
		{
			// the hidden boxes leave no gap
			if (!this.currentBox.Visible)
				this.newBox.Focus();
			else
				this.currentBox.Focus();
		}
	}
}
