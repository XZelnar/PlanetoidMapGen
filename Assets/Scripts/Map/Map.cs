using UnityEngine;
using System.Collections.Generic;

public class Map : MonoBehaviour
{
    public GameObject MapTilePrefab;
    public GameObject MapRiverPrefab;

    public float WaterPercentage = 0.5f;
    public float MountainPercentage = 0.2f;
    
    float WaterLevel;
    float MountainLevel;

    public int Seed = 0;

    public Material
            materialSea,
            materialCoast,
            materialGround,
            materialMountains,
            materialMountaintop;
    public Material materialRiver;
    public Material materialWireframe;

    public ComputeShader visibilityOverlay_csUpdate;
    public Material materialVisibilityOverlay;
    public Material visibilityOverlay_rasterizerSolid, visibilityOverlay_rasterizerMin;
    public Material visibilityOverlayMaterial;
    
    Graph graph;
    Octree octree = new Octree();
    List<MapTile> mapTiles = new List<MapTile>();
    List<MapObject> mapObjects = new List<MapObject>();



    /// <summary>
    /// Sets static variables and generates map
    /// </summary>
    void Start()
    {
        MapChunkVisibilityOverlay.csUpdate = visibilityOverlay_csUpdate;
        MapChunkVisibilityOverlay.rasterizerSolid = visibilityOverlay_rasterizerSolid;
        MapChunkVisibilityOverlay.rasterizerMin = visibilityOverlay_rasterizerMin;
        MapChunkVisibilityOverlay.visibilityOverlayMaterial = visibilityOverlayMaterial;
        Generate();
    }

    /// <summary>
    /// Generates map
    /// </summary>
    public void Generate()
    {
        Profiler.BeginSample("MapGen");
        graph = new Graph(Seed);
        graph.Generate(10000, 0.025f, 2);
        
        GraphAddNoise();
        graph.UpdateCornersPosition();
        graph.UpdateNodesPosition();
        WaterLevel = GenerateWater(WaterPercentage);
        graph.SetCornersMinLevel(WaterLevel);
        MountainLevel = GenerateMountains(MountainPercentage);
        GenerateMountaintops(0.05f);
        GenerateRivers(200);
        
        CreateMapObjects();
        CreateOctree();
        Profiler.EndSample();
    }

    #region Mapgen
    /// <summary>
    /// Generates noise for graph
    /// </summary>
    public void GraphAddNoise()
    {
        Profiler.BeginSample("GraphAddNoise");
        System.Random random = new System.Random(Seed);
        Vector3 offset = new Vector3(random.Next(-1000, 1000), random.Next(-1000, 1000), random.Next(-1000, 1000));
        for (int i = 0; i < graph.corners.Count; i++)
            graph.corners[i].height = GetCornerNoise(graph.corners[i].Position + offset);
        Profiler.EndSample();
    }

    private float GetCornerNoise(Vector3 pos)
    {
        pos *= 1.2f;
        float a = (SimplexNoise.Noise.Generate(pos.x, pos.y, pos.z) + SimplexNoise.Noise.Generate(pos.x * 2, pos.y * 2, pos.z * 2) / 2 +
            SimplexNoise.Noise.Generate(pos.x * 4, pos.y * 4, pos.z * 4) / 4) / 1.75f + 0.2f;
        if (a > 0)
        {
            a = Mathf.Pow(a, 3f);
            if (a > 1)
                a = 1 - (a - 1);
            a = Mathf.Sqrt(a);
        }
        return a;
    }

