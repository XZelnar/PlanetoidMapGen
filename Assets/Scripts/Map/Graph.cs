using UnityEngine;
using System.Collections.Generic;

public class Graph
{
    public class Node
    {
        public enum NodeType : uint
        {
            Sea,
            Coast,
            Ground,
            Mountains,
            Mountaintop
        }

        private Vector3 position;
        public List<Edge> edges = new List<Edge>();

        public NodeType type = NodeType.Ground;
        public int Index { get; internal set; }

        public Vector3 Position
        {
            get { return position; }
            set
            {
                position = value;
                Height = position.magnitude;
                PositionNormalized = position.normalized;
            }
        }

        public Vector3 PositionNormalized { get; private set; }

        public float Height { get; private set; }

        public static int id = 0;
        public int ID = id++;

        public override string ToString()
        {
            return "Node ID:" + ID.ToString();
        }
    }

    public class Corner
    {
        private Vector3 position;
        public List<Edge> edges = new List<Edge>();

        public float height = 1;
        public bool processed = false;

        public static int id = 0;
        public int ID = id++;

        public Vector3 Position
        {
            get { return position; }
            set
            {
                position = value;
                PositionNormalized = position.normalized;
            }
        }

        public Vector3 PositionNormalized { get; private set; }

        public override string ToString()
        {
            return "Corner ID:" + ID.ToString();
        }

        public bool IsConnectedTo(Corner c)
        {
            for (int i = 0; i < edges.Count; i++)
                if (edges[i].GetOtherCorner(this) == c)
                    return true;
            return false;
        }
    }

    public class Edge
    {
        public enum EdgeType
        {
            Regular,
            River
        }

        private Corner c1;
        private Corner c2;
        private Node n1;
        private Node n2;
        public Vector3[] intPoints;
        public Vector3[] n2n;

        public EdgeType type = EdgeType.Regular;
        public float riverWidth = 0;

        public static int id = 0;
        public int ID = id++;

        public override string ToString()
        {
            return "Edge ID:" + ID.ToString();
        }

        public Corner C1
        {
            get { return c1; }
            set
            {
                if (c1 != null)
                    c1.edges.Remove(this);
                c1 = value;
                if (c1 != null)
                    c1.edges.Add(this);
            }
        }
        public Corner C2
        {
            get { return c2; }
            set
            {
                if (c2 != null)
                    c2.edges.Remove(this);
                c2 = value;
                if (c2 != null)
                    c2.edges.Add(this);
            }
        }
        public Node N1
        {
            get { return n1; }
            set
            {
                if (n1 != null)
                    n1.edges.Remove(this);
                n1 = value;
                if (n1 != null)
                    n1.edges.Add(this);
            }
        }
        public Node N2
        {
            get { return n2; }
            set
            {
                if (n2 != null)
                    n2.edges.Remove(this);
                n2 = value;
                if (n2 != null)
                    n2.edges.Add(this);
            }
        }

        public Corner GetOtherCorner(Corner c)
        {
            if (c == c1)
                return c2;
            return c1;
        }

        public Node GetOtherNode(Node n)
        {
            if (n == n1)
                return n2;
            return n1;
        }

        public bool HasCorner(Corner c)
        {
            return c == c1 || c == c2;
        }
    }

    public List<Node> nodes { get; private set; }
    public List<Corner> corners { get; private set; }
    public List<Edge> edges { get; private set; }

    System.Random random;
    public int randomSeed { get; private set; }

    public Graph(int seed = 0)
    {
        randomSeed = seed;
        random = new System.Random(seed);
    }

