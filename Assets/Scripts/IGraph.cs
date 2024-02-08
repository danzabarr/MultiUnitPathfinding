using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public interface IGraph<Node>
{
	List<Node> Neighbours(Node current);
	float EdgeCost(Node current, Node next);
	float HeuristicCost(Node current, Node next);
}
