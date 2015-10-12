using UnityEngine;
using System.Collections.Generic;


public class MapRiver : MapObject
{
    public Graph.Corner GraphCorner { get; private set; }

    public MapRiver(Graph.Corner c, Mesh o, Material mat) : base(o, mat)
    {
        GraphCorner = c;
        Position = c.Position;
    }
}
