using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RandomTransform 
{
    [Header("Translation")]
    public Vector3 offsetMin = Vector3.zero;
    public Vector3 offsetMax = Vector3.zero;

    [Header("Rotation")]
    public Vector3 rotationMin = Vector3.zero;
    public Vector3 rotationMax = Vector3.one * 360;

    [Header("Scale")]
    public Vector3 scaleMin = Vector3.one;
    public Vector3 scaleMax = Vector3.one;

    public Matrix4x4 GenerateMatrix()
    {
        Random.InitState(System.DateTime.Now.Millisecond);

        Vector3 T = new Vector3
        (
            Random.Range(offsetMin.x, offsetMax.x),
            Random.Range(offsetMin.y, offsetMax.y),
            Random.Range(offsetMin.z, offsetMax.z)
        );

        Vector3 R = new Vector3
        (
            Random.Range(rotationMin.x, rotationMax.x),
            Random.Range(rotationMin.y, rotationMax.y),
            Random.Range(rotationMin.z, rotationMax.z)
        );

        Vector3 S = new Vector3
        (
            Random.Range(scaleMin.x, scaleMax.x),
            Random.Range(scaleMin.y, scaleMax.y),
            Random.Range(scaleMin.z, scaleMax.z)
        );

        Debug.Log($"T: {T}, R: {R}, S: {S}");

        return Matrix4x4.TRS(T, Quaternion.Euler(R), S);
    }
}

public static class TransformExtensions
{
    public static void RandomisePosition(this Transform transform, Vector3 offsetMin, Vector3 offsetMax)
    {
        transform.localPosition = new Vector3
        (
            Random.Range(offsetMin.x, offsetMax.x),
            Random.Range(offsetMin.y, offsetMax.y),
            Random.Range(offsetMin.z, offsetMax.z)
        );
    }

    public static void RandomiseRotation(this Transform transform, Vector3 rotationMin, Vector3 rotationMax)
    {
        transform.localRotation = Quaternion.Euler
        (
            Random.Range(rotationMin.x, rotationMax.x),
            Random.Range(rotationMin.y, rotationMax.y),
            Random.Range(rotationMin.z, rotationMax.z)
        );
    }

    public static void RandomiseScale(this Transform transform, Vector3 scaleMin, Vector3 scaleMax)
    {
        transform.localScale = new Vector3
        (
            Random.Range(scaleMin.x, scaleMax.x),
            Random.Range(scaleMin.y, scaleMax.y),
            Random.Range(scaleMin.z, scaleMax.z)
        );
    }

    public static void Randomise(this Transform transform, RandomTransform rt)
    {
        transform.SetLocalPositionAndRotation
        (
            new Vector3(
                Random.Range(rt.offsetMin.x, rt.offsetMax.x),
                Random.Range(rt.offsetMin.y, rt.offsetMax.y),
                Random.Range(rt.offsetMin.z, rt.offsetMax.z)
            ), 
            Quaternion.Euler
            (
                Random.Range(rt.rotationMin.x, rt.rotationMax.x),
                Random.Range(rt.rotationMin.y, rt.rotationMax.y),
                Random.Range(rt.rotationMin.z, rt.rotationMax.z)
            )
        );
        transform.localScale = new Vector3
        (
            Random.Range(rt.scaleMin.x, rt.scaleMax.x),
            Random.Range(rt.scaleMin.y, rt.scaleMax.y),
            Random.Range(rt.scaleMin.z, rt.scaleMax.z)
        );
    }
}
