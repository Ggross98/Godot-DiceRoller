using DiceRoller.Physics;
using DiceRoller.Settings;
using Godot;

#nullable enable

namespace DiceRoller.Presentation;

public static class DiceTheme
{
    public static void Apply(SettingsStore settings)
    {
        ApplyHtml(settings, "body_color", color =>
        {
            SetAlbedo(DiceAssets.BodyMaterial, color);
            SetAlbedo(DiceAssets.BodyLockedMaterial, color);
        });
        ApplyHtml(settings, "number_color", color =>
            SetAlbedo(DiceAssets.NumbersMaterial, color));
        ApplyHtml(settings, "border_color", color =>
        {
            if (GD.Load<Material>(DiceAssets.OutlineMaterial) is ShaderMaterial outline)
                outline.SetShaderParameter("border_color", color);
        });
    }

    static void ApplyHtml(SettingsStore settings, string key, System.Action<Color> apply)
    {
        string html = settings.Get(key, "");
        if (string.IsNullOrEmpty(html))
            return;
        apply(Color.FromHtml(html));
    }

    static void SetAlbedo(string path, Color color)
    {
        if (GD.Load<Material>(path) is StandardMaterial3D material)
            material.AlbedoColor = color;
    }
}
