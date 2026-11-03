using System.Collections;
using UnityEngine;

// 평소엔 켜져 있다가 랜덤 간격으로 짧게 여러 번 깜빡이는 고장난 조명 효과.
[RequireComponent(typeof(Light))]
public class LightRandomFlicker : MonoBehaviour
{
    [Header("발생 간격 (초)")]
    [SerializeField, Min(0f)] private float _minInterval = 1.5f;
    [SerializeField, Min(0f)] private float _maxInterval = 5f;

    [Header("깜빡임")]
    [SerializeField, Min(1)] private int _minFlickerCount = 2;
    [SerializeField, Min(1)] private int _maxFlickerCount = 6;
    [SerializeField] private Vector2 _offDurationRange = new Vector2(0.03f, 0.12f); // 어두워져 있는 시간
    [SerializeField] private Vector2 _onDurationRange = new Vector2(0.03f, 0.15f);  // 깜빡임 사이 켜져 있는 시간
    [Tooltip("깜빡일 때 기본 세기 대비 밝기 비율 범위 (0이면 완전히 꺼짐)")]
    [SerializeField] private Vector2 _dimRatioRange = new Vector2(0f, 0.3f);

    private Light _light;
    private float _baseIntensity;
    private Coroutine _flickerLoop;

    private void Awake()
    {
        _light = GetComponent<Light>();
        _baseIntensity = _light.intensity;
    }

    private void OnEnable()
    {
        _flickerLoop = StartCoroutine(FlickerLoop());
    }

    private void OnDisable()
    {
        if (_flickerLoop != null)
        {
            StopCoroutine(_flickerLoop);
            _flickerLoop = null;
        }

        _light.intensity = _baseIntensity;
    }

    private IEnumerator FlickerLoop()
    {
        while (true)
        {
            _light.intensity = _baseIntensity;
            yield return new WaitForSeconds(Random.Range(_minInterval, _maxInterval));

            int count = Random.Range(_minFlickerCount, _maxFlickerCount + 1);
            for (int i = 0; i < count; i++)
            {
                _light.intensity = _baseIntensity * Random.Range(_dimRatioRange.x, _dimRatioRange.y);
                yield return new WaitForSeconds(Random.Range(_offDurationRange.x, _offDurationRange.y));

                _light.intensity = _baseIntensity;
                yield return new WaitForSeconds(Random.Range(_onDurationRange.x, _onDurationRange.y));
            }
        }
    }
}
