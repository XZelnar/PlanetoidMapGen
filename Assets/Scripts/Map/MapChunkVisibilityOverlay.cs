using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class MapChunkVisibilityOverlay
{
    //Structs for rasterization queues
    struct TriangleConst
    {
        public Vector2 p1, p2, p3;
        public float c;
    }
    struct TriangleGrad
    {
        public Vector2 p1, p2, p3;
        public float c1, c2, c3;
    }

    public RenderTexture texCur, texDest;//2 textures for animation: current and desired
    public Mesh mesh;//Baked mesh
    internal int texW = 1, texH = 1;//Texture size
    [System.NonSerialized]
    MapChunk chunk;//Parent chunk
    Material renderMaterial;//Material to render overlay with. Has texture initialized to texCur

    List<TriangleConst> triConstQueue = new List<TriangleConst>();//Queue of tile triangles
    List<TriangleGrad> triGradQueue = new List<TriangleGrad>();//Queue if gradient triangles
    int updateTimer = 0, updateMax = (int)(1f / 0.02f);//Timer for texture animation. Unchangd overlays are not animated

    Vector2 texelSize;//Size of texel
    float maxTexelDistance;//Used in inflation

    //Variables used for projecting world coordinates to UV coordinates
    internal Vector3 projD0, projD1, projD2;
    internal Vector2 projMul, projMin;

    static Matrix4x4 matVisScale = Matrix4x4.Scale(new Vector3(1.02f, 1.02f, 1.02f));//Render overlay slightly above terrain
    internal static Material visibilityOverlayMaterial;//Static material to instantiate renderMaterial from
    public static Material rasterizerSolid, rasterizerMin;//Materials for rasterizig triangles
    public static ComputeShader csUpdate;//Animation shader
    public static int csUpdate_functionIndex = -1;



    public MapChunkVisibilityOverlay(MapChunk t)
    {
        chunk = t;
        updateTimer = updateMax;

        if (csUpdate_functionIndex == -1)
            csUpdate_functionIndex = csUpdate.FindKernel("CSMain");
    }

    public void CreateTextures()
    {
        if (texW == 0) texW = 1;//TODO rm
        if (texH == 0) texH = 1;
        if (texW == 0) throw new System.Exception("texW must be > 0");
        if (texH == 0) throw new System.Exception("texH must be > 0");
        texelSize = new Vector2(1f / texW, 1f / texH);
        maxTexelDistance = texelSize.magnitude;

        //Create textures
        texCur = new RenderTexture(texW, texH, 0, RenderTextureFormat.RFloat);
        texCur.enableRandomWrite = true;
        texCur.generateMips = false;
        texCur.Create();
        texDest = new RenderTexture(texW, texH, 0, RenderTextureFormat.RFloat);
        texDest.enableRandomWrite = true;
        texDest.generateMips = false;
        texCur.filterMode = FilterMode.Point;
        texDest.Create();

        //Clear textures
        Graphics.SetRenderTarget(texCur);
        GL.Clear(true, true, Color.white);
        Graphics.SetRenderTarget(texDest);
        GL.Clear(true, true, Color.white);
        Graphics.SetRenderTarget(null);

        //Instantiate material
        renderMaterial = new Material(visibilityOverlayMaterial);
        renderMaterial.SetTexture("_Mask", texCur);
    }

    public void Update()
    {
        if (triConstQueue.Count > 0 || triGradQueue.Count > 0)//Something needs rasterizing
        {
            Graphics.SetRenderTarget(texDest);

            if (triConstQueue.Count > 0)//Solid trinagles need rasterizing
            {
                rasterizerSolid.SetPass(0);
                GL.Begin(GL.TRIANGLES);
                TriangleConst t;
                for (int i = 0; i < triConstQueue.Count; i++)//Draw every triangle in queue
                {
                    t = triConstQueue[i];
                    GL.Color(new Color(t.c, 1, 1, 1));
                    GL.Vertex(t.p1);
                    GL.Vertex(t.p2);
                    GL.Vertex(t.p3);
                }
                GL.End();
                triConstQueue.Clear();//Clear queue
            }

            if (triGradQueue.Count > 0)//Gradient trinagles need rasterizing
            {
                rasterizerMin.SetPass(0);
                GL.Begin(GL.TRIANGLES);
                TriangleGrad t;
                for (int i = 0; i < triGradQueue.Count; i++)//Draw every triangle in queue
                {
                    t = triGradQueue[i];
                    GL.Color(new Color(t.c1, 1, 1, 1));
                    GL.Vertex(t.p1);
                    GL.Color(new Color(t.c2, 1, 1, 1));
                    GL.Vertex(t.p2);
                    GL.Color(new Color(t.c3, 1, 1, 1));
                    GL.Vertex(t.p3);
                }
                triGradQueue.Clear();//Clear queue
            }

            GL.End();
            Graphics.SetRenderTarget(null);
            updateTimer = updateMax;
        }

        if (updateTimer > 0)//Animate if necessary
        {
            updateTimer--;
            csUpdate.SetTexture(csUpdate_functionIndex, "texCur", texCur);
            csUpdate.SetTexture(csUpdate_functionIndex, "texDest", texDest);
            csUpdate.Dispatch(csUpdate_functionIndex, texW / 16, texH / 16, 1);
        }
    }

    /// <summary>
    /// Project 3D points onto UV space
    /// </summary>
    /// <param name="p">Point to project</param>
    /// <returns></returns>
    public Vector2 PointToTexCoord(Vector3 p)
    {
        var tv2 = new Vector2(Vector3.Dot(p - projD0, projD1), Vector3.Dot(p - projD0, projD2)) - projMin;
        tv2.x *= projMul.x;
        tv2.y *= projMul.y;
        return tv2;
    }

    /// <summary>
    /// Adds tile to rasterizer queue
    /// </summary>
    /// <param name="t"></param>
    public void UpdateTileVisibility(MapTile t)
    {
        float vv = t.VisibilityOpacity;
        Mesh m = t.mesh;//Get tile mesh
        Vector2[] v = new Vector2[m.vertexCount];//Vertices coordinates in UV space
        Vector2 ce = Vector2.zero;
        for (int i = 0; i < v.Length; i++)//Convert all vertices to UV space
            ce += v[i] = PointToTexCoord(m.vertices[i].normalized);
        ce /= v.Length;//Find center point
        for (int i = 0; i < v.Length; i++)//Inflate all vertices a little
            v[i] += (v[i] - ce).normalized * maxTexelDistance;
        int[] ind = m.GetIndices(0);//Get mesh indices
        TriangleConst c;
        for (int i = 0; i < ind.Length; i += 3)//For every triangle add its projected version to rasterizer queue
        {
            c = new TriangleConst();
            c.c = vv;
            c.p1 = v[ind[i]];
            c.p2 = v[ind[i + 1]];
            c.p3 = v[ind[i + 2]];

            triConstQueue.Add(c);
        }
    }

    /// <summary>
    /// Makes gradient between tiles where needed
    /// </summary>
    /// <param name="t"></param>
    public void UpdateTileNeighborVisibility(MapTile t)
    {
        Vector2 t1, t2, t3, t4, tc;
        Vector3 tv1, tv2, tv3, tv4;
        float c1, c2 = t.VisibilityOpacity;
        int j;
        Graph.Corner c;
        bool b;
        for (int i = 0; i < t.neighbors.Count; i++)//Loop; through all neighbors
        {
            if (t.neighbors[i].VisibilityOpacity >= t.VisibilityOpacity)//Ignore neighbors with lower/same opacity
                continue;

            //Project edge corners
            t1 = PointToTexCoord(tv1 = t.GraphNode.edges[i].C1.PositionNormalized);
            t2 = PointToTexCoord(tv2 = t.GraphNode.edges[i].C2.PositionNormalized);

            //Compute third point
            j = i == 0 ? t.neighbors.Count - 1 : i - 1;
            if (b = t.GraphNode.edges[j].HasCorner(t.GraphNode.edges[i].C1))
                c = t.GraphNode.edges[i].C1;
            else
                c = t.GraphNode.edges[i].C2;
            t3 = PointToTexCoord(tv3 = c.PositionNormalized + (t.GraphNode.edges[j].GetOtherCorner(c).PositionNormalized - c.PositionNormalized).normalized * 0.02f);

            //Compute fourth point 
            j = i == t.neighbors.Count - 1 ? 0 : i + 1;
            c = t.GraphNode.edges[i].GetOtherCorner(c);
            t4 = PointToTexCoord(tv4 = c.PositionNormalized + (t.GraphNode.edges[j].GetOtherCorner(c).PositionNormalized - c.PositionNormalized).normalized * 0.02f);

            //Inflate points
            tc = (t1 + t2 + t3 + t4) / 4;
            t1 += (t1 - tc).normalized * maxTexelDistance * 2;
            t2 += (t2 - tc).normalized * maxTexelDistance * 2;
            t3 += (t3 - tc).normalized * maxTexelDistance * 2;
            t4 += (t4 - tc).normalized * maxTexelDistance * 2;

            //Get opacities
            c1 = t.neighbors[i].VisibilityOpacity;

            //Add triangles to queue
            triGradQueue.Add(new TriangleGrad() { p1 = t1, p2 = t2, p3 = t3, c1 = c1, c2 = c1, c3 = c2 });
            if (b)
                triGradQueue.Add(new TriangleGrad() { p1 = t4, p2 = t3, p3 = t2, c1 = c2, c2 = c2, c3 = c1 });
            else
                triGradQueue.Add(new TriangleGrad() { p1 = t4, p2 = t1, p3 = t3, c1 = c2, c2 = c1, c3 = c2 });
        }
    }

    /// <summary>
    /// Renders overlay
    /// </summary>
    public void Render()
    {
        renderMaterial.SetPass(0);
        Graphics.DrawMeshNow(mesh, matVisScale);
    }
}