using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Color = UnityEngine.Color;


public class Map : MapGeneratorBase
{
	[Header("Mouse")]
	public Vector2Int mouseTile;
	public Vector3 mouseMesh;
	public Vector2Int mouseChunkCoord;
	public Chunk mouseChunk;
	public Area mouseArea;

	[Header("Gizmos")]
	public bool drawNodes;
	public bool drawEdges;
	public bool drawAreas;
	public bool drawRamps;
	public bool drawCliffs;
	public bool drawPath;

	[Header("Debugging")]
	public Transform start;
	public Transform goal;
	public List<Transform> goals = new List<Transform>();
	public AbstractObstruction obstruction;
	public Transform ray;
	public Agent selected;

	void Update()
	{
		mouseTile = Vector2Int.zero;
		mouseMesh = Vector3.zero;
		mouseChunk = null;
		mouseChunkCoord = Vector2Int.zero;
		mouseArea = null;

		if (Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, out mouseChunk))
		{
			mouseMesh = hit.point;
			mouseTile = hit.point.ToTileCoord();
			mouseChunkCoord = mouseTile.ToChunkCoord(chunkSize);
			mouseArea = GetArea(mouseTile.x, mouseTile.y);
		
			if (Input.GetMouseButtonDown(0) && selected != null)
			{
				selected.SetPath(AStar(selected.transform.position, hit.point));
			}
		}
	}

	void OnDrawGizmos()
	{
		if (drawCliffs)
		{
			Gizmos.color = Color.red;
			foreach (Chunk chunk in chunks.Values)
			{
				for (int x = 0; x < chunkSize; x++)
					for (int y = 0; y < chunkSize; y++)
					{
						Vector2Int tile = new Vector2Int(x, y) + chunk.chunkPosition * chunkSize;
						if (IsCliff(tile))
							Gizmos.DrawCube(OnGround(tile.x, tile.y), Vector3.one);
					}
			}
		}

		if (drawPath && start != null && goal != null)
		{
			if (start != null) start.position = OnGround(start.position.x, start.position.z);
			if (goal != null) goal.position = OnGround(goal.position.x, goal.position.z);

			List<Node> path = AStar(start.position, goal.position);

			if (path != null)
			{
				Gizmos.color = Color.blue;

				foreach (Node neighbour in Neighbours(path[0]))
				{
					Gizmos.DrawLine(path[0].position, neighbour.position);
				}

				for (int i = 1; i < path.Count; i++)
				{
					Gizmos.DrawLine(path[i - 1].position, path[i].position);
					Gizmos.DrawSphere(path[i].position, 0.5f);
				}
			}
		}
		{
			int i = 0;
			foreach (Area area in areas)
			{
				//	areaIndex %= areas.Count;
				//	areaIndex += areas.Count;
				//	areaIndex %= areas.Count;
				//	mouseArea = areas[areaIndex];

				if (drawAreas && area.Tiles != null)
				{
					Gizmos.color = Color.HSVToRGB((float)i / areas.Count, 1f, 1f);
					Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.5f);
					foreach (Vector2Int tile in area.Tiles)
					{
						Gizmos.DrawCube
						(
							new Vector3(tile.x, terrainSettings.Sample(tile.x, tile.y), tile.y),
							new Vector3(1f, 0.001f, 1f)
						);
					}
				}

				//if (false)
				if (drawNodes || drawEdges || GetNode(mouseTile.x, mouseTile.y) != null)
				{
					GUIStyle style = new GUIStyle();
					style.normal.textColor = Color.red;
					foreach (Node node in area.Nodes)
					{
						if (drawNodes)
						{
							Gizmos.color = Color.black;
							Gizmos.DrawSphere(node.position, 0.25f);
						}

						if (drawEdges || node.tile == mouseTile)
						{
							foreach (var pair in adjacency)
							{
								Node n = pair.Key;
								SerializableDictionary<Node, float> dictionary = pair.Value;
								foreach (var pair2 in dictionary)
								{
									Node n2 = pair2.Key;
									float cost = pair2.Value;
									if (n == node)
									{
										Gizmos.color = Color.red;
										Gizmos.DrawLine(n.position, n2.position);
										Handles.Label((n.position + n2.position) / 2.0f, cost.ToString(), style);
									}
								}
							}
						}
					}
				}
				i++;
			}
		}
		if (drawRamps)
		{
			Gizmos.color = Color.yellow;
			foreach (Ramp ramp in ramps)
			{
				Vector2Int start = ramp.position;
				Vector2Int end = start + ramp.length * (ramp.orientation == Orientation.HORIZONTAL ? Vector2Int.right : Vector2Int.up);
			
				Gizmos.DrawWireSphere(start.X0Y(), 0.25f);
				Gizmos.DrawWireSphere(end.X0Y(), 0.25f);

				Vector3 p00 = ramp.n00.position;
				Vector3 p01 = ramp.n01.position;
				Gizmos.DrawLine(p00, p01);


				if (ramp.length > 0)
				{
					Vector3 p10 = ramp.n10.position;
					Vector3 p11 = ramp.n11.position;
					Gizmos.DrawLine(p10, p11);
					Gizmos.DrawLine(p00, p10);
					Gizmos.DrawLine(p01, p11);
				}

				Handles.Label((p00 + p01) / 2.0f, ramp.length + "");
				//Vector3 p10 = ramp.n10.position;
				//Vector3 p11 = ramp.n11.position;
				//Gizmos.DrawLine(p10, p11);
				//Gizmos.DrawLine(p00, p10);
				//Gizmos.DrawLine(p01, p11);


				/*
				Vector2Int startEx = start + ramp.length * (ramp.orientation == Orientation.HORIZONTAL ? Vector2Int.right : Vector2Int.up);
				Vector2Int end = start + 2 * (ramp.orientation == Orientation.HORIZONTAL ? Vector2Int.up : Vector2Int.right);
				Vector2Int endEx = startEx + 2 * (ramp.orientation == Orientation.HORIZONTAL ? Vector2Int.up : Vector2Int.right);

				Vector3 p00 = OnGround(start.x, start.y);
				Vector3 p10 = OnGround(startEx.x, startEx.y);
				Vector3 p01 = OnGround(end.x, end.y);
				Vector3 p11 = OnGround(endEx.x, endEx.y);

				Gizmos.DrawLine(p00, p10);
				Gizmos.DrawLine(p01, p11);
				Gizmos.DrawLine(p00, p01);
				Gizmos.DrawLine(p10, p11);
				*/

			}
		}
	}


	/// <summary>
	/// Runs the A* algorithm to find a path between position and goal.
	/// </summary>
	/// <param name="start"></param>
	/// <param name="goal"></param>
	/// <returns></returns>
	List<Node> AStar(Vector3 start, Vector3 goal)
	{
		Node TempStart(float x, float z, Node tempEnd = null)
		{
			Vector3 position = OnGround(x, z);
			Vector2Int tile = position.ToTileCoord();
			Area area = GetArea(tile.x, tile.y);
			int areaID = area != null ? area.ID : -1;

			// Create a temporary node for the start position.
			// Connect the start node to all the visible nodes in the area
			Node temp = new Node(tile, position, areaID);
			if (area != null)
			{
				foreach (Node node in area.Nodes)
					if (IsVisible(new Vector2(x, z), node.position.XZ(), true, true))
						Connect(temp, node, false);
			}

			// we could be on a ramp
			else if (TryGetRamp(tile, out Ramp ramp))
			{
				Debug.Log("On ramp");
				foreach (Node node in ramp.Nodes)
					Connect(temp, node, false);
			}

			// Also try to connect to the end node
			if (tempEnd != null)
			{
				if (IsVisible(temp.position.XZ(), tempEnd.position.XZ(), true, true))
					Connect(temp, tempEnd, false);
			}

			return temp;
		}

		Node TempEnd(float x, float z)
		{
			Vector3 position = OnGround(x, z);
			Vector2Int tile = position.ToTileCoord();
			Area area = GetArea(tile.x, tile.y);
			int areaID = area != null ? area.ID : -1;

			// Create a temporary node for the end position.
			// Connect all the visible nodes in the area to the end node.
			Node temp = new Node(tile, position, areaID);
			if (area != null)
			{
				foreach (Node node in area.Nodes)
					if (IsVisible(node.position.XZ(), new Vector2(x, z), true, true))
						Connect(node, temp, false);
			}
			return temp;
		}

		Node tempEnd = TempEnd(goal.x, goal.z);
		Node tempStart = TempStart(start.x, start.z, tempEnd);

		List<Node> path = Search.AStar(tempStart, tempEnd, this, (Node node, float cost) =>
		{
			return false;// IsVisible(node.position.XZ(), tempEnd.position.XZ(), true, true);
		});

		if (false && path != null && path.Count > 0)
		{
			Gizmos.color = Color.blue;
			foreach (Node neighbour in Neighbours(path[0]))
			{
				Gizmos.DrawLine(path[0].position, neighbour.position);
			}
		}

		RemoveNode(tempStart);
		RemoveNode(tempEnd);

		return path;
	}

	/// <summary>
	/// Does physics raycasts on all the chunk meshes and returns the first hit.
	/// </summary>
	/// <param name="ray"></param>
	/// <param name="hit"></param>
	/// <param name="chunk"></param>
	/// <returns></returns>
	bool Raycast(Ray ray, out RaycastHit hit, out Chunk chunk)
	{
		hit = new RaycastHit();
		chunk = null;
		foreach (Chunk c in chunks.Values)
			if (c.Raycast(ray, out hit))
			{
				chunk = c;
				return true;
			}

		return false;
	}
}
