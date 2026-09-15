using System;
using System.Collections.Generic;
using DiceRoller.Core;
using Godot;

#nullable enable

namespace DiceRoller.Settings;

public partial class SettingsStore : Node
{
    const string Path = "user://settings.json";

    readonly Dictionary<string, object> _values = new()
    {
        ["gravity"] = 4f,
        ["allow_locked_move"] = false,
        ["invert_scroll"] = false,
        ["invert_resize"] = false,
    };

    public event Action<string, object>? Changed;

    public override void _Ready() => Load();

    public T Get<T>(string key, T fallback)
    {
        if (!_values.TryGetValue(key, out var value))
            return fallback;
        if (value is T typed)
            return typed;
        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch (Exception)
        {
            return fallback;
        }
    }

    public void Set(string key, object value)
    {
        _values[key] = value;
        Changed?.Invoke(key, value);
        Save();
    }

    public void Load()
    {
        if (!FileAccess.FileExists(Path))
            return;

        using var file = FileAccess.Open(Path, FileAccess.ModeFlags.Read);
        if (file is null)
            return;

        foreach (var (key, value) in SettingsCodec.Deserialize(file.GetAsText()))
            _values[key] = value;
    }

    public void Save()
    {
        using var file = FileAccess.Open(Path, FileAccess.ModeFlags.Write);
        file?.StoreString(SettingsCodec.Serialize(_values));
    }
}
