using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float movementSpeed = 8.0f;
    public float followTranslateSpeed = 0.5f;
	public float followRotateSpeed = 0.1f;

    private Transform target;

	void Start()
	{
		target = new GameObject("Camera Follow Target").transform;
		target.position = transform.position;
		target.rotation = transform.rotation;
	}

	void Update()
    {
        if (Input.GetKey(KeyCode.W))
			target.position += target.forward.XZ().X0Y() * movementSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.S))
			target.position -= target.forward.XZ().X0Y() * movementSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.A))
            target.position -= target.right.XZ().X0Y() * movementSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.D))
            target.position += target.right.XZ().X0Y() * movementSpeed * Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            Vector3 targetPosition = target.position - target.forward.XZ().X0Y() * target.position.y / transform.forward.y;
			target.RotateAround(targetPosition, Vector3.up, Mathf.RoundToInt(target.eulerAngles.y / 45) * 45 + 45 - target.eulerAngles.y);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
			Vector3 targetPosition = target.position - target.forward.XZ().X0Y() * target.position.y / target.forward.y;
			target.RotateAround(targetPosition, Vector3.up, Mathf.RoundToInt(target.eulerAngles.y / 45) * 45 - 45 - target.eulerAngles.y);
		}

        if (Input.mousePresent)
        {
            //transform.RotateAround(transform.position, Vector3.up, Input.GetAxis("Mouse X"));
            //transform.RotateAround(transform.position, transform.right, -Input.GetAxis("Mouse Y"));
		}
        
        transform.position = Vector3.Slerp(transform.position, target.position, followTranslateSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, followRotateSpeed * Time.deltaTime);
    }
}
