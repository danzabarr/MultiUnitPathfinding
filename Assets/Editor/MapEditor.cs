using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Map))]
public class MapEditor : Editor
{
    public override void OnInspectorGUI()
    {
		base.OnInspectorGUI();

		Map map = (Map)target;

		if (GUILayout.Button("Generate"))
			map.Regenerate();

		if (GUILayout.Button("Delete"))
			map.DeleteAll();
	}
}
