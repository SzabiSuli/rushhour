namespace rushhour.src.Nodes.Nodes3D;

using Godot;

public partial class VertexDrawer : MultiMeshInstance3D {
    public const int MaxInstances = 20000;
    public const float SpriteSize = 1f;

    public override void _Ready() {
        var quadMesh = new QuadMesh {
            Size = new Vector2(SpriteSize, SpriteSize)
        };

        // No BillboardMode here - we build the facing transform manually so
        // that per-instance scale is not overwritten by the billboard pass.
        MaterialOverride = new StandardMaterial3D() {
            AlbedoColor = Colors.White,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled, // visible from both sides
            AlbedoTexture = ResourceLoader.Load<Texture2D>("res://assets/circle.png"),
            VertexColorUseAsAlbedo = true,
        };

        Multimesh = new MultiMesh {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = true,
            InstanceCount = MaxInstances,
            VisibleInstanceCount = 0,
            Mesh = quadMesh
        };
    }

    // Updates all vertex instance transforms, colors, and scales.
    // Manually builds a camera-facing (billboard) basis per instance so that
    // scale from GetScale() is correctly preserved in the final transform.
    public void UpdateVisuals() {
        int count = Vertex.Dict.Count;
        Multimesh.VisibleInstanceCount = count;

        // Get the camera-facing basis once per frame.
        // The camera's basis columns are: X=right, Y=up, Z=back (towards camera).
        // A quad facing the camera needs its local X/Y to align with camera right/up.
        Basis cameraBasis = Camera3d.Instance.GlobalTransform.Basis;

        int i = 0;
        foreach (Vertex v in Vertex.Dict.Values) {
            float scale = v.GetScale();
            // Scale the facing basis uniformly - this is what billboard mode prevents us doing
            Basis scaledBasis = cameraBasis.Scaled(Vector3.One * scale);
            Multimesh.SetInstanceTransform(i, new Transform3D(scaledBasis, v.Position));
            Multimesh.SetInstanceColor(i, v.GetColor());
            i++;
        }
    }
}
