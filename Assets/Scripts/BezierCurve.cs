using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Implementation of Bezier curve with one control point
/// </summary>
public class BezierCurve3
{
    public Vector3 StartPoint { get; set; }
    public Vector3 EndPoint { get; set; }
    public Vector3 P1 { get; set; }

    public Vector3 GetPoint(float t)
    {
        return Vector3.Lerp(Vector3.Lerp(StartPoint, P1, t), Vector3.Lerp(P1, EndPoint, t), t);
    }
}