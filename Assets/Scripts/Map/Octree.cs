using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class OctreeNode
{
    [System.NonSerialized]
    public List<OctreeNode> children = new List<OctreeNode>();//List of child nodes
    public MapChunk value = null;//Values of deepest nodes
    public Bounds AABB = new Bounds();//Node bounds
    public GameObject gameObject;//Used for visualization
    [System.NonSerialized]
    public OctreeNode parent = null;//Parent node

    /// <summary>
    /// Expands AABB of parent node
    /// </summary>
    public void ExpandParentAABB()
    {
        if (parent != null)
        {
            parent.AABB.Encapsulate(AABB);
            parent.ExpandParentAABB();
        }
    }
}

[System.Serializable]
public class Octree
{
    public OctreeNode trunk { get; private set; }//Tree trunk
    public OctreeNode[] deepestNodes { get; private set; }//Array of deepest nodes

    /// <summary>
    /// Initializes and divides tree
    /// </summary>
    /// <param name="depthDivision">Depth of division</param>
    /// <param name="transformParent">Parent for tree trunk GameObject</param>
    public void InitializeTree(int depthDivision, Transform transformParent = null)
    {
        trunk = new OctreeNode();
        trunk.gameObject = new GameObject();
        trunk.gameObject.transform.parent = transformParent;
        trunk.AABB = new Bounds(Vector3.zero, new Vector3(4, 4, 4));
        DivideTree(2);
        InitializeDeepestNodesValue();
    }

    /// <summary>
    /// Starts recursive tree division and updates deepestNodes
    /// </summary>
    /// <param name="depth"></param>
    private void DivideTree(int depth)
    {
        List<OctreeNode> _deepestNodes = new List<OctreeNode>();
        DivideTreeNode(trunk, depth, _deepestNodes);
        deepestNodes = _deepestNodes.ToArray();
    }

    /// <summary>
    /// Recursively divides tree
    /// </summary>
    /// <param name="n">Current node</param>
    /// <param name="depth">Depth of division</param>
    /// <param name="_deepestNodes">List to add deepest nodes to</param>
    private void DivideTreeNode(OctreeNode n, int depth, List<OctreeNode> _deepestNodes)
    {
        var parentBB = n.AABB;
        OctreeNode t;
        Vector3 e = parentBB.extents /= 2;

        n.children.Add(t = new OctreeNode());
        t.AABB = parentBB;
        t.AABB.center += new Vector3(e.x, e.y, e.z);

        n.children.Add(t = new OctreeNode());
        t.AABB = parentBB;
        t.AABB.center += new Vector3(-e.x, e.y, e.z);

        n.children.Add(t = new OctreeNode());
        t.AABB = parentBB;
        t.AABB.center += new Vector3(e.x, -e.y, e.z);

        n.children.Add(t = new OctreeNode());
        t.AABB = parentBB;
        t.AABB.center += new Vector3(-e.x, -e.y, e.z);

        n.children.Add(t = new OctreeNode());
        t.AABB = parentBB;
        t.AABB.center += new Vector3(e.x, e.y, -e.z);

        n.children.Add(t = new OctreeNode());
        t.AABB = parentBB;
        t.AABB.center += new Vector3(-e.x, e.y, -e.z);

        n.children.Add(t = new OctreeNode());
        t.AABB = parentBB;
        t.AABB.center += new Vector3(e.x, -e.y, -e.z);

        n.children.Add(t = new OctreeNode());
        t.AABB = parentBB;
        t.AABB.center += new Vector3(-e.x, -e.y, -e.z);

        for (int i = 0; i < n.children.Count; i++)
        {
            t = n.children[i];
            t.parent = n;
            t.gameObject = new GameObject();
            t.gameObject.transform.position = t.AABB.center;
            t.gameObject.transform.localScale = t.AABB.size;
            t.gameObject.transform.parent = n.gameObject.transform;
        }

        if (depth-- > 0)
            for (int i = 0; i < n.children.Count; i++)
                DivideTreeNode(n.children[i], depth, _deepestNodes);
        else
            _deepestNodes.AddRange(n.children);
    }

    /// <summary>
    /// Initializes values of deepest nodes
    /// </summary>
    private void InitializeDeepestNodesValue()
    {
        for (int i = 0; i < deepestNodes.Length; i++)
            deepestNodes[i].value = new MapChunk();
    }

    /// <summary>
    /// Starts recursive removal of empty nodes and updates deepestNodes
    /// </summary>
    public void RemoveEmptyTreeNodes()
    {
        List<OctreeNode> _deepestNodes = new List<OctreeNode>();
        _deepestNodes.AddRange(deepestNodes);
        for (int i = 0; i < trunk.children.Count; i++)
            __removeEmptyTreeNodes(trunk.children[i], _deepestNodes);
        deepestNodes = _deepestNodes.ToArray();
    }

    /// <summary>
    /// Recursively removes empty nodes
    /// </summary>
    /// <param name="n">Current node</param>
    /// <param name="_deepestNodes">List of deepest nodes to remove values from</param>
    private void __removeEmptyTreeNodes(OctreeNode n, List<OctreeNode> _deepestNodes)
    {
        for (int i = n.children.Count - 1; i >= 0; i--)
            __removeEmptyTreeNodes(n.children[i], _deepestNodes);
        if (n.children.Count == 0 && (n.value == null || n.value.mapObjects.Count == 0))
        {
            _deepestNodes.Remove(n);
            GameObject.Destroy(n.gameObject);
            n.parent.children.Remove(n);
        }
    }
}
