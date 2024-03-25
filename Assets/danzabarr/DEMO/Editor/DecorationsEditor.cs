using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Decorations))]
public class GeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
		base.OnInspectorGUI();

		Decorations generator = (Decorations)target;

		if (GUILayout.Button("Generate"))
			generator.Regenerate();

		if (GUILayout.Button("Delete"))
			generator.DeleteAll();
	}
}
