using System;
using DiceRoller.Core;
using Godot;

#nullable enable

namespace DiceRoller.Physics;

[GlobalClass]
public partial class DiceInteractionController : Node3D
{
    const float MaxThrowSpeed = 80f;
    const float MinDraggingHeight = 1f;
    const float RotateStep = Mathf.Pi / 32f;

    Vector3 _mousePosition;
    DieBody? _hoveredDie;
    float _draggingHeight = MinDraggingHeight;
    int _rotationDirection;
    RayCast3D _mouseRay = null!;
    Timer _rotateTicks = null!;
    Area3D _grabArea = null!;
    CollisionShape3D _grabShape = null!;
    Area3D _minDistance = null!;
    Area3D _maxDistance = null!;
    Action? _hoveredMouseExited;

    public override void _Ready()
    {
        _mouseRay = new RayCast3D
        {
            CollisionMask = 2,
            Enabled = true,
            DebugShapeThickness = 0,
        };
        AddChild(_mouseRay);

        _rotateTicks = GetNode<Timer>("RotateTicks");
        _rotateTicks.Timeout += OnRotateTicksTimeout;

        _grabArea = GetNode<Area3D>("GrabArea");
        _grabShape = GetNode<CollisionShape3D>("GrabArea/CollisionShape");
        _minDistance = GetNode<Area3D>("MinDistance");
        _maxDistance = GetNode<Area3D>("MinDistance/MaxDistance");
        _grabArea.GravityPoint = true;
        _grabArea.GravityPointCenter = Vector3.Zero;
        _grabArea.Gravity = 100f;
        _grabArea.LinearDamp = 10f;
        _grabArea.AngularDamp = 20f;
        SetGrabOverride(false);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_hoveredDie is not null && !GodotObject.IsInstanceValid(_hoveredDie))
            _hoveredDie = null;

        if (_hoveredDie is not null && _hoveredDie.Interaction == InteractionState.Dragged)
        {
            SetCursor(dragged: true);
            MoveDieToMouse(_hoveredDie);
        }

        var camera = GetViewport().GetCamera3D();
        if (camera is null)
            return;

        var viewportMouse = GetViewport().GetMousePosition();
        var rayOrigin = camera.ProjectRayOrigin(viewportMouse);
        var rayDirection = camera.ProjectRayNormal(viewportMouse);
        _mouseRay.GlobalPosition = rayOrigin;
        _mouseRay.TargetPosition = _mouseRay.ToLocal(
            rayOrigin + rayDirection * (rayOrigin.DistanceTo(Vector3.Zero) + 10f));
        _mouseRay.ForceRaycastUpdate();

        _mousePosition = DragMath.IntersectYPlane(rayOrigin, rayDirection, _draggingHeight);

        var hitDie = _mouseRay.GetCollider() as DieBody;
        if (hitDie is not null)
        {
            if (_hoveredDie is null || _hoveredDie.Interaction != InteractionState.Dragged)
                SetHoveredDie(hitDie);
            return;
        }

