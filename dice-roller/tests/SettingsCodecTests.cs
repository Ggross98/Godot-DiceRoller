using DiceRoller.Core;
using Xunit;

namespace DiceRoller.Tests;

public class SettingsCodecTests
{
    [Fact]
    public void Roundtrip_preserves_float_bool_and_html_color()
    {
        var source = new Dictionary<string, object>
        {
            ["gravity"] = 4.5f,
            ["invert_scroll"] = true,
            ["body_color"] = "#ff00aa",
        };

        string json = SettingsCodec.Serialize(source);
        var loaded = SettingsCodec.Deserialize(json);

        Assert.Equal(4.5, Convert.ToDouble(loaded["gravity"]), 5);
        Assert.Equal(true, loaded["invert_scroll"]);
        Assert.Equal("#ff00aa", loaded["body_color"]);
    }
}
