using UnityEngine;

// 타이틀이 숨 쉬듯 아주 미세하게 움직이게 한다.
// 축마다 주기를 다르게 줘서 움직임이 반복되는 티가 덜 나게 한다.
[RequireComponent(typeof(RectTransform))]
public class UITitleIdle : MonoBehaviour
{
    private const float TwoPi = Mathf.PI * 2f;

    [Header("진폭")]
    [SerializeField] private float _positionAmplitude = 3f;   // px
    [SerializeField] private float _rotationAmplitude = 0.2f; // 도
    [SerializeField] private float _scaleAmplitude = 0.005f;

    [Header("주기 (초)")]
    [SerializeField, Min(0.1f)] private float _positionPeriod = 4.2f;
    [SerializeField, Min(0.1f)] private float _rotationPeriod = 6.7f;
    [SerializeField, Min(0.1f)] private float _scalePeriod = 5.3f;

    [Header("파형 (끄면 Sin 사용)")]
    [SerializeField] private bool _useCurve;
    [Tooltip("0~1 구간 한 주기, 값 범위 -1~1")]
    [SerializeField]
    private AnimationCurve _waveCurve = new AnimationCurve(
        new Keyframe(0f, 0f, TwoPi, TwoPi),
        new Keyframe(0.25f, 1f, 0f, 0f),
        new Keyframe(0.5f, 0f, -TwoPi, -TwoPi),
        new Keyframe(0.75f, -1f, 0f, 0f),
        new Keyframe(1f, 0f, TwoPi, TwoPi));

    private RectTransform _rectTransform;
    private Vector2 _basePosition;
    private Quaternion _baseRotation;
    private Vector3 _baseScale;
    private float _time;

    private void Awake()
    {
        _rectTransform = (RectTransform)transform;
        _basePosition = _rectTransform.anchoredPosition;
        _baseRotation = _rectTransform.localRotation;
        _baseScale = _rectTransform.localScale;
    }

    private void OnDisable()
    {
        _rectTransform.anchoredPosition = _basePosition;
        _rectTransform.localRotation = _baseRotation;
        _rectTransform.localScale = _baseScale;
    }

    private void Update()
    {
        _time += Time.unscaledDeltaTime;

        float y = Wave(_positionPeriod, 0f) * _positionAmplitude;
        float rotationZ = Wave(_rotationPeriod, 0.3f) * _rotationAmplitude;
        float scale = 1f + Wave(_scalePeriod, 0.6f) * _scaleAmplitude;

        _rectTransform.anchoredPosition = _basePosition + new Vector2(0f, y);
        _rectTransform.localRotation = _baseRotation * Quaternion.Euler(0f, 0f, rotationZ);
        _rectTransform.localScale = _baseScale * scale;
    }

    // -1~1 범위의 주기 파형
    private float Wave(float period, float phase)
    {
        float cycle = _time / period + phase;
        return _useCurve ? _waveCurve.Evaluate(Mathf.Repeat(cycle, 1f)) : Mathf.Sin(cycle * TwoPi);
    }
}
