using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Chunk))]
[ExecuteInEditMode]
public class Decorations : MonoBehaviour
{
    private Chunk chunk;
    public Material material;
	private SerializableDictionary<Mesh, Matrix4x4[]> matrices;
    public Mesh[] meshes;
    public int seed;

    public float density = 0.1f;

    [System.Flags]
    public enum Placement
    {
        None = 0,
        Flat = 1 << 0,
        Ramp = 1 << 1,
        Cliff = 1 << 2,
        Water = 1 << 3,
        Land = Flat | Ramp | Cliff,
        Walkable = Flat | Ramp,
        All = Flat | Ramp | Cliff | Water,
    }

    public static int ObstructionType(Placement placement)
    {
        if (placement == Placement.All)
            return Chunk.FLAT;
        if (placement == Placement.Flat)
            return Chunk.FLAT;
        if (placement == Placement.Ramp)
            return Chunk.RAMP;
        if (placement == Placement.Cliff)
            return Chunk.CLIFF;
        if (placement == Placement.Water)
            return Chunk.WATER;
        return Chunk.OUT_OF_BOUNDS;
    }

    public static Placement PlacementType(int obstruction)
    {
        if (obstruction == Chunk.FLAT)
            return Placement.Flat;
        if (obstruction == Chunk.RAMP)
            return Placement.Ramp;
        if (obstruction == Chunk.CLIFF)
            return Placement.Cliff;
        if (obstruction == Chunk.WATER)
            return Placement.Water;
        return Placement.None;
    }

    public bool IsSet(Placement flagToCheck)
    {
        return (placement & flagToCheck) != 0;
    }

    public bool All(params Placement[] flags)
    {
        foreach (Placement flag in flags)
            if (!IsSet(flag))
                return false;
        return true;
    }

    public bool Any(params Placement[] flags)
    {
        foreach (Placement flag in flags)
            if (IsSet(flag))
                return true;
        return false;
    }

    public Placement placement;

    public void DeleteAll()
    {
        matrices = new SerializableDictionary<Mesh, Matrix4x4[]>();
    }

    [ContextMenu("Regenerate")]
    public void Regenerate()
    {
        chunk = GetComponent<Chunk>();
        matrices = new SerializableDictionary<Mesh, Matrix4x4[]>();
        Random.InitState(seed);

        Dictionary<Vector2Int, Matrix4x4> matrixMap = new Dictionary<Vector2Int, Matrix4x4>();

        for (int i = 0; i < chunk.size.x * chunk.size.y; i++)
        {
            int x = i % chunk.size.x;
            int z = i / chunk.size.x;

            if (Random.value > density)
                continue;

            if (matrixMap.ContainsKey(new Vector2Int(x, z)))
                continue;

            if (!IsSet(PlacementType(chunk.GetPermanentObstructionType(x, z))))
                continue;

                Debug.Log("Generating decoration at " + x + ", " + z);

            
            Vector3 position = new Vector3(x, 0, z) + (chunk.chunkPosition * chunk.size).X0Y();
            Vector3 scale = Vector3.one;
            Vector3 rotation = new Vector3(0, Random.Range(0, 360), 0);

            matrixMap.Add(new Vector2Int(x, z), Matrix4x4.TRS(position, Quaternion.Euler(rotation), scale));
        }

        foreach (KeyValuePair<Vector2Int, Matrix4x4> pair in matrixMap)
        {
            Mesh mesh = meshes[Random.Range(0, meshes.Length)];
            if (!matrices.ContainsKey(mesh))
                matrices.Add(mesh, new Matrix4x4[0]);

            List<Matrix4x4> newMatrices = new List<Matrix4x4>(matrices[mesh]);
            newMatrices.Add(pair.Value);
            matrices[mesh] = newMatrices.ToArray();
        }
    }

    public void Update()
    {
        if (matrices == null)
            return;

        foreach (KeyValuePair<Mesh, Matrix4x4[]> pair in matrices)
            Graphics.DrawMeshInstanced(pair.Key, 0, material, pair.Value);
    }
}
