using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[ExecuteInEditMode]
public class SyncMesh : MonoBehaviour
{
    private MeshFilter thisMesh;
    public MeshFilter targetMesh;

    public void Update()
    {
        if (thisMesh == null)
            thisMesh = GetComponent<MeshFilter>();

        if (targetMesh == null)
            return;

        thisMesh.sharedMesh = targetMesh.sharedMesh;
    }
}
