using UnityEngine;

// 마우스 왼쪽 버튼을 누르면 바라보는 방향으로 총알을 발사하고 싶다.
public class PlayerGunFire : MonoBehaviour
{
    [SerializeField] private ParticleSystem _hitEffectPrefab;

    [Header("스탯")]

    [SerializeField] private int _maxBulletCount = 30;
    [SerializeField] private float _reloadingTime = 3f;
    [SerializeField] private float _range = 100f; // 사거리
    [SerializeField] private float _damage = 10f; // 공격력
    [SerializeField] private float _fireInterval = 0.1f; // 발사 속도

    private float _fireTimer;
    private int _bulletCount = 0;
    private bool _isReloading = false;
    private float _reloadingTimer = 0f;

    private void Start()
    {
        _bulletCount = _maxBulletCount;
    }

    private void Update()
    {
        // 0. 발사 타이머를 지난 시간만큼 줄인다.
        _fireTimer -= Time.deltaTime;

        // 1. 마우스 왼쪽 버튼 입력 받기
        if (Input.GetButton("Fire1") && _fireTimer <= 0f && _bulletCount > 0)
        {
            _bulletCount--;
            _fireTimer = _fireInterval;
            Shoot();
        }

        // 2. R키 누르면 총알 재장전 시작
        if (Input.GetButtonDown("Reload") && _isReloading == false)
        {
            _isReloading = true;
            _reloadingTimer = 0f;
        }

        // 3. 재장전 중
        if (_isReloading)
        {
            _reloadingTimer += Time.deltaTime;
            Debug.Log($"재장전 중: {_reloadingTimer}");
            if (_reloadingTimer >= _reloadingTime) // 재장전 시간이 지났다면
            {
                _isReloading = false;
                Reloading(); // 재장전 완료!
                Debug.Log($"재장전 완료");
            }
        }
    }

    private void Reloading()
    {
        _bulletCount = _maxBulletCount;
    }

    private void Shoot()
    {
        // 2. 레이(광선)를 생성하고 발사될 위치와 진행 방향 설정하기
        var cameraTransform = Camera.main.transform;
        var ray = new Ray(cameraTransform.position, cameraTransform.forward);

        // 3. 레이가 부딛힌 대상의 정보를 저장할 변수 생성하기
        var hitInfo = new RaycastHit();

        // 4. 레이를 발사하고, 만일 부딛힌 물체가 있다면 이펙트 표시하기
        var isHit = Physics.Raycast(ray, out hitInfo, _range);
        if (isHit)
        {
            // Debug.Log($"{hitInfo.collider.name}이 총알을 맞았다. 데미지: {_damage}");

            var effect = Instantiate(_hitEffectPrefab);
            effect.transform.position = hitInfo.point; // 맞은 자리
            effect.transform.forward = hitInfo.normal; // 수직 위치로 회전
            effect.Play();
        }
    }
}
