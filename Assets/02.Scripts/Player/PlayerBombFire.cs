using UnityEngine;

// 마우스 오른쪽 버튼을 누르면 시선이 바라보는 방향으로 수류탄을 던지고 싶다.
public class PlayerBombFire : MonoBehaviour
{
    [SerializeField] private Transform _firePoint;
    [SerializeField] private BombPool _bombPool;
    [SerializeField] private float _firstPower = 15f;
    [SerializeField] private int _maxCount = 10;
    [SerializeField] private float _throwFadeTime = 0.3f;

    private Animator _animator;
    private float _additionalForce = 0f;
    private int _bombCount;
    private float _throwPower;
    private int _throwLayer;
    private float _throwWeight;
    private int _throw = Animator.StringToHash("Throw");
    private Player _player;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _throwLayer = _animator.GetLayerIndex("Throw Layer");
        _bombCount = _maxCount;
        _throwPower = _firstPower;
        _player = GetComponent<Player>();
    }

    private void Update()
    {
        UpdateThrowWeight();

        if (GameManager.Instance.State != GameState.Playing) return;
        // 1. 마우스 오른쪽 버튼 입력 받기
        if (Input.GetButton("Fire2") && _player.WeaponMode == WeaponMode.Normal)
        {
            _additionalForce += 1f * Time.deltaTime;
            _throwPower = _firstPower + _additionalForce;
        }

        if (Input.GetButtonUp("Fire2") && _player.WeaponMode == WeaponMode.Normal)
        {
            if (_bombCount > 0 && Shoot()) _bombCount--;
            _additionalForce = 0f;
            _throwPower = _firstPower;
        }
    }

    private bool Shoot()
    {
        Camera viewCamera = Camera.main;
        if (_bombPool == null || _firePoint == null || viewCamera == null) return false;

        // 미리 만들어 둔 폭탄을 발사 위치로 옮긴 후 활성화합니다.
        Bomb bomb = _bombPool.GetBomb(_firePoint.position, Quaternion.identity);
        if (bomb == null) return false;

        bomb.Launch(viewCamera.transform.forward * _throwPower);
        _throwWeight = 1f;
        _animator.SetTrigger(_throw);
        return true;
    }

    private void UpdateThrowWeight()
    {
        AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(_throwLayer);
        if (state.IsName("Throw") && state.normalizedTime >= 0.5f)
        {
            _throwWeight = Mathf.MoveTowards(_throwWeight, 0f, Time.deltaTime / _throwFadeTime);
        }

        _animator.SetLayerWeight(_throwLayer, _throwWeight);
    }
}
