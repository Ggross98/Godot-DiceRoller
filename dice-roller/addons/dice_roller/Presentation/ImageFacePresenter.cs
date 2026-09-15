using System.Collections.Generic;
using DiceRoller.Core;
using Godot;

#nullable enable

namespace DiceRoller.Presentation;

/// <summary>
/// Overlays a Decal per slot whose <see cref="FaceContent.TextureKey"/> is a <c>res://</c> texture.
/// Does not rewrite <see cref="DieFaceMap"/> or mesh UVs (baked numerals stay on the default glTF).
/// </summary>
public sealed class ImageFacePresenter : IFacePresenter
{
    const float ProjectionDepth = 0.16f;
    const float OutwardBias = 0.03f;
    const int NumeralsSurface = 1;

    readonly Dictionary<FaceSlotId, Decal> _decals = new();
    readonly Dictionary<string, Texture2D> _textures = new();
    readonly StandardMaterial3D _hiddenNumerals = new()
    {
        Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        AlbedoColor = new Color(0, 0, 0, 0),
    };

    MeshInstance3D? _mesh;
    DieFaceMap? _map;
    bool _hidNumerals;

    public bool HideBakedNumerals { get; set; }

    public void Bind(MeshInstance3D mesh, DieFaceMap map)
    {
        _mesh = mesh;
        _map = map;
    }

    public void Apply(FaceLayout layout)
    {
        if (_mesh is null || _map is null)
            return;

        bool anyTexture = false;
        foreach (var (slot, sample) in _map.Samples)
        {
            var content = layout[slot];
            if (string.IsNullOrEmpty(content.TextureKey))
            {
                RemoveDecal(slot);
                continue;
            }

            if (TryShow(slot, sample, content.TextureKey))
                anyTexture = true;
        }

        SetNumeralsHidden(HideBakedNumerals && anyTexture);
    }

    public void Unbind()
    {
        foreach (var slot in new List<FaceSlotId>(_decals.Keys))
            RemoveDecal(slot);
        _decals.Clear();
        SetNumeralsHidden(false);
        _mesh = null;
        _map = null;
    }

    bool TryShow(FaceSlotId slot, Vector3 sample, string textureKey)
    {
        if (!textureKey.StartsWith("res://"))
        {
            GD.PushWarning($"ImageFacePresenter: TextureKey must be a res:// path, got '{textureKey}'.");
            RemoveDecal(slot);
            return false;
        }

        var texture = LoadTexture(textureKey);
        if (texture is null)
        {
            RemoveDecal(slot);
            return false;
        }

        var decal = GetOrCreateDecal(slot);
        decal.TextureAlbedo = texture;
        decal.Transform = PoseForSample(sample, _mesh!.GetAabb());
        float width = FaceWidth(_map!.Hull, _mesh.GetAabb());
        decal.Size = new Vector3(width, ProjectionDepth, width);
        return true;
    }

    Decal GetOrCreateDecal(FaceSlotId slot)
    {
        if (_decals.TryGetValue(slot, out var existing) && GodotObject.IsInstanceValid(existing))
            return existing;

        var decal = new Decal
        {
            Name = $"Face_{slot.Value}",
            NormalFade = 0.5f,
            UpperFade = 0.15f,
            LowerFade = 0.15f,
        };
        _mesh!.AddChild(decal);
        _decals[slot] = decal;
        return decal;
    }

    void RemoveDecal(FaceSlotId slot)
    {
        if (!_decals.Remove(slot, out var decal))
            return;
        if (GodotObject.IsInstanceValid(decal))
            decal.QueueFree();
    }

    void SetNumeralsHidden(bool hide)
    {
        if (_mesh is null || hide == _hidNumerals)
            return;
        _hidNumerals = hide;
        if (_mesh.Mesh is null || _mesh.Mesh.GetSurfaceCount() <= NumeralsSurface)
            return;
        _mesh.SetSurfaceOverrideMaterial(NumeralsSurface, hide ? _hiddenNumerals : null);
    }

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

    static Transform3D PoseForSample(Vector3 sample, Aabb aabb)
    {
        var normal = sample.LengthSquared() < 1e-8f ? Vector3.Up : sample.Normalized();
        float radius = aabb.Size.Length() > 0f ? aabb.GetLongestAxisSize() * 0.5f : 0.5f;
        var origin = aabb.GetCenter() + normal * (radius + OutwardBias);

        // Godot 4 Decal projects along local -Y, so +Y faces outward.
        var y = normal;
        var reference = Mathf.Abs(normal.Dot(Vector3.Up)) > 0.95f ? Vector3.Forward : Vector3.Up;
        var x = reference.Cross(y);
        if (x.LengthSquared() < 1e-8f)
            x = Vector3.Right;
        x = x.Normalized();
        var z = x.Cross(y).Normalized();
        return new Transform3D(new Basis(x, y, z), origin);
    }

    static float FaceWidth(HullKind hull, Aabb aabb)
    {
        float longest = aabb.GetLongestAxisSize();
        float factor = hull switch
        {
            HullKind.D4 => 0.7f,
            HullKind.D6 => 0.72f,
            HullKind.D8 => 0.55f,
            HullKind.D10 => 0.4f,
            HullKind.D12 => 0.42f,
            HullKind.D20 => 0.32f,
            _ => 0.5f,
        };
        return longest * factor;
    }
}
