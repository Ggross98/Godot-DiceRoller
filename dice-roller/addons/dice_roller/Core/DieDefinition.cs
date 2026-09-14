namespace DiceRoller.Core;

public sealed class DieDefinition
{
    public required HullKind Hull { get; init; }
    public required FaceLayout Layout { get; init; }

    public DieDefinition CloneForSpawn() => new()
    {
        Hull = Hull,
        Layout = Layout.Clone(),
    };
}
