using UnityEngine;
using UnityEditor.Animations;
using UnityEditor;

/// <summary>
/// Convenience tool for adding triggers to all states in an animator controller.
/// </summary>
public class AddTriggersToStates : MonoBehaviour
{
    [MenuItem("Tools/Add Triggers to Any State")]
    static void AddTriggers()
    {  
        AnimatorController animatorController = Selection.activeObject as AnimatorController;
        foreach (AnimatorControllerLayer layer in animatorController.layers)
        {
            foreach (ChildAnimatorState cas in layer.stateMachine.states)
            {
                animatorController.AddParameter(cas.state.name, AnimatorControllerParameterType.Trigger);
                AnimatorState state = cas.state;
                AnimatorStateTransition transition = layer.stateMachine.AddAnyStateTransition(state);
                transition.AddCondition(AnimatorConditionMode.If, 0, state.name);
            }
        }
    }

    [MenuItem("Tools/Remove Triggers from Any State")]
    static void RemoveTriggers()
    {
        AnimatorController animatorController = Selection.activeObject as AnimatorController;
        // delete all trigger parameters from the controller
        foreach (AnimatorControllerParameter parameter in animatorController.parameters)
            if (parameter.type == AnimatorControllerParameterType.Trigger)
                animatorController.RemoveParameter(parameter);

        // delete all transitions from any state
        foreach (AnimatorControllerLayer layer in animatorController.layers)
            layer.stateMachine.anyStateTransitions = new AnimatorStateTransition[0];
    }
}
