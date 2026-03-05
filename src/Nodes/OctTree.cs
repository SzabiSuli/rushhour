namespace rushhour.src.Nodes;

using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Barnes-Hut OctTree for O(n log n) repulsion force approximation in 3D.
/// Rebuilt every frame from the current vertex positions.
/// </summary>
public static class OctTree
{
	/// <summary>
	/// Barnes-Hut approximation parameter. Higher values = faster but less accurate.
	/// Typical range: 0.5 (accurate) to 1.5 (fast). 0.8 is a good default.
	/// </summary>
	public const float Theta = 1.5f;

	/// <summary>
	/// The OctTree built for the current frame. Set by BuildAndSetCurrent().
	/// </summary>
	private static OctTreeNode? _current;

	public static OctTreeNode? GetCurrent() => _current;

	/// <summary>
	/// Builds the OctTree from the given vertices and stores it as the current tree.
	/// Should be called once per frame in MainScene._Process(), before vertices process.
	/// </summary>
	public static void BuildAndSetCurrent(List<Vertex> vertices)
	{
		if (vertices.Count == 0)
		{
			_current = null;
			return;
		}

		_current = Build(vertices);
	}

	/// <summary>
	/// Builds an OctTree from a list of vertices.
	/// </summary>
	public static OctTreeNode Build(List<Vertex> vertices)
	{
		// Compute bounding box enclosing all vertices with padding
		Vector3 min = vertices[0].Position;
		Vector3 max = vertices[0].Position;

		for (int i = 1; i < vertices.Count; i++)
		{
			Vector3 pos = vertices[i].Position;
			min = min.Min(pos);
			max = max.Max(pos);
		}

		// TODO is this necessary?
		// Add small padding so no vertex sits exactly on the boundary
		Vector3 padding = (max - min) * 0.01f + Vector3.One * 0.1f;
		min -= padding;
		max += padding;

		// Make the bounding box cubic (equal side lengths) for uniform subdivision
		float maxSide = Mathf.Max(max.X - min.X, Mathf.Max(max.Y - min.Y, max.Z - min.Z));
		Vector3 center = (min + max) / 2f;
		Vector3 halfExtent = Vector3.One * (maxSide / 2f);
		min = center - halfExtent;
		max = center + halfExtent;

		OctTreeNode root = new OctTreeNode(min, max);

		foreach (var vertex in vertices)
		{
			Insert(root, vertex);
		}

		return root;
	}

	/// <summary>
	/// Inserts a vertex into the OctTree, subdividing as needed.
	/// </summary>
	private static void Insert(OctTreeNode node, Vertex vertex)
	{
		if (node.Count == 0)
		{
			// Empty node — place the vertex here as a leaf
			node.Body = vertex;
			node.CenterOfMass = vertex.Position;
			node.Count = 1;
			return;
		}

		if (node.IsLeaf)
		{
			// This leaf already has a body — subdivide
			Vertex existing = node.Body!;
			node.Body = null;

			// Re-insert the existing body into a child
			int existingOctant = node.GetOctant(existing.Position);
			node.EnsureChild(existingOctant);
			Insert(node.Children[existingOctant]!, existing);

			// Insert the new body into a child
			int newOctant = node.GetOctant(vertex.Position);
			node.EnsureChild(newOctant);
			Insert(node.Children[newOctant]!, vertex);

			// Update this node's aggregate data
			node.CenterOfMass = (existing.Position + vertex.Position) / 2f;
			node.Count = 2;
			return;
		}

		// Internal node — insert into the appropriate child
		int octant = node.GetOctant(vertex.Position);
		node.EnsureChild(octant);
		Insert(node.Children[octant]!, vertex);

		// Update aggregate: running center of mass and count
		node.CenterOfMass = (node.CenterOfMass * node.Count + vertex.Position) / (node.Count + 1);
		node.Count++;
	}

	/// <summary>
	/// Computes the approximate repulsion force on a target vertex using the Barnes-Hut criterion.
	/// Returns the total force vector to be applied.
	/// </summary>
	public static Vector3 ComputeForce(OctTreeNode node, Vertex target, float theta)
	{
		if (node.Count == 0)
			return Vector3.Zero;

		// Leaf node with a single body
		if (node.IsLeaf)
		{
			if (node.Body == target)
				return Vector3.Zero;

			return ComputeDirectForce(target.Position, node.CenterOfMass, 1);
		}

		// Internal node — check Barnes-Hut criterion
		Vector3 diff = node.CenterOfMass - target.Position;
		float distance = diff.Length();

		if (distance < 0.001f)
		{
			// Too close to center of mass — recurse into children to avoid division issues
			return RecurseChildren(node, target, theta);
		}

		float cellSize = node.BoundsMax.X - node.BoundsMin.X; // Cubic, so any axis works
		float ratio = cellSize / distance;

		if (ratio < theta)
		{
			// Cell is far enough — approximate the entire cluster as one body
			return ComputeDirectForce(target.Position, node.CenterOfMass, node.Count);
		}

		// Cell is too close — recurse into children
		return RecurseChildren(node, target, theta);
	}

