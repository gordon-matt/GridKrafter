using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GridKrafter.Demo.Api;
using GridKrafter.WisejDemo.Services.Api;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Panels
{
	/// <summary>
	/// The audit trail: who did what and when, with filters, paging and a CSV export.
	/// </summary>
	public partial class AuditPanel : Wisej.Web.UserControl
	{
		private const int PageSize = 200;
		private const int ExportLimit = 10000;
		private const string AllPeople = "All people";
		private const string AllActions = "All actions";

		// The filter the grid currently shows, so "Load more" and the export match it.
		private DateTime? appliedFrom;
		private DateTime? appliedTo;
		private string appliedUser;
		private string appliedAction;
		private string appliedText;
		private int total;

		public AuditPanel()
		{
			InitializeComponent();
		}

		private void AuditPanel_Load(object sender, EventArgs e)
		{
			this.toDate.Value = DateTime.Today;
			this.fromDate.Value = DateTime.Today.AddDays(-7);

			try
			{
				var facets = DataApi.AuditFacets();

				this.userFilter.Items.Add(AllPeople);
				foreach (string user in facets.Users)
					this.userFilter.Items.Add(user);

				this.actionFilter.Items.Add(AllActions);
				foreach (string family in new[] { "sign-in", "user", "board", "template" })
					this.actionFilter.Items.Add(family);
				foreach (string action in facets.Actions)
					this.actionFilter.Items.Add(action);
			}
			catch (DataApiException ex)
			{
				if (this.userFilter.Items.Count == 0)
					this.userFilter.Items.Add(AllPeople);
				if (this.actionFilter.Items.Count == 0)
					this.actionFilter.Items.Add(AllActions);
				this.countLabel.Text = ex.Message;
			}

			this.userFilter.SelectedIndex = 0;
			this.actionFilter.SelectedIndex = 0;

			Search();
		}

		private void searchButton_Click(object sender, EventArgs e)
		{
			Search();
		}

		private void searchText_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
				Search();
		}

		private void loadMoreButton_Click(object sender, EventArgs e)
		{
			try
			{
				var page = DataApi.Audit(this.appliedFrom, this.appliedTo, this.appliedUser, this.appliedAction,
					this.appliedText, this.entriesGrid.Rows.Count, PageSize);
				AddRows(page);
				this.total = page.Total;
				UpdateFooter();
			}
			catch (DataApiException ex)
			{
				AlertBox.Show(ex.Message, MessageBoxIcon.Warning);
			}
		}

		private void exportButton_Click(object sender, EventArgs e)
		{
			try
			{
				var page = DataApi.Audit(this.appliedFrom, this.appliedTo, this.appliedUser, this.appliedAction,
					this.appliedText, 0, ExportLimit);

				var csv = new StringBuilder();
				csv.Append(Row("Time", "Person", "Action", "Target", "Details", "Address"));
				foreach (var entry in page.Entries)
					csv.Append(Row(FormatTime(entry.At), entry.UserName, entry.Action, entry.Target, entry.Detail, entry.ClientAddress));

				var stream = new MemoryStream();
				byte[] bytes = new UTF8Encoding(true).GetPreamble();
				stream.Write(bytes, 0, bytes.Length);
				bytes = Encoding.UTF8.GetBytes(csv.ToString());
				stream.Write(bytes, 0, bytes.Length);
				stream.Position = 0;

				Application.Download(stream, "audit-trail-" + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".csv");

				DataApi.Report("export.audit", "audit trail", page.Entries.Count + " entries, " + FilterSummary());
			}
			catch (DataApiException ex)
			{
				AlertBox.Show(ex.Message, MessageBoxIcon.Warning);
			}
		}

		private void entriesGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex != this.actionColumn.Index)
				return;

			string action = e.Value as string;
			if (IsFailure(action))
				e.CellStyle.ForeColor = System.Drawing.Color.FromName("@danger");
		}

		private void Search()
		{
			this.appliedFrom = this.fromDate.Value.Date;
			this.appliedTo = this.toDate.Value.Date.AddDays(1);
			this.appliedUser = this.userFilter.SelectedIndex > 0 ? this.userFilter.Text : null;
			this.appliedAction = this.actionFilter.SelectedIndex > 0 ? this.actionFilter.Text : null;
			this.appliedText = string.IsNullOrWhiteSpace(this.searchText.Text) ? null : this.searchText.Text.Trim();

			try
			{
				var page = DataApi.Audit(this.appliedFrom, this.appliedTo, this.appliedUser, this.appliedAction,
					this.appliedText, 0, PageSize);
				this.entriesGrid.Rows.Clear();
				AddRows(page);
				this.total = page.Total;
				UpdateFooter();
			}
			catch (DataApiException ex)
			{
				this.entriesGrid.Rows.Clear();
				this.total = 0;
				this.loadMoreButton.Visible = false;
				this.countLabel.Text = ex.Message;
			}
		}

		private void AddRows(AuditPage page)
		{
			foreach (var entry in page.Entries)
				this.entriesGrid.Rows.Add(FormatTime(entry.At), entry.UserName, entry.Action, entry.Target, entry.Detail, entry.ClientAddress);
		}

		private void UpdateFooter()
		{
			int shown = this.entriesGrid.Rows.Count;
			this.countLabel.Text = this.total == 0
				? "No entries match."
				: "Showing " + shown + " of " + this.total + " entries";
			this.loadMoreButton.Visible = shown < this.total;
		}

		private string FilterSummary()
		{
			var parts = new List<string>
			{
				this.appliedFrom.Value.ToString("yyyy-MM-dd") + " to " + this.appliedTo.Value.AddDays(-1).ToString("yyyy-MM-dd")
			};
			if (this.appliedUser != null)
				parts.Add("person " + this.appliedUser);
			if (this.appliedAction != null)
				parts.Add("action " + this.appliedAction);
			if (this.appliedText != null)
				parts.Add("text \"" + this.appliedText + "\"");
			return string.Join(", ", parts);
		}

		private static string FormatTime(DateTimeOffset at)
		{
			return at.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
		}

		private static bool IsFailure(string action)
		{
			if (string.IsNullOrEmpty(action))
				return false;
			return action.EndsWith(".failed", StringComparison.Ordinal)
				|| action.EndsWith(".locked", StringComparison.Ordinal)
				|| action == "access.denied"
				|| action == "page.denied"
				|| action == "account.locked";
		}

		private static string Row(params string[] fields)
		{
			var line = new StringBuilder();
			for (int i = 0; i < fields.Length; i++)
			{
				if (i > 0)
					line.Append(',');
				line.Append('"').Append((fields[i] ?? "").Replace("\"", "\"\"")).Append('"');
			}
			line.Append("\r\n");
			return line.ToString();
		}
	}
}
