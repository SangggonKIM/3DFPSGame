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

    public void Flash()
    {
        StartCoroutine(Flash_Coroutine());
    }

    public void Fade()
    {
        StartCoroutine(Flash_Coroutine());
    }

    public IEnumerator Flash_Coroutine()
    {
        _image.enabled = true;

        yield return new WaitForSeconds(_flashTime);

        _image.enabled = false;
    }

    public IEnumerator Fade_Coroutine()
    {
        // 1. 0.4로 보이게 한다.
        // 2. 0.4 에서 0.2로 페이드아웃한다.
    }
}