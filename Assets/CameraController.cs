using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float speed = 0.1f;
    void Update()
    {
        if (Input.GetKey(KeyCode.W))
            transform.position += transform.forward * speed;

        if (Input.GetKey(KeyCode.S))
            transform.position -= transform.forward * speed;

        if (Input.GetKey(KeyCode.A))
            transform.position -= transform.right * speed;

        if (Input.GetKey(KeyCode.D))
            transform.position += transform.right * speed;

        if (Input.mousePresent)
        {
            transform.RotateAround(transform.position, Vector3.up, Input.GetAxis("Mouse X"));
            transform.RotateAround(transform.position, transform.right, -Input.GetAxis("Mouse Y"));
		}
        
    }
}
