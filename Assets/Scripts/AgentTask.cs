using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;

[Inspectable]
public class AgentTask 
{
	[Inspectable] public string taskName;

	public AgentTask(string taskName)
	{
		this.taskName = taskName;
	}
}
