using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GridKrafter.WisejDemo.Model
{
	/// <summary>An order in the recent orders widget.</summary>
	public sealed record Order(int Number, string Customer, DateTimeOffset Placed, string Status, decimal Total);

	/// <summary>A gauge in the server load widget.</summary>
	public sealed record Meter(string Name, string Icon, int Value);

	/// <summary>An entry in the activity widget.</summary>
	public sealed record ActivityEntry(string Icon, string Color, string Text, DateTimeOffset At);

	/// <summary>
	/// Made-up data for the widgets, the same as the MVC demo's. Orders arrive every couple of
	/// minutes, so a refresh shows something new without any state on the server.
	/// </summary>
	public static class DemoData
	{
		private const int ORDER_EVERY_MINUTES = 2;
		private const int FIRST_ORDER = 10_000;

		private static readonly string[] Customers =
		{
			"Ava Thompson", "Liam Chen", "Noah Patel", "Mia Rossi", "Lucas Müller", "Emma Dubois",
			"Oliver Smith", "Sofia García", "Ethan Kim", "Isla Murphy", "Leo Novak", "Zara Ahmed",
		};

		private static readonly string[] Statuses = { "Shipped", "Delivered", "Delivered", "Shipped", "Delivered", "Refunded" };

		/// <summary>Gets the culture the figures are formatted in.</summary>
		public static CultureInfo Culture { get; } = CultureInfo.GetCultureInfo("en-US");

		/// <summary>The latest orders, newest first.</summary>
		public static IReadOnlyList<Order> RecentOrders(int count = 8)
		{
			var now = DateTimeOffset.UtcNow;
			long slot = now.ToUnixTimeSeconds() / (ORDER_EVERY_MINUTES * 60);
			return Enumerable.Range(0, count).Select(i =>
			{
				long number = slot - i;
				return new Order(
					FIRST_ORDER + (int)(number % 90_000),
					Customers[(int)(number * 7 % Customers.Length)],
					DateTimeOffset.FromUnixTimeSeconds(number * ORDER_EVERY_MINUTES * 60),
					i < 2 ? "Pending" : Statuses[(int)(number * 5 % Statuses.Length)],
					20 + (number * 37 % 480) + (number * 13 % 100 / 100m));
			}).ToList();
		}

		/// <summary>Server figures that wander a little on each call.</summary>
		public static IReadOnlyList<Meter> ServerLoad() => new[]
		{
			new Meter("CPU", "cpu", Random.Shared.Next(18, 72)),
			new Meter("Memory", "layers", Random.Shared.Next(58, 76)),
			new Meter("Disk", "box", Random.Shared.Next(80, 86)),
			new Meter("Network", "activity", Random.Shared.Next(6, 64)),
		};

		/// <summary>What happened lately, newest first.</summary>
		public static IReadOnlyList<ActivityEntry> Activity()
		{
			var now = DateTimeOffset.UtcNow;
			var latest = RecentOrders(1)[0];
			return new[]
			{
				new ActivityEntry("cart", "green", $"New order #{latest.Number} from {latest.Customer}", latest.Placed),
				new ActivityEntry("users", "blue", "Liam Chen joined the team", now.AddMinutes(-9)),
				new ActivityEntry("alert", "red", "Disk usage passed 80% on db-02", now.AddMinutes(-16)),
				new ActivityEntry("message", "violet", "Mia commented on the release notes", now.AddMinutes(-23)),
				new ActivityEntry("zap", "amber", "Deployed version 4.12.1", now.AddHours(-2)),
			};
		}

		/// <summary>An age as the MVC demo's browser writes it, such as <c>9 minutes ago</c>.</summary>
		public static string Ago(DateTimeOffset at)
		{
			var age = DateTimeOffset.UtcNow - at;
			int minutes = (int)age.TotalMinutes, hours = (int)Math.Round(age.TotalHours);
			return minutes < 1 ? "this minute"
				: minutes < 60 ? (minutes == 1 ? "1 minute ago" : $"{minutes} minutes ago")
				: hours == 1 ? "1 hour ago" : $"{hours} hours ago";
		}

		/// <summary>The widget colour of an order status.</summary>
		public static string ToneOf(string status) => status switch
		{
			"Pending" => "amber",
			"Shipped" => "blue",
			"Delivered" => "green",
			_ => "red",
		};

		/// <summary>The widget colour of a meter.</summary>
		public static string ToneOf(int value) => value switch
		{
			>= 85 => "red",
			>= 65 => "amber",
			_ => "green",
		};

		/// <summary>The address of a Bootstrap icon.</summary>
		public static string IconUrl(string name) => "resource.wx/Wisej.Ext.BootstrapIcons/" + name + ".svg";
	}
}
