using System;
using UnityEngine;
using UnityEngine.UI;

public class ZombieHealthBar : MonoBehaviour
{
    [SerializeField] private Slider _slider;

    [SerializeField] private Zombie _zombie;

    private Transform _cameraTransform;

    private void Awake()
    {
        if (_zombie == null)
        {
            _zombie = GetComponentInParent<Zombie>();
        }

        _zombie.OnHit += RefreshHealth;
    }

    private void Start()
    {
        // Camera.main을 좀비 수만큼 매 프레임 조회하지 않도록 한 번만 찾아 둔다.
        _cameraTransform = Camera.main.transform;
    }

    // 풀에서 다시 꺼내질 때(체력 초기화 후 활성화) 한 번 갱신한다.
    private void OnEnable()
    {
        RefreshHealth();
    }

    private void OnDestroy()
    {
        if (_zombie != null)
        {
            _zombie.OnHit -= RefreshHealth;
        }
    }

    private void LateUpdate()
    {
        // 체력은 OnHit 이벤트로만 갱신하고, 여기서는 빌보드만 처리한다.
        // 빌보드: 체력바가 카메라를 쳐다보게한다.
        transform.forward = _cameraTransform.forward;
    }

    private void RefreshHealth()
    {
        // 0 ~ 100% -> 0 ~ 1
        // _slider.value = 좀비의 현재 체력 / 좀비의 최대 체력
        _slider.value = _zombie.Health / _zombie.MaxHealth;
    }
}
