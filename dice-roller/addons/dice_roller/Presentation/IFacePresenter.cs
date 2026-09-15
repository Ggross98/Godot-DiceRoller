using DiceRoller.Core;
using Godot;

namespace DiceRoller.Presentation;

public interface IFacePresenter
{
    void Bind(MeshInstance3D mesh, DieFaceMap map);
    void Apply(FaceLayout layout);
    void Unbind();
}