        if (_hoveredDie is not null && _hoveredDie.Interaction != InteractionState.Dragged)
            SetHoveredDie(null);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton released &&
            released.ButtonIndex == MouseButton.Left &&
            !released.Pressed)
        {
            SetGrabOverride(false);
        }

        if (@event is InputEventMouseMotion)
            _grabArea.Position = _mousePosition;

        if (_hoveredDie is null || !GodotObject.IsInstanceValid(_hoveredDie))
        {
            if (@event is InputEventMouseButton empty &&
                empty.ButtonIndex == MouseButton.Left &&
                empty.Pressed)
            {
                _draggingHeight = DragMath.GrabHeightFromRadius(GrabRadius());
                _grabArea.Position = _mousePosition;
                SetGrabOverride(true);
            }

            return;
        }

        if (@event is InputEventMouseMotion && _hoveredDie.Interaction == InteractionState.Clicked)
            BeginDrag(_hoveredDie);

        HandleMouseButton(@event);
        HandleKey(@event);
    }

    void HandleMouseButton(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouse)
            return;

        if (mouse.ButtonIndex == MouseButton.Left)
        {
            if (mouse.DoubleClick)
            {
                ThrowDieRandomly(_hoveredDie!);
                GetViewport().SetInputAsHandled();
            }
            else if (mouse.Pressed && _hoveredDie!.Interaction == InteractionState.Hovered)
            {
                _hoveredDie.Interaction = InteractionState.Clicked;
                GetViewport().SetInputAsHandled();
            }
            else if (!mouse.Pressed && _hoveredDie!.Interaction == InteractionState.Dragged)
            {
                _hoveredDie.Interaction = InteractionState.Hovered;
                ResetForces(_hoveredDie);
                ThrowDieInDragDirection(_hoveredDie);
                GetViewport().SetInputAsHandled();
            }
        }

        if (mouse.ButtonIndex == MouseButton.Right && mouse.Pressed)
        {
            if (_hoveredDie!.Interaction == InteractionState.Hovered)
            {
                _hoveredDie.SetLocked(!_hoveredDie.Locked);
                GetViewport().SetInputAsHandled();
            }
            else if (_hoveredDie.Interaction == InteractionState.Dragged)
            {
                _hoveredDie.Interaction = InteractionState.Hovered;
                PutDieDown(_hoveredDie);
                GetViewport().SetInputAsHandled();
            }
        }
    }

    void HandleKey(InputEvent @event)
    {
        if (@event is not InputEventKey)
            return;

        var die = _hoveredDie!;
        if (die.Interaction == InteractionState.Dragged)
        {
            if (@event.IsActionPressed("left"))
            {
                _rotationDirection = -1;
                RotateDie();
                _rotateTicks.Start();
                GetViewport().SetInputAsHandled();
            }
            else if (@event.IsActionPressed("right"))
            {
                _rotationDirection = 1;
                RotateDie();
                _rotateTicks.Start();
                GetViewport().SetInputAsHandled();
            }
            else if (@event.IsActionReleased("left") || @event.IsActionReleased("right"))
            {
                _rotationDirection = 0;
                _rotateTicks.Stop();
                _rotateTicks.WaitTime = 0.2;
                GetViewport().SetInputAsHandled();
            }
        }

        if (@event.IsActionPressed("down"))
        {
            if (die.Interaction == InteractionState.Hovered)
            {
                die.SetLocked(!die.Locked);
                GetViewport().SetInputAsHandled();
            }
            else if (die.Interaction == InteractionState.Dragged)
            {
                die.Interaction = InteractionState.Hovered;
                PutDieDown(die);
                GetViewport().SetInputAsHandled();
            }
        }

        if (@event.IsActionPressed("up"))
        {
            die.SetLocked(false);
            die.Interaction = InteractionState.None;
            _draggingHeight = MinDraggingHeight;
            ThrowDieRandomly(die);
            GetViewport().SetInputAsHandled();
        }

        if (@event.IsActionPressed("delete"))
        {
            die.Die();
            SetHoveredDie(null);
            GetViewport().SetInputAsHandled();
        }
    }

    void BeginDrag(DieBody die)
    {
        // STATIC frozen dice ignore LinearVelocity; unlock before drag/throw.
        die.SetLocked(false);
        die.Interaction = InteractionState.Dragged;
    }

    void ThrowDieInDragDirection(DieBody die)
    {
        var delta = _mousePosition - die.GlobalPosition;
        var raw = delta * new Vector3(100f, 0f, 100f);
        die.LinearVelocity = DragMath.ClampHorizontalThrow(delta, MaxThrowSpeed);
        die.AngularVelocity = -raw.Cross(Vector3.Up);
    }

    void ThrowDieRandomly(DieBody die)
    {
        die.LinearVelocity = new Vector3(
            -1 + (int)(GD.Randi() % 3),
            20,
            -1 + (int)(GD.Randi() % 3));
        die.AngularVelocity = new Vector3(
            -15 + (int)(GD.Randi() % 30),
            -15 + (int)(GD.Randi() % 30),
            -15 + (int)(GD.Randi() % 30));
    }

    void SetHoveredDie(DieBody? die)
    {
        if (die == _hoveredDie)
            return;

        DisconnectHoveredMouseExited();

        if (die is not null)
        {
            die.Interaction = InteractionState.Hovered;
            _hoveredMouseExited = () => OnDieMouseExited(die);
            die.MouseExited += _hoveredMouseExited;
            _hoveredDie = die;
            SetCursor(dragged: false);
        }
        else
        {
            _hoveredDie = null;
            Input.SetDefaultCursorShape(Input.CursorShape.Arrow);
        }
    }

    void DisconnectHoveredMouseExited()
    {
        if (_hoveredDie is null || _hoveredMouseExited is null)
            return;
        if (GodotObject.IsInstanceValid(_hoveredDie))
            _hoveredDie.MouseExited -= _hoveredMouseExited;
        _hoveredMouseExited = null;
    }

    void MoveDieToMouse(DieBody die)
    {
        ResetForces(die);
        var dragPosition = new Vector3(_mousePosition.X, _draggingHeight, _mousePosition.Z);
        dragPosition = AvoidObstacles(dragPosition);
        die.GlobalPosition = dragPosition;
        RotateRolledSideUp(die);
    }

    Vector3 AvoidObstacles(Vector3 dragPosition)
    {
        _minDistance.Position = dragPosition;
        int minOverlaps = _minDistance.GetOverlappingBodies().Count;
        int maxOverlaps = _maxDistance.GetOverlappingBodies().Count;
        _draggingHeight = DragMath.NextDraggingHeight(
            _draggingHeight, minOverlaps, maxOverlaps, MinDraggingHeight);
        return new Vector3(_mousePosition.X, _draggingHeight, _mousePosition.Z);
    }

    void RotateRolledSideUp(DieBody die)
    {
        var up = die.FaceMap.ReadUpSlot(die.GlobalTransform);
        if (!up.IsValid)
            return;

        var local = die.FaceMap[up.Slot];
        var sideDir = (die.ToGlobal(local) - die.GlobalPosition).Normalized();
        var rotation = DragMath.RotationToAlignUp(sideDir);
        if (rotation is not { } axisAngle)
            return;

        die.Rotate(axisAngle.Axis, axisAngle.Angle);
    }

    void PutDieDown(DieBody die)
    {
        ResetForces(die);
        _draggingHeight = MinDraggingHeight;
        float y = die.GetOriginToLowestYHeight();

        var ray = new RayCast3D { CollisionMask = 2 };
        AddChild(ray);
        ray.GlobalPosition = die.GlobalPosition;
        ray.TargetPosition = Vector3.Down * die.GlobalPosition.DistanceTo(Vector3.Zero);
        ray.AddException(die);
        ray.ForceRaycastUpdate();

        var hitDie = ray.GetCollider() as DieBody;
        float? highest = hitDie is null ? null : hitDie.GetHighestYPositionGlobal();
        y = DragMath.PutDownOriginY(y, highest);
        ray.QueueFree();

        die.GlobalPosition = new Vector3(_mousePosition.X, y, _mousePosition.Z);
        die.SetLocked(true);
    }

    void ResetForces(DieBody die)
    {
        die.Sleeping = false;
        die.LinearVelocity = Vector3.Zero;
        die.AngularVelocity = Vector3.Zero;
    }

    void OnDieMouseExited(DieBody die)
    {
        if (_hoveredDie is null || die != _hoveredDie)
            return;
        if (die.Interaction is InteractionState.Dragged or InteractionState.Clicked)
            return;

        die.Interaction = InteractionState.None;
        SetHoveredDie(null);
    }

    void OnRotateTicksTimeout()
    {
        if (_hoveredDie is null || !GodotObject.IsInstanceValid(_hoveredDie))
            return;
        RotateDie();
        if (_rotateTicks.WaitTime > 0.06)
            _rotateTicks.WaitTime -= 0.02;
    }

    void RotateDie()
    {
        if (_hoveredDie is null || !GodotObject.IsInstanceValid(_hoveredDie))
            return;
        _hoveredDie.Rotate(Vector3.Up, RotateStep * _rotationDirection);
    }

    static void SetCursor(bool dragged)
    {
        bool web = OS.HasFeature("web");
        if (dragged)
        {
            Input.SetDefaultCursorShape(web ? Input.CursorShape.CanDrop : Input.CursorShape.Drag);
            return;
        }

        Input.SetDefaultCursorShape(web ? Input.CursorShape.Drag : Input.CursorShape.CanDrop);
    }

    void SetGrabOverride(bool on)
    {
        var mode = on ? Area3D.SpaceOverride.Replace : Area3D.SpaceOverride.Disabled;
        _grabArea.GravitySpaceOverride = mode;
        _grabArea.LinearDampSpaceOverride = mode;
        _grabArea.AngularDampSpaceOverride = mode;
    }

    float GrabRadius() =>
        _grabShape.Shape is SphereShape3D sphere ? sphere.Radius : 8f;
}
