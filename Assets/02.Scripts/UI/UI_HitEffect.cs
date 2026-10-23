using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Image))]
public class UI_HitEffect : MonoBehaviour
{
    [SerializeField] private float _flashTime = 0.2f;

    private Image _image;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _image.enabled = false;
    }

    public void Flash(float alpha)
    {
        Color color = _image.color;
        color.a = alpha;
        _image.color = color;
        StartCoroutine(Flash_Coroutine());
    }

    public void Fade()
    {
        StartCoroutine(Fade_Coroutine());
    }

    public IEnumerator Flash_Coroutine()
    {
        _image.enabled = true;

        yield return StartCoroutine(Fade_Coroutine());

        _image.enabled = false;
    }

    public IEnumerator Fade_Coroutine()
    {
        // 2. 0.4 에서 0.2로 페이드아웃한다.
        Color color = _image.color;
        color.a = 0.4f;
        _image.color = color;
        for (int i = 0; i < 20; i++)
        {
            color.a -= 0.02f;
            _image.color = color;
            yield return new WaitForSeconds(_flashTime / 20);
        }
    }
}