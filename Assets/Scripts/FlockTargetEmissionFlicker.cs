using UnityEngine;

[RequireComponent(typeof(FlockTarget))]
public class FlockTargetEmissionFlicker : MonoBehaviour
{
    private static readonly int _emissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly int _emissionMapId = Shader.PropertyToID("_EmissionMap");

    [SerializeField]
    private Renderer _targetRenderer;

    [SerializeField]
    private bool _searchRendererInChildren = true;

    [SerializeField]
    private Light _targetLight;

    [SerializeField]
    private bool _searchLightInChildren = true;

    [SerializeField, Min(0)]
    private int _materialIndex;

    [SerializeField, Min(0f)]
    private float _activeEmissionIntensityBoost = 4f;

    [SerializeField, Min(0f)]
    private float _flickerAmplitude = 0.35f;

    [SerializeField, Min(0f)]
    private float _flickerFrequency = 9f;

    [SerializeField, Min(0f)]
    private float _activeLightIntensityMultiplier = 1f;

    [SerializeField]
    private Color _fallbackEmissionColor = Color.white;

    private FlockTarget _flockTarget;
    private Material _material;
    private Color _baseEmissionColor;
    private float _baseLightIntensity;
    private bool _baseLightEnabled;
    private float _flickerSeed;
    private bool _hasEmissionColor;
    private bool _wasActive;

    private void Awake()
    {
        _flockTarget = GetComponent<FlockTarget>();
        _flickerSeed = Random.value * 100f;
        ResolveMaterial();
        ResolveLight();
    }

    private void OnEnable()
    {
        ResolveMaterial();
        ResolveLight();
        ApplyCurrentState(true);
    }

    private void OnDisable()
    {
        RestoreActiveVisuals();
    }

    private void Update()
    {
        ApplyCurrentState(false);
    }

    private void ResolveMaterial()
    {
        if (_targetRenderer == null)
        {
            _targetRenderer = _searchRendererInChildren
                ? GetComponentInChildren<Renderer>()
                : GetComponent<Renderer>();
        }

        if (_targetRenderer == null || _targetRenderer.sharedMaterials.Length == 0)
        {
            _material = null;
            _hasEmissionColor = false;
            return;
        }

        var materials = _targetRenderer.materials;
        var materialIndex = Mathf.Clamp(_materialIndex, 0, materials.Length - 1);
        _material = materials[materialIndex];
        _hasEmissionColor = _material.HasProperty(_emissionColorId);

        if (!_hasEmissionColor)
        {
            return;
        }

        _material.EnableKeyword("_EMISSION");
        _baseEmissionColor = _material.GetColor(_emissionColorId);

        if (_baseEmissionColor.maxColorComponent <= 0f && _material.HasProperty(_emissionMapId))
        {
            _baseEmissionColor = _fallbackEmissionColor;
        }
    }

    private void ResolveLight()
    {
        if (_targetLight == null)
        {
            _targetLight = _searchLightInChildren
                ? GetComponentInChildren<Light>()
                : GetComponent<Light>();
        }

        if (_targetLight != null)
        {
            _baseLightIntensity = _targetLight.intensity;
            _baseLightEnabled = _targetLight.enabled;
        }
    }

    private void ApplyCurrentState(bool force)
    {
        if ((_material == null || !_hasEmissionColor) && _targetLight == null)
        {
            return;
        }

        var isActive = _flockTarget.IsActive;
        if (!isActive)
        {
            if (_wasActive || force)
            {
                ApplyInactiveVisuals();
            }

            _wasActive = false;
            return;
        }

        var flicker = GetFlickerIntensity();
        ApplyEmission(_activeEmissionIntensityBoost + flicker);
        ApplyLight(flicker);
        _wasActive = true;
    }

    private float GetFlickerIntensity()
    {
        if (_flickerAmplitude <= 0f || _flickerFrequency <= 0f)
        {
            return 0f;
        }

        var noise = Mathf.PerlinNoise(_flickerSeed, Time.time * _flickerFrequency) * 2f - 1f;
        var wave = Mathf.Sin((Time.time + _flickerSeed) * _flickerFrequency) * 0.35f;
        return (noise + wave) * _flickerAmplitude;
    }

    private void ApplyEmission(float intensityBoost)
    {
        var multiplier = Mathf.Pow(2f, intensityBoost);
        _material.SetColor(_emissionColorId, _baseEmissionColor * multiplier);
    }

    private void ApplyLight(float flicker)
    {
        if (_targetLight == null)
        {
            return;
        }

        _targetLight.enabled = true;

        var multiplier = Mathf.Max(0f, _activeLightIntensityMultiplier + flicker);
        _targetLight.intensity = _baseLightIntensity * multiplier;
    }

    private void RestoreActiveVisuals()
    {
        RestoreEmission();
        RestoreLight();
    }

    private void ApplyInactiveVisuals()
    {
        RestoreEmission();
        DisableLight();
    }

    private void RestoreEmission()
    {
        if (_material == null || !_hasEmissionColor)
        {
            return;
        }

        _material.SetColor(_emissionColorId, _baseEmissionColor);
    }

    private void RestoreLight()
    {
        if (_targetLight == null)
        {
            return;
        }

        _targetLight.intensity = _baseLightIntensity;
        _targetLight.enabled = _baseLightEnabled;
    }

    private void DisableLight()
    {
        if (_targetLight == null)
        {
            return;
        }

        _targetLight.enabled = false;
    }
}
