using System.Globalization;

namespace GridKrafter.Demo.Mvc.Models;

/// <summary>An order in the recent orders widget.</summary>
public sealed record Order(int Number, string Customer, DateTimeOffset Placed, string Status, decimal Total);

/// <summary>A gauge in the server load widget.</summary>
public sealed record Meter(string Name, string Icon, int Value);

/// <summary>An entry in the activity widget.</summary>
public sealed record ActivityEntry(string Icon, string Color, string Text, DateTimeOffset At);

/// <summary>
/// Made-up data for the partial views. Orders arrive every couple of minutes, so a refresh
/// shows something new without any state on the server.
/// </summary>
public static class DemoData
{
    private const int ORDER_EVERY_MINUTES = 2;
    private const int FIRST_ORDER = 10_000;

    private static readonly string[] Customers =
    [
        "Ava Thompson", "Liam Chen", "Noah Patel", "Mia Rossi", "Lucas Müller", "Emma Dubois",
        "Oliver Smith", "Sofia García", "Ethan Kim", "Isla Murphy", "Leo Novak", "Zara Ahmed",
    ];

    private static readonly string[] Statuses = ["Shipped", "Delivered", "Delivered", "Shipped", "Delivered", "Refunded"];

    /// <summary>Gets the culture the figures are formatted in.</summary>
    public static CultureInfo Culture { get; } = CultureInfo.GetCultureInfo("en-US");

    /// <summary>The latest orders, newest first.</summary>
    /// <param name="count">How many.</param>
    /// <returns>The orders.</returns>
    public static IReadOnlyList<Order> RecentOrders(int count = 8)
    {
        var now = DateTimeOffset.UtcNow;
        long slot = now.ToUnixTimeSeconds() / (ORDER_EVERY_MINUTES * 60);
        return [.. Enumerable.Range(0, count).Select(i =>
        {
            long number = slot - i;
            return new Order(
                FIRST_ORDER + (int)(number % 90_000),
                Customers[(int)(number * 7 % Customers.Length)],
                DateTimeOffset.FromUnixTimeSeconds(number * ORDER_EVERY_MINUTES * 60),
                i < 2 ? "Pending" : Statuses[(int)(number * 5 % Statuses.Length)],
                20 + (number * 37 % 480) + (number * 13 % 100 / 100m));
        })];
    }

    /// <summary>Server figures that wander a little on each request.</summary>
    /// <returns>The meters.</returns>
    public static IReadOnlyList<Meter> ServerLoad() =>
    [
        new("CPU", "cpu", Random.Shared.Next(18, 72)),
        new("Memory", "layers", Random.Shared.Next(58, 76)),
        new("Disk", "box", Random.Shared.Next(80, 86)),
        new("Network", "activity", Random.Shared.Next(6, 64)),
    ];

    /// <summary>What happened lately, newest first.</summary>
    /// <returns>The entries.</returns>
    public static IReadOnlyList<ActivityEntry> Activity()
    {
        var now = DateTimeOffset.UtcNow;
        var latest = RecentOrders(1)[0];
        return
        [
            new("cart", "green", $"New order #{latest.Number} from {latest.Customer}", latest.Placed),
            new("users", "blue", "Liam Chen joined the team", now.AddMinutes(-9)),
            new("alert", "red", "Disk usage passed 80% on db-02", now.AddMinutes(-16)),
            new("message", "violet", "Mia commented on the release notes", now.AddMinutes(-23)),
            new("zap", "amber", "Deployed version 4.12.1", now.AddHours(-2)),
        ];
    }

    /// <summary>A short, culture-neutral age, such as <c>5 min ago</c>.</summary>
    /// <param name="at">When it happened.</param>
    /// <returns>The age.</returns>
    public static string Ago(DateTimeOffset at)
    {
        var age = DateTimeOffset.UtcNow - at;
        return age.TotalMinutes < 1 ? "just now"
            : age.TotalHours < 1 ? $"{(int)age.TotalMinutes} min ago"
            : $"{(int)age.TotalHours} h ago";
    }

    /// <summary>The badge colour for an order status.</summary>
    /// <param name="status">The status.</param>
    /// <returns>A GridKrafter colour class.</returns>
    public static string ToneOf(string status) => status switch
    {
        "Pending" => "gk-color-amber",
        "Shipped" => "gk-color-blue",
        "Delivered" => "gk-color-green",
        _ => "gk-color-red",
    };

    /// <summary>The bar colour for a meter.</summary>
    /// <param name="value">The value, as a percentage.</param>
    /// <returns>A GridKrafter colour class.</returns>
    public static string ToneOf(int value) => value switch
    {
        >= 85 => "gk-color-red",
        >= 65 => "gk-color-amber",
        _ => "gk-color-green",
    };
}
