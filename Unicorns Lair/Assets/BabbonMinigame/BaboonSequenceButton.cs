using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BaboonSequenceButton : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private int buttonIndex;

    [Header("Colors (applied to the tinted materials)")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private float flashDuration = 0.35f;

    [Header("Materials to tint")]
    [Tooltip("Renderer holding the djembe materials. Auto-found in children if left empty.")]
    [SerializeField] private Renderer targetRenderer;
    [Tooltip("Material slots are matched if their name contains any of these tokens (e.g. Material.002 -> '002').")]
    [SerializeField] private string[] colorMaterialNames = { "002", "004" };
    [Tooltip("Fallback: if no material names match, these material slot indices are tinted instead.")]
    [SerializeField] private int[] colorMaterialIndices = { 2, 4 };

    [Header("Pop Animation (instead of highlight)")]
    [Tooltip("How much the object scales up at the peak of the pop (1 = no scaling).")]
    [SerializeField] private float popScale = 1.18f;
    [Tooltip("How far the object rises (local units) at the peak of the pop.")]
    [SerializeField] private float popHeight = 0.2f;

    [Header("Clicking")]
    [Tooltip("Camera used for click raycasts. Defaults to Camera.main.")]
    [SerializeField] private Camera clickCamera;
    [Tooltip("If no collider exists on this object or its children, a BoxCollider is added automatically.")]
    [SerializeField] private bool autoAddCollider = true;

    [Header("Animation")]
    public string animationTrigger;

    private BaboonSequenceManager manager;
    private readonly List<Material> _tintMats = new List<Material>();
    private Vector3 _baseLocalPos;
    private Vector3 _baseScale;
    private bool _interactable;
    private bool _animating;

    public int ButtonIndex => buttonIndex;

    static readonly int ID_BaseColor = Shader.PropertyToID("_Base_Color");
    static readonly int ID_Color = Shader.PropertyToID("_Color");

    private void Awake()
    {
        if (targetRenderer == null) targetRenderer = GetComponentInChildren<Renderer>();

        _baseLocalPos = transform.localPosition;
        _baseScale = transform.localScale;

        GatherTintMaterials();
        EnsureCollider();

        if (clickCamera == null) clickCamera = Camera.main;

        ApplyNormalColor();
    }

    void GatherTintMaterials()
    {
        _tintMats.Clear();
        if (targetRenderer == null) return;


        var mats = targetRenderer.materials;


        if (colorMaterialNames != null)
        {
            foreach (var m in mats)
            {
                if (m == null) continue;
                foreach (var token in colorMaterialNames)
                {
                    if (!string.IsNullOrEmpty(token) && m.name.Contains(token))
                    {
                        if (!_tintMats.Contains(m)) _tintMats.Add(m);
                        break;
                    }
                }
            }
        }

        if (_tintMats.Count == 0 && colorMaterialIndices != null)
        {
            foreach (var idx in colorMaterialIndices)
                if (idx >= 0 && idx < mats.Length && mats[idx] != null && !_tintMats.Contains(mats[idx]))
                    _tintMats.Add(mats[idx]);
        }
    }

    void EnsureCollider()
    {
        if (!autoAddCollider) return;
        if (GetComponentInChildren<Collider>() != null) return;

        var host = targetRenderer != null ? targetRenderer.gameObject : gameObject;
        host.AddComponent<BoxCollider>();
    }

    public void Initialize(BaboonSequenceManager owner, int index)
    {
        manager = owner;
        buttonIndex = index;
    }

    void Update()
    {
        if (!_interactable) return;

        var pointer = Pointer.current;
        if (pointer == null || !pointer.press.wasPressedThisFrame) return;

        var cam = clickCamera != null ? clickCamera : Camera.main;
        if (cam == null) return;
        clickCamera = cam;

        Ray ray = cam.ScreenPointToRay(pointer.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            if (hit.collider != null && hit.collider.GetComponentInParent<BaboonSequenceButton>() == this)
                PressButton();
        }
    }

    private void PressButton()
    {
        manager?.NotifyButtonPressed(this);
    }

    public IEnumerator Flash()
    {
        _animating = true;
        SetTint(highlightColor);

        float half = Mathf.Max(0.01f, flashDuration * 0.5f);

        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            ApplyPop(Mathf.SmoothStep(0f, 1f, t / half));
            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            ApplyPop(Mathf.SmoothStep(1f, 0f, t / half));
            yield return null;
        }

        ApplyPop(0f);
        SetTint(normalColor);
        _animating = false;
    }

    void ApplyPop(float amount)
    {
        transform.localScale = _baseScale * Mathf.Lerp(1f, popScale, amount);
        transform.localPosition = _baseLocalPos + Vector3.up * (popHeight * amount);
    }

    public void ApplyNormalColor()
    {
        if (!_animating)
        {
            transform.localScale = _baseScale;
            transform.localPosition = _baseLocalPos;
        }
        SetTint(normalColor);
    }

    void SetTint(Color c)
    {
        foreach (var m in _tintMats)
        {
            if (m == null) continue;

            if (m.HasProperty(ID_BaseColor))
                m.SetColor(ID_BaseColor, c);

            if (m.HasProperty(ID_Color))
                m.SetColor(ID_Color, c);
        }
    }

    public void SetInteractable(bool value)
    {
        _interactable = value;
    }
}