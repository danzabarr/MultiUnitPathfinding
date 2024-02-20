using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float speed = 0.1f;
    void Update()
    {
        if (Input.GetKey(KeyCode.W))
            transform.position += transform.forward.XZ().X0Z() * speed;

        if (Input.GetKey(KeyCode.S))
            transform.position -= transform.forward.XZ().X0Z() * speed;

        if (Input.GetKey(KeyCode.A))
            transform.position -= transform.right.XZ().X0Z() * speed;

        if (Input.GetKey(KeyCode.D))
            transform.position += transform.right.XZ().X0Z() * speed;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            Vector3 target = transform.position - transform.forward.XZ().X0Z() * transform.position.y / transform.forward.y;
            transform.RotateAround(target, Vector3.up, Mathf.RoundToInt(transform.eulerAngles.y / 45) * 45 + 45 - transform.eulerAngles.y);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
			Vector3 target = transform.position - transform.forward.XZ().X0Z() * transform.position.y / transform.forward.y;
			transform.RotateAround(target, Vector3.up, Mathf.RoundToInt(transform.eulerAngles.y / 45) * 45 - 45 - transform.eulerAngles.y);
		}

        if (Input.mousePresent)
        {
            //transform.RotateAround(transform.position, Vector3.up, Input.GetAxis("Mouse X"));
            //transform.RotateAround(transform.position, transform.right, -Input.GetAxis("Mouse Y"));
		}
        
    }
}
