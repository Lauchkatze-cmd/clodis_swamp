using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerController))]
public class PoisonGroundCheck : MonoBehaviour
{
    [Header("Poison texture")]
    [Tooltip("The terrain layer that counts as poison (drag 'violet' here). If empty, the name below is used.")]
    [SerializeField] private TerrainLayer poisonLayer;
    [Tooltip("Used when the field above is empty: the first terrain layer whose name contains this text.")]
    [SerializeField] private string poisonLayerName = "violet";
    [Tooltip("How strongly (0-1) the poison texture must be painted to count. Paint with Brush Strength 1!")]
    [Range(0.05f, 1f)]
    [SerializeField] private float poisonThreshold = 0.5f;
    [Tooltip("How far below Clodi's feet the ground is searched.")]
    [SerializeField] private float maxGroundDistance = 0.6f;
    [SerializeField] private LayerMask groundLayers = ~0;
    [Tooltip("Seconds between two ground checks (0.05 = 20 times per second).")]
    [SerializeField] private float checkInterval = 0.05f;

    [Header("Slow down")]
    [Tooltip("Walk speed on poison = normal walk speed x this value (sprinting is slowed too).")]
    [Range(0.1f, 1f)]
    [SerializeField] private float speedMultiplier = 0.4f;

    [Header("Feedback")]
    [Tooltip("Colour Clodi turns on poison (multiplied with his normal colours).")]
    [SerializeField] private Color poisonTint = new Color(0.45f, 1f, 0.35f);
    [SerializeField] private Color screenEdgeColor = new Color(0.45f, 1f, 0.15f, 0.45f);
    [SerializeField] private string warningText = "POISON! You are slowed down";
    [SerializeField] private Color warningTextColor = new Color(0.6f, 1f, 0.3f);
    [Tooltip("How fast the feedback fades in and out.")]
    [SerializeField] private float fadeSpeed = 4f;
    [Tooltip("Optional effect that follows Clodi's feet while on poison (e.g. a Cartoon FX bubbles prefab).")]
    [SerializeField] private GameObject poisonEffectPrefab;

    [Header("Sound (optional - a bubbling sound is generated if empty)")]
    [SerializeField] private AudioClip enterSound;
    [SerializeField] private AudioClip loopSound;
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 0.6f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private PlayerController controller;
    private Rigidbody body;
    private Collider bodyCollider;
    private float normalWalkSpeed;
    private float nextCheckTime;
    private float feedback; 

    private TerrainData cachedTerrainData;
    private int cachedLayerIndex = -1;
    private bool warnedMissingLayer;

    private readonly List<Renderer> renderers = new List<Renderer>();
    private readonly List<Color[]> originalColors = new List<Color[]>();
    private MaterialPropertyBlock block;
    private bool tintActive;

    private Image screenEdge;
    private Text warningLabel;
    private AudioSource audioSource;
    private GameObject activeEffect;

    public bool IsOnPoison { get; private set; }

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        body = GetComponent<Rigidbody>();
        bodyCollider = GetComponent<Collider>();
        block = new MaterialPropertyBlock();

