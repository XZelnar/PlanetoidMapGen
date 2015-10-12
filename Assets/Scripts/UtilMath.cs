using UnityEngine;
using System.Collections.Generic;

public static class UtilMath
{
    //https://en.wikipedia.org/wiki/M%C3%B6ller%E2%80%93Trumbore_intersection_algorithm
    public static float TriangleRayIntersection(Vector3 V1, Vector3 V2, Vector3 V3, Ray R)
    {
        Vector3 e1, e2;
        Vector3 P, Q, T;
        float det, inv_det, u, v;
        float t;
        
        e1 = V2 - V1;
        e2 = V3 - V1;
        P = Vector3.Cross(R.direction, e2);
        det = Vector3.Dot(e1, P);
        if (det > -0.0001f && det < 0.0001f)
            return -1;
        inv_det = 1.0f / det;
        
        T = R.origin-V1;
        
        u = Vector3.Dot(T, P) * inv_det;
        if (u < 0f || u > 1f)
            return -2;
        
        Q = Vector3.Cross(T, e1);
        
        v = Vector3.Dot(R.direction, Q) * inv_det;
        if (v < 0f || u + v > 1f)
            return -3;

        t = Vector3.Dot(e2, Q) * inv_det;

        if (t > 0.0001f)
            return t;

        return -4;
    }

    public static void SphericalToCartesian(Vector2 positionSpherical, ref Vector3 positionCartesian)
    {
        positionCartesian.x = Mathf.Cos(positionSpherical.x) * Mathf.Sin(positionSpherical.y);
        positionCartesian.z = Mathf.Sin(positionSpherical.x) * Mathf.Sin(positionSpherical.y);
        positionCartesian.y = Mathf.Cos(positionSpherical.y);
    }




    //RNG with Gaussian distribution
    public static float NextGaussian(ref System.Random rnd)
    {
        float NextGaussian_v1, NextGaussian_v2, NextGaussian_s;
        do
        {
            NextGaussian_v1 = 2.0f * (float)rnd.NextDouble() - 1.0f;
            NextGaussian_v2 = 2.0f * (float)rnd.NextDouble() - 1.0f;
            NextGaussian_s = NextGaussian_v1 * NextGaussian_v1 + NextGaussian_v2 * NextGaussian_v2;
        } while (NextGaussian_s >= 1.0f || NextGaussian_s == 0f);

        NextGaussian_s = Mathf.Sqrt((-2.0f * Mathf.Log(NextGaussian_s)) / NextGaussian_s);

        return NextGaussian_v1 * NextGaussian_s;
    }

    //RNG with Gaussian distribution
    public static float NextGaussian(float max, ref System.Random rnd)
    {
        return NextGaussian(ref rnd) * max;
    }
}