    /// <summary>
    /// Sets certain percentage of tiles as water
    /// </summary>
    /// <param name="percentage"></param>
    /// <returns>Water level</returns>
    private float GenerateWater(float percentage = 0.3f)
    {
        Profiler.BeginSample("Generate water");
        int i;

        if (percentage <= 0)//No water
            return 0;
        if (percentage >= 1)//Set all tiles as sea
        {
            for (i = 0; i < graph.nodes.Count; i++)
                graph.nodes[i].type = Graph.Node.NodeType.Sea;//Set is as Sea
            return 2;
        }

        int requiredTiles = (int)(percentage * graph.nodes.Count);//Convert percentage to number of tiles
        int prevTiles = 0;//Count of water tiles on previous iteration
        float prevLevel = .5f;//Previous water height
        int curTiles = 0;//Count of water tiles on current iteration
        float curLevel = prevLevel + .05f;//Current water level

        while (curLevel <= 2)//Iterate from minimum height of tiles to maximum height of tiles
        {
            for (i = 0; i < graph.nodes.Count; i++)//Check all nodes
                if (graph.nodes[i].Height <= curLevel)//See if node is below current water level
                    curTiles++;

            if (curTiles >= requiredTiles)//If enough nodes are water
            {
                //Lerp between previous and current iteration for more precise water level
                curLevel = Mathf.Lerp(prevLevel, curLevel, (curTiles - requiredTiles) / (curTiles - prevTiles));
                break;
            }
            //Update variables for next iteration
            prevTiles = curTiles;
            prevLevel = curLevel;
            curLevel += 0.05f;
            curTiles = 0;
        }

        for (i = 0; i < graph.nodes.Count; i++)
            if (graph.nodes[i].Height <= curLevel)//If node is below final water level
                graph.nodes[i].type = Graph.Node.NodeType.Sea;//Set is as Sea
        Profiler.EndSample();

        return curLevel;
    }

    /// <summary>
    /// Generates mountains
    /// </summary>
    /// <param name="percentage"></param>
    /// <returns>Mountain level</returns>
    private float GenerateMountains(float percentage = 0.3f)
    {
        return SetTopTilesAs(percentage, Graph.Node.NodeType.Mountains);
    }

    /// <summary>
    /// Generates mountaintops
    /// </summary>
    /// <param name="percentage"></param>
    /// <returns></returns>
    private float GenerateMountaintops(float percentage = 0.1f)
    {
        return SetTopTilesAs(percentage, Graph.Node.NodeType.Mountaintop);
    }

    /// <summary>
    /// Sets certain percentage of top tiles as type
    /// </summary>
    /// <param name="percentage"></param>
    /// <returns></returns>
    private float SetTopTilesAs(float percentage, Graph.Node.NodeType type)
    {
        Profiler.BeginSample("SetTopTilesAs");
        int i;

        if (percentage <= 0)
            return 0;
        if (percentage >= 1)
        {
            for (i = 0; i < graph.nodes.Count; i++)
                graph.nodes[i].type = type;
            return 2;
        }

        int requiredTiles = (int)(percentage * graph.nodes.Count);
        int prevTiles = 0;
        float prevLevel = 2;
        int curTiles = 0;
        float curLevel = prevLevel - .05f;

        while (curLevel >= 1)
        {
            for (i = 0; i < graph.nodes.Count; i++)
                if (graph.nodes[i].Height >= curLevel)
                    curTiles++;
            if (curTiles >= requiredTiles)
            {
                curLevel = Mathf.Lerp(prevLevel, curLevel, (curTiles - requiredTiles) / (curTiles - prevTiles));
                break;
            }
            prevTiles = curTiles;
            prevLevel = curLevel;
            curLevel -= 0.05f;
            curTiles = 0;
        }

        for (i = 0; i < graph.nodes.Count; i++)
            if (graph.nodes[i].Height >= curLevel)
                graph.nodes[i].type = type;
        Profiler.EndSample();

        return curLevel;
    }

    /// <summary>
    /// Finds coasts and sets their type
    /// </summary>
    private void FindCoasts()
    {
        Graph.Node n, n2;
        int j;

        for (int i = 0; i < graph.nodes.Count; i++)
            if ((n = graph.nodes[i]).type == Graph.Node.NodeType.Sea)
                for (j = 0; j < n.edges.Count; j++)
                    if ((n2 = n.edges[j].GetOtherNode(n)).type == Graph.Node.NodeType.Ground)
                        n2.type = Graph.Node.NodeType.Coast;
    }

