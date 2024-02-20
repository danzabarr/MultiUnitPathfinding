using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Agent : MonoBehaviour
{
    private StateMachine state;

    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector2 idleTether;
    [SerializeField] private AgentTask task;

    private List<Node> path;

    public StateMachine State => state == null ? state = GetComponent<StateMachine>() : state;
    public Transform FollowTarget => followTarget;
    public Vector2 IdleTether => idleTether;
    public AgentTask Task => task;

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            Follow(transform);
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            Wait();
        }
    }

    public void Follow(Transform followTarget)
    {
        if (followTarget == null)
        {
            Wait();
            return;
        }

        this.followTarget = followTarget;
        EventBus.Trigger("Follow", state);
        Debug.Log("Following");
    }

    public void Wait()
    {
        EventBus.Trigger("Wait", state);
        Debug.Log("Waiting");
    }

    public void GoAndWait(Vector2 position)
    {

    }

    public void Cancel()
    {

    }

    public void Feed(int type, int amount)
    {

    }

    public AgentTask LookForNeedSatisfyingTask()
    {
        return new AgentTask("Pick berries");
    }

    public AgentTask LookForPlayerTask()
    {
        return null;
    }
}
