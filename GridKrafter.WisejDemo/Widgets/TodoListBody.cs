using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json.Nodes;
using GridKrafter.WisejDemo.Model;
using Wisej.Web;

namespace GridKrafter.WisejDemo.Widgets
{
	/// <summary>Tasks saved with the board, as the Blazor demo's to-do list and the React demo's tasks.</summary>
	public class TodoListBody : WidgetBody
	{
		private static readonly (string Text, bool Done)[] Starter =
		{
			("Drag a widget by its header", true),
			("Resize one in edit mode", false),
			("Add a widget from the catalog", false),
			("Switch to dark mode", false),
		};

		private readonly List<TodoItem> tasks = new List<TodoItem>();
		private readonly TextBox draft = new TextBox();
		private readonly Button add = new Button();
		private readonly Panel list = new Panel();
		private readonly Label left = new Label();
		private readonly Label empty = new Label();
		private readonly Button clearDone = new Button();

		public TodoListBody()
		{
			var addRow = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(0, 4, 0, 4) };
			this.draft.Dock = DockStyle.Fill;
			this.draft.Watermark = "Add a task";
			this.draft.MaxLength = 200;
			this.draft.TextChanged += (s, e) => this.add.Enabled = !string.IsNullOrWhiteSpace(this.draft.Text);
			this.draft.KeyDown += Draft_KeyDown;
			this.add.Dock = DockStyle.Right;
			this.add.Width = 64;
			this.add.Text = "Add";
			this.add.Enabled = false;
			this.add.Margin = new Padding(8, 0, 0, 0);
			this.add.Click += (s, e) => AddTask();
			addRow.Controls.Add(this.draft);
			addRow.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 8 });
			addRow.Controls.Add(this.add);

			var footer = new Panel { Dock = DockStyle.Bottom, Height = 34 };
			this.left.Dock = DockStyle.Fill;
			this.left.TextAlign = ContentAlignment.MiddleLeft;
			this.left.ForeColor = Color.FromName("@grayText");
			this.clearDone.Dock = DockStyle.Right;
			this.clearDone.Width = 110;
			this.clearDone.Text = "Clear finished";
			this.clearDone.Display = Display.Label;
			this.clearDone.BorderStyle = BorderStyle.None;
			this.clearDone.BackColor = Color.Transparent;
			this.clearDone.Click += (s, e) => { this.tasks.RemoveAll(t => t.Done); Changed(); };
			footer.Controls.Add(this.left);
			footer.Controls.Add(this.clearDone);

			this.list.Dock = DockStyle.Fill;
			this.list.AutoScroll = true;
			this.empty.Dock = DockStyle.Top;
			this.empty.Height = 32;
			this.empty.TextAlign = ContentAlignment.MiddleCenter;
			this.empty.ForeColor = Color.FromName("@grayText");

			this.Controls.Add(this.list);
			this.Controls.Add(footer);
			this.Controls.Add(addRow);

			Fill(Starter.Select(t => new TodoItem { Text = t.Text, Done = t.Done }));
		}

		protected override void OnLoadContent()
		{
			if (Load("items") is JsonArray saved)
				Fill(saved.OfType<JsonObject>().Select(o => new TodoItem { Text = (string)o["text"] ?? "", Done = (bool?)o["done"] ?? false }));
			else
				Fill(Starter.Select(t => new TodoItem { Text = t.Text, Done = t.Done }));
		}

		private void Fill(IEnumerable<TodoItem> items)
		{
			this.tasks.Clear();
			this.tasks.AddRange(items);
			ShowTasks();
		}

		private void Draft_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
				AddTask();
		}

		private void AddTask()
		{
			if (string.IsNullOrWhiteSpace(this.draft.Text))
				return;

			this.tasks.Add(new TodoItem { Text = this.draft.Text.Trim() });
			this.draft.Text = "";
			Changed();
			this.draft.Focus();
		}

		// Shows the tasks again and saves them.
		private void Changed()
		{
			ShowTasks();
			var items = new JsonArray(this.tasks.Select(t => (JsonNode)new JsonObject { ["text"] = t.Text, ["done"] = t.Done }).ToArray());
			Save("items", items);
		}

		private void ShowTasks()
		{
			this.list.SuspendLayout();
			this.list.Controls.Clear(true);
			if (this.tasks.Count == 0)
			{
				this.empty.Text = "Nothing to do.";
				this.list.Controls.Add(this.empty);
			}
			else
			{
				// docked to the top, the last one added is the topmost: add them from the last
				for (int i = this.tasks.Count - 1; i >= 0; i--)
					this.list.Controls.Add(CreateRow(this.tasks[i]));
			}

			this.list.ResumeLayout(true);

			int open = this.tasks.Count(t => !t.Done);
			this.left.Text = open + " left";
			this.clearDone.Enabled = this.tasks.Any(t => t.Done);
		}

		private Control CreateRow(TodoItem task)
		{
			var row = new Panel { Dock = DockStyle.Top, Height = 34 };

			var check = new CheckBox
			{
				Dock = DockStyle.Fill,
				Text = task.Text,
				Checked = task.Done,
			};
			if (task.Done)
			{
				check.ForeColor = Color.FromName("@grayText");
				check.Font = new Font("default", 14F, FontStyle.Strikeout, GraphicsUnit.Pixel);
			}
			check.CheckedChanged += (s, e) => { task.Done = check.Checked; Changed(); };

			var delete = new Button
			{
				Dock = DockStyle.Right,
				Width = 32,
				Display = Display.Icon,
				ImageSource = DemoData.IconUrl("trash"),
				BorderStyle = BorderStyle.None,
				BackColor = Color.Transparent,
				ToolTipText = "Delete " + task.Text,
			};
			delete.Click += (s, e) => { this.tasks.Remove(task); Changed(); };

			row.Controls.Add(check);
			row.Controls.Add(delete);
			return row;
		}

		private sealed class TodoItem
		{
			public string Text { get; set; }
			public bool Done { get; set; }
		}
	}
}
