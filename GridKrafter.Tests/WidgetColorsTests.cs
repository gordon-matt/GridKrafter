namespace GridKrafter.Tests;

public class WidgetColorsTests
{
    [Theory]
    [InlineData("blue")]
    [InlineData("Rose")]
    public void IsNamed_PaletteName_ReturnsTrue(string color)
    {
        Assert.True(WidgetColors.IsNamed(color));
    }

    [Theory]
    [InlineData("#0ea5e9")]
    [InlineData("#fff")]
    [InlineData("rgb(14 165 233)")]
    [InlineData("oklch(0.7 0.15 230)")]
    [InlineData("hsl(200, 80%, 40%)")]
    public void IsCustom_CssColour_ReturnsTrue(string color)
    {
        Assert.True(WidgetColors.IsCustom(color));
    }

    [Theory]
    [InlineData("blue")]
    [InlineData("red; background: url(x)")]
    [InlineData("#fff;color:red")]
    [InlineData("expression(alert(1))")]
    [InlineData("rgb(1,2,3)} body {display:none")]
    [InlineData("")]
    [InlineData(null)]
    public void IsCustom_NameOrUnsafe_ReturnsFalse(string? color)
    {
        Assert.False(WidgetColors.IsCustom(color));
    }
}
