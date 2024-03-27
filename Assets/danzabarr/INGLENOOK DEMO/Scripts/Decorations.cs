using System.Collections.Generic;
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
    public RandomTransform offset;
    
    public bool IsSet(Placement flagToCheck)
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
	[SerializeField] private SerializableDictionary<Mesh, Matrix4x4[]> matrices;

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

        HashSet<Vector2Int> set = new HashSet<Vector2Int>();

        foreach (Decoration decoration in decorations)
        {
            if (decoration.mesh == null)
                continue;

            List<Matrix4x4> matrixList = new List<Matrix4x4>();

            for (int x = 0; x < chunk.size.x; x++)
            {
                for (int y = 0; y < chunk.size.y; y++)
                {
                    if (set.Contains(new Vector2Int(x, y)))
                        continue;

                    if (!decoration.IsSet(PlacementType(chunk.GetTileType(x, y))))
                        continue;

                    if (Random.value > decoration.density)
                        continue;

                    Vector2Int tile = new Vector2Int(x, y) + chunk.chunkPosition * chunk.size;
                    
                    //Apply the scale and rotation locally, then translate to the world position
                    Matrix4x4 matrix = Matrix4x4.Translate(chunk.OnGround(tile.X0Y())) * decoration.offset.GenerateMatrix();
                    matrixList.Add(matrix);
                    set.Add(tile);
                }
            }

            matrices[decoration.mesh] = matrixList.ToArray();
        }
        
    }

    public void Update()
    {
        if (matrices == null)
            return;

        MaterialPropertyBlock properties = new MaterialPropertyBlock();

        foreach (KeyValuePair<Mesh, Matrix4x4[]> pair in matrices)
        {
            // null checks
            if (pair.Key == null || pair.Value == null)
                continue;

            Graphics.DrawMeshInstanced(pair.Key, 0, material, pair.Value, pair.Value.Length, properties, UnityEngine.Rendering.ShadowCastingMode.On, true, layer);
        }
            //Graphics.DrawMeshInstanced(pair.Key, 0, material, pair.Value);
    }
}
