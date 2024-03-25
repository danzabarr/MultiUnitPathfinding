using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// disables the camera and uses the Render function to render the scene at a fixed rate
[RequireComponent(typeof(Camera))]
public class CameraThrottle : MonoBehaviour
{
    public Camera cam;
    public float fps = 10;
    
    private static int delayStart = 0;

    void Start () 
    {
        cam = GetComponent<Camera>();
        InvokeRepeating ("Render", ++delayStart, 1f / fps);
    }
    
    void OnDestroy()
    {
        CancelInvoke();
    }

    void Render()
    {
        if (cam == null)
            cam = GetComponent<Camera>();
        //cam.enabled = true;
        cam.Render();
    }
    
    void OnPostRender()
    {
        //if (cam == null)
        //    cam = GetComponent<Camera>();
        //cam.enabled = false;
    }
}
