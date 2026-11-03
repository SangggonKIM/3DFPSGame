using System.Collections;
using UnityEngine;

// 대상 UI들을 순서대로(Stagger) 페이드 + 슬라이드 업 + 스케일로 등장시킨다.
// 대상마다 CanvasGroup이 필요하며 없으면 자동으로 추가한다.
public class UIIntroAnimator : MonoBehaviour
{
    [Tooltip("등장 순서대로 넣는다.")]
    [SerializeField] private RectTransform[] _targets;

    [Header("Timing (초, timeScale 무시)")]
    [SerializeField, Min(0f)] private float _startDelay = 0.2f;
    [SerializeField, Min(0f)] private float _stagger = 0.1f;
    [SerializeField, Min(0.01f)] private float _duration = 0.45f;

    [Header("Motion")]
    [SerializeField] private float _offsetY = 30f; // 원래 위치보다 이만큼 아래에서 올라온다 (px)
    [SerializeField] private float _startScale = 0.97f;
    [SerializeField] private AnimationCurve _curve = new AnimationCurve(new Keyframe(0f, 0f, 0f, 2.5f), new Keyframe(1f, 1f, 0f, 0f));
    [SerializeField] private bool _playOnEnable = true;

    private CanvasGroup[] _groups;
    private Vector2[] _basePositions;
    private Vector3[] _baseScales;
    private Coroutine _routine;

    private void Awake()
    {
        int count = _targets.Length;
        _groups = new CanvasGroup[count];
        _basePositions = new Vector2[count];
        _baseScales = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            RectTransform target = _targets[i];
            if (!target.TryGetComponent(out _groups[i]))
            {
                _groups[i] = target.gameObject.AddComponent<CanvasGroup>();
            }

            _basePositions[i] = target.anchoredPosition;
            _baseScales[i] = target.localScale;
        }
    }

    private void OnEnable()
    {
        if (_playOnEnable) Play();
    }

    private void OnDisable()
    {
        // 중간에 꺼져도 UI가 투명한 채로 남지 않도록 최종 상태로 맞춘다.
        StopRoutine();
        for (int i = 0; i < _targets.Length; i++)
        {
            Apply(i, 1f);
        }
    }

    public void Play()
    {
        if (!isActiveAndEnabled) return;

        StopRoutine();
        _routine = StartCoroutine(PlayRoutine());
    }

    private void StopRoutine()
    {
        if (_routine == null) return;

        StopCoroutine(_routine);
        _routine = null;
    }

    private IEnumerator PlayRoutine()
    {
        int count = _targets.Length;
        for (int i = 0; i < count; i++)
        {
            Apply(i, 0f);
        }

        float totalTime = _startDelay + _stagger * Mathf.Max(0, count - 1) + _duration;
        float elapsed = 0f;

        while (elapsed < totalTime)
        {
            elapsed += Time.unscaledDeltaTime;
            for (int i = 0; i < count; i++)
            {
                Apply(i, Mathf.Clamp01((elapsed - _startDelay - _stagger * i) / _duration));
            }
            yield return null;
        }

        for (int i = 0; i < count; i++)
        {
            Apply(i, 1f);
        }
        _routine = null;
    }

    private void Apply(int index, float progress)
    {
        float eased = _curve.Evaluate(progress);
        bool isDone = progress >= 1f;

        CanvasGroup group = _groups[index];
        group.alpha = Mathf.Clamp01(eased);
        group.interactable = isDone;
        group.blocksRaycasts = isDone; // 등장 중에는 클릭되지 않게 막는다.

        RectTransform target = _targets[index];
        target.anchoredPosition = _basePositions[index] + new Vector2(0f, -_offsetY * (1f - eased));
        target.localScale = _baseScales[index] * Mathf.LerpUnclamped(_startScale, 1f, eased);
    }
}