    /// <summary>
    /// Generates rivers
    /// </summary>
    /// <param name="count">Number of rivers</param>
    private void GenerateRivers(int count)
    {
        Graph.Corner cur, c = null;
        int i;
        float minHeightDelta, t;
        Graph.Corner cminHeightDelta = null;
        Graph.Edge e, eminHeightDelta = null;
        List<Graph.Edge> ebuf = new List<Graph.Edge>();//Stores processed edges
        System.Random random = new System.Random(Seed);

        while (count > 0)//Don't stop until all rivers are generated
        {
            cycleStart:
            cur = graph.corners[random.Next(0, graph.corners.Count)];//Randomly select a corner
            if (IsCornerNearNodeOfType(cur, Graph.Node.NodeType.Sea))//If corner is already near sea - ignore it
                continue;

            while (true)//Go down edges
            {
                minHeightDelta = 5;
                for (i = 0; i < cur.edges.Count; i++)//Loop through all connected edges
                {
                    if ((t = (c = (e = cur.edges[i]).GetOtherCorner(cur)).height - cur.height) < minHeightDelta && !c.processed)//Find edge with minimum height difference
                    {
                        minHeightDelta = t;
                        cminHeightDelta = c;
                        eminHeightDelta = e;
                    }
                    c.processed = true;
                }

                if (minHeightDelta > 0.1f)//No more edges to go down on. Bad river
                {
                    ebuf.Clear();//Clear edge buffer
                    goto cycleStart;//And start over
                }

                ebuf.Add(eminHeightDelta);//Add new edge to buffer

                cur = cminHeightDelta;
                if (IsCornerNearNodeOfType(cur, Graph.Node.NodeType.Sea))//Generate river if new corner is in sea
                    break;
            }

            for (i = 0; i < graph.corners.Count; i++)//Reset flags
                graph.corners[i].processed = false;

            for (i = 0; i < ebuf.Count; i++)//For every edge in buffer
            {
                ebuf[i].type = Graph.Edge.EdgeType.River;
                ebuf[i].riverWidth = Mathf.Max(0.05f, Mathf.Min(.2f, ebuf[i].riverWidth + 0.05f));//Increase river width
            }
            ebuf.Clear();

            count--;
        }
    }

    private bool IsCornerNearNodeOfType(Graph.Corner c, Graph.Node.NodeType t)
    {
        for (int i = 0; i < c.edges.Count; i++)
            if (c.edges[i].N1.type == t || c.edges[i].N2.type == t)
                return true;
        return false;
    }
    #endregion



    /// <summary>
    /// Updates octree and processes input
    /// </summary>
    void Update()
    {
        UpdateOctreeVisibility();

        for (int i = 0; i < octree.deepestNodes.Length; i++)
            octree.deepestNodes[i].value.Update();

        if (Input.GetButton("Fire1"))
        {
            var r = Camera.main.ScreenPointToRay(Input.mousePosition);
            MapTile obj = GetTileAtIntersection(r);
            if (obj != null)
                obj.SetVisible();
        }
        if (Input.GetButton("Fire3"))
        {
            var r = Camera.main.ScreenPointToRay(Input.mousePosition);
            MapTile obj = GetTileAtIntersection(r);
            if (obj != null)
                obj.SetSeen();
        }
    }

    /// <summary>
    /// Creates objects for tiles and rivers
    /// </summary>
    private void CreateMapObjects()
    {
        Profiler.BeginSample("Create tiles");
        mapObjects.Clear();

        Graph.Node n, n2;
        int j;
        for (int i = 0; i < graph.nodes.Count; i++)
        {
            n = graph.nodes[i];
            mapTiles.Add(new MapTile(n, graph.GetMeshForNode(i), MaterialFromNodeType(n.type)));
        }

        //Establish tile neighbors
        for (int i = 0; i < graph.nodes.Count; i++)
        {
            n = graph.nodes[i];
            for (j = 0; j < n.edges.Count; j++)
            {
                n2 = n.edges[j].GetOtherNode(n);
                mapTiles[i].AddNeighbor(mapTiles[n2.Index]);
            }
        }

        var a = graph.GetMeshesForRivers(WaterLevel);
        var e = a.GetEnumerator();
        while (e.MoveNext())
            mapObjects.Add(new MapRiver(graph.corners[e.Current.Key], e.Current.Value, materialRiver));
        Profiler.EndSample();
    }
    
