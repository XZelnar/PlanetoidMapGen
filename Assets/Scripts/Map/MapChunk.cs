using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class MapChunk
{
    [System.NonSerialized]
    public List<MapObject> mapObjects = new List<MapObject>();
    [System.NonSerialized]
    public List<MapTile> mapTiles = new List<MapTile>();


    Dictionary<Material, Mesh[]> renderQueue = new Dictionary<Material, Mesh[]>();
    public MapChunkVisibilityOverlay visibilityOverlay { get; private set; }
    Mesh wireframe = null;

    static Matrix4x4 mTiles = Matrix4x4.identity;//Matrix for tiles/objects
    static Matrix4x4 mRivers = Matrix4x4.Scale(new Vector3(1.002f, 1.002f, 1.002f));//Rivers are rendered slightly above tiles
    static Matrix4x4 mWireframe = Matrix4x4.Scale(new Vector3(1.004f, 1.004f, 1.004f));//Wireframe is rendered above rivers

    /// <summary>
    /// Creates render queue, visibility overlay and bakes meshes
    /// </summary>
    public void Initialize()
    {
        GenerateRenderQueue();
        RenderQueueBakeMeshes();
        visibilityOverlay = new MapChunkVisibilityOverlay(this);
        GenerateVisibilityOverlayMesh();
        visibilityOverlay.CreateTextures();
        GenerateWireframe();
    }

    /// <summary>
    /// Generates render queue
    /// </summary>
    private void GenerateRenderQueue()
    {
        renderQueue.Clear();
        Dictionary<Material, List<Mesh>> tpd = new Dictionary<Material, List<Mesh>>();//Pair material-meshes
        MapObject o;

        for (int i = 0; i < mapObjects.Count; i++)//Go through all obects
        {
            o = mapObjects[i];
            o.chunk = this;
            if (!tpd.ContainsKey(o.material))//If new paterial - add it to dictionary
                tpd[o.material] = new List<Mesh>();
            tpd[o.material].Add(o.mesh);//Add mesh to dictionary
        }

        var e = tpd.GetEnumerator();
        while (e.MoveNext())//Copy combinations to real queue
            renderQueue[e.Current.Key] = e.Current.Value.ToArray();
    }

    /// <summary>
    /// Bakes tile and river meshes for render queue
    /// </summary>
    private void RenderQueueBakeMeshes()
    {
        int i, j;
        Mesh m, mt;
        Mesh[] ma;

        List<Vector3> vertices = new List<Vector3>();//List of vertices
        List<int> indices = new List<int>();//List of indices
        int[] vind;//List of vertex indices for current mesh
        int[] tind;//List of indices of current mesh

        Dictionary<Material, Mesh[]> renderQueue2 = new Dictionary<Material, Mesh[]>();//Updated render queue

        var e = renderQueue.GetEnumerator();
        while (e.MoveNext())//Go through all material-meshes combinations
        {
            if (!e.Current.Key.name.StartsWith("Tile") && e.Current.Key.name != "MaterialRiver")//Filter out non-tile objects
            {
                renderQueue2[e.Current.Key] = e.Current.Value;
                continue;
            }

            ma = e.Current.Value;
            vertices.Clear();
            indices.Clear();

            for (i = 0; i < ma.Length; i++)//Go through all meshes related to current material
            {
                mt = ma[i];
                vind = new int[mt.vertexCount];
                for (j = 0; j < mt.vertexCount; j++)//Go through all vertices
                {
                    if (vertices.Contains(mt.vertices[j]))//If vertex was already added
                        vind[j] = vertices.IndexOf(mt.vertices[j]);//Copy its index
                    else//New vertex
                    {
                        vertices.Add(mt.vertices[j]);//Add it
                        vind[j] = vertices.Count - 1;//Save its index
                    }
                }

                tind = mt.GetIndices(0);//Get indices of current mesh
                for (j = 0; j < tind.Length; j++)//Go through them
                    indices.Add(vind[tind[j]]);//Update them to the current list of vertices and add to index list
            }

            //Create new mesh
            m = new Mesh();
            m.vertices = vertices.ToArray();
            m.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
            renderQueue2[e.Current.Key] = new Mesh[] { m };//Add new mesh to render queue
        }

        renderQueue = renderQueue2;//Update actual render queue
    }

    private void GenerateVisibilityOverlayMesh()
    {
        Vector3 d0 = Vector3.zero, d1 = Vector3.zero, d2 = Vector3.zero, tv3;//Used for UV computation
        Vector2 tv2, uvmin = new Vector2(10000, 10000), uvmax = new Vector2(-10000, -10000), vmul;//Used for UV normalization
        int i;
        Mesh m, mt;
        Mesh[] ma;

        List<Vector3> vertices = new List<Vector3>();
        List<int> indices = new List<int>();
        List<Vector2> uv = new List<Vector2>();
        int[] vind;//List of vertex indices for current mesh
        int[] tind;//List of indices of current mesh

        var e = renderQueue.GetEnumerator();
        while (e.MoveNext())//Go through all material-mesh combinations
        {
            if (!e.Current.Key.name.StartsWith("Tile"))//Ignore everything but tiles
                continue;

            ma = e.Current.Value;

            if (d0 == Vector3.zero)//First mesh initializes UV variables
            {
                d0 = ma[0].vertices[0].normalized;//Coordinate system center
                d1 = (ma[0].vertices[1].normalized - d0).normalized;//First axis
                d2 = Vector3.Cross(d0, d1).normalized;//Second axis
            }

            mt = ma[0];//Combination contains only one mesh after queue naking
            vind = new int[mt.vertexCount];

            for (i = 0; i < mt.vertexCount; i++)//Loop through all vertices
            {
                if (vertices.Contains(mt.vertices[i]))//If vertex vas already added
                    vind[i] = vertices.IndexOf(mt.vertices[i]);//Get its index
                else//New vertex
                {
                    vind[i] = vertices.Count;//Save its index
                    vertices.Add(tv3 = mt.vertices[i]);//Add it to list of vertices
                    tv3.Normalize();//Normalize its position
                    tv3 -= d0;//Get its position relative to UV coordinate system
                    uv.Add(tv2 = new Vector2(Vector3.Dot(tv3, d1), Vector3.Dot(tv3, d2)));//Project it onto UV coordinate system axes
                    uvmin = Vector2.Min(uvmin, tv2);//Update UV min value
                    uvmax = Vector2.Max(uvmax, tv2);//Update UV max value
                }
            }

            tind = mt.GetIndices(0);//Get mesh indices
            for (i = 0; i < tind.Length; i++)//Loop through all indices
                indices.Add(vind[tind[i]]);//Update them to the current list of vertices and add to index list
        }//Go through all material-mesh combinations

        //Compute visibility overlay texture size from min and max UV value. Can be replaced with constants
        visibilityOverlay.texW = ((int)((uvmax.x - uvmin.x) * 1000) / 16) * 16;
        visibilityOverlay.texH = ((int)((uvmax.y - uvmin.y) * 1000) / 16) * 16;
        //Compute normalization vector i.e. vector used to convert UV coordinates' range from [uvmin, uvmax] to [0, 1]
        vmul = new Vector2(1f / (uvmax.x - uvmin.x), 1f / (uvmax.y - uvmin.y));

        for (i = 0; i < uv.Count; i++)//Loop through all UV coordinates and normalize them
        {
            tv2 = uv[i] - uvmin;
            uv[i] = new Vector2(tv2.x * vmul.x, tv2.y * vmul.y);
        }

        //Create mesh
        m = new Mesh();
        m.vertices = vertices.ToArray();
        m.uv = uv.ToArray();
        m.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
        visibilityOverlay.mesh = m;

        //Copy variables to visibility overlay
        visibilityOverlay.projD0 = d0;
        visibilityOverlay.projD1 = d1;
        visibilityOverlay.projD2 = d2;
        visibilityOverlay.projMin = uvmin;
        visibilityOverlay.projMul = vmul;
    }

    private void GenerateWireframe()
    {
        Profiler.BeginSample("GenerateWireframe");

        int i, j;
        int ic = 0;

        for (i = 0; i < mapTiles.Count; i++)
            ic += mapTiles[i].GraphNode.edges.Count;
        ic *= 2;

        List<Graph.Corner> corners = new List<Graph.Corner>(mapTiles.Count * 3);
        List<Vector3> vertices = new List<Vector3>(mapTiles.Count * 3);
        int[] indices = new int[ic];
        Graph.Node n;
        Graph.Edge e;
        ic = 0;

        for (i = 0; i < mapTiles.Count; i++)
        {
            n = mapTiles[i].GraphNode;
            for (j = 0; j < n.edges.Count; j++)
            {
                e = n.edges[j];
                if (corners.Contains(e.C1))
                    indices[ic++] = corners.IndexOf(e.C1);
                else
                {
                    indices[ic++] = vertices.Count;
                    vertices.Add(e.C1.Position);
                    corners.Add(e.C1);
                }

                if (corners.Contains(e.C2))
                    indices[ic++] = corners.IndexOf(e.C2);
                else
                {
                    indices[ic++] = vertices.Count;
                    vertices.Add(e.C2.Position);
                    corners.Add(e.C2);
                }
            }
        }

        if (wireframe == null)
            wireframe = new Mesh();
        else
            wireframe.Clear();
        wireframe.SetVertices(vertices);
        wireframe.SetIndices(indices, MeshTopology.Lines, 0);
        Profiler.EndSample();
    }

    public void Update()
    {
        visibilityOverlay.Update();
    }

    public void Render()
    {
        Mesh[] m;
        int i;
        Matrix4x4 curmat;
        var e = renderQueue.GetEnumerator();
        while (e.MoveNext())//Loop through all material-mesh combinations
        {
            if (e.Current.Key.name == "MaterialRiver")//Rivers are rendered slightly higher
                curmat = mRivers;
            else
                curmat = mTiles;

            e.Current.Key.SetPass(0);//Set material pass
            m = e.Current.Value;
            for (i = 0; i < m.Length; i++)//Render all meshes
                Graphics.DrawMeshNow(m[i], curmat);
        }
    }

    public void RenderWireframe()
    {
        Graphics.DrawMeshNow(wireframe, mWireframe);
    }

    public void RenderVisibilityOverlay()
    {
        visibilityOverlay.Render();
    }
}