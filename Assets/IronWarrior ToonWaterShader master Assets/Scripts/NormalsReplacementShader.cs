using UnityEngine;

public class NormalsReplacementShader : MonoBehaviour
{
    [SerializeField]
    Shader normalsShader;

    private RenderTexture renderTexture;
    private Camera cam;

    private void Start()
    {
        Camera thisCamera = GetComponent<Camera>();

        // Create a render texture matching the main camera's current dimensions.
        renderTexture = new RenderTexture(thisCamera.pixelWidth, thisCamera.pixelHeight, 24);
        // Surface the render texture as a global variable, available to all shaders.
        Shader.SetGlobalTexture("_CameraNormalsTexture", renderTexture);

        // Setup a copy of the camera to render the scene using the normals shader.
        GameObject copy = new GameObject("Normals camera");
        cam = copy.AddComponent<Camera>();
        cam.CopyFrom(thisCamera);
        cam.transform.SetParent(transform);
        cam.targetTexture = renderTexture;
        cam.SetReplacementShader(normalsShader, "RenderType");
        cam.depth = thisCamera.depth - 1;
    }
}
