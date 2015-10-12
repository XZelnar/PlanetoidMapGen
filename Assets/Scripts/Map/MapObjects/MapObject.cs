using UnityEngine;
using System.Collections.Generic;


public class MapObject
{
    public Vector3 Position { get; protected set; }
    internal Mesh mesh { get; private set; }
    internal Material material { get; private set; }
    internal MapChunk chunk { get; set; }

    public MapObject(Mesh o, Material mat)
    {
        mesh = o;
        material = mat;
        Position = Vector3.zero;
    }
}