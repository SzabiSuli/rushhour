namespace rushhour.src.Nodes.Nodes3D;

using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class EdgeDrawer : MultiMeshInstance3D {
    private ImmediateMesh _mesh = null!;

    public override void _Ready() {
        _mesh = new ImmediateMesh();

        var meshInstance = new MeshInstance3D {
            Mesh = _mesh,
            // VertexColorUseAsAlbedo lets us set per-edge colors via SurfaceSetColor
            MaterialOverride = new StandardMaterial3D() {
                AlbedoColor = Colors.White,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                VertexColorUseAsAlbedo = true,
            }
        };
        AddChild(meshInstance);
    }

    // Rebuilds all edge line geometry from current vertex positions and edge colors.
    // Called once per physics frame by GraphScene.
    public void UpdateVisuals() {
        _mesh.ClearSurfaces();

        IEnumerable<Edge> visableEdges = Edge.Dict.Values.Where(e => !e.Hidden);
        if (!visableEdges.Any()) return;

        _mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
        foreach (Edge edge in visableEdges) {
            Color color = edge.GetColor();
            _mesh.SurfaceSetColor(color);
            _mesh.SurfaceAddVertex(edge.From.Position);
            _mesh.SurfaceAddVertex(edge.To.Position);
        }
        _mesh.SurfaceEnd();
    }
}
