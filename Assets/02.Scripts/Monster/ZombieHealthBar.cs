using System;
using UnityEngine;
using UnityEngine.UI;

public class ZombieHealthBar : MonoBehaviour
{
    [SerializeField] private Slider _slider;

    [SerializeField] private Zombie _zombie;

    private void Awake()
    {
        if (_zombie == null)
        {
            _zombie = GetComponentInParent<Zombie>();
        }
    }

    private void LateUpdate()
    {
        // 0 ~ 100% -> 0 ~ 1
        // _slider.value = 좀비의 현재 체력 / 좀비의 최대 체력
        _slider.value = _zombie.Health / _zombie.MaxHealth;

        // 빌보드: 체력바가 카메라를 쳐다보게한다.
        transform.forward = Camera.main.transform.forward;
    }
}
