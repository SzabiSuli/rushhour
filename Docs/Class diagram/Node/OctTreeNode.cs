namespace rushhour.src.Nodes.Nodes3D;

using System.Collections.Generic;
using System.Linq;
using Godot;

// A node in the Barnes-Hut OctTree, representing a cubic region of 3D space.
public class OctTreeNode {
    // Each node must be either a:
    //      Leaf - Body contains a Vertex, Children are all null
    //      Node - Body is null, Children has at least one non null nodes


    // Barnes-Hut approximation parameter. Higher values = faster but less accurate.
    // Typical range: 0.5 (accurate) to 1.5 (fast). 1 is a good default.
    public const float theta = 1f;

    public Vector3 BoundsMin;

    public Vector3 BoundsMax;

    // 8 children, one per octant. Null if the octant is empty or this is a leaf.
    // Octant indexing: bit 0 = X axis, bit 1 = Y axis, bit 2 = Z axis.
    // 0 = smaller than midpoint, 1 = greater than midpoint for each axis.
    public OctTreeNode?[] Children = new OctTreeNode?[8];

    public Vector3 CenterOfMass;

    public int Count;

    public Vertex? Body;

    public bool IsLeaf => Body != null;

    // Construct methods
    // Constructor is private because we need to insert a node after initialising
    private OctTreeNode(Vector3 boundsMin, Vector3 boundsMax) {
        BoundsMin = boundsMin;
        BoundsMax = boundsMax;
        CenterOfMass = Vector3.Zero;
        Count = 0;
        Body = null;
    }

    public static OctTreeNode? Build(IEnumerable<Vertex> vertices) {
        // Don't build a tree if no vertices are passed through
        if (!vertices.Any()) return null;

        // Compute bounding box enclosing all vertices with padding
        Vector3 min = Vector3.Inf;
        Vector3 max = -Vector3.Inf;

        Vector3 sum = Vector3.Zero;

        foreach (Vertex vertex in vertices) {
            Vector3 pos = vertex.Position;
            min = min.Min(pos);
            max = max.Max(pos);
            sum += pos;
        }
        
        // Make the center the center of mass to make a balanced Tree
        Vector3 center = sum / vertices.Count();

        // Make the bounding box cubic (equal side lengths) for uniform subdivision
        float maxDistance = new[] {
            center.X - min.X,
            center.Y - min.Y,
            center.Z - min.Z,
            max.X - center.X,
            max.Y - center.Y,
            max.Z - center.Z,
        }.Max();

        Vector3 halfExtent = Vector3.One * maxDistance;
        min = center - halfExtent;
        max = center + halfExtent;

        OctTreeNode root = new OctTreeNode(min, max);

        foreach (var vertex in vertices) {
            root.Insert(vertex);
        }

        return root;
    }

    // Structure methods
    private void Insert(Vertex vertex) {
        if (Count == 0) {
            // Empty node - place the vertex here as a leaf
            Body = vertex;
            CenterOfMass = vertex.Position;
            Count = 1;
            return;
        }

        if (IsLeaf) {
            // This leaf already has a body - subdivide
            Vertex existing = Body!;
            Body = null;

            // Re-insert the existing body into a child
            int existingOctant = GetOctant(existing.Position);
            EnsureChild(existingOctant);
            Children[existingOctant]!.Insert(existing);

            // Insert the new body into a child
            int newOctant = GetOctant(vertex.Position);
            EnsureChild(newOctant);
            Children[newOctant]!.Insert(vertex);

            // Update this node's aggregate data
			CenterOfMass = (existing.Position + vertex.Position) / 2f;
			Count = 2;
			return;
		}

		// Internal node - insert into the appropriate child
		int octant = GetOctant(vertex.Position);
		EnsureChild(octant);
		Children[octant]!.Insert(vertex);

		// Update aggregate: running center of mass and count
		CenterOfMass = (CenterOfMass * Count + vertex.Position) / (Count + 1);
		Count++;
	}

    // Determines which octant (0-7) a position falls into relative to this node's center.
	public int GetOctant(Vector3 position) {
		Vector3 center = (BoundsMin + BoundsMax) / 2f;
		int octant = 0;
		if (position.X >= center.X) octant += 1;
		if (position.Y >= center.Y) octant += 2;
		if (position.Z >= center.Z) octant += 4;
		return octant;
	}

    // Creates the child node for the given octant if it doesn't exist yet.
    public void EnsureChild(int octant) {
        if (Children[octant] != null) return;

        Vector3 center = (BoundsMin + BoundsMax) / 2f;
        Vector3 childMin = BoundsMin;
        Vector3 childMax = center;

        if ((octant & 1) != 0) { childMin.X = center.X; childMax.X = BoundsMax.X; }
        if ((octant & 2) != 0) { childMin.Y = center.Y; childMax.Y = BoundsMax.Y; }
        if ((octant & 4) != 0) { childMin.Z = center.Z; childMax.Z = BoundsMax.Z; }

        Children[octant] = new OctTreeNode(childMin, childMax);
    }

    // Force methods
	// Computes the approximate repulsion force on a target vertex using the Barnes-Hut criterion.
	// Returns the total force vector to be applied.
	public Vector3 ComputeForce(Vertex target) {
		if (Count == 0)
			return Vector3.Zero;

		// Leaf node with a single body
		if (IsLeaf) {
			if (Body == target) {
				return Vector3.Zero;
            }

			return ComputeDirectForce(target.Position);
		}

		// Internal node - check Barnes-Hut criterion
		Vector3 diff = CenterOfMass - target.Position;
		float distance = diff.Length();

		if (distance < 0.001f) {
			// Too close to center of mass - recurse into children to avoid division issues
			return SumChildForces(target);
		}

		float cellSize = BoundsMax.X - BoundsMin.X; // Cubic, so any axis works
		float ratio = cellSize / distance;

		if (ratio < theta) {
			// Cell is far enough - approximate the entire cluster as one body
			return ComputeDirectForce(target.Position);
		}

		// Cell is too close - recurse into children
		return SumChildForces(target);
	}

	private  Vector3 SumChildForces(Vertex target) {
		Vector3 totalForce = Vector3.Zero;
		for (int i = 0; i < 8; i++) {
			if (Children[i] != null) {
				totalForce += Children[i]!.ComputeForce(target);
			}
		}
		return totalForce;
	}

	// Computes the direct repulsion force from a body (or cluster of bodies) at sourcePos
	// on a target at targetPos. Uses the same inverse-square law as Vertex.ApplyRepulsionForce.
	// The mass parameter scales the force for clusters.
	private Vector3 ComputeDirectForce(Vector3 targetPos) {
		Vector3 distanceVector = CenterOfMass - targetPos;
		float distSq = distanceVector.LengthSquared();

		if (distSq > Vertex.influenceRadius * Vertex.influenceRadius) {
			// Beyond influence radius - skip (matches existing early-out)
			return Vector3.Zero;
		}

		if (distSq < 0.0001f) {
			// Prevent division by zero for overlapping vertices
			return Vector3.Zero;
		}

		float dist = Mathf.Sqrt(distSq);
		Vector3 direction = distanceVector / dist;

		// F = -repulsionForce * direction / distance² * mass
        // Negative because it's repulsion (away from source)
        return direction / distSq * (-Vertex.repulsionForce * Count);
    }
}
