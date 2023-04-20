using Xunit;

namespace Aptabase.Data;

public class NanoIdTests
{
    [Fact]
    public void New_WithoutPreffix()
    {
        var id = NanoId.New();
        Assert.Equal(22, id.Length);
    }

    [Fact]
    public void New_WithPreffix()
    {
        var id = NanoId.New("user_");
        Assert.StartsWith("user_", id);
        Assert.Equal(22, id.Length);
    }
}