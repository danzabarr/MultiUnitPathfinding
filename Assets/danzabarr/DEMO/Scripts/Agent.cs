using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Agent : Waypoint, IAgent<Node>
{
	private Map map; // cached reference

	private List<Node> path = new List<Node>();

	public float speed = 1;

	public override void Update()
	{
		base.Update(); // update the waypoint position

		if (HasPath())
		{
			Node next = GetNext();
			Vector3 direction = (next.position - transform.position).normalized;
			transform.position += direction * speed * Time.deltaTime;

			if (Vector3.Distance(transform.position, next.position) < 0.1f)
				Pop();
		}
	}

	public bool HasPath() => path != null && path.Count > 0;

	public Node Pop()
	{
		if (!HasPath())
			return null;
		Node next = path[0];
		path.RemoveAt(0);
		return next;
	}
	 
	public void ClearPath()
	{
		path.Clear();
	}

	public AgentTask LookForTask()
	{
		return null;
	}

	public Node GetStart()
	{
		return path[0];
	}

	public Node GetGoal()
	{
		return path[path.Count - 1];
	}

	public Node GetNext()
	{
		return HasPath() ? path[0] : null;
	}

    public void SetPath(IEnumerable<Node> path)
    {
		if (path == null)
			this.path.Clear();
		else
			this.path = new List<Node>(path);
    }

	public void PathTo(Vector3 goal)
	{
		ClearPath();

		if (map == null)
			map = FindObjectOfType<Map>();

		if (map != null)
			SetPath(map.AStar(Node, goal));
	}

	public void PathTo(Node goal)
	{
		ClearPath();

		if (map == null)
			map = FindObjectOfType<Map>();

		if (map != null)
			SetPath(map.AStar(Node, goal.position));
	}

	public void PathTo(string waypoint)
	{
		ClearPath();

		GameObject obj = GameObject.Find(waypoint);
		if (obj == null)
			return;

		if (!obj.TryGetComponent<Waypoint>(out var wp))
			return;

		if (!wp.enabled)
			return;

		PathTo(wp.Node);
	}
}
