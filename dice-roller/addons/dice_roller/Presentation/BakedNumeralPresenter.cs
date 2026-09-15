using DiceRoller.Core;
using Godot;

namespace DiceRoller.Presentation;

/// <summary>
/// Numerals are already baked into the mesh textures. Bind/Apply/Unbind are no-ops.
/// </summary>
public sealed class BakedNumeralPresenter : IFacePresenter
{
    public void Bind(MeshInstance3D mesh, DieFaceMap map)
    {
    }

    public void Apply(FaceLayout layout)
    {
    }

    public void Unbind()
    {
    }
}
