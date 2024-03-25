using UnityEngine;

/// <summary>
/// These can be placed by hand, and attached to moving objects.
/// </summary>
[ExecuteAlways]
public class Waypoint : MonoBehaviour
{
    protected Map map; // cache the reference, there won't be too many waypoints
    public float threshold = 0.001f;
    private Node node = new Node(Vector2Int.zero, Vector3.zero, -1);
    public Node Node => node;
    public float GroundDistanceFromNode => node == null ? 0 : Vector3.Distance(transform.position.XZ(), node.position.XZ());
    public bool NeedsUpdating 
    {   
        get => GroundDistanceFromNode > threshold || forceUpdate;
        set => forceUpdate = value;
    }
    private bool forceUpdate = false;

    public bool IsOrphaned()
    {
        if (map == null)
            map = FindObjectOfType<Map>();
        return map.NeighbourCount(node) == 0;
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

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, 0.25f);
    }
}
