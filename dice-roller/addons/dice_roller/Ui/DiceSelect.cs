using System;
using DiceRoller.Core;
using Godot;

#nullable enable

namespace DiceRoller.Ui;

[GlobalClass]
public partial class DiceSelect : HBoxContainer
{
    [Export]
    public HullKind Hull { get; set; } = HullKind.D6;

    public event Action<HullKind, int>? AddDice;
    public event Action<HullKind>? RemoveDice;

    TextureRect _icon = null!;
    NumberInput _amount = null!;

    public override void _Ready()
    {
        _icon = GetNode<TextureRect>("Icon");
        _amount = GetNode<NumberInput>("NumberInput");
        GetNode<Button>("Add").Pressed += () => AddDice?.Invoke(Hull, _amount.GetNumber());
        GetNode<Button>("Clear").Pressed += () => RemoveDice?.Invoke(Hull);
        _amount.TextSubmitted += _ => AddDice?.Invoke(Hull, _amount.GetNumber());
        ApplyHull();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("clear") && GetViewport().GuiGetFocusOwner() == _amount)
        {
            RemoveDice?.Invoke(Hull);
            GetViewport().SetInputAsHandled();
        }
    }

    void ApplyHull()
    {
        _icon.Texture = HullUi.Icon(Hull);
        _icon.TooltipText = $"Dice type ({HullUi.TypeName(Hull)})";
    }
}
