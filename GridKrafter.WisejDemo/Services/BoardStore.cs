using System;
using System.Collections.Generic;
using GridKrafter.Demo.Api;
using GridKrafter.Models;
using GridKrafter.Serialization;
using GridKrafter.WisejDemo.Services.Api;
using GridKrafter.WisejDemo.Services.Security;

namespace GridKrafter.WisejDemo.Services
{
	/// <summary>
	/// The boards of a key for the signed-in person: their own arrangement, and the starting layout published for
	/// their role. A board page shows its own definition, with the template applied over it, then their arrangement.
	/// </summary>
	public sealed class SavedBoard
	{
		/// <summary>The person's own arrangement, or <see langword="null"/>.</summary>
		public DashboardBoard Own { get; init; }

		/// <summary>The template of the person's role, or <see langword="null"/>.</summary>
		public DashboardBoard Template { get; init; }

		/// <summary>Who published the template.</summary>
		public string TemplatePublishedBy { get; init; }

		/// <summary>The board's own definition with the template, then the person's arrangement, applied over it.</summary>
		/// <param name="model">The page's own board; changed in place.</param>
		/// <param name="keep">Whether an item the page no longer defines stays (one added from the catalog).</param>
		public DashboardBoard ApplyTo(DashboardBoard model, Func<DashboardItem, bool> keep)
		{
			if (this.Template != null)
				BoardMerge.Apply(model, this.Template, keep);
			if (this.Own != null)
				BoardMerge.Apply(model, this.Own, keep);
			return model;
		}

		/// <summary>What the page's status line says about where the board on show comes from.</summary>
		public string Status =>
			this.Own != null ? "This is your board, saved on the server."
			: this.Template != null ? "This is your role's starting board, published by " + this.TemplatePublishedBy + ". Change something and your own copy is saved."
			: "This is the page's own board. Change something and it's saved on the server.";
	}

	/// <summary>
	/// Keeps the boards people arrange through the data API, by person and board key: "home" for the Dashboard,
	/// each commodity or business board's own key for the others.
	/// </summary>
	public static class BoardStore
	{
		/// <summary>The key of the Dashboard's own board.</summary>
		public const string HomeKey = "home";

		/// <summary>The person's arrangement of the board and their role's template.</summary>
		public static SavedBoard Load(string key)
		{
			var dto = DataApi.Board(Key(key));
			return new SavedBoard
			{
				Own = Parse(dto?.Own),
				Template = Parse(dto?.Template),
				TemplatePublishedBy = dto?.TemplatePublishedBy,
			};
		}

		/// <summary>
		/// Keeps the person's arrangement. Content is dropped: the page's own board and the catalog supply it again by id
		/// and kind. Nothing is kept for a role that may not arrange boards.
		/// </summary>
		/// <returns>Whether it was kept.</returns>
		public static bool Save(string key, DashboardBoard board)
		{
			if (!CurrentUser.Can(Permissions.EditBoards))
				return false;

			DataApi.SaveBoard(Key(key), Serialize(board));
			return true;
		}

		/// <summary>Forgets the person's arrangement of the board: their role's template, or the page's own board, shows again.</summary>
		public static void Reset(string key) => DataApi.ResetBoard(Key(key));

		/// <summary>The roles that have a template for the board.</summary>
		public static IReadOnlyList<TemplateDto> Templates(string key) => DataApi.Templates(Key(key));

		/// <summary>Publishes the board as the starting layout of the roles.</summary>
		public static void Publish(string key, DashboardBoard board, IReadOnlyList<string> roles) =>
			DataApi.PublishTemplate(Key(key), roles, Serialize(board));

		/// <summary>Withdraws a role's template of the board.</summary>
		public static void Withdraw(string key, string role) => DataApi.WithdrawTemplate(Key(key), role);

		private static string Key(string key) => string.IsNullOrWhiteSpace(key) ? HomeKey : key.ToLowerInvariant();

		private static string Serialize(DashboardBoard b)
		{
			var board = DashboardJson.Clone(b);
			foreach (var item in board.Items)
			{
				item.Html = null;
				item.Url = null;
				item.Template = null;
			}
			return DashboardJson.Serialize(board, indented: false);
		}

		private static DashboardBoard Parse(string json)
		{
			if (string.IsNullOrWhiteSpace(json))
				return null;

			var parsed = DashboardJson.TryParse(json);
			return parsed.IsSuccess ? parsed.Value : null;
		}
	}
}