    /// <summary>
    /// Renders map
    /// </summary>
    void OnRenderObject()
    {
        //Render objects
        for (int i = 0; i < octree.deepestNodes.Length; i++)
            if (octree.deepestNodes[i].gameObject.activeInHierarchy)
                octree.deepestNodes[i].value.Render();

        //Render wirerame
        materialWireframe.SetPass(0);
        for (int i = 0; i < octree.deepestNodes.Length; i++)
            if (octree.deepestNodes[i].gameObject.activeInHierarchy)
                octree.deepestNodes[i].value.RenderWireframe();

        //Render visibility overlay
        for (int i = 0; i < octree.deepestNodes.Length; i++)
            if (octree.deepestNodes[i].gameObject.activeInHierarchy)
                octree.deepestNodes[i].value.RenderVisibilityOverlay();
    }

    private Material MaterialFromNodeType(Graph.Node.NodeType t)
    {
        switch (t)
        {
            case Graph.Node.NodeType.Sea:
                return materialSea;
            case Graph.Node.NodeType.Coast:
                return materialCoast;
            case Graph.Node.NodeType.Ground:
                return materialGround;
            case Graph.Node.NodeType.Mountains:
                return materialMountains;
            case Graph.Node.NodeType.Mountaintop:
                return materialMountaintop;
        }
        return null;
    }

    /// <summary>
    /// Initializes octree, sorts map objects and removes empty nodes
    /// </summary>
    private void CreateOctree()
    {
        octree.InitializeTree(2, transform);
        PlaceObjectsInTree();
        octree.RemoveEmptyTreeNodes();
        InitializeMapChunks();
    }

    /// <summary>
    /// Sorts objects into octree
    /// </summary>
    private void PlaceObjectsInTree()
    {
        int j;
        MapTile t;
        OctreeNode n;
        for (int i = 0; i < mapTiles.Count; i++)//Sort all tiles
        {
            t = mapTiles[i];
            for (j = 0; j < octree.deepestNodes.Length; j++)//Go through all deepest nodes
                if (octree.deepestNodes[j].AABB.Contains(t.GraphNode.Position))//If tile inside node
                {
                    (n = octree.deepestNodes[j]).value.mapObjects.Add(mapTiles[i]);//Add it as an object
                    n.value.mapTiles.Add(mapTiles[i]);//Add it as a tile
                    n.AABB.Encapsulate(t.AABB);//Expand node's AABB
                    break;
                }
        }

        MapObject o;
        for (int i = 0; i < mapObjects.Count; i++)//Sort all objects
        {
            o = mapObjects[i];
            for (j = 0; j < octree.deepestNodes.Length; j++)//Go through all deepest nodes
                if (octree.deepestNodes[j].AABB.Contains(o.Position))//If tile inside node
                {
                    octree.deepestNodes[j].value.mapObjects.Add(mapObjects[i]);//Add it as an object
                    break;
                }
        }


        for (j = 0; j < octree.deepestNodes.Length; j++)//Expand all nodes' AABB
            octree.deepestNodes[j].ExpandParentAABB();
    }
    
    private void InitializeMapChunks()
    {
        for (int j = 0; j < octree.deepestNodes.Length; j++)
            octree.deepestNodes[j].value.Initialize();
    }

    public void UpdateOctreeVisibility()
    {
        var p = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        __updateOctreeNodeVisibility(octree.trunk, p);
    }

    /// <summary>
    /// Recursively updates tree visibility
    /// </summary>
    /// <param name="n">Current node</param>
    /// <param name="p">Camera frustum planes</param>
    private void __updateOctreeNodeVisibility(OctreeNode n, Plane[] p)
    {
        if (GeometryUtility.TestPlanesAABB(p, n.AABB) && CanCameraSeeAABB(n.AABB, Camera.main.transform.position))
        {
            n.gameObject.SetActive(true);

            for (int i = 0; i < n.children.Count; i++)
                __updateOctreeNodeVisibility(n.children[i], p);
        }
        else
            n.gameObject.SetActive(false);
    }

