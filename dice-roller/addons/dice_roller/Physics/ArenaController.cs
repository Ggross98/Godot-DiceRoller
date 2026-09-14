using DiceRoller.Core;
using Godot;

#nullable enable

namespace DiceRoller.Physics;

[GlobalClass]
public partial class ArenaController : Node3D
{
    [Export]
    public NodePath? ManagerPath { get; set; }

    public float AreaScale { get; private set; } = 0.2f;

    DiceManager? _manager;

    public override void _Ready()
    {
        _manager = ManagerPath is not null
            ? GetNodeOrNull<DiceManager>(ManagerPath)
            : GetNodeOrNull<DiceManager>("../DiceManager");
        Scale = Vector3.One * AreaScale;
        if (_manager is not null)
            _manager.AreaScale = AreaScale;
    }

    public void Resize(bool sizeUp, float factor = 1f)
    {
        AreaScale = ArenaMath.NextAreaScale(AreaScale, sizeUp, factor);
        Scale = Vector3.One * AreaScale;
        if (_manager is null)
            return;
        _manager.AreaScale = AreaScale;
        _manager.WakeAll();
    }
}
