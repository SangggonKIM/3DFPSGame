using System.Collections;
using TMPro;
using UnityEngine;

// 로그인 실패 등에서 UI를 좌우로 짧게 흔들고, 오류 문구를 Fade In/Out 한다.
// 사용 예: passwordFieldEffect.PlayErrorShake();  passwordFieldEffect.ShowError("비밀번호가 틀렸습니다.");
[RequireComponent(typeof(RectTransform))]
public class UIErrorShake : MonoBehaviour
{
    [Header("Shake")]
    [Tooltip("원래 위치 기준 X 오프셋(px)을 순서대로 거친다. 마지막은 0이어야 제자리로 돌아온다.")]
    [SerializeField] private float[] _offsetsX = { 8f, -8f, 5f, -5f, 0f };
    [SerializeField, Min(0.01f)] private float _shakeDuration = 0.25f;

    [Header("Error Text (선택)")]
    [SerializeField] private TMP_Text _errorText;
    [SerializeField, Min(0f)] private float _errorFadeDuration = 0.2f;

    private RectTransform _rectTransform;
    private Vector2 _basePosition;
    private Coroutine _shakeRoutine;
    private Coroutine _fadeRoutine;
    private float _errorTargetAlpha;

    private void Awake()
    {
        _rectTransform = (RectTransform)transform;
        _basePosition = _rectTransform.anchoredPosition;
        if (_errorText != null) _errorTargetAlpha = _errorText.alpha;
    }

    private void OnDisable()
    {
        StopRoutine(ref _shakeRoutine);
        _rectTransform.anchoredPosition = _basePosition;

        // Fade 도중 꺼지면 목표 알파로 맞춰 어중간한 상태가 남지 않게 한다.
        StopRoutine(ref _fadeRoutine);
        if (_errorText != null) _errorText.alpha = _errorTargetAlpha;
    }

    public void PlayErrorShake()
    {
        if (!isActiveAndEnabled || _offsetsX.Length == 0) return;

        StopRoutine(ref _shakeRoutine);
        _shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    public void ShowError(string message)
    {
        if (_errorText == null) return;

        _errorText.text = message;
        FadeErrorTo(1f);
    }

    public void HideError()
    {
        FadeErrorTo(0f);
    }

    private IEnumerator ShakeRoutine()
    {
        float segmentDuration = _shakeDuration / _offsetsX.Length;
        float from = 0f;

        foreach (float to in _offsetsX)
        {
            float elapsed = 0f;
            while (elapsed < segmentDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float x = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / segmentDuration));
                _rectTransform.anchoredPosition = _basePosition + new Vector2(x, 0f);
                yield return null;
            }
            from = to;
        }

        _rectTransform.anchoredPosition = _basePosition;
        _shakeRoutine = null;
    }

    private void FadeErrorTo(float targetAlpha)
    {
        if (_errorText == null) return;

        _errorTargetAlpha = targetAlpha;
        if (!isActiveAndEnabled)
        {
            _errorText.alpha = targetAlpha;
            return;
        }

        StopRoutine(ref _fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha));
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        float startAlpha = _errorText.alpha;
        float elapsed = 0f;

        while (elapsed < _errorFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _errorText.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / _errorFadeDuration);
            yield return null;
        }

        _errorText.alpha = targetAlpha;
        _fadeRoutine = null;
    }

    private void StopRoutine(ref Coroutine routine)
    {
        if (routine == null) return;

        StopCoroutine(routine);
        routine = null;
    }
}
