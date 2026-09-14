using DiceRoller.Core;

namespace DiceRoller.Presentation;

/// <summary>
/// Numerals are already baked into the mesh textures. Apply is a no-op hook for later presenters.
/// </summary>
public sealed class BakedNumeralPresenter : IFacePresenter
{
    public void Apply(FaceLayout layout)
    {
    }
}