    /// <summary>
    /// Initializes graph from convex hull
    /// </summary>
    /// <param name="ch"></param>
    /// <param name="nodesCount"></param>
    private void ProcessConvexHull(MIConvexHull.ConvexHull<Vertex, Face> ch, int nodesCount = 10000)
    {
        Profiler.BeginSample("ProcessConvexHull");


        nodes.Clear();
        corners.Clear();
        edges.Clear();

        Node n, n1, n2;
        Corner c, c1, c2;
        Edge e;

        Dictionary<Vertex, Node> dVertexNode = new Dictionary<Vertex, Node>(nodesCount);//Relate vertices to node class instances
        Dictionary<Node, List<Face>> dNodeFace = new Dictionary<Node, List<Face>>(nodesCount);//Relate nodes to faces that contain them
        Dictionary<Face, Corner> dFaceCorner = new Dictionary<Face, Corner>(nodesCount * 2);//Relate faces to corners created from them

        Profiler.BeginSample("Nodes");
        int ind = 0;
        var v = ch.Points.GetEnumerator();
        while (v.MoveNext())//Create all nodes
        {
            n = new Node();
            n.Index = ind++;
            n.Position = new Vector3((float)v.Current.Position[0], (float)v.Current.Position[1], (float)v.Current.Position[2]).normalized;
            nodes.Add(n);
            dVertexNode[v.Current] = n;
            dNodeFace[n] = new List<Face>();
        }
        Profiler.EndSample();
        
        Profiler.BeginSample("Corners");
        var f = ch.Faces.GetEnumerator();
        while (f.MoveNext())//Create all corners
        {
            c = new Corner();
            c.Position = f.Current.Center.normalized;
            corners.Add(c);
            dFaceCorner[f.Current] = c;

            dNodeFace[dVertexNode[f.Current.Vertices[0]]].Add(f.Current);
            dNodeFace[dVertexNode[f.Current.Vertices[1]]].Add(f.Current);
            dNodeFace[dVertexNode[f.Current.Vertices[2]]].Add(f.Current);
        }
        Profiler.EndSample();

        //Create all edges
        Profiler.BeginSample("Edges");
        Vector3 v3;
        Face fj, fk;
        Vertex fjv0, fjv1, fjv2, fkv0, fkv1, fkv2;
        List<Face> dnf;
        int j, k;
        for (int i = 0; i < nodes.Count; i++)//Loop through all nodes
        {
            n = nodes[i];
            dnf = dNodeFace[n];

            for (j = 0; j < dnf.Count - 1; j++)//Process all faces containing this node
            {
                fj = dnf[j];
                fjv0 = fj.Vertices[0];
                fjv1 = fj.Vertices[1];
                fjv2 = fj.Vertices[2];
                for (k = j + 1; k < dnf.Count; k++)//Process all remaining faces to find neighbor ones
                {
                    fk = dnf[k];
                    fkv0 = fk.Vertices[0];
                    fkv1 = fk.Vertices[1];
                    fkv2 = fk.Vertices[2];

                    n1 = null;
                    n2 = null;
                    if (fjv0 == fkv0 || fjv0 == fkv1 || fjv0 == fkv2)
                        n1 = dVertexNode[fjv0];
                    if (fjv1 == fkv0 || fjv1 == fkv1 || fjv1 == fkv2)
                        if (n1 == null)
                            n1 = dVertexNode[fjv1];
                        else
                            n2 = dVertexNode[fjv1];
                    if (fjv2 == fkv0 || fjv2 == fkv1 || fjv2 == fkv2)
                        n2 = dVertexNode[fjv2];

                    if (n1 == null || n2 == null)//Not a neighbor
                        continue;
                    c1 = dFaceCorner[dnf[j]];
                    c2 = dFaceCorner[dnf[k]];
                    if (c1.IsConnectedTo(c2))//Prevent adding similar edges twice
                        continue;

                    e = new Edge();
                    e.N1 = n1;
                    e.N2 = n2;
                    e.C1 = c1;
                    e.C2 = c2;
                    edges.Add(e);
                }
            }//Process all faces containing this node

            v3.x = v3.y = v3.z = 0;
            for (j = 0; j < n.edges.Count; j++)//Set node position as average position of all corners
            {
                e = n.edges[j];
                v3 += e.C1.Position;
                v3 += e.C2.Position;
            }
            n.Position = v3.normalized;
        }//Loop through all nodes
        Profiler.EndSample();

        Profiler.EndSample();
    }

