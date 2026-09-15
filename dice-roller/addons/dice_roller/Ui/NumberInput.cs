using System.Linq;
using Godot;

#nullable enable

namespace DiceRoller.Ui;

[GlobalClass]
public partial class NumberInput : LineEdit
{
    bool _mouseInside;

    public override void _Ready()
    {
        if (string.IsNullOrEmpty(Text))
            Text = "1";
        Alignment = HorizontalAlignment.Center;
        MouseEntered += () => _mouseInside = true;
        MouseExited += () => _mouseInside = false;
        TextChanged += OnTextChanged;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton && !_mouseInside)
        {
            ReleaseFocus();
            return;
        }

        if (@event is InputEventKey key && key.Pressed && key.Keycode == Key.Escape)
        {
            ReleaseFocus();
            return;
        }

        if (@event is InputEventMouseButton { Pressed: true } wheel && _mouseInside)
        {
            if (wheel.ButtonIndex == MouseButton.WheelUp)
            {
                ModifyNumber(-1);
                GetViewport().SetInputAsHandled();
            }
            else if (wheel.ButtonIndex == MouseButton.WheelDown)
            {
                ModifyNumber(1);
                GetViewport().SetInputAsHandled();
            }
        }
    }

    public int GetNumber()
    {
        if (int.TryParse(Text, out int n) && n > 0)
            return n;
        return 1;
    }

    void ModifyNumber(int step)
    {
        int next = GetNumber() + step;
        if (next > 0)
            Text = next.ToString();
    }

    void OnTextChanged(string newText)
    {
        int caret = CaretColumn;
        var digits = new string(newText.ToCharArray().Where(char.IsDigit).ToArray());
        if (digits.Length > 0 && int.TryParse(digits, out int n) && n > 0)
            Text = n.ToString();
        else
            Text = "";
        CaretColumn = Mathf.Min(caret, Text.Length);
    }
}
