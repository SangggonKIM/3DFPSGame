using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Image))]
public class UI_HitEffect : MonoBehaviour
{
    [SerializeField] private float _flashTime = 0.2f;
    [SerializeField] private float _fadeTime = 0.7f;

    private Image _image;
    private Coroutine _effectCoroutine;
    private WaitForSeconds _flashWait;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _image.enabled = false;
        _flashWait = new WaitForSeconds(_flashTime);
    }

    private void Start()
    {
        Player player = GameObject.FindAnyObjectByType<Player>();
        if (player != null)
        {
            player.OnDamaged += OnPlayerDamaged;
        }
    }

    private void OnPlayerDamaged()
    {
        Fade(0.6f);
    }

    public void Flash(float alpha)
    {
        SetAlpha(alpha);
        RestartEffect(Flash_Coroutine());
    }

    public void Fade(float alpha)
    {
        SetAlpha(alpha);
        RestartEffect(Fade_Coroutine());
    }

    // 연속으로 맞아도 코루틴이 겹쳐 쌓이지 않도록 이전 효과를 멈추고 새로 시작한다.
    private void RestartEffect(IEnumerator routine)
    {
        if (_effectCoroutine != null) StopCoroutine(_effectCoroutine);
        _effectCoroutine = StartCoroutine(routine);
    }

    public IEnumerator Flash_Coroutine()
    {
        _image.enabled = true;

        yield return _flashWait;

        _image.enabled = false;
    }

    public IEnumerator Fade_Coroutine()
    {
        _image.enabled = true;

        float elapsedTime = 0f;

        float startAlpha = _image.color.a;

        while (elapsedTime < _fadeTime)
        {
            float alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / _fadeTime);

            SetAlpha(alpha);

            elapsedTime += Time.deltaTime;
            yield return null; // 한 프레임씩 한다!
        }

        SetAlpha(0f);
        _image.enabled = false;
    }

    private void SetAlpha(float alpha)
    {
        Color color = _image.color;
        color.a = alpha;
        _image.color = color;
    }
}
