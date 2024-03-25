using System.Collections.Generic;

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