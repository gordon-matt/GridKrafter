using System;
using System.Collections.Generic;
using System.Linq;
using GridKrafter.Demo.Api;
using GridKrafter.Models;
using GridKrafter.WisejDemo.Services;
using GridKrafter.WisejDemo.Services.Api;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Dialogs
{
	/// <summary>
	/// Publishes the board on show, as it is arranged now, as the starting layout of the roles checked, and withdraws
	/// it from the roles unchecked. People who arranged their own copy keep it until they reset the board.
	/// </summary>
	public partial class PublishTemplateDialog : Form
	{
		private readonly string boardKey;
		private readonly DashboardBoard board;
		private readonly HashSet<string> published = new HashSet<string>(StringComparer.Ordinal);

		public PublishTemplateDialog()
		{
			InitializeComponent();
		}

		/// <summary>The dialog for the board of the key, titled as its page.</summary>
		public PublishTemplateDialog(string boardKey, string boardTitle, DashboardBoard board) : this()
		{
			this.boardKey = boardKey;
			this.board = board;
			this.introLabel.Text = "Publish the " + boardTitle + " board, as it is arranged now, as the starting layout of these roles:";
		}

		private void PublishTemplateDialog_Load(object sender, EventArgs e)
		{
			try
			{
				var templates = BoardStore.Templates(this.boardKey).ToDictionary(t => t.RoleId, StringComparer.Ordinal);
				foreach (var role in DataApi.Roles())
				{
					string text = role.Name;
					if (templates.TryGetValue(role.Id, out var template))
					{
						this.published.Add(role.Id);
						text += "  ·  published by " + template.PublishedBy + ", " + template.PublishedAt.LocalDateTime.ToString("g");
					}

					int index = this.rolesList.Items.Add(new RoleChoice(role.Id, text));
					this.rolesList.SetItemChecked(index, this.published.Contains(role.Id));
				}
			}
			catch (DataApiException ex)
			{
				ShowError(ex.Message);
				this.publishButton.Enabled = false;
			}
		}

		private void publishButton_Click(object sender, EventArgs e)
		{
			var chosen = this.rolesList.CheckedItems.Cast<RoleChoice>().Select(c => c.Id).ToList();
			var withdrawn = this.published.Where(id => !chosen.Contains(id)).ToList();
			if (chosen.Count == 0 && withdrawn.Count == 0)
			{
				ShowError("Check at least one role.");
				return;
			}

			try
			{
				if (chosen.Count > 0)
					BoardStore.Publish(this.boardKey, this.board, chosen);
				foreach (string role in withdrawn)
					BoardStore.Withdraw(this.boardKey, role);
			}
			catch (DataApiException ex)
			{
				ShowError(ex.Message);
				return;
			}

			string message = chosen.Count > 0 ? "Published for " + chosen.Count + (chosen.Count == 1 ? " role." : " roles.") : "";
			if (withdrawn.Count > 0)
				message += (message.Length > 0 ? " " : "") + "Withdrawn from " + withdrawn.Count + (withdrawn.Count == 1 ? " role." : " roles.");
			AlertBox.Show(message, MessageBoxIcon.Information);

			this.DialogResult = DialogResult.OK;
			Close();
		}

		private void ShowError(string text)
		{
			this.errorLabel.Text = text;
			this.errorLabel.Visible = true;
		}

		// a role in the list: its id, shown by its name and its template's date
		private sealed class RoleChoice
		{
			public RoleChoice(string id, string text)
			{
				this.Id = id;
				this.Text = text;
			}

			public string Id { get; }

			public string Text { get; }

			public override string ToString() => this.Text;
		}
	}
}