    /// <summary>
    /// Checks wether AABB can be seen from certain point
    /// </summary>
    /// <param name="b"></param>
    /// <param name="campos"></param>
    /// <returns></returns>
    private bool CanCameraSeeAABB(Bounds b, Vector3 campos)
    {
        Vector3 e = b.extents;
        Vector3 t;

        t = b.center + new Vector3(e.x, e.y, e.z) - campos;
        if (!Physics.Raycast(campos, t, t.magnitude))
            return true;

        t = b.center + new Vector3(-e.x, e.y, e.z) - campos;
        if (!Physics.Raycast(campos, t, t.magnitude))
            return true;

        t = b.center + new Vector3(e.x, -e.y, e.z) - campos;
        if (!Physics.Raycast(campos, t, t.magnitude))
            return true;

        t = b.center + new Vector3(-e.x, -e.y, e.z) - campos;
        if (!Physics.Raycast(campos, t, t.magnitude))
            return true;

        t = b.center + new Vector3(e.x, e.y, -e.z) - campos;
        if (!Physics.Raycast(campos, t, t.magnitude))
            return true;

        t = b.center + new Vector3(-e.x, e.y, -e.z) - campos;
        if (!Physics.Raycast(campos, t, t.magnitude))
            return true;

        t = b.center + new Vector3(e.x, -e.y, -e.z) - campos;
        if (!Physics.Raycast(campos, t, t.magnitude))
            return true;

        t = b.center + new Vector3(-e.x, -e.y, -e.z) - campos;
        if (!Physics.Raycast(campos, t, t.magnitude))
            return true;

        return false;
    }

    /// <summary>
    /// Tile picking
    /// </summary>
    /// <param name="ray"></param>
    /// <returns>Returns closest tile ray intersects</returns>
    public MapTile GetTileAtIntersection(Ray ray)
    {
        OctreeNode n;
        List<OctreeNode> openNodes = new List<OctreeNode>();
        List<OctreeNode> openDeepestNodes = new List<OctreeNode>();
        List<MapTile> openTiles = new List<MapTile>();
        int i, j;
        MapTile m;
        int curNode = 0;
        int curTile = 0;

        openNodes.Add(octree.trunk);//Start check with trunk

        while (curNode < openNodes.Count)//Process all nodes in queue
        {
            n = openNodes[curNode++];
            if (n.children.Count == 0)//If deepest node
            {
                openDeepestNodes.Add(n);//Add to list
                continue;
            }
            for (i = 0; i < n.children.Count; i++)//Loop through all children
                if (n.gameObject.activeInHierarchy && n.children[i].AABB.IntersectRay(ray))//If ray hit AABB
                    openNodes.Add(n.children[i]);//Add to processing queue
        }

        for (i = 0; i < openDeepestNodes.Count; i++)//Loop through deepest nodes
        {
            n = openDeepestNodes[i];
            for (j = 0; j < n.value.mapTiles.Count; j++)//Loop through all tiles in node
            {
                m = n.value.mapTiles[j];
                if (m.AABB.IntersectRay(ray))//If ray intersects tile's AABB
                    openTiles.Add(m);//Add it to tile queue
            }
        }
        
        Graph.Node gn;
        Graph.Edge ge;
        float mind = 1000000, d;
        MapTile minmap = null;

        for (curTile = 0; curTile < openTiles.Count; curTile++)//Loop through all potential tiles
        {
            gn = openTiles[curTile].GraphNode;
            for (i = 0; i < gn.edges.Count; i++)//Loop through tile's edges
            {
                ge = gn.edges[i];
                if ((d = UtilMath.TriangleRayIntersection(gn.Position, ge.C1.Position, ge.C2.Position, ray)) > 0)//If ray intersects triangle created from tile middle and edge points
                {
                    if (d > mind)//There already is a tile that is closer to camera
                        break;//Ignore this one

                    mind = d;//Save this tile
                    minmap = openTiles[curTile];
                    break;//Ignore the rest of triangles in this tile
                }
            }
        }

        return minmap;//Return closest tile or null if none was hit
    }
}
