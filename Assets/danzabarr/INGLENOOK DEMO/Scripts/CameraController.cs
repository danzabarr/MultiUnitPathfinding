using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CameraMode
{
    Follow,
    Focus,
    Free,
}

public class CameraController : MonoBehaviour
{
    
    [Header("General")]
    public CameraMode mode = CameraMode.Follow;
    
    /// <summary>
    /// The target to follow. Change this to change the follow target.
    /// </summary>

    public Transform anchor;

    /// <summary>
    /// The lerp target for the camera. This is the object that the camera will move towards.
    /// This is used to smooth the camera movement.
    /// </summary>
    private Transform lerpTarget;

    public float lerpXZSpeed = 3f;
    public float lerpYSpeed = 1f;
    public float lerpYawSpeed = 3f;
    public float lerpPitchSpeed = 0.5f;

    /// <summary>
    /// Set to offset the follow target in the y axis
    /// </summary>
    public float anchorOffsetY = 2.0f;

    /// <summary>
    /// Set to offset the follow target in the direction of its forward axis
    /// </summary>
    public float anchorOffsetForward = 5.0f;
    

    [Header("Follow Mode")]
    public float followRotationSpeed = 90.0f;
    public float followHeight = 5.0f;
    public float followAngle = 45.0f;

    [Header("Focus Mode")]
    public float focusDistance = 5.0f;
    public float focusHeight = 2.0f;
    public float focusYawOffset = 0.0f;

    [Header("Free Mode")]
    public float freeSpeed = 8.0f;



    public void Free()
    {
        Debug.Log("Camera Mode: Free");
        mode = CameraMode.Free;
    }

    public void Focus(Transform follow)
    {
        Debug.Log($"Camera Mode: Focus on {follow.name}");
        this.anchor = follow;
        mode = CameraMode.Focus;
    }

    public void Follow(Transform follow)
    {
        Debug.Log("Camera Mode: Follow player");
        anchor = follow;
        mode = CameraMode.Follow;
    }

	void Start()
	{
        lerpTarget = new GameObject("Camera Follow Target").transform;
        lerpTarget.position = transform.position;
        lerpTarget.rotation = transform.rotation;
	}

