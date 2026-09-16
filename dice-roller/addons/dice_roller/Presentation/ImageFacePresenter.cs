using System.Collections.Generic;
using DiceRoller.Core;
using Godot;

#nullable enable

namespace DiceRoller.Presentation;

/// <summary>
/// Covers each textured slot with an opaque quad. Optional <see cref="HideBakedNumerals"/>
/// drops the baked-numeral surface so pips cannot show around the image.
/// </summary>
public sealed class ImageFacePresenter : IFacePresenter
{
    const float OutwardBias = 0.05f;
    const int BodySurface = 0;

    readonly Dictionary<FaceSlotId, MeshInstance3D> _faces = new();
    readonly Dictionary<string, Texture2D> _textures = new();
    readonly QuadMesh _quad = new();
    readonly MeshDataTool _meshTool = new();

    MeshInstance3D? _mesh;
    DieFaceMap? _map;
    Mesh? _sourceMesh;
    ArrayMesh? _bodyOnlyMesh;

    public bool HideBakedNumerals { get; set; }

    public void Bind(MeshInstance3D mesh, DieFaceMap map)
    {
        if (_mesh is not null && _mesh != mesh)
            RestoreSourceMesh();

        _mesh = mesh;
        _map = map;
        if (HideBakedNumerals)
            SetNumeralsHidden(true);
    }

    public void Apply(FaceLayout layout)
    {
        if (_mesh is null || _map is null)
            return;

        if (HideBakedNumerals)
            SetNumeralsHidden(true);

        float width = FaceWidth(_map.Hull, _mesh.GetAabb());
        _quad.Size = new Vector2(width, width);

        foreach (var (slot, sample) in _map.Samples)
        {
            var content = layout[slot];
            if (string.IsNullOrEmpty(content.TextureKey))
            {
                RemoveFace(slot);
                continue;
            }

            TryShow(slot, sample, content.TextureKey);
        }
    }

    public void Unbind()
    {
        foreach (var slot in new List<FaceSlotId>(_faces.Keys))
            RemoveFace(slot);
        _faces.Clear();
        SetNumeralsHidden(false);
        _mesh = null;
        _map = null;
    }

    bool TryShow(FaceSlotId slot, Vector3 sample, string textureKey)
    {
        if (!textureKey.StartsWith("res://"))
        {
            GD.PushWarning($"ImageFacePresenter: TextureKey must be a res:// path, got '{textureKey}'.");
            RemoveFace(slot);
            return false;
        }

        var texture = LoadTexture(textureKey);
        if (texture is null)
        {
            RemoveFace(slot);
            return false;
        }

        var face = GetOrCreateFace(slot);
        if (face.MaterialOverride is not StandardMaterial3D material)
        {
            material = CreateFaceMaterial();
            face.MaterialOverride = material;
        }

        material.AlbedoTexture = texture;
        face.Transform = PoseForQuad(sample, _mesh!.GetAabb());
        return true;
    }

    MeshInstance3D GetOrCreateFace(FaceSlotId slot)
    {
        if (_faces.TryGetValue(slot, out var existing) && GodotObject.IsInstanceValid(existing))
            return existing;

        var face = new MeshInstance3D
        {
            Name = $"Face_{slot.Value}",
            Mesh = _quad,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            SortingOffset = 1f,
        };
        _mesh!.AddChild(face);
        _faces[slot] = face;
        return face;
    }

    void RemoveFace(FaceSlotId slot)
    {
        if (!_faces.Remove(slot, out var face))
            return;
        if (GodotObject.IsInstanceValid(face))
            face.QueueFree();
    }

    void SetNumeralsHidden(bool hide)
    {
        if (hide)
            StripNumeralsSurface();
        else
            RestoreSourceMesh();
    }

    void StripNumeralsSurface()
    {
        if (_mesh?.Mesh is null)
            return;
        if (_bodyOnlyMesh is not null && _mesh.Mesh == _bodyOnlyMesh)
            return;

        _sourceMesh ??= _mesh.Mesh;
        if (_sourceMesh is not ArrayMesh source)
        {
            GD.PushError($"ImageFacePresenter: cannot strip numerals from {_sourceMesh.GetType().Name}");
            return;
        }

        if (source.GetSurfaceCount() <= BodySurface)
            return;

        Error err = _meshTool.CreateFromSurface(source, BodySurface);
        if (err != Error.Ok)
        {
            GD.PushError($"ImageFacePresenter: CreateFromSurface failed ({err}).");
            return;
        }

        var stripped = new ArrayMesh();
        _meshTool.CommitToSurface(stripped);
        var material = source.SurfaceGetMaterial(BodySurface);
        if (material is not null)
            stripped.SurfaceSetMaterial(0, material);

        _bodyOnlyMesh = stripped;
        _mesh.Mesh = stripped;
    }

    void RestoreSourceMesh()
    {
        if (_mesh is not null && _sourceMesh is not null)
            _mesh.Mesh = _sourceMesh;
        _bodyOnlyMesh = null;
        _sourceMesh = null;
    }

    static StandardMaterial3D CreateFaceMaterial() => new()
    {
        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        Transparency = BaseMaterial3D.TransparencyEnum.Disabled,
        CullMode = BaseMaterial3D.CullModeEnum.Disabled,
        TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmaps,
    };

    Texture2D? LoadTexture(string path)
    {
        if (_textures.TryGetValue(path, out var cached))
            return cached;

        var texture = GD.Load<Texture2D>(path);
        if (texture is null)
        {
            GD.PushWarning($"ImageFacePresenter: could not load TextureKey '{path}'.");
            return null;
        }

        _textures[path] = texture;
        return texture;
    }

    static Transform3D PoseForQuad(Vector3 sample, Aabb aabb)
    {
        var normal = sample.LengthSquared() < 1e-8f ? Vector3.Up : sample.Normalized();
        float radius = aabb.GetLongestAxisSize() * 0.5f;
        var origin = aabb.GetCenter() + normal * (radius + OutwardBias);

        // QuadMesh faces +Z, so +Z is outward.
        var z = normal;
        var reference = Mathf.Abs(normal.Dot(Vector3.Up)) > 0.95f ? Vector3.Forward : Vector3.Up;
        var x = reference.Cross(z);
        if (x.LengthSquared() < 1e-8f)
            x = Vector3.Right;
        x = x.Normalized();
        var y = z.Cross(x).Normalized();
        return new Transform3D(new Basis(x, y, z), origin);
    }

    static float FaceWidth(HullKind hull, Aabb aabb)
    {
        float longest = aabb.GetLongestAxisSize();
        float factor = hull switch
        {
            HullKind.D4 => 0.85f,
            HullKind.D6 => 0.98f,
            HullKind.D8 => 0.7f,
            HullKind.D10 => 0.55f,
            HullKind.D12 => 0.55f,
            HullKind.D20 => 0.42f,
            _ => 0.7f,
        };
        return longest * factor;
    }
}
