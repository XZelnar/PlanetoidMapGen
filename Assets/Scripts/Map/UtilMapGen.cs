using UnityEngine;
using System.Collections.Generic;

public static class UtilMapGen
{
    public static Vertex[] CreateRandomPointsSph(int count, float distanceThreshold, int seed)
    {
        System.Random rnd = new System.Random(seed);//Initialize RNG with provided seed
        distanceThreshold /= 2;

        Profiler.BeginSample("CreateRandomPointsSph");
        Vertex[] points = new Vertex[count];//Create array to store points
        int p3ddim = (int)(2f / distanceThreshold);//Number of cells for spatial indexing
        bool[,,] p3d = new bool[p3ddim + 1, p3ddim + 1, p3ddim + 1];//Spatial index

        //Add poles
        points[0] = new Vertex(new double[] { 0, 1, 0 });
        points[1] = new Vertex(new double[] { 0, -1, 0 });
        //Spatial index = (coordinate + 1) / 2 * spatialIndexDimantion
        p3d[(int)((0 + 1f) / 2 * p3ddim), (int)((1 + 1f) / 2 * p3ddim), (int)((0 + 1f) / 2 * p3ddim)] = true;
        p3d[(int)((0 + 1f) / 2 * p3ddim), (int)((-1 + 1f) / 2 * p3ddim), (int)((0 + 1f) / 2 * p3ddim)] = true;

        Vector2 t2;
        Vector3 t3 = Vector3.zero;
        float d2 = distanceThreshold * distanceThreshold;
        int cur = 2;

        int p3dx, p3dy, p3dz;

        int iter = count * 10;
        while (cur < count)
        {
            iter--;
            if (iter <= 0)
                throw new System.Exception("CreateRandomPointsSph minDistance too big");
            t2.x = (float)rnd.Next(0, 6283) / 1000f;//Generate azimuthal angle
            t2.y = (float)rnd.Next(0, 3141) / 1000f;//Generate polar angle
            UtilMath.SphericalToCartesian(t2, ref t3);//Transform spherical coordinates to Cartesian coordinates

            //Get point coordinates in spatial index
            p3dx = (int)((t3.x + 1f) / 2 * p3ddim);
            p3dy = (int)((t3.y + 1f) / 2 * p3ddim);
            p3dz = (int)((t3.z + 1f) / 2 * p3ddim);

            //Check nearby cells

            #region y
            if (p3d[p3dx, p3dy, p3dz])
                continue;
            if (p3dx > 0)
            {
                if (p3d[p3dx - 1, p3dy, p3dz])
                    continue;
                if (p3dz > 0)
                    if (p3d[p3dx - 1, p3dy, p3dz - 1])
                        continue;
                if (p3dz < p3ddim)
                    if (p3d[p3dx - 1, p3dy, p3dz + 1])
                        continue;
            }
            if (p3dx < p3ddim)
            {
                if (p3d[p3dx + 1, p3dy, p3dz])
                    continue;
                if (p3dz > 0)
                    if (p3d[p3dx + 1, p3dy, p3dz - 1])
                        continue;
                if (p3dz < p3ddim)
                    if (p3d[p3dx + 1, p3dy, p3dz + 1])
                        continue;
            }
            if (p3dz > 0)
                if (p3d[p3dx, p3dy, p3dz - 1])
                    continue;
            if (p3dz < p3ddim)
                if (p3d[p3dx, p3dy, p3dz + 1])
                    continue;
            #endregion

            #region y - 1
            if (p3dy > 0)
            {
                if (p3d[p3dx, p3dy - 1, p3dz])
                    continue;
                if (p3dx > 0)
                {
                    if (p3d[p3dx - 1, p3dy - 1, p3dz])
                        continue;
                    if (p3dz > 0)
                        if (p3d[p3dx - 1, p3dy - 1, p3dz - 1])
                            continue;
                    if (p3dz < p3ddim)
                        if (p3d[p3dx - 1, p3dy - 1, p3dz + 1])
                            continue;
                }
                if (p3dx < p3ddim)
                {
                    if (p3d[p3dx + 1, p3dy - 1, p3dz])
                        continue;
                    if (p3dz > 0)
                        if (p3d[p3dx + 1, p3dy - 1, p3dz - 1])
                            continue;
                    if (p3dz < p3ddim)
                        if (p3d[p3dx + 1, p3dy - 1, p3dz + 1])
                            continue;
                }
                if (p3dz > 0)
                    if (p3d[p3dx, p3dy - 1, p3dz - 1])
                        continue;
                if (p3dz < p3ddim)
                    if (p3d[p3dx, p3dy - 1, p3dz + 1])
                        continue;
            }
            #endregion

            #region y + 1
            if (p3dy < p3ddim)
            {
                if (p3d[p3dx, p3dy + 1, p3dz])
                    continue;
                if (p3dx > 0)
                {
                    if (p3d[p3dx - 1, p3dy + 1, p3dz])
                        continue;
                    if (p3dz > 0)
                        if (p3d[p3dx - 1, p3dy + 1, p3dz - 1])
                            continue;
                    if (p3dz < p3ddim)
                        if (p3d[p3dx - 1, p3dy + 1, p3dz + 1])
                            continue;
                }
                if (p3dx < p3ddim)
                {
                    if (p3d[p3dx + 1, p3dy + 1, p3dz])
                        continue;
                    if (p3dz > 0)
                        if (p3d[p3dx + 1, p3dy + 1, p3dz - 1])
                            continue;
                    if (p3dz < p3ddim)
                        if (p3d[p3dx + 1, p3dy + 1, p3dz + 1])
                            continue;
                }
                if (p3dz > 0)
                    if (p3d[p3dx, p3dy + 1, p3dz - 1])
                        continue;
                if (p3dz < p3ddim)
                    if (p3d[p3dx, p3dy + 1, p3dz + 1])
                        continue;
            }
            #endregion
            
            p3d[p3dx, p3dy, p3dz] = true;
            points[cur++] = new Vertex(t3);//Add point to array
        }
        Profiler.EndSample();

        return points;
    }
}
