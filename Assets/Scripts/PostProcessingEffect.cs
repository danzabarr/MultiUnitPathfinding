using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class PostProcessingEffect : MonoBehaviour
{
	public Camera cam;
	public Material material;

	void OnRenderImage(RenderTexture src, RenderTexture dest)
	{
		if (cam == null)
			cam = GetComponent<Camera>();

		cam.depthTextureMode = DepthTextureMode.DepthNormals;

		if (material != null)
			Graphics.Blit(src, dest, material);
		else
			Graphics.Blit(src, dest);
	}
}