    /// <summary>
    /// Generates graph
    /// </summary>
    /// <param name="nodesCount"></param>
    /// <param name="minDistance"></param>
    /// <param name="relaxationIterations"></param>
    public void Generate(int nodesCount = 10000, float minDistance = 0.022f, int relaxationIterations = 2)
    {
        Profiler.BeginSample("Generate graph");

        Profiler.BeginSample("Create lists");
        nodes = new List<Node>(nodesCount);
        corners = new List<Corner>(nodesCount);
        edges = new List<Edge>(nodesCount * 2);
        Profiler.EndSample();

        MIConvexHull.ConvexHull<Vertex, Face> ch = null;
        Vertex[] p = null;

        int iter = 10;
        while (iter-- > 0)
        {
            try
            {
                p = UtilMapGen.CreateRandomPointsSph(nodesCount, minDistance, randomSeed);
                Profiler.BeginSample("Create hull");
                ch = MIConvexHull.ConvexHull.Create<Vertex, Face>(p);
                Profiler.EndSample();
                break;
            }
            catch (System.Exception e) { Debug.LogError(e.Message); }
        }
        if (iter == -1)
            throw new System.Exception("Failed to generate convex hull");

        ProcessConvexHull(ch, nodesCount);
        Profiler.BeginSample("Relaxation");
        while (relaxationIterations-- > 0)
        {
            Profiler.BeginSample("Update nodes");
            for (int i = 0; i < nodes.Count; i++)
                p[i].Set(nodes[i].Position);
            Profiler.EndSample();
            Profiler.BeginSample("Create hull");
            ch = MIConvexHull.ConvexHull.Create<Vertex, Face>(p);
            Profiler.EndSample();
            ProcessConvexHull(ch, nodesCount);
        }
        Profiler.EndSample();

        Profiler.BeginSample("SortNodesEdges");
        SortNodesEdges();
        Profiler.EndSample();

        Profiler.EndSample();
    }

    /// <summary>
    /// Sorts graph edges so that edges that share corners are noxt to each other in list
    /// </summary>
    private void SortNodesEdges()
    {
        int i, j, k;
        Node n;
        Edge e;
        Corner ccur;
        for (i = 0; i < nodes.Count; i++)
        {
            n = nodes[i];
            ccur = n.edges[0].C1;
            for (j = 1; j < n.edges.Count - 1; j++)
                for (k = j; k < n.edges.Count; k++)
                    if (n.edges[k].HasCorner(ccur))
                    {
                        ccur = n.edges[k].GetOtherCorner(ccur);
                        if (k == j)
                            break;
                        e = n.edges[j];
                        n.edges[j] = n.edges[k];
                        n.edges[k] = e;
                        break;
                    }
        }
    }

    /// <summary>
    /// Recalculates position of orners using corner height
    /// </summary>
    public void UpdateCornersPosition()
    {
        Profiler.BeginSample("UpdateCornersPosition");
        for (int i = 0; i < corners.Count; i++)
        {
            corners[i].height = 1 + corners[i].height / 4;
            corners[i].Position = corners[i].Position.normalized * corners[i].height;
        }

        for (int i = 0; i < edges.Count; i++)
            edges[i].C1 = edges[i].C1;
        Profiler.EndSample();
    }

    /// <summary>
    /// Recalculates position of nodes as average of their corners
    /// </summary>
    public void UpdateNodesPosition()
    {
        Node n;
        float t;
        int j;
        for (int i = 0; i < nodes.Count; i++)
        {
            n = nodes[i];
            t = 0;
            for (j = 0; j < n.edges.Count; j++)
            {
                t += n.edges[j].C1.height;
                t += n.edges[j].C2.height;
            }
            n.Position *= t / n.edges.Count / 2;
        }
    }

    /// <summary>
    /// Clamps corner height
    /// </summary>
    /// <param name="v"></param>
    public void SetCornersMinLevel(float v)
    {
        for (int i = 0; i < corners.Count; i++)
            if (corners[i].height < v)
                corners[i].Position = corners[i].PositionNormalized * (corners[i].height = v);
    }

    private bool IsCornerNearNodeOfType(Corner c, Node.NodeType t)
    {
        for (int i = 0; i < c.edges.Count; i++)
            if (c.edges[i].N1.type == t || c.edges[i].N2.type == t)
                return true;
        return false;
    }



