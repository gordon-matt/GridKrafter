using System;
using System.Collections.Generic;
using System.Linq;
using GridKrafter.Demo.Api;
using GridKrafter.WisejDemo.Services.Api;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Dialogs
{
	/// <summary>
	/// Adds a user, or changes one, through the data API. The API checks the rules (a user name that is taken, the
	/// password's strength, the last administrator) and answers why something is refused; that is shown in the dialog.
	/// </summary>
	public partial class UserDialog : Form
	{
		private IReadOnlyList<RoleDto> roles = Array.Empty<RoleDto>();
		private UserDto editing;

		public UserDialog()
		{
			InitializeComponent();
		}

		/// <summary>The user as the API answered after saving; <see langword="null"/> until then.</summary>
		public UserDto Result { get; private set; }

		/// <summary>A dialog that adds a user.</summary>
		public static UserDialog ForNew(IReadOnlyList<RoleDto> roles)
		{
			var dialog = new UserDialog();
			dialog.Text = "New user";
			dialog.FillRoles(roles, null);
			dialog.activeCheck.Visible = false;
			return dialog;
		}

		/// <summary>A dialog that changes a user: the user name stays, the password is reset elsewhere.</summary>
		public static UserDialog ForEdit(UserDto user, IReadOnlyList<RoleDto> roles)
		{
			if (user == null)
				throw new ArgumentNullException(nameof(user));

			var dialog = new UserDialog();
			dialog.editing = user;
			dialog.Text = "Edit " + user.UserName;
			dialog.userNameBox.Text = user.UserName;
			dialog.userNameBox.ReadOnly = true;
			dialog.displayNameBox.Text = user.DisplayName;
			dialog.emailBox.Text = user.Email;
			dialog.activeCheck.Checked = user.IsActive;
			dialog.passwordBox.Visible = false;
			dialog.confirmBox.Visible = false;
			dialog.mustChangeCheck.Visible = false;
			dialog.FillRoles(roles, user.RoleId);
			dialog.ClientSize = new System.Drawing.Size(380, 400);
			return dialog;
		}

		private void FillRoles(IReadOnlyList<RoleDto> list, string selectedId)
		{
			this.roles = list ?? Array.Empty<RoleDto>();
			this.roleCombo.Items.Clear();
			foreach (var role in this.roles)
				this.roleCombo.Items.Add(role.Name);

			int index = -1;
			for (int i = 0; i < this.roles.Count; i++)
			{
				if (string.Equals(this.roles[i].Id, selectedId, StringComparison.Ordinal))
					index = i;
			}

			// a new user starts with the role that allows the least
			if (index < 0 && selectedId == null && this.roles.Count > 0)
				index = this.roles.Select((role, i) => (role, i)).OrderBy(r => BitCount(r.role.Permissions)).First().i;

			this.roleCombo.SelectedIndex = index;
		}

		private static int BitCount(Permissions permissions)
		{
			int count = 0;
			for (int value = (int)permissions; value != 0; value &= value - 1)
				count++;
			return count;
		}

		private void saveButton_Click(object sender, EventArgs e)
		{
			this.errorLabel.Visible = false;
			this.errorLabel.Text = "";

			int index = this.roleCombo.SelectedIndex;
			if (index < 0 || index >= this.roles.Count)
			{
				ShowError("Choose a role.");
				this.roleCombo.Focus();
				return;
			}

			string roleId = this.roles[index].Id;
			string displayName = this.displayNameBox.Text.Trim();
			string email = this.emailBox.Text.Trim();

			try
			{
				if (this.editing == null)
				{
					string userName = this.userNameBox.Text.Trim();
					if (userName.Length == 0)
					{
						ShowError("Enter a user name.");
						this.userNameBox.Focus();
						return;
					}

					if (this.passwordBox.Text != this.confirmBox.Text)
					{
						ShowError("The two passwords don't match.");
						this.confirmBox.Focus();
						return;
					}

					this.Result = DataApi.CreateUser(new CreateUserRequest(userName, displayName, email, roleId, this.passwordBox.Text, this.mustChangeCheck.Checked));
				}
				else
				{
					this.Result = DataApi.UpdateUser(this.editing.Id, new UpdateUserRequest(displayName, email, roleId, this.activeCheck.Checked));
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

		private void UserDialog_Load(object sender, EventArgs e)
		{
			if (this.editing == null)
				this.userNameBox.Focus();
			else
				this.displayNameBox.Focus();
		}
	}
}
