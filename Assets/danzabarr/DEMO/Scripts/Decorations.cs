using System.Collections;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;

[System.Flags]
public enum Placement
{
    None = 0,
    Flat = 1 << 0,
    Ramp = 1 << 1,
    Bridge = 1 << 2,
    Cliff = 1 << 3,
    Water = 1 << 4,
    Land = Flat | Ramp | Cliff,
    Walkable = Flat | Ramp | Bridge,
    All = Flat | Ramp | Cliff | Water | Bridge
}

[System.Serializable]
public class Decoration
{
    public Placement placement;
    public float density;
    public Mesh mesh;
    public RandomTransform offset;public bool IsSet(Placement flagToCheck)
    {
        return (placement & flagToCheck) != 0;
    }
}

[RequireComponent(typeof(Chunk))]
[ExecuteInEditMode]
public class Decorations : MonoBehaviour
{
    /// <summary>
    /// Decorations use one material for all meshes.
    /// They are drawn using GPU instancing.
    /// </summary>
    public Material material;

    [Header("Layer")]
    public int layer;

    /// <summary>
    /// Array of decorations to scatter.
    /// </summary>
    public Decoration[] decorations;

    private Chunk chunk;
	private SerializableDictionary<Mesh, Matrix4x4[]> matrices;

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
        if (placement == Placement.Bridge)
            return Chunk.BRIDGE;
            
        return Chunk.OUT_OF_BOUNDS;
    }

    public static Placement PlacementType(int obstruction)
    {
        if (obstruction >= Chunk.FLAT)
            return Placement.Flat;
        
        if (obstruction == Chunk.RAMP)
            return Placement.Ramp;
        
        if (obstruction == Chunk.CLIFF)
            return Placement.Cliff;
        
        if (obstruction == Chunk.WATER)
            return Placement.Water;

        if (obstruction == Chunk.BRIDGE)
            return Placement.Bridge;

        return Placement.None;
    }

    public void DeleteAll()
    {
        matrices = new SerializableDictionary<Mesh, Matrix4x4[]>();
    }

    [ContextMenu("Regenerate")]
    public void Regenerate()
    {
        chunk = GetComponent<Chunk>();
        matrices = new SerializableDictionary<Mesh, Matrix4x4[]>();

        float sum_density = 0;
        foreach (Decoration decoration in decorations)
            sum_density += decoration.density;
        sum_density = Mathf.Max(sum_density, 1);

        Random.InitState(chunk.chunkPosition.x * 1000 + chunk.chunkPosition.y);

        for (int i = 0; i < chunk.size.x * chunk.size.y; i++)
        {
            int x = i % chunk.size.x;
            int z = i / chunk.size.x;

            float randomValue = Random.value * sum_density;

            foreach (Decoration decoration in decorations)
            {
                float density = decoration.density;

                if (randomValue > density)
                {
                    randomValue -= density;
                    continue;
                }

                Placement tileType = PlacementType(chunk.GetPermanentObstructionType(x, z));

                if (!decoration.IsSet(tileType))
                    continue;

                
                Vector3 position = new Vector3(x, 0, z) + (chunk.chunkPosition * chunk.size).X0Y();
                position = chunk.OnGround(position);

                Matrix4x4 matrix = Matrix4x4.Translate(position);
                matrix *= decoration.offset.GenerateMatrix();

                Mesh mesh = decoration.mesh;
                if (!matrices.ContainsKey(mesh))
                    matrices.Add(mesh, new Matrix4x4[0]);

                List<Matrix4x4> newMatrices = new List<Matrix4x4>(matrices[mesh]);
                newMatrices.Add(matrix);
                matrices[mesh] = newMatrices.ToArray();

                break;
            }
        }
    }

    public void Update()
    {
        if (matrices == null)
            return;

        MaterialPropertyBlock properties = new MaterialPropertyBlock();

        foreach (KeyValuePair<Mesh, Matrix4x4[]> pair in matrices)
            //Graphics.DrawMeshInstanced(pair.Key, 0, material, pair.Value);
            Graphics.DrawMeshInstanced(pair.Key, 0, material, pair.Value, pair.Value.Length, properties, UnityEngine.Rendering.ShadowCastingMode.On, true, layer);
    }
}
