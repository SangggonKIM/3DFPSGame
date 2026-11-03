using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 버튼 Hover/Press/Release 시 스케일과 색상으로 반응한다.
// 색상을 이 컴포넌트가 직접 다루므로 Button의 Transition은 None으로 둔다.
[RequireComponent(typeof(RectTransform))]
public class UIButtonFeel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private const float OvershootPhaseRatio = 0.4f; // Release 시간 중 Overshoot 지점까지 걸리는 비율

    [Header("Scale")]
    [SerializeField] private float _hoverScale = 1.03f;
    [SerializeField] private float _pressScale = 0.93f;
    [SerializeField] private float _overshootScale = 1.04f;

    [Header("Duration (초, timeScale 무시)")]
    [SerializeField, Min(0f)] private float _hoverDuration = 0.15f;
    [SerializeField, Min(0f)] private float _pressDuration = 0.08f;
    [SerializeField, Min(0f)] private float _releaseDuration = 0.22f;
    [SerializeField] private AnimationCurve _curve = new AnimationCurve(new Keyframe(0f, 0f, 0f, 2f), new Keyframe(1f, 1f, 0f, 0f));

    [Header("Color")]
    [SerializeField] private Graphic _background;
    [SerializeField] private TMP_Text _label;
    [SerializeField] private Color _hoverColor = Color.white;
    [SerializeField] private Color _hoverTextColor = new Color(0.1f, 0.1f, 0.1f, 1f);

    private RectTransform _rectTransform;
    private Selectable _selectable;
    private Vector3 _baseScale;
    private Color _baseColor;
    private Color _baseTextColor;

    private float _currentScale = 1f;
    private float _currentHighlight; // 0 = 기본 색, 1 = Hover 색
    private bool _isHovered;
    private bool _isPressed;
    private Coroutine _animation;

    private void Awake()
    {
        _rectTransform = (RectTransform)transform;
        _selectable = GetComponent<Selectable>();
        _baseScale = _rectTransform.localScale;

        if (_background == null) _background = GetComponent<Graphic>();
        if (_label == null) _label = GetComponentInChildren<TMP_Text>(true);
        if (_background != null) _baseColor = _background.color;
        if (_label != null) _baseTextColor = _label.color;
    }

    private void OnDisable()
    {
        // 연출 도중 꺼져도 원래 크기/색으로 돌아가도록 정리한다.
        StopAnimation();
        _isHovered = false;
        _isPressed = false;
        Apply(1f, 0f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        if (_isPressed || !IsInteractable()) return;

        Play(TweenRoutine(_hoverScale, 1f, _hoverDuration));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        if (_isPressed) return;

        Play(TweenRoutine(1f, 0f, _hoverDuration));
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || !IsInteractable()) return;

        _isPressed = true;
        Play(TweenRoutine(_pressScale, _currentHighlight, _pressDuration));
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isPressed) return;

        _isPressed = false;
        Play(ReleaseRoutine());
    }

    private bool IsInteractable()
    {
        return _selectable == null || _selectable.IsInteractable();
    }

    // 항상 하나의 연출만 돌도록 기존 코루틴을 끊고 새로 시작한다.
    private void Play(IEnumerator routine)
    {
        if (!isActiveAndEnabled) return;

        StopAnimation();
        _animation = StartCoroutine(routine);
    }

    private void StopAnimation()
    {
        if (_animation == null) return;

        StopCoroutine(_animation);
        _animation = null;
    }

    private IEnumerator ReleaseRoutine()
    {
        // 포인터가 아직 위에 있으면 Hover 상태로, 벗어났으면 기본 상태로 복귀한다.
        float restScale = _isHovered ? _hoverScale : 1f;
        float restHighlight = _isHovered ? 1f : 0f;

        yield return TweenRoutine(_overshootScale, restHighlight, _releaseDuration * OvershootPhaseRatio);
        yield return TweenRoutine(restScale, restHighlight, _releaseDuration * (1f - OvershootPhaseRatio));
    }

    private IEnumerator TweenRoutine(float targetScale, float targetHighlight, float duration)
    {
        float startScale = _currentScale;
        float startHighlight = _currentHighlight;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = _curve.Evaluate(Mathf.Clamp01(elapsed / duration));
            Apply(Mathf.LerpUnclamped(startScale, targetScale, t), Mathf.Lerp(startHighlight, targetHighlight, t));
            yield return null;
        }

        Apply(targetScale, targetHighlight);
    }

    private void Apply(float scale, float highlight)
    {
        _currentScale = scale;
        _currentHighlight = highlight;

        _rectTransform.localScale = _baseScale * scale;
        if (_background != null) _background.color = Color.Lerp(_baseColor, _hoverColor, highlight);
        if (_label != null) _label.color = Color.Lerp(_baseTextColor, _hoverTextColor, highlight);
    }
}
