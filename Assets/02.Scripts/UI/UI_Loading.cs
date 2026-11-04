using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 로딩 화면. 다음 씬을 비동기로 불러오며 진행률을 슬라이더와 글자로 보여준다.
public class UI_Loading : MonoBehaviour
{
    // allowSceneActivation이 false면 progress는 0.9에서 멈춘다.
    private const float LoadCompleteProgress = 0.9f;
    private const float PercentMultiplier = 100f;

    [SerializeField] private Slider _progressSlider;
    [SerializeField] private TextMeshProUGUI _progressText;
    [SerializeField] private float _fillSpeed = 0.5f; // 초당 슬라이더가 차오르는 최대 비율
    [SerializeField] private float _completeDelay = 0.3f; // 100%를 보여준 뒤 씬 전환까지 대기 시간
    [SerializeField] private string _defaultSceneName = "GameScene"; // LoadingScene을 바로 실행했을 때 불러올 씬

    private void Awake()
    {
        _progressSlider.onValueChanged.AddListener(UpdateProgressText);
    }

    private void OnDestroy()
    {
        _progressSlider.onValueChanged.RemoveListener(UpdateProgressText);
    }

    private void Start()
    {
        _progressSlider.value = _progressSlider.minValue;
        UpdateProgressText(_progressSlider.value);

        StartCoroutine(Load_Coroutine());
    }

    private IEnumerator Load_Coroutine()
    {
        Application.backgroundLoadingPriority = ThreadPriority.Low;

        string sceneName = string.IsNullOrEmpty(SceneLoader.NextSceneName) ? _defaultSceneName : SceneLoader.NextSceneName;

        // 비동기 씬 로드!
        // 반환: 비동기 진행사항을 확인할 수 있는 객체
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        // 수많은 객체의 생성을 쪼개서 하나하나 생성할 준비를..
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            // 0에서 1사이의 값으로 보간
            float targetProgress = Mathf.Clamp01(operation.progress / LoadCompleteProgress);

            // 바로 대입하면 0 → 1로 순간 이동하므로 일정 속도로 따라가게 한다.
            // 이전 씬에서 timeScale이 0이었을 수 있어 unscaled 시간을 쓰되,
            // 씬 로드 직후 첫 프레임은 수 초가 걸릴 수 있어 maximumDeltaTime으로 제한한다.
            float deltaTime = Mathf.Min(Time.unscaledDeltaTime, Time.maximumDeltaTime);
            _progressSlider.value = Mathf.MoveTowards(_progressSlider.value, targetProgress, _fillSpeed * deltaTime);

            if (_progressSlider.value >= _progressSlider.maxValue)
            {
                yield return new WaitForSecondsRealtime(_completeDelay);
                operation.allowSceneActivation = true;
                yield break;
            }

            yield return null;
        }
    }

    // 슬라이더 값이 바뀔 때마다 호출되어 숫자가 항상 바와 일치한다.
    private void UpdateProgressText(float value)
    {
        int percent = Mathf.FloorToInt(value * PercentMultiplier);
        _progressText.text = $"불러오는 중 {percent}%";
    }
}