        CollectRenderers();
        BuildOverlay();
        SetupAudio();
    }

    private void Start()
    {
        
        normalWalkSpeed = controller.WalkSpeed;
    }

    private void Update()
    {
        if (Time.time >= nextCheckTime)
        {
            nextCheckTime = Time.time + checkInterval;
            bool onPoison = CheckPoisonGround();
            if (onPoison != IsOnPoison)
            {
                if (onPoison) EnterPoison();
                else ExitPoison();
            }
        }

        UpdateFeedback();
    }

    private void OnDisable()
    {
        if (IsOnPoison) ExitPoison();
        feedback = 0f;
        ClearTint();
        if (screenEdge != null) screenEdge.color = Color.clear;
        if (warningLabel != null) warningLabel.enabled = false;
        if (audioSource != null) audioSource.Stop();
    }

    private bool CheckPoisonGround()
    {
        Vector3 origin;
        if (bodyCollider != null)
        {
            Bounds bounds = bodyCollider.bounds;
            origin = new Vector3(bounds.center.x, bounds.min.y + 0.3f, bounds.center.z);
        }
        else
        {
            origin = transform.position + Vector3.up * 0.3f;
        }

        
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 0.3f + maxGroundDistance, groundLayers, QueryTriggerInteraction.Ignore);
        bool found = false;
        RaycastHit ground = default;
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
            if (body != null && hit.rigidbody == body) continue;
            if (!found || hit.distance < ground.distance)
            {
                ground = hit;
                found = true;
            }
        }

        if (!found) return IsOnPoison;

        if (!(ground.collider is TerrainCollider)) return false;

        Terrain terrain = ground.collider.GetComponent<Terrain>();
        return terrain != null && GetPoisonStrength(terrain, ground.point) >= poisonThreshold;
    }

    private float GetPoisonStrength(Terrain terrain, Vector3 worldPosition)
    {
        TerrainData data = terrain.terrainData;
        int layerIndex = GetPoisonLayerIndex(data);
        if (layerIndex < 0) return 0f;

        Vector3 local = worldPosition - terrain.GetPosition();
        float normalizedX = Mathf.Clamp01(local.x / data.size.x);
        float normalizedZ = Mathf.Clamp01(local.z / data.size.z);
        int mapX = Mathf.Clamp(Mathf.RoundToInt(normalizedX * (data.alphamapWidth - 1)), 0, data.alphamapWidth - 1);
        int mapZ = Mathf.Clamp(Mathf.RoundToInt(normalizedZ * (data.alphamapHeight - 1)), 0, data.alphamapHeight - 1);

        float[,,] weights = data.GetAlphamaps(mapX, mapZ, 1, 1);
        return weights[0, 0, layerIndex];
    }

    private int GetPoisonLayerIndex(TerrainData data)
    {
        if (data == cachedTerrainData) return cachedLayerIndex;

        cachedTerrainData = data;
        cachedLayerIndex = -1;
        TerrainLayer[] layers = data.terrainLayers;
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] == null) continue;
            bool sameAsset = poisonLayer != null && layers[i] == poisonLayer;
            bool sameName = poisonLayer == null && !string.IsNullOrEmpty(poisonLayerName)
                            && layers[i].name.ToLower().Contains(poisonLayerName.ToLower());
            if (sameAsset || sameName)
            {
                cachedLayerIndex = i;
                break;
            }
        }

        if (cachedLayerIndex < 0 && !warnedMissingLayer)
        {
            warnedMissingLayer = true;
            string wanted = poisonLayer != null ? poisonLayer.name : poisonLayerName;
            Debug.LogWarning($"PoisonGroundCheck: the terrain has no layer '{wanted}'. Add it in the terrain's Paint Texture tool.", this);
        }
        return cachedLayerIndex;
    }


    private void EnterPoison()
    {
        IsOnPoison = true;
        controller.WalkSpeed = normalWalkSpeed * speedMultiplier;

        if (audioSource != null && enterSound != null) audioSource.PlayOneShot(enterSound, soundVolume);

        if (poisonEffectPrefab != null && activeEffect == null)
        {
            activeEffect = Instantiate(poisonEffectPrefab, transform.position, Quaternion.identity, transform);
        }
    }

    private void ExitPoison()
    {
        IsOnPoison = false;
        controller.WalkSpeed = normalWalkSpeed;

        if (activeEffect != null)
        {
            Destroy(activeEffect);
            activeEffect = null;
        }
    }



    private void UpdateFeedback()
    {
        float target = IsOnPoison ? 1f : 0f;
        if (Mathf.Approximately(feedback, target) && !IsOnPoison) return;

        feedback = Mathf.MoveTowards(feedback, target, fadeSpeed * Time.deltaTime);
        float pulse = 0.8f + 0.2f * Mathf.Sin(Time.time * 6f);

        if (feedback > 0f) ApplyTint(feedback * pulse);
        else ClearTint();

        if (screenEdge != null)
        {
            Color edge = screenEdgeColor;
            edge.a *= feedback * pulse;
            screenEdge.color = edge;
        }

        if (warningLabel != null)
        {
            warningLabel.enabled = feedback > 0.01f;
            Color textColor = warningTextColor;
            textColor.a = feedback;
            warningLabel.color = textColor;
            float scale = 1f + 0.05f * Mathf.Sin(Time.time * 6f);
            warningLabel.rectTransform.localScale = Vector3.one * scale;
        }

        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.volume = soundVolume * feedback;
            if (feedback > 0f && !audioSource.isPlaying) audioSource.Play();
            else if (feedback <= 0f && audioSource.isPlaying) audioSource.Stop();
        }
    }

    private void CollectRenderers()
    {
        foreach (Renderer rend in GetComponentsInChildren<Renderer>(true))
        {
            if (rend is ParticleSystemRenderer || rend is TrailRenderer || rend is LineRenderer) continue;
            Material[] materials = rend.sharedMaterials;
            Color[] colors = new Color[materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                Material mat = materials[i];
                if (mat != null && mat.HasProperty(BaseColorId)) colors[i] = mat.GetColor(BaseColorId);
                else if (mat != null && mat.HasProperty(ColorId)) colors[i] = mat.GetColor(ColorId);
                else colors[i] = Color.white;
            }
            renderers.Add(rend);
            originalColors.Add(colors);
        }
    }

    private void ApplyTint(float amount)
    {
        tintActive = true;
        for (int r = 0; r < renderers.Count; r++)
        {
            if (renderers[r] == null) continue;
            Color[] colors = originalColors[r];
            for (int i = 0; i < colors.Length; i++)
            {
                Color normal = colors[i];
                Color green = new Color(normal.r * poisonTint.r, normal.g * poisonTint.g, normal.b * poisonTint.b, normal.a);
                Color color = Color.Lerp(normal, green, Mathf.Clamp01(amount));
                block.Clear();
                block.SetColor(BaseColorId, color);
                block.SetColor(ColorId, color);
                renderers[r].SetPropertyBlock(block, i);
            }
        }
    }

    private void ClearTint()
    {
        if (!tintActive) return;
        tintActive = false;
        block.Clear();
        for (int r = 0; r < renderers.Count; r++)
        {
            if (renderers[r] == null) continue;
            for (int i = 0; i < originalColors[r].Length; i++) renderers[r].SetPropertyBlock(block, i);
        }
    }

    private void BuildOverlay()
    {
        GameObject canvasObject = new GameObject("Poison Overlay Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90; 
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject edgeObject = new GameObject("Poison Screen Edge");
        edgeObject.transform.SetParent(canvasObject.transform, false);
        screenEdge = edgeObject.AddComponent<Image>();
        screenEdge.sprite = CreateEdgeSprite();
        screenEdge.color = Color.clear;
        screenEdge.raycastTarget = false;
        RectTransform edgeRect = screenEdge.rectTransform;
        edgeRect.anchorMin = Vector2.zero;
        edgeRect.anchorMax = Vector2.one;
        edgeRect.offsetMin = Vector2.zero;
        edgeRect.offsetMax = Vector2.zero;

        Font font = null;
        try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
        if (font == null) return;

        GameObject textObject = new GameObject("Poison Warning");
        textObject.transform.SetParent(canvasObject.transform, false);
        warningLabel = textObject.AddComponent<Text>();
        warningLabel.text = warningText;
        warningLabel.font = font;
        warningLabel.fontSize = 40;
        warningLabel.fontStyle = FontStyle.Bold;
        warningLabel.alignment = TextAnchor.MiddleCenter;
        warningLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
        warningLabel.raycastTarget = false;
        warningLabel.enabled = false;
        Outline outline = textObject.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, -2f);

        RectTransform textRect = warningLabel.rectTransform;
        textRect.anchorMin = new Vector2(0.5f, 0f);
        textRect.anchorMax = new Vector2(0.5f, 0f);
        textRect.pivot = new Vector2(0.5f, 0f);
        textRect.anchoredPosition = new Vector2(0f, 140f);
        textRect.sizeDelta = new Vector2(900f, 60f);
    }

    private static Sprite CreateEdgeSprite()
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x / (float)(size - 1) * 2f - 1f;
                float dy = y / (float)(size - 1) * 2f - 1f;
                float distance = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) * 0.6f + new Vector2(dx, dy).magnitude * 0.4f;
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 1f, distance));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }


    private void SetupAudio()
    {
        GameObject audioObject = new GameObject("Poison Sound");
        audioObject.transform.SetParent(transform, false);
        audioSource = audioObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f; 
        audioSource.volume = 0f;
        audioSource.clip = loopSound;
    }
    
}
