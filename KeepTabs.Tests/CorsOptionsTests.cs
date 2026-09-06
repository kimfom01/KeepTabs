using KeepTabs.Extensions;

namespace KeepTabs.Tests;

public sealed class CorsOptionsTests
{
    [Theory]
    [InlineData("https://app.example.com", "https://app.example.com")]
    [InlineData("https://a.example.com,https://b.example.com", "https://a.example.com|https://b.example.com")]
    [InlineData("  https://a.example.com , https://b.example.com/ ", "https://a.example.com|https://b.example.com")]
    [InlineData("https://a.example.com,https://a.example.com,HTTPS://A.EXAMPLE.COM", "https://a.example.com")]
    public void AllowedOriginsParseFromCommaSeparatedValues(string raw, string expectedPipeJoined)
    {
        Assert.Equal(expectedPipeJoined.Split('|'), CorsOptions.ParseAllowedOrigins(raw));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(",,,")]
    public void AllowedOriginsFallBackToDefaultWhenUnset(string? raw)
    {
        Assert.Equal(CorsOptions.DefaultAllowedOrigins, CorsOptions.ParseAllowedOrigins(raw));
    }

    [Fact]
    public void AllowedOriginsNormalizeArraySyntax()
    {
        var normalized = CorsOptions.NormalizeOrigins(
            ["https://a.example.com/", " https://b.example.com ", null, "", "https://a.example.com"]);

        Assert.Equal(["https://a.example.com", "https://b.example.com"], normalized);
    }
}
