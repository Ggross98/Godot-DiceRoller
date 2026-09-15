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
            SetAlbedo("res://dice/materials/Body.material", color);
            SetAlbedo("res://dice/materials/BodyLocked.material", color);
        });
        ApplyHtml(settings, "number_color", color =>
            SetAlbedo("res://dice/materials/Numbers.material", color));
        ApplyHtml(settings, "border_color", color =>
        {
            if (GD.Load<Material>("res://dice/materials/outline.tres") is ShaderMaterial outline)
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
