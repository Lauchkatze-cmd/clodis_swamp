using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows live 3D copies of Clodi and an enemy on the start screen.
/// The copies stand in a hidden "photo studio" far outside the level.
/// A second camera films them with a transparent background into a Render Texture,
/// and the RawImage on this object shows that picture on the UI.
/// Put this on a RawImage inside the start screen.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class StartScreenShowcase : MonoBehaviour
{
    [Header("Models from the scene (drag them in from the Hierarchy)")]
    [SerializeField] private Transform leftModel;
    [SerializeField] private Transform rightModel;

    [Header("Look")]
    [Tooltip("How much each model turns towards the middle of the screen.")]
    [SerializeField] private float turnTowardsCenter = 30f;
    [Tooltip("Distance of the models from the middle (0 = centre, 1 = screen edge).")]
    [Range(0.2f, 0.95f)]
    [SerializeField] private float spread = 0.68f;
    [Tooltip("Bigger value = models appear smaller.")]
    [SerializeField] private float padding = 1.15f;
    [SerializeField] private float extraLight = 1.5f;
    [SerializeField] private int textureHeight = 720;

    // Far away from the level, so the game camera never sees the studio
    private static readonly Vector3 StudioPosition = new Vector3(2000f, 0f, 2000f);

    private RawImage rawImage;
    private GameObject studio;
    private Camera studioCamera;
    private RenderTexture renderTexture;

    private void Awake()
    {
        rawImage = GetComponent<RawImage>();
        rawImage.raycastTarget = false;
    }

    private void Start()
    {
        BuildStudio();
    }

    private void OnEnable()
    {
        if (studio != null) studio.SetActive(true);
    }

    private void OnDisable()
    {
        // Start screen closed: stop filming, saves performance during the game
        if (studio != null) studio.SetActive(false);
    }

    private void OnDestroy()
    {
        if (studio != null) Destroy(studio);
        if (renderTexture != null) renderTexture.Release();
    }

    private void BuildStudio()
    {
        if (leftModel == null || rightModel == null)
        {
            Debug.LogWarning("StartScreenShowcase: drag Clodi's model and the enemy model into the slots.");
            return;
        }

        studio = new GameObject("Start Screen Studio");
        studio.transform.position = StudioPosition;

        Transform left = CreateCopy(leftModel, turnTowardsCenter);
        Transform right = CreateCopy(rightModel, -turnTowardsCenter);

        // Size of the picture
        Rect rect = rawImage.rectTransform.rect;
        float aspect = rect.height > 1f ? rect.width / rect.height : 3f;

        // Camera size so that the taller model fits
        float tallest = Mathf.Max(GetBounds(left).size.y, GetBounds(right).size.y);
        float halfHeight = tallest * 0.5f * padding;
        float halfWidth = halfHeight * aspect;

        // Stand both models on the studio floor, left and right of the middle
        PlaceModel(left, -halfWidth * spread);
        PlaceModel(right, halfWidth * spread);

        Debug.Log($"Showcase: left '{left.name}' size {GetBounds(left).size}, right '{right.name}' size {GetBounds(right).size}, " +
                  $"picture {rect.width:0}x{rect.height:0}");

        renderTexture = new RenderTexture(Mathf.RoundToInt(textureHeight * aspect), textureHeight, 24, RenderTextureFormat.ARGB32);
        renderTexture.name = "Start Screen Showcase";

        GameObject cameraObject = new GameObject("Studio Camera");
        cameraObject.transform.SetParent(studio.transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, halfHeight, -10f);
        cameraObject.transform.localRotation = Quaternion.identity;

        studioCamera = cameraObject.AddComponent<Camera>();
        studioCamera.orthographic = true;
        studioCamera.orthographicSize = halfHeight;
        studioCamera.nearClipPlane = 0.1f;
        studioCamera.farClipPlane = 30f;
        studioCamera.clearFlags = CameraClearFlags.SolidColor;
        studioCamera.backgroundColor = new Color(0f, 0f, 0f, 0f); // transparent
        studioCamera.allowHDR = false;  // keeps the transparent background
        studioCamera.allowMSAA = false;
        studioCamera.depth = -10f;
        studioCamera.targetTexture = renderTexture;

        // Soft extra light from the front, only reaches the studio
        GameObject lightObject = new GameObject("Studio Light");
        lightObject.transform.SetParent(studio.transform, false);
        lightObject.transform.localPosition = new Vector3(0f, halfHeight * 2f, -6f);
        Light fillLight = lightObject.AddComponent<Light>();
        fillLight.type = LightType.Point;
        fillLight.range = 40f;
        fillLight.intensity = extraLight;

        rawImage.texture = renderTexture;
        rawImage.color = Color.white;
    }

    private Transform CreateCopy(Transform model, float turnAngle)
    {
        GameObject copy = Instantiate(model.gameObject, studio.transform);
        copy.name = model.name + " (Showcase)";
        copy.transform.localScale = model.lossyScale;

        // Face the camera, keeping the model's own rotation relative to its character
        Quaternion relativeRotation = model.parent != null
            ? Quaternion.Inverse(model.parent.rotation) * model.rotation
            : model.rotation;
        copy.transform.rotation = Quaternion.Euler(0f, 180f + turnAngle, 0f) * relativeRotation;

        // Only keep the look and the animation: no gameplay scripts, physics or sounds
        foreach (Behaviour behaviour in copy.GetComponentsInChildren<Behaviour>(true))
        {
            if (!(behaviour is Animator)) Destroy(behaviour);
        }
        foreach (Collider modelCollider in copy.GetComponentsInChildren<Collider>(true)) Destroy(modelCollider);
        foreach (Rigidbody body in copy.GetComponentsInChildren<Rigidbody>(true)) Destroy(body);

        // Animated meshes: always draw them and keep their size up to date,
        // otherwise Unity can wrongly think they are off screen and hide them
        foreach (SkinnedMeshRenderer skinnedMesh in copy.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            skinnedMesh.updateWhenOffscreen = true;
        }

        // Play the idle animation, also while the game is paused on the start screen
        foreach (Animator animator in copy.GetComponentsInChildren<Animator>(true))
        {
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.applyRootMotion = false;
            if (animator.runtimeAnimatorController == null) continue;

            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Bool) animator.SetBool(parameter.nameHash, false);
                if (parameter.type == AnimatorControllerParameterType.Float) animator.SetFloat(parameter.nameHash, 0f);
            }
        }

        return copy.transform;
    }

    private void PlaceModel(Transform model, float targetX)
    {
        Bounds bounds = GetBounds(model);
        Vector3 offset = new Vector3(
            StudioPosition.x + targetX - bounds.center.x,
            StudioPosition.y - bounds.min.y,
            StudioPosition.z - bounds.center.z);
        model.position += offset;
    }

    /// <summary>Size of the visible model, only counting real meshes (no particles, trails or empty renderers).</summary>
    private static Bounds GetBounds(Transform model)
    {
        bool found = false;
        Bounds bounds = new Bounds(model.position, Vector3.one);

        foreach (Renderer modelRenderer in model.GetComponentsInChildren<Renderer>())
        {
            bool isMesh = modelRenderer is MeshRenderer || modelRenderer is SkinnedMeshRenderer;
            if (!isMesh || !modelRenderer.enabled) continue;

            if (modelRenderer is SkinnedMeshRenderer skinnedMesh) skinnedMesh.ResetBounds();
            Bounds rendererBounds = modelRenderer.bounds;
            if (rendererBounds.size.sqrMagnitude < 0.0001f) continue;

            if (!found) bounds = rendererBounds;
            else bounds.Encapsulate(rendererBounds);
            found = true;
        }

        return bounds;
    }
}
