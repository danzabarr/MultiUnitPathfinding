using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IActor
{
    void StartLine(string text);
    void StartAnimation(string trigger);
    void StartTask(string task);
    void StartFace(Vector3 target);
    void StartMove(string waypoint);
    void StartPrompt(string key, string text);
    void StartOption(string key, string value, string text);
}

/// <summary>
/// An actor is an entity in a dialogue.
/// While an actor is involved in a dialogue, 
/// they can speak lines, ask questions, perform animations, and do tasks.
/// </summary>
public class Actor : MonoBehaviour
{
    public Agent agent; // for movement and pathfinding
    public Animator animator; // for animations

    void TurnToFace(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        transform.forward = direction;
    }

    public string trigger;

    public void OnValidate()
    {
        animator.SetTrigger(trigger);
    }

    void SetAnimationTrigger(string trigger)
    {
        animator.SetTrigger(trigger);
    }

    void ResetAnimationTrigger(string trigger)
    {
        animator.ResetTrigger(trigger);
    }

    void PathTo(string waypoint)
    {
        agent.PathTo(waypoint);
    }
}