    /// <summary>
    /// Generates mesh for tile
    /// </summary>
    /// <param name="ind"></param>
    /// <returns></returns>
    public Mesh GetMeshForNode(int ind)
    {
        Node n = nodes[ind];
        Mesh m = new Mesh();

        Vector3[] vertices = new Vector3[n.edges.Count];
        int[] indices = new int[(n.edges.Count - 2) * 3];
        Dictionary<Corner, int> dCornerVInd = new Dictionary<Corner, int>(n.edges.Count);

        int curv = 0;
        Corner c;
        Edge e;
        int i;
        for (i = 0; i < n.edges.Count; i++)
        {
            e = n.edges[i];
            if (!(c = e.C1).processed)
            {
                dCornerVInd[c] = curv;
                vertices[curv++] = c.Position;
                c.processed = true;
            }
            if (!(c = e.C2).processed)
            {
                dCornerVInd[c] = curv;
                vertices[curv++] = c.Position;
                c.processed = true;
            }
        }

        Vector3 v1 = vertices[0], v2, v3;
        int i2, i3;
        int curind = 0;
        for (i = 0; i < n.edges.Count; i++)
        {
            e = n.edges[i];
            e.C1.processed = false;
            e.C2.processed = false;

            i2 = dCornerVInd[e.C1];
            if (i2 == 0)
                continue;
            i3 = dCornerVInd[e.C2];
            if (i3 == 0)
                continue;

            v2 = vertices[i2];
            v3 = vertices[i3];

            //http://mathoverflow.net/questions/44096/detecting-whether-directed-cycle-is-clockwise-or-counterclockwise
            if (v1.x * v2.y * v3.z + v2.x * v3.y * v1.z + v3.x * v1.y * v2.z - v3.x * v2.y * v1.z - v2.x * v1.y * v3.z - v1.x * v3.y * v2.z > 0)
            {
                indices[curind + 1] = i2;
                indices[curind + 2] = i3;
            }
            else
            {
                indices[curind + 1] = i3;
                indices[curind + 2] = i2;
            }
            curind += 3;
        }

        m.vertices = vertices;
        m.SetIndices(indices, MeshTopology.Triangles, 0);

        return m;
    }

