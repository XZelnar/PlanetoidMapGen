using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Processes user input and updates camera transformation
/// </summary>
public class CameraControl : MonoBehaviour
{
    Vector3 oldMousePos;
    bool oldFire2Pressed = false;
    float oldMouseRot;

    Vector3 rotation = new Vector3();

    float scale = 1;
    public float minScale = 0.6f;
    public float maxScale = 1f;

    void Start()
    {
        oldMouseRot = Input.mouseScrollDelta.y;
    }
	
	void Update ()
	{
        if (oldFire2Pressed)//Rotate camera
        {
            rotation += new Vector3((-Input.mousePosition.y + oldMousePos.y) / 8, (Input.mousePosition.x - oldMousePos.x) / 8, 0) * scale;
            transform.rotation = Quaternion.Euler(rotation);
            oldMousePos = Input.mousePosition;
        }

        if (oldFire2Pressed && Input.GetButtonUp("Fire2"))
            oldFire2Pressed = false;
        if (!oldFire2Pressed && Input.GetButtonDown("Fire2"))
        {
            oldFire2Pressed = true;
            oldMousePos = Input.mousePosition;
        }

        if (Input.mouseScrollDelta.y != 0)//Zoom in/out
        {
            scale = Mathf.Clamp(scale - Input.mouseScrollDelta.y / 20, minScale, maxScale);
            transform.localScale = new Vector3(scale, scale, scale);
        }
    }
}
