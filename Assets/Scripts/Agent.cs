using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Agent : MonoBehaviour
{
	private List<Node> path = new List<Node>();
	private Node current;

	public float speed = 1;
	public void PathTo(Node goal, IGraph<Node> graph)
	{
		path = Search.AStar(current, goal, graph, (_, _) => false);
	}

	public void SetPath(List<Node> path)
	{
		this.path = path;
	}

	public void Update()
	{
		if (HasPath)
		{
			Vector3 direction = (NextNode.position - transform.position).normalized;
			transform.position += direction * speed * Time.deltaTime;

			if (Vector3.Distance(transform.position, NextNode.position) < 0.1f)
				current = Pop();
		}
	}

	public Node CurrentNode => current;

	public bool HasPath => path != null && path.Count > 0;

	public Node NextNode => HasPath ? path[0] : current;

	public Node Pop()
	{
		if (HasPath)
		{
			Node next = NextNode;
			path.RemoveAt(0);
			return next;
		}
		return current;
	}
	 
	public void ClearPath()
	{
		path.Clear();
	}

	public AgentTask LookForTask()
	{
		return null;
	}
}


