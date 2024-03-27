using UnityEngine;

/// <summary>
/// These can be placed by hand, and attached to moving objects.
/// Base class of Agents, so an agent is a moving waypoint for other agents.
/// </summary>
[ExecuteInEditMode]
public class Waypoint : MonoBehaviour
{
    protected Map map;
    private Node node = new Node(Vector2Int.zero, Vector3.zero, -1);
    public Node Node => node;
    public float GroundDistanceFromNode => node == null ? 0 : Vector3.Distance(transform.position.XZ(), node.position.XZ());
    public bool NeedsUpdating 
    {   
        get => GroundDistanceFromNode > float.Epsilon || forceUpdate;
        set => forceUpdate = value;
    }
    private bool forceUpdate = true;

    public bool IsOrphaned()
    {
        if (map == null)
            map = FindObjectOfType<Map>();
        return map.NeighbourCount(node) == 0;
    }

    void Awake()
    {
        map = FindObjectOfType<Map>();

        if (map != null)
            map.UpdateWaypoint(this);
    }

    public void OnEnable()
    {
        if (map == null)
            map = FindObjectOfType<Map>();

        if (map != null)
            map.UpdateWaypoint(this);
    }

    public void OnDisable()
    {
        if (map == null)
            map = FindObjectOfType<Map>();

        if (map != null)
            map.UpdateWaypoint(this);
    }

    public virtual void Update()
    {
        if (map == null)
            map = FindObjectOfType<Map>();

        if (map != null && NeedsUpdating)
            map.UpdateWaypoint(this);

        forceUpdate = false;
    }

    public virtual void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, 1f);
    }

    public virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        foreach (Node neighbour in map.Neighbours(node))
            Gizmos.DrawLine(node.position, neighbour.position);
    }
}
