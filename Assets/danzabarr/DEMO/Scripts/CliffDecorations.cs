using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Chunk))]
public class CliffDecorations : BatchRenderer
{
	[ContextMenu("Regenerate Cliff Rocks")]
	public void GenerateRocks()
	{
        Chunk chunk = GetComponent<Chunk>();

    	//Random.InitState(chunkPosition.x * 1000 + chunkPosition.y);
		Random.InitState(System.DateTime.Now.Millisecond);
		List<Matrix4x4> matrices = new List<Matrix4x4>();
		for (int x = 0; x < chunk.size.x; x++)
		{
			for (int y = 0; y < chunk.size.y; y++)
			{
				// check not near any ramps
				if (chunk.GetPermanentObstructionType(x + 1, y) == Chunk.RAMP)
					continue;

				if (chunk.GetPermanentObstructionType(x - 1, y) == Chunk.RAMP)
					continue;

				if (chunk.GetPermanentObstructionType(x, y + 1) == Chunk.RAMP)
					continue;

				if (chunk.GetPermanentObstructionType(x, y - 1) == Chunk.RAMP)
					continue;



				if (chunk.GetPermanentObstructionType(x, y) == Chunk.CLIFF)
				{
					for (int i = 0; i < Random.Range(1, 4); i++)
					{
						Vector2 range = new Vector2(-0.125f, 0.125f);
						Vector2Int tile = new Vector2Int(x, y) + chunk.chunkPosition * chunk.size;
						(Vector3 position, Vector3 normal) = chunk.OnMesh(new Vector3(tile.x + Random.Range(range.x, range.y), 0, tile.y + Random.Range(range.x, range.y)));
						Quaternion rotation = // up is normal
							Quaternion.LookRotation(Vector3.Cross(normal, Vector3.forward), normal) *
							Quaternion.Euler(0, Random.Range(0, 360), 0);
						Vector3 scale = Random.Range(0.5f, 0.7f) * new Vector3(1, Random.Range(0.25f, 0.5f), 1);

						//Apply the scale and rotation locally, then translate to the world position

						Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, scale);
						matrices.Add(matrix);
					}
				}
			}
		}
		SetMatrices(matrices.ToArray());
	}
}
