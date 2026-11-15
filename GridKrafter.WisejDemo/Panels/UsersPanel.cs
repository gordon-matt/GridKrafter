using System;
using System.Collections.Generic;
using System.Linq;
using GridKrafter.Demo.Api;
using GridKrafter.WisejDemo.Dialogs;
using GridKrafter.WisejDemo.Services.Api;
using GridKrafter.WisejDemo.Services.Security;
using GridKrafter.WisejDemo.Services.Shell;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Panels
{
	/// <summary>
	/// The administration of users and roles: who may sign in, with which role, and what each role allows and sees.
	/// Everything goes through the data API, which checks the administrator's own role again on every call.
	/// </summary>
	public partial class UsersPanel : Wisej.Web.UserControl
	{
		private const string ACTIVE = "Active";
		private const string TURNED_OFF = "Turned off";
		private const string LOCKED = "Locked";

		private IReadOnlyList<RoleDto> roles = Array.Empty<RoleDto>();

		// the panel names of the pages in the checklist, in the order they are listed
		private readonly List<string> pagePanels = new List<string>();

		// while a role is being shown, the boxes' events are not changes
		private bool showingRole;

		public UsersPanel()
		{
			InitializeComponent();

			// the status is a chip in the theme's own tones, so no colour is written here
			this.statusColumn.ToneMap[ACTIVE] = ChipTone.Success;
			this.statusColumn.ToneMap[TURNED_OFF] = ChipTone.Neutral;
			this.statusColumn.ToneMap[LOCKED] = ChipTone.Danger;

			// every page but those all users see and those the permissions open
			foreach (var page in PageInfos.All)
			{
				if (Roles.AlwaysVisible.Contains(page.Panel) || page.Panel == Roles.UsersPage || page.Panel == Roles.AuditPage)
					continue;

				this.pagePanels.Add(page.Panel);
				this.pageList.Items.Add(page.Title);
			}
		}

		private void UsersPanel_Load(object sender, EventArgs e)
		{
			LoadRoles(null);
			LoadUsers(null);
		}

		#region Users

		private UserDto SelectedUser =>
			this.usersGrid.SelectedRows.Count > 0 ? this.usersGrid.SelectedRows[0].Tag as UserDto : null;

		private void LoadUsers(int? selectId)
		{
			IReadOnlyList<UserDto> users;
			try
			{
				users = DataApi.Users();
			}
			catch (DataApiException ex)
			{
				MessageBox.Show(ex.Message, "Users", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			this.usersGrid.Rows.Clear();
			foreach (var user in users.OrderBy(u => u.UserName, StringComparer.OrdinalIgnoreCase))
			{
				int index = this.usersGrid.Rows.Add(
					user.UserName,
					user.DisplayName,
					user.Email,
					RoleName(user.RoleId),
					Status(user),
					LastSignIn(user.LastSignIn));
				this.usersGrid.Rows[index].Tag = user;
			}

			this.usersGrid.ClearSelection();
			if (selectId.HasValue)
			{
				foreach (DataGridViewRow row in this.usersGrid.Rows)
				{
					if (row.Tag is UserDto user && user.Id == selectId.Value)
					{
						row.Selected = true;
						break;
					}
				}
			}

			UpdateUserButtons();
		}

		private string RoleName(string roleId) =>
			this.roles.FirstOrDefault(r => r.Id == roleId)?.Name ?? roleId;

		private static string Status(UserDto user) =>
			user.IsLocked ? LOCKED : user.IsActive ? ACTIVE : TURNED_OFF;

		private static string LastSignIn(DateTimeOffset? at)
		{
			if (!at.HasValue)
				return "Never";

			// in the browser's time zone, when it is known
			var zone = Application.Browser?.TimezoneInfo ?? TimeZoneInfo.Local;
			return TimeZoneInfo.ConvertTime(at.Value, zone).ToString("g");
		}

		private void UpdateUserButtons()
		{
			var user = this.SelectedUser;
			this.editButton.Enabled = user != null;
			this.resetPasswordButton.Enabled = user != null;
			this.unlockButton.Enabled = user != null && user.IsLocked;
		}

		private void usersGrid_SelectionChanged(object sender, EventArgs e)
		{
			UpdateUserButtons();
		}

		private void usersGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex >= 0)
				EditSelected();
		}

		private void newUserButton_Click(object sender, EventArgs e)
		{
			UserDialog.ForNew(this.roles).ShowDialog((form, result) =>
			{
				if (result == DialogResult.OK)
					LoadUsers(((UserDialog)form).Result?.Id);
			});
		}

		private void editButton_Click(object sender, EventArgs e)
		{
			EditSelected();
		}

		private void EditSelected()
		{
			var user = this.SelectedUser;
			if (user == null)
				return;

			UserDialog.ForEdit(user, this.roles).ShowDialog((form, result) =>
			{
				if (result != DialogResult.OK)
					return;

				// the administrator changed their own account: their session follows it
				if (user.Id == CurrentUser.Session?.Id)
					RefreshOwnSession();

				LoadUsers(user.Id);
			});
		}

		private void resetPasswordButton_Click(object sender, EventArgs e)
		{
			var user = this.SelectedUser;
			if (user == null)
				return;

			PasswordDialog.ForReset(user.Id, user.UserName).ShowDialog((form, result) =>
			{
				// a reset also unlocks the user
				if (result == DialogResult.OK)
					LoadUsers(user.Id);
			});
		}

		private void unlockButton_Click(object sender, EventArgs e)
		{
			var user = this.SelectedUser;
			if (user == null)
				return;

			try
			{
				DataApi.Unlock(user.Id);
			}
			catch (DataApiException ex)
			{
				MessageBox.Show(ex.Message, "Unlock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			LoadUsers(user.Id);
		}

		#endregion

		#region Roles

		private RoleDto SelectedRole
		{
			get
			{
				int index = this.roleList.SelectedIndex;
				return index >= 0 && index < this.roles.Count ? this.roles[index] : null;
			}
		}

		private void LoadRoles(string selectId)
		{
			try
			{
				this.roles = DataApi.Roles() ?? Array.Empty<RoleDto>();
			}
			catch (DataApiException ex)
			{
				MessageBox.Show(ex.Message, "Roles", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				this.roles = Array.Empty<RoleDto>();
			}

			this.roleList.Items.Clear();
			foreach (var role in this.roles)
				this.roleList.Items.Add(role.Name);

			int index = 0;
			for (int i = 0; i < this.roles.Count; i++)
			{
				if (this.roles[i].Id == selectId)
					index = i;
			}

			if (this.roles.Count > 0)
				this.roleList.SelectedIndex = index;
			else
				ShowRole(null);
		}

		private void roleList_SelectedIndexChanged(object sender, EventArgs e)
		{
			ShowRole(this.SelectedRole);
		}

		private void ShowRole(RoleDto role)
		{
			this.showingRole = true;
			try
			{
				var allows = role?.Permissions ?? Permissions.None;
				bool isAdmin = role?.Id == Roles.Admin;

				this.roleDescriptionLabel.Text = role?.Description ?? "";
				this.editBoardsCheck.Checked = allows.HasFlag(Permissions.EditBoards);
				this.publishTemplatesCheck.Checked = allows.HasFlag(Permissions.PublishTemplates);
				this.manageUsersCheck.Checked = isAdmin || allows.HasFlag(Permissions.ManageUsers);
				this.viewAuditCheck.Checked = allows.HasFlag(Permissions.ViewAudit);

				// the administrator's role always manages users: someone must be able to
				this.manageUsersCheck.Enabled = role != null && !isAdmin;
				this.editBoardsCheck.Enabled = role != null;
				this.publishTemplatesCheck.Enabled = role != null;
				this.viewAuditCheck.Enabled = role != null;
				this.pageList.Enabled = role != null;
				this.saveRoleButton.Enabled = role != null;

				var pages = new HashSet<string>(role?.Pages ?? Array.Empty<string>(), StringComparer.Ordinal);
				for (int i = 0; i < this.pagePanels.Count; i++)
					this.pageList.SetItemChecked(i, pages.Contains(this.pagePanels[i]));

				this.savedLabel.Text = "";
			}
			finally
			{
				this.showingRole = false;
			}
		}

		private void roleSetting_Changed(object sender, EventArgs e)
		{
			if (!this.showingRole)
				this.savedLabel.Text = "";
		}

		private void pageList_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			if (!this.showingRole)
				this.savedLabel.Text = "";
		}

		private void saveRoleButton_Click(object sender, EventArgs e)
		{
			var role = this.SelectedRole;
			if (role == null)
				return;

			var allows = Permissions.None;
			if (this.editBoardsCheck.Checked)
				allows |= Permissions.EditBoards;
			if (this.publishTemplatesCheck.Checked)
				allows |= Permissions.PublishTemplates;
			if (this.manageUsersCheck.Checked || role.Id == Roles.Admin)
				allows |= Permissions.ManageUsers;
			if (this.viewAuditCheck.Checked)
				allows |= Permissions.ViewAudit;

			// the pages checked here, and those of the role this list does not show (kept as they are)
			var pages = new List<string>();
			for (int i = 0; i < this.pagePanels.Count; i++)
			{
				if (this.pageList.GetItemChecked(i))
					pages.Add(this.pagePanels[i]);
			}
			foreach (var page in role.Pages ?? Array.Empty<string>())
			{
				if (!this.pagePanels.Contains(page) && !pages.Contains(page))
					pages.Add(page);
			}

			try
			{
				DataApi.UpdateRole(role.Id, new UpdateRoleRequest(allows, pages));
			}
			catch (DataApiException ex)
			{
				MessageBox.Show(ex.Message, "Save role", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			// the administrator's own role may have changed
			if (role.Id == CurrentUser.Session?.RoleId)
				RefreshOwnSession();

			LoadRoles(role.Id);
			this.savedLabel.Text = "Saved.";
		}

		#endregion

		private static void RefreshOwnSession()
		{
			try
			{
				CurrentUser.Refresh();
			}
			catch (DataApiException ex)
			{
				MessageBox.Show(ex.Message, "Users & roles", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
		}
	}
}
