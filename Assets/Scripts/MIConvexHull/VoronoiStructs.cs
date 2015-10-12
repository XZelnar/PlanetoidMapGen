using UnityEngine;
using System.Collections.Generic;

public class Vertex : MIConvexHull.IVertex
{
    public Vertex(double[] location)
    {
        Position = location;
    }

    public Vertex()
    {
        Position = new double[3];
    }

    public Vertex(Vector3 v)
    {
        Position = new double[3];
        Position[0] = v.x;
        Position[1] = v.y;
        Position[2] = v.z;
    }

    public void Set(Vector3 v)
    {
        Position[0] = v.x;
        Position[1] = v.y;
        Position[2] = v.z;
    }

    public double[] Position { get; set; }
}
public class Face : MIConvexHull.ConvexFace<Vertex, Face>
{
    public Vector3 Center
    {
        get
        {
            return new Vector3(
                (float)(Vertices[0].Position[0] + Vertices[1].Position[0] + Vertices[2].Position[0]) / 3,
                (float)(Vertices[0].Position[1] + Vertices[1].Position[1] + Vertices[2].Position[1]) / 3,
                (float)(Vertices[0].Position[2] + Vertices[1].Position[2] + Vertices[2].Position[2]) / 3
                );
        }
    }
}