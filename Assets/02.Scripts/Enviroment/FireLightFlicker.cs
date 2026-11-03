using UnityEngine;

// 불빛처럼 Light 세기를 Perlin Noise로 불규칙하게 흔든다.
[RequireComponent(typeof(Light))]
public class FireLightFlicker : MonoBehaviour
{
    private const float NoiseSeedRange = 100f;

    [SerializeField, Range(0f, 1f)] private float _flickerAmount = 0.35f; // 기본 세기 대비 흔들림 비율
    [SerializeField, Min(0f)] private float _flickerSpeed = 6f;

    private Light _light;
    private float _baseIntensity;
    private float _seed;

    private void Awake()
    {
        _light = GetComponent<Light>();
        _baseIntensity = _light.intensity;
        _seed = Random.Range(0f, NoiseSeedRange);
    }

    private void OnDisable()
    {
        _light.intensity = _baseIntensity;
    }

    private void Update()
    {
        float noise = Mathf.PerlinNoise(_seed, Time.time * _flickerSpeed) * 2f - 1f;
        _light.intensity = _baseIntensity * (1f + noise * _flickerAmount);
    }
}
