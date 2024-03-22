using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class State : StateGroup
{
    public abstract void OnEnter();
    public abstract void OnUpdate();
    public abstract void OnExit();
    public abstract void Enter(State prev);
    public abstract void Update();
    public abstract void Exit(State next);

    public void Transition(string key)
    {
        Transition(key, this);
    }
}

public abstract class StateGroup
{
    public bool propagateUpdate = true;  // controls whether a state propagates updates upwards to its parent
    public bool updateWhenInactive = true; // controls whether a state updates when it is the parent of the current state

    public StateGroup parent;
    public Dictionary<string, State> transitions = new Dictionary<string, State>();
    public abstract void AddState(State state);
    public abstract void RemoveState(State state);

    protected void Transition(string key, State current)
    {
        if (transitions.ContainsKey(key))
        {
            State next = transitions[key];
            current.Exit(next);
            next.Enter(current);
            return;
        }

        if (parent != null)
            parent.Transition(key, current);
    }
}
