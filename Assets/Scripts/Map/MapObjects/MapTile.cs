using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class MapTile : MapObject
{
    public enum VisibilityState
    {
        Unseen,
        Seen,
        Visible
    }

    public Graph.Node GraphNode { get; private set; }
    public Bounds AABB { get; private set; }
    [System.NonSerialized]
    public List<MapTile> neighbors = new List<MapTile>();
    public VisibilityState visibilityState { get; private set; }
    public float VisibilityOpacity { get { return visibilityState == VisibilityState.Seen ? .5f : visibilityState == VisibilityState.Unseen ? 1 : 0; } }


    public MapTile(Graph.Node n, Mesh m, Material mat) : base(m, mat)
    {
        GraphNode = n;
        Position = n.Position;
        visibilityState = VisibilityState.Unseen;

        //Calculate AABB
        Vector3 min, max;
        min = max = m.vertices[0];
        for (int i = 1; i < m.vertexCount; i++)
        {
            min = Vector3.Min(min, m.vertices[i]);
            max = Vector3.Max(max, m.vertices[i]);
        }

        AABB = new Bounds((min + max) / 2, (max - min));
    }

    public void AddNeighbor(MapTile t)
    {
        if (!neighbors.Contains(t))
            neighbors.Add(t);
    }

    public void SetSeen()
    {
        if (visibilityState == VisibilityState.Seen)//Ignore if not changing
            return;
        visibilityState = VisibilityState.Seen;
        //Re-render tile on visibility overlay texture
        chunk.visibilityOverlay.UpdateTileVisibility(this);
        chunk.visibilityOverlay.UpdateTileNeighborVisibility(this);

        for (int i = 0; i < neighbors.Count; i++)//Update neighbors
            neighbors[i].OnNeighborVisibilityChanged(this);
    }

    public void SetVisible()
    {
        if (visibilityState == VisibilityState.Visible)//Ignore if not changing
            return;
        visibilityState = VisibilityState.Visible;
        //Re-render tile on visibility overlay texture
        chunk.visibilityOverlay.UpdateTileVisibility(this);

        for (int i = 0; i < neighbors.Count; i++)//Update neighbors
            neighbors[i].OnNeighborVisibilityChanged(this);
    }

    public void OnNeighborVisibilityChanged(MapTile neighbor)
    {
        chunk.visibilityOverlay.UpdateTileVisibility(this);
        chunk.visibilityOverlay.UpdateTileNeighborVisibility(this);
    }
}