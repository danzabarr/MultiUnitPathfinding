using System.Collections;
using System.Collections.Generic;

public interface IGraph<Node>
{
	IEnumerable<Node> Neighbours(Node current);
	int NeighbourCount(Node current);
	float EdgeCost(Node current, Node next);
	float HeuristicCost(Node current, Node next);
}
