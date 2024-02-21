using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public interface IAgent<Node>
{
	/// <summary>
	/// Set the path for the agent to follow.
	/// </summary>
	/// <param name="path"></param>
	void SetPath(IEnumerable<Node> path);

	/// <summary>
	/// Removes the current path.
	/// </summary>
	void ClearPath();

	/// <summary>
	/// Implementer returns true if the agent has a path.
	/// </summary>
	/// <returns></returns>
	bool HasPath();

	/// <summary>
	/// The node at the start of the path.
	/// </summary>
	/// <returns></returns>
	Node GetStart();

	/// <summary>
	/// The goal node.
	/// </summary>
	/// <returns></returns>
	Node GetGoal();

	/// <summary>
	/// Implementer must return the node that the agent is currently moving towards.
	/// </summary>
	/// <returns></returns>
	Node GetNext();
}	


public class Agent : MonoBehaviour
{
	private List<Node> path = new List<Node>();

	public float speed = 1;

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
				Pop();
		}
	}

	public bool HasPath => path != null && path.Count > 0;

	public Node NextNode => HasPath ? path[0] : null;

	public Node Pop()
	{
		if (!HasPath)
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
}