	private static Vector3 RecurseChildren(OctTreeNode node, Vertex target, float theta)
	{
		Vector3 totalForce = Vector3.Zero;
		for (int i = 0; i < 8; i++)
		{
			if (node.Children[i] != null)
			{
				totalForce += ComputeForce(node.Children[i]!, target, theta);
			}
		}
		return totalForce;
	}

	/// <summary>
	/// Computes the direct repulsion force from a body (or cluster of bodies) at sourcePos
	/// on a target at targetPos. Uses the same inverse-square law as Vertex.ApplyRepulsionForce.
	/// The mass parameter scales the force for clusters.
	/// </summary>
	private static Vector3 ComputeDirectForce(Vector3 targetPos, Vector3 sourcePos, int mass)
	{
		Vector3 distanceVector = sourcePos - targetPos;
		float distSq = distanceVector.LengthSquared();

		if (distSq > Vertex.influenceRadius * Vertex.influenceRadius)
		{
			// Beyond influence radius — skip (matches existing early-out)
			return Vector3.Zero;
		}

		if (distSq < 0.0001f)
		{
			// Prevent division by zero for overlapping vertices
			return Vector3.Zero;
		}

		float dist = Mathf.Sqrt(distSq);
		Vector3 direction = distanceVector / dist;

		// F = -repulsionForce * direction / distance² * mass
		// Negative because it's repulsion (away from source)
		return direction / distSq * (-Vertex.repulsionForce * mass);
	}
}

/// <summary>
/// A node in the Barnes-Hut OctTree, representing a cubic region of 3D space.
/// </summary>
public class OctTreeNode
{
	/// <summary>Minimum corner of the axis-aligned bounding box.</summary>
	public Vector3 BoundsMin;

	/// <summary>Maximum corner of the axis-aligned bounding box.</summary>
	public Vector3 BoundsMax;

	/// <summary>
	/// 8 children, one per octant. Null if the octant is empty or this is a leaf.
	/// Octant indexing: bit 0 = X axis, bit 1 = Y axis, bit 2 = Z axis.
	/// 0 = below midpoint, 1 = above midpoint for each axis.
	/// </summary>
	public OctTreeNode?[] Children = new OctTreeNode?[8];

	/// <summary>Weighted center of mass of all bodies in this subtree.</summary>
	public Vector3 CenterOfMass;

	/// <summary>Number of bodies contained in this subtree.</summary>
	public int Count;

	/// <summary>The single vertex stored here, if this is a leaf node. Null for internal nodes.</summary>
	public Vertex? Body;

	/// <summary>True if this node is a leaf (has a body and no children).</summary>
	public bool IsLeaf => Body != null;

	public OctTreeNode(Vector3 boundsMin, Vector3 boundsMax)
	{
		BoundsMin = boundsMin;
		BoundsMax = boundsMax;
		CenterOfMass = Vector3.Zero;
		Count = 0;
		Body = null;
	}

	/// <summary>
	/// Determines which octant (0-7) a position falls into relative to this node's center.
	/// </summary>
	public int GetOctant(Vector3 position)
	{
		Vector3 center = (BoundsMin + BoundsMax) / 2f;
		int octant = 0;
		if (position.X >= center.X) octant |= 1;
		if (position.Y >= center.Y) octant |= 2;
		if (position.Z >= center.Z) octant |= 4;
		return octant;
	}

	/// <summary>
	/// Creates the child node for the given octant if it doesn't exist yet.
	/// </summary>
	public void EnsureChild(int octant)
	{
		if (Children[octant] != null) return;

		Vector3 center = (BoundsMin + BoundsMax) / 2f;
		Vector3 childMin = BoundsMin;
		Vector3 childMax = center;

		if ((octant & 1) != 0) { childMin.X = center.X; childMax.X = BoundsMax.X; }
		if ((octant & 2) != 0) { childMin.Y = center.Y; childMax.Y = BoundsMax.Y; }
		if ((octant & 4) != 0) { childMin.Z = center.Z; childMax.Z = BoundsMax.Z; }

		Children[octant] = new OctTreeNode(childMin, childMax);
	}
}
