using System.Collections;
using TMPro;
using UnityEngine;

// 랜덤 간격으로 아주 짧은 Glitch를 낸다.
// RGB Split은 원본 뒤에 그려지는 색상 복제 Text 두 장으로 표현한다.
[RequireComponent(typeof(TMP_Text))]
public class UITitleGlitch : MonoBehaviour
{
    [Header("발생 간격 (초)")]
    [SerializeField, Min(0f)] private float _minInterval = 2f;
    [SerializeField, Min(0f)] private float _maxInterval = 6f;

    [Header("지속 시간 (초)")]
    [SerializeField, Min(0.01f)] private float _minDuration = 0.03f;
    [SerializeField, Min(0.01f)] private float _maxDuration = 0.08f;
    [SerializeField, Range(1, 4)] private int _maxSteps = 3; // 한 번의 Glitch 안에서 튀는 횟수

    [Header("변형")]
    [SerializeField] private Vector2 _offsetXRange = new Vector2(2f, 5f); // px
    [SerializeField] private Vector2 _scaleXRange = new Vector2(0.98f, 1.03f);
    [SerializeField, Range(0f, 1f)] private float _minAlpha = 0.6f;

    [Header("RGB Split")]
    [SerializeField] private bool _useRgbSplit = true;
    [SerializeField] private Color _splitColorA = new Color(1f, 0.1f, 0.1f, 0.6f);
    [SerializeField] private Color _splitColorB = new Color(0.1f, 0.9f, 1f, 0.6f);
    [SerializeField] private Vector2 _splitOffsetRange = new Vector2(2f, 6f); // px

    private TMP_Text _text;
    private RectTransform _rectTransform;
    private Vector2 _basePosition;
    private Vector3 _baseScale;
    private Color _baseColor;

    private TextMeshProUGUI _splitA;
    private TextMeshProUGUI _splitB;
    private Coroutine _glitchLoop;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
        _rectTransform = (RectTransform)transform;
        _basePosition = _rectTransform.anchoredPosition;
        _baseScale = _rectTransform.localScale;
        _baseColor = _text.color;

        if (_useRgbSplit)
        {
            _splitA = CreateSplitLayer("_SplitA", _splitColorA);
            _splitB = CreateSplitLayer("_SplitB", _splitColorB);
        }
    }

    private void OnEnable()
    {
        _glitchLoop = StartCoroutine(GlitchLoop());
    }

    private void OnDisable()
    {
        if (_glitchLoop != null)
        {
            StopCoroutine(_glitchLoop);
            _glitchLoop = null;
        }

        ResetState();
    }

    private void OnDestroy()
    {
        // 복제 Text는 형제 오브젝트라 직접 정리한다.
        if (_splitA != null) Destroy(_splitA.gameObject);
        if (_splitB != null) Destroy(_splitB.gameObject);
    }

    private IEnumerator GlitchLoop()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(Random.Range(_minInterval, _maxInterval));

            int steps = Random.Range(1, _maxSteps + 1);
            float stepDuration = Random.Range(_minDuration, _maxDuration) / steps;
            for (int i = 0; i < steps; i++)
            {
                ApplyGlitch();
                yield return new WaitForSecondsRealtime(stepDuration);
            }

            ResetState();
        }
    }

    private void ApplyGlitch()
    {
        float offsetX = RandomSign() * Random.Range(_offsetXRange.x, _offsetXRange.y);
        Vector3 scale = new Vector3(_baseScale.x * Random.Range(_scaleXRange.x, _scaleXRange.y), _baseScale.y, _baseScale.z);

        _rectTransform.anchoredPosition = _basePosition + new Vector2(offsetX, 0f);
        _rectTransform.localScale = scale;

        Color color = _baseColor;
        color.a *= Random.Range(_minAlpha, 1f);
        _text.color = color;

        float split = Random.Range(_splitOffsetRange.x, _splitOffsetRange.y);
        ShowSplit(_splitA, offsetX - split, scale);
        ShowSplit(_splitB, offsetX + split, scale);
    }

    private void ShowSplit(TextMeshProUGUI layer, float offsetX, Vector3 scale)
    {
        if (layer == null) return;

        layer.text = _text.text;
        layer.rectTransform.anchoredPosition = _basePosition + new Vector2(offsetX, 0f);
        layer.rectTransform.localScale = scale;
        layer.gameObject.SetActive(true);
    }

    private void ResetState()
    {
        _rectTransform.anchoredPosition = _basePosition;
        _rectTransform.localScale = _baseScale;
        _text.color = _baseColor;

        if (_splitA != null) _splitA.gameObject.SetActive(false);
        if (_splitB != null) _splitB.gameObject.SetActive(false);
    }

    private TextMeshProUGUI CreateSplitLayer(string suffix, Color color)
    {
        var go = new GameObject(name + suffix, typeof(RectTransform));
        go.layer = gameObject.layer;

        // 원본보다 앞 순서에 두어 원본 뒤에 그려지게 한다.
        var rectTransform = (RectTransform)go.transform;
        rectTransform.SetParent(_rectTransform.parent, false);
        rectTransform.SetSiblingIndex(_rectTransform.GetSiblingIndex());
        rectTransform.anchorMin = _rectTransform.anchorMin;
        rectTransform.anchorMax = _rectTransform.anchorMax;
        rectTransform.pivot = _rectTransform.pivot;
        rectTransform.sizeDelta = _rectTransform.sizeDelta;
        rectTransform.anchoredPosition = _basePosition;
        rectTransform.localRotation = _rectTransform.localRotation;
        rectTransform.localScale = _baseScale;

        var layer = go.AddComponent<TextMeshProUGUI>();
        layer.font = _text.font;
        layer.fontSharedMaterial = _text.fontSharedMaterial;
        layer.fontSize = _text.fontSize;
        layer.fontStyle = _text.fontStyle;
        layer.alignment = _text.alignment;
        layer.textWrappingMode = _text.textWrappingMode;
        layer.characterSpacing = _text.characterSpacing;
        layer.raycastTarget = false;
        layer.color = color;

        go.SetActive(false);
        return layer;
    }

    private static float RandomSign()
    {
        return Random.value < 0.5f ? -1f : 1f;
    }
}
