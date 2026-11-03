using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// InputField 포커스 시 살짝 커지고 배경/테두리 색이 바뀐다. 포커스 해제 시 원래대로 돌아간다.
[RequireComponent(typeof(TMP_InputField))]
public class UIInputFieldFeel : MonoBehaviour
{
    [SerializeField] private float _focusScale = 1.01f;
    [SerializeField, Min(0f)] private float _duration = 0.15f;
    [SerializeField] private AnimationCurve _curve = new AnimationCurve(new Keyframe(0f, 0f, 0f, 2f), new Keyframe(1f, 1f, 0f, 0f));

    [Header("Background")]
    [Tooltip("비워두면 InputField의 Target Graphic을 사용한다.")]
    [SerializeField] private Graphic _background;
    [SerializeField] private Color _focusBackgroundColor = new Color(1f, 0.98f, 0.93f, 1f);

    [Header("Border")]
    [SerializeField] private Outline _outline;
    [SerializeField] private Color _focusOutlineColor = new Color(1f, 0.78f, 0.1f, 1f);

    private TMP_InputField _inputField;
    private RectTransform _rectTransform;
    private Vector3 _baseScale;
    private Color _baseBackgroundColor;
    private Color _baseOutlineColor;

    private float _focus; // 0 = 기본, 1 = 포커스
    private Coroutine _routine;

    private void Awake()
    {
        _inputField = GetComponent<TMP_InputField>();
        _rectTransform = (RectTransform)transform;
        _baseScale = _rectTransform.localScale;

        if (_background == null) _background = _inputField.targetGraphic;
        if (_outline == null) _outline = GetComponent<Outline>();
        if (_background != null) _baseBackgroundColor = _background.color;
        if (_outline != null) _baseOutlineColor = _outline.effectColor;
    }

    private void OnEnable()
    {
        _inputField.onSelect.AddListener(HandleSelect);
        _inputField.onDeselect.AddListener(HandleDeselect);
    }

    private void OnDisable()
    {
        _inputField.onSelect.RemoveListener(HandleSelect);
        _inputField.onDeselect.RemoveListener(HandleDeselect);

        StopRoutine();
        Apply(0f);
    }

    private void HandleSelect(string text)
    {
        AnimateTo(1f);
    }

    private void HandleDeselect(string text)
    {
        AnimateTo(0f);
    }

    private void AnimateTo(float target)
    {
        if (!isActiveAndEnabled) return;

        StopRoutine();
        _routine = StartCoroutine(AnimateRoutine(target));
    }

    private void StopRoutine()
    {
        if (_routine == null) return;

        StopCoroutine(_routine);
        _routine = null;
    }

    private IEnumerator AnimateRoutine(float target)
    {
        float start = _focus;
        float elapsed = 0f;

        while (elapsed < _duration)
        {
            elapsed += Time.unscaledDeltaTime;
            Apply(Mathf.Lerp(start, target, _curve.Evaluate(Mathf.Clamp01(elapsed / _duration))));
            yield return null;
        }

        Apply(target);
        _routine = null;
    }

    private void Apply(float focus)
    {
        _focus = focus;

        _rectTransform.localScale = _baseScale * Mathf.Lerp(1f, _focusScale, focus);
        if (_background != null) _background.color = Color.Lerp(_baseBackgroundColor, _focusBackgroundColor, focus);
        if (_outline != null) _outline.effectColor = Color.Lerp(_baseOutlineColor, _focusOutlineColor, focus);
    }
}
