using System;
using System.Collections.Generic;
using Godot;

#nullable enable

namespace DiceRoller.Settings;

public partial class SettingsStore : Node
{
    readonly Dictionary<string, object> _values = new()
    {
        ["gravity"] = 4f,
        ["allow_locked_move"] = false,
        ["invert_scroll"] = false,
        ["invert_resize"] = false,
    };

    public event Action<string, object>? Changed;

    public T Get<T>(string key, T fallback)
    {
        if (_values.TryGetValue(key, out var value) && value is T typed)
            return typed;
        return fallback;
    }

    public void Set(string key, object value)
    {
        _values[key] = value;
        Changed?.Invoke(key, value);
    }
}
