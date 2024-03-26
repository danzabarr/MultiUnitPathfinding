using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleDialogue : MonoBehaviour
{
    [TextArea(3, 10)]
    public string text;
    private CameraController cameraController;
    private DialogueBox dialogueBox;

    private Agent speaker;
    private Agent listener; // the agent that is listening to the dialogue

    void Start()
    {
        speaker = GetComponent<Agent>();
        cameraController = FindObjectOfType<CameraController>();
        dialogueBox = FindObjectOfType<DialogueBox>(true);
    }

    public void StartDialogue(Agent listener)
    {
        this.listener = listener;
        listener.agentState = AgentState.Talking;
        if (speaker != null)
        {
            speaker.agentState = AgentState.Talking;
            speaker.TurnToFace(listener.transform.position);
        }
        listener.TurnToFace(transform.position);
        cameraController.Focus(transform);
        dialogueBox.Show(text, EndDialogue);
    }

    public void EndDialogue()
    {
        dialogueBox.Hide();
        cameraController.Follow(listener.transform);
        listener.agentState = AgentState.Controlled;
        if (speaker != null)
            speaker.agentState = AgentState.Idle;
    }
}
