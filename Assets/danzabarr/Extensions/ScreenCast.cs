using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ScreenCast : MonoBehaviour
{
    private Camera cam;

    private void Start()
    {
		cam = GetComponent<Camera>();
	}

    public Vector3 ScreenToWorld(Vector3 screenPos)
    {
		if (cam == null)
			cam = GetComponent<Camera>();

		return cam.ScreenToWorldPoint(screenPos);
	}

	public Vector3 WorldToScreen(Vector3 worldPos)
	{
		if (cam == null)
			cam = GetComponent<Camera>();

		return cam.WorldToScreenPoint(worldPos);
	}

	public Ray ScreenPointToRay(Vector3 screenPos)
	{
		if (cam == null)
			cam = GetComponent<Camera>();

		return cam.ScreenPointToRay(screenPos);
	}

	public bool RayPlaneIntersection(Ray ray, Plane plane, out float distance) 
	{
		return plane.Raycast(ray, out distance);
	}

	public bool RayXZPlaneIntersection(Ray ray, float y, out float distance)
	{
		return RayPlaneIntersection(ray, new Plane(Vector3.up, new Vector3(0, y, 0)), out distance);
	}
	
	public bool ScreenPointOnPlane(Vector3 screenPos, Plane plane, out Vector3 point, out float distance)
	{
		Ray ray = ScreenPointToRay(screenPos);
		if (RayPlaneIntersection(ray, plane, out distance))
		{
			point = ray.GetPoint(distance);
			return true;
		}
		point = Vector3.zero;
		return false;
	}

	public bool ScreenPointOnXZPlane(Vector3 screenPos, float y, out Vector3 point, out float distance)
	{
		return ScreenPointOnPlane(screenPos, new Plane(Vector3.up, new Vector3(0, y, 0)), out point, out distance);
	}

	public bool MouseOnPlane(Plane plane, out Vector3 point, out float distance)
	{
		return ScreenPointOnPlane(Input.mousePosition, plane, out point, out distance);
	}

	public bool MouseOnXZPlane(float y, out Vector3 point, out float distance)
	{
		return ScreenPointOnXZPlane(Input.mousePosition, y, out point, out distance);
	}
}