    /// <summary>
    /// Generates river meshes
    /// </summary>
    /// <param name="waterLevel"></param>
    /// <returns>Combination corner index-mesh</returns>
    public Dictionary<int, Mesh> GetMeshesForRivers(float waterLevel)
    {
        Dictionary<int, Mesh> ma = new Dictionary<int, Mesh>();//Key is index of the corner, Value is Mesh for that corner.
        Mesh m;
        Edge e1, e2;
        int e1ind, e2ind;
        BezierCurve3 bc1 = new BezierCurve3(), bc2 = new BezierCurve3();
        Corner c;
        Vector3 v1, v2;
        Vector3 midpoint1, midpoint2, midpoint3, midpoint4, midpoint5, midpoint6;
        Vector3 p1, p2, p3;
        int j;
        float sqrmin = waterLevel * waterLevel;
        int edgesWithWater;
        float f, f2;

        #region vertices and indices declaration
        const int RiverToOceanVerticesPerSide = 5;
        const int RiverDoubleVerticesPerSide = 9;
        const int RiverTripleVerticesPerSide = 9;
        int[] indicesRiverStart = new int[] { 0, 1, 2 };
        int[] indicesRiverToOcean = new int[] {
            0, 1, 5, 5, 1, 6,
            1, 2, 6, 6, 2, 7,
            2, 3, 7, 7, 3, 8,
            3, 4, 8, 8, 4, 9
        };
        int[] indicesRiverDouble = new int[] {
            0, 1,  9,  9, 1, 10,
            1, 2, 10, 10, 2, 11,
            2, 3, 11, 11, 3, 12,
            3, 4, 12, 12, 4, 13,
            4, 5, 13, 13, 5, 14,
            5, 6, 14, 14, 6, 15,
            6, 7, 15, 15, 7, 16,
            7, 8, 16, 16, 8, 17
        };
        int[] indicesRiverTriple = new int[] {
            0, 1, 26, 26, 1, 25,
            1, 2, 25, 25, 2, 24,
            2, 3, 24, 24, 3, 23,
            3, 4, 23, 23, 4, 22,

            9, 10, 8, 8, 10, 7,
            10, 11, 7, 7, 11, 6,
            11, 12, 6, 6, 12, 5,
            12, 13, 5, 5, 13, 4,

            18, 19, 17, 17, 19, 16,
            19, 20, 16, 16, 20, 15,
            20, 21, 15, 15, 21, 14,
            21, 22, 14, 14, 22, 13,

            4, 13, 22
        };
        Vector3[] verticesRiverStart = new Vector3[3];
        Vector3[] verticesRiverToOcean = new Vector3[RiverToOceanVerticesPerSide * 2];
        Vector3[] verticesRiverDouble = new Vector3[RiverDoubleVerticesPerSide * 2];
        Vector3[] verticesRiverTriple = new Vector3[RiverTripleVerticesPerSide * 3];
        #endregion

        for (int i = 0; i < corners.Count; i++)//Loop through all corners
        {
            c = corners[i];
            edgesWithWater = (c.edges[0].type == Edge.EdgeType.River ? 1 : 0) + (c.edges[1].type == Edge.EdgeType.River ? 1 : 0) + (c.edges[2].type == Edge.EdgeType.River ? 1 : 0);//Count river edges

            if (edgesWithWater == 0)//No rivers near corner
                continue;

            #region 1 edge with river
            if (edgesWithWater == 1)//1 edge with river
            {
                e1ind = (c.edges[0].type == Edge.EdgeType.River ? 0 : (c.edges[1].type == Edge.EdgeType.River ? 1 : 2));//Get the river edge
                e1 = c.edges[e1ind];

                #region outflow to ocean
                if (IsCornerNearNodeOfType(c, Node.NodeType.Sea))//Outflow to ocean
                {
                    v2 = (e1.C1.Position + e1.C2.Position) / 2;//Get edge middle
                    f = e1.riverWidth / 20;//River width
                    v1 = Vector3.Cross(e1.GetOtherCorner(c).Position - c.Position, c.Position).normalized * f;//Midpoint offset
                    midpoint1 = v2 + v1;//Compute midpoints
                    midpoint2 = v2 - v1;

                    p1 = (c.edges[(e1ind + 1) % 3].GetOtherCorner(c).Position - c.Position).normalized * (f * 3) + c.Position;//Compute middle of other two edges
                    p2 = (c.edges[(e1ind + 2) % 3].GetOtherCorner(c).Position - c.Position).normalized * (f * 3) + c.Position;

                    //Determine closest points
                    if (((midpoint1 - p1).sqrMagnitude + (midpoint2 - p2).sqrMagnitude) < ((midpoint1 - p2).sqrMagnitude + (midpoint2 - p1).sqrMagnitude))//m1 to v1
                    {
                        bc1.StartPoint = midpoint1;
                        bc1.EndPoint = p1;
                        bc1.P1 = c.Position + v1;

                        bc2.StartPoint = midpoint2;
                        bc2.EndPoint = p2;
                        bc2.P1 = c.Position - v1;
                    }
                    else//m1 to v2
                    {
                        bc1.StartPoint = midpoint1;
                        bc1.EndPoint = p2;
                        bc1.P1 = c.Position + v1;

                        bc2.StartPoint = midpoint2;
                        bc2.EndPoint = p1;
                        bc2.P1 = c.Position - v1;
                    }

                    for (j = 0, f = 0; j < RiverToOceanVerticesPerSide; j++, f += (1f / (RiverToOceanVerticesPerSide - 1)))//Get points from Bezier curve
                    {
                        verticesRiverToOcean[j] = bc1.GetPoint(f);
                        verticesRiverToOcean[j + RiverToOceanVerticesPerSide] = bc2.GetPoint(f);
                    }

                    for (j = 0; j < verticesRiverToOcean.Length; j++)//Don't let rivers drop below ocean level
                        if (verticesRiverToOcean[j].sqrMagnitude < sqrmin)
                            verticesRiverToOcean[j] = verticesRiverToOcean[j].normalized * waterLevel;

                    //Create mesh
                    OrderMeshTriangles(verticesRiverToOcean, indicesRiverToOcean);
                    m = new Mesh();
                    m.vertices = verticesRiverToOcean;
                    m.SetIndices(indicesRiverToOcean, MeshTopology.Triangles, 0);
                    ma.Add(i, m);
                }//Outflow to ocean
                #endregion

                #region river start
                else//River start
                {
                    verticesRiverStart[0] = c.Position;
                    v2 = (e1.C1.Position + e1.C2.Position) / 2;//Get edge middle
                    v1 = Vector3.Cross(e1.C1.Position - e1.C2.Position, c.Position).normalized * (e1.riverWidth / 20f);//Midpoint offset
                    verticesRiverStart[1] = v2 + v1;//Midpoints
                    verticesRiverStart[2] = v2 - v1;

                    //Create mesh
                    OrderMeshTriangles(verticesRiverStart, indicesRiverStart);
                    m = new Mesh();
                    m.vertices = verticesRiverStart;
                    m.SetIndices(indicesRiverStart, MeshTopology.Triangles, 0);
                    ma.Add(i, m);
                }
                #endregion
            }//1 edge with river
            #endregion
            #region 2 edges with river
            else if (edgesWithWater == 2)//2 edges with water
            {
                e1ind = (c.edges[0].type == Edge.EdgeType.River ? 0 : 1);//Find river edges
                e1 = c.edges[e1ind];
                e2ind = (c.edges[2].type == Edge.EdgeType.River ? 2 : 1);
                e2 = c.edges[e2ind];

                f = e1.riverWidth / 20;//River width
                v2 = p1 = (e1.C1.Position + e1.C2.Position) / 2;//Edge middle
                v1 = Vector3.Cross(e1.GetOtherCorner(c).Position - c.Position, c.Position).normalized * f;//Midpoint offset
                midpoint1 = v2 + v1;//Midpoints
                midpoint2 = v2 - v1;

                f = e2.riverWidth / 20;//River width
                v2 = (e2.C1.Position + e2.C2.Position) / 2;//Edge middle
                v1 = Vector3.Cross(e2.GetOtherCorner(c).Position - c.Position, c.Position).normalized * f;//Midpoint offset
                midpoint3 = v2 + v1;//Midpoints
                midpoint4 = v2 - v1;

                p1 = c.Position - (p1 + v2) / 2;//Offset for Bezier intermediate point

                //1st curve
                bc1.StartPoint = midpoint1;
                bc1.EndPoint = midpoint4;
                bc1.P1 = (midpoint1 + midpoint4) / 2 + p1;

                //2nd curve
                bc2.StartPoint = midpoint2;
                bc2.EndPoint = midpoint3;
                bc2.P1 = (midpoint2 + midpoint3) / 2 + p1;

                for (j = 0, f = 0; j < RiverDoubleVerticesPerSide; j++, f += (1f / (RiverDoubleVerticesPerSide - 1)))//Get points from Bezier curve
                {
                    verticesRiverDouble[j] = bc1.GetPoint(f);
                    verticesRiverDouble[j + RiverDoubleVerticesPerSide] = bc2.GetPoint(f);
                }

                //Create mesh
                OrderMeshTriangles(verticesRiverDouble, indicesRiverDouble);
                m = new Mesh();
                m.vertices = verticesRiverDouble;
                m.SetIndices(indicesRiverDouble, MeshTopology.Triangles, 0);
                ma.Add(i, m);
            }//2 edges with water
            #endregion
            #region 3 edges with river
            else
            {
                e1 = c.edges[0];
                f = e1.riverWidth / 20;//River width
                v2 = p1 = (e1.C1.Position + e1.C2.Position) / 2;//Edge middle
                v1 = Vector3.Cross(e1.GetOtherCorner(c).Position - c.Position, c.Position).normalized * f;//Midpoint offset
                midpoint1 = v2 + v1;//Midpoints
                midpoint2 = v2 - v1;

                e1 = c.edges[1];
                f = e1.riverWidth / 20;//River width
                v2 = p2 = (e1.C1.Position + e1.C2.Position) / 2;//Edge middle
                v1 = Vector3.Cross(e1.GetOtherCorner(c).Position - c.Position, c.Position).normalized * f;//Midpoint offset
                midpoint3 = v2 + v1;//Midpoints
                midpoint4 = v2 - v1;

                e1 = c.edges[2];
                f = e1.riverWidth / 20;//River width
                v2 = p3 = (e1.C1.Position + e1.C2.Position) / 2;//Edge middle
                v1 = Vector3.Cross(e1.GetOtherCorner(c).Position - c.Position, c.Position).normalized * f;//Midpoint offset
                midpoint5 = v2 + v1;//Midpoints
                midpoint6 = v2 - v1;

                #region edge1-edge2
                //Find closest midpoints
                v1 = midpoint1; v2 = midpoint3;
                f = (v1 - v2).sqrMagnitude;
                if ((f2 = (midpoint2 - midpoint3).sqrMagnitude) < f)
                {
                    f = f2; v1 = midpoint2;
                }
                if ((f2 = (midpoint1 - midpoint4).sqrMagnitude) < f)
                {
                    f = f2; v1 = midpoint1; v2 = midpoint4;
                }
                if ((f2 = (midpoint2 - midpoint4).sqrMagnitude) < f)
                {
                    v1 = midpoint2; v2 = midpoint4;
                }

                //Bezier curve
                bc1.StartPoint = v1;
                bc1.EndPoint = v2;
                bc1.P1 = (v1 + v2) / 2 + (c.Position - (p1 + p2) / 2);
                for (j = 0, f = 0; j < RiverTripleVerticesPerSide; j++, f += (1f / (RiverTripleVerticesPerSide - 1)))//Get points from Bezier curve
                    verticesRiverTriple[j] = bc1.GetPoint(f);
                #endregion

                #region edge2-edge3
                //find closest midpoints
                v1 = midpoint3; v2 = midpoint5;
                f = (v1 - v2).sqrMagnitude;
                if ((f2 = (midpoint4 - midpoint5).sqrMagnitude) < f)
                {
                    f = f2; v1 = midpoint4;
                }
                if ((f2 = (midpoint3 - midpoint6).sqrMagnitude) < f)
                {
                    f = f2; v1 = midpoint3; v2 = midpoint6;
                }
                if ((f2 = (midpoint4 - midpoint6).sqrMagnitude) < f)
                {
                    v1 = midpoint4; v2 = midpoint6;
                }

                //Bezier curve
                bc1.StartPoint = v1;
                bc1.EndPoint = v2;
                bc1.P1 = (v1 + v2) / 2 + (c.Position - (p2 + p3) / 2);
                for (j = 0, f = 0; j < RiverTripleVerticesPerSide; j++, f += (1f / (RiverTripleVerticesPerSide - 1)))//Get points from Bezier curve
                    verticesRiverTriple[j + RiverTripleVerticesPerSide] = bc1.GetPoint(f);
                #endregion

                #region edge1-edge3
                //find closest midpoints
                v1 = midpoint1; v2 = midpoint5;
                f = (v1 - v2).sqrMagnitude;
                if ((f2 = (midpoint2 - midpoint5).sqrMagnitude) < f)
                {
                    f = f2; v1 = midpoint2;
                }
                if ((f2 = (midpoint1 - midpoint6).sqrMagnitude) < f)
                {
                    f = f2; v1 = midpoint1; v2 = midpoint6;
                }
                if ((f2 = (midpoint2 - midpoint6).sqrMagnitude) < f)
                {
                    v1 = midpoint2; v2 = midpoint6;
                }

                //Bezier curve
                bc1.StartPoint = v2;
                bc1.EndPoint = v1;
                bc1.P1 = (v1 + v2) / 2 + (c.Position - (p1 + p3) / 2);
                for (j = 0, f = 0; j < RiverTripleVerticesPerSide; j++, f += (1f / (RiverTripleVerticesPerSide - 1)))//Get points from Bezier curve
                    verticesRiverTriple[j + RiverTripleVerticesPerSide + RiverTripleVerticesPerSide] = bc1.GetPoint(f);
                #endregion

                //Create mesh
                OrderMeshTriangles(verticesRiverTriple, indicesRiverTriple);
                m = new Mesh();
                m.vertices = verticesRiverTriple;
                m.SetIndices(indicesRiverTriple, MeshTopology.Triangles, 0);
                ma.Add(i, m);
            }
            #endregion
        }//Loop through all corners

        return ma;
    }

    /// <summary>
    /// Orders mesh triangle vertices
    /// </summary>
    /// <param name="vertices"></param>
    /// <param name="indices"></param>
    private void OrderMeshTriangles(Vector3[] vertices, int[] indices)
    {
        int t;
        for (int i = 0; i < indices.Length; i += 3)
            if (!IsOrdered(vertices[indices[i]], vertices[indices[i + 1]], vertices[indices[i + 2]]))
            {
                t = indices[i];
                indices[i] = indices[i + 1];
                indices[i + 1] = t;
            }
    }

    /// <summary>
    /// Checks if triangle is ordered
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="v3"></param>
    /// <returns></returns>
    private bool IsOrdered(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        return v1.x * v2.y * v3.z + v2.x * v3.y * v1.z + v3.x * v1.y * v2.z - v3.x * v2.y * v1.z - v2.x * v1.y * v3.z - v1.x * v3.y * v2.z > 0;
    }
}