	void Update()
    {
        if (mode == CameraMode.Free || anchor == null)
        {
            if (Input.GetKey(KeyCode.W))
                lerpTarget.position += lerpTarget.forward.XZ().X0Y() * freeSpeed * Time.deltaTime;

            if (Input.GetKey(KeyCode.S))
                lerpTarget.position -= lerpTarget.forward.XZ().X0Y() * freeSpeed * Time.deltaTime;

            if (Input.GetKey(KeyCode.A))
                lerpTarget.position -= lerpTarget.right.XZ().X0Y() * freeSpeed * Time.deltaTime;

            if (Input.GetKey(KeyCode.D))
                lerpTarget.position += lerpTarget.right.XZ().X0Y() * freeSpeed * Time.deltaTime;
        
            if (Input.GetKeyDown(KeyCode.E))
            {
                // not rounded
                Vector3 targetPosition = lerpTarget.position - lerpTarget.forward.XZ().X0Y() * lerpTarget.position.y / transform.forward.y;
                lerpTarget.RotateAround(targetPosition, Vector3.up, followRotationSpeed * Time.deltaTime);
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                Vector3 targetPosition = lerpTarget.position - lerpTarget.forward.XZ().X0Y() * lerpTarget.position.y / lerpTarget.forward.y;
                lerpTarget.RotateAround(targetPosition, Vector3.up, -followRotationSpeed * Time.deltaTime);
            }
        }

        else if (mode == CameraMode.Follow)
        {
            // constraints:
            // the y position of the camera is fixed
            // the rotation of the camera is fixed
            // the camera is always looking at the follow object

            lerpTarget.position = new Vector3(lerpTarget.position.x, followHeight, lerpTarget.position.z);
            lerpTarget.rotation = Quaternion.Euler(followAngle, lerpTarget.eulerAngles.y, 0);

            float targetY = lerpTarget.position.y;
            Vector3 followPosition = anchor.position + anchor.forward.XZ().X0Y() * anchorOffsetForward + Vector3.up * anchorOffsetY;

            lerpTarget.position = followPosition;

            float hypotenuse = Vector3.Distance(transform.position, followPosition);
            // move the camera backwards along the forward vector until its y position is where it should be
            lerpTarget.position -= lerpTarget.forward * hypotenuse;

            lerpTarget.position = new Vector3(lerpTarget.position.x, targetY, lerpTarget.position.z);

            if (Input.GetKeyDown(KeyCode.E))
            {
                Vector3 targetPosition = lerpTarget.position - lerpTarget.forward.XZ().X0Y() * lerpTarget.position.y / transform.forward.y;
                lerpTarget.RotateAround(targetPosition, Vector3.up, Mathf.RoundToInt(lerpTarget.eulerAngles.y / 45) * 45 + 45 - lerpTarget.eulerAngles.y);
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                Vector3 targetPosition = lerpTarget.position - lerpTarget.forward.XZ().X0Y() * lerpTarget.position.y / lerpTarget.forward.y;
                lerpTarget.RotateAround(targetPosition, Vector3.up, Mathf.RoundToInt(lerpTarget.eulerAngles.y / 45) * 45 - 45 - lerpTarget.eulerAngles.y);
            }

            // snap rotation to the nearest 45 degree angle
            lerpTarget.rotation = Quaternion.Euler(followAngle, Mathf.RoundToInt(lerpTarget.eulerAngles.y / 45) * 45, 0);
        }
    
        else if (mode == CameraMode.Focus)
        {
            // constraints:
            // the y position of the camera is fixed
            // the camera moves to look at the follow object's front, 
            // such that their forward vectors in the xz plane are
            // parallel, and the camera is at a fixed distance from the follow object
            // the angle is fixed, and lower than in follow mode


            Vector3 followPosition = anchor.position + anchor.forward.XZ().X0Y() * anchorOffsetForward + Vector3.up * anchorOffsetY;
         
            // the camera is always looking at the follow object
            // it rotates by an offset angle so the camera is not directly behind the object
            // whether to rotate the camera to the left or right is determined by the current position of the camera, the follow object, and the follow object's forward vector

            Vector2 anchorForward = anchor.forward.XZ();
            Vector2 cameraForward = lerpTarget.forward.XZ();
            float angle = Vector2.SignedAngle(anchorForward, cameraForward);


            


            lerpTarget.position = followPosition;
            
            lerpTarget.position += anchor.forward.XZ().X0Y() * focusDistance;
            lerpTarget.position = new Vector3(lerpTarget.position.x, focusHeight, lerpTarget.position.z);
            lerpTarget.LookAt(followPosition);

            //lerpTarget.RotateAround(followPosition, Vector3.up, focusYawOffset);

            //get pos from lerpTarget without changing the rotation
            Vector3 position = lerpTarget.position;
            Vector3 axis = Vector3.up;
            float offset = focusYawOffset;

            position = position - followPosition;
            position = Quaternion.AngleAxis(offset, axis) * position;
            position = position + followPosition;
            float distanceLeft = Vector3.Distance(position, transform.position);

            position = position - followPosition;
            position = Quaternion.AngleAxis(-offset, axis) * position;
            position = position + followPosition;
            float distanceRight = Vector3.Distance(position, transform.position);

            if (distanceLeft < distanceRight)
                lerpTarget.RotateAround(followPosition, Vector3.up, focusYawOffset);
            else
                lerpTarget.RotateAround(followPosition, Vector3.up, -focusYawOffset);
            //rotate the lerpTarget around the follow object by the offset angle






        }

        float x = Mathf.Lerp(transform.position.x, lerpTarget.position.x, lerpXZSpeed * Time.deltaTime);
        float y = Mathf.Lerp(transform.position.y, lerpTarget.position.y, lerpYSpeed * Time.deltaTime);
        float z = Mathf.Lerp(transform.position.z, lerpTarget.position.z, lerpXZSpeed * Time.deltaTime);

        float yaw = Mathf.LerpAngle(transform.eulerAngles.y, lerpTarget.eulerAngles.y, lerpYawSpeed * Time.deltaTime);
        float pitch = Mathf.LerpAngle(transform.eulerAngles.x, lerpTarget.eulerAngles.x, lerpPitchSpeed * Time.deltaTime);

        transform.position = new Vector3(x, y, z);
        transform.eulerAngles = new Vector3(pitch, yaw, 0);

        //transform.position = Vector3.Slerp(transform.position, lerpTarget.position, lerpPositionSpeed * Time.deltaTime);
        //transform.rotation = Quaternion.Slerp(transform.rotation, lerpTarget.rotation, lerpRotationSpeed * Time.deltaTime);
    }
}
