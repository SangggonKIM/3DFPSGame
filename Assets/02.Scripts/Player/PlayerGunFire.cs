using System;
using System.Collections;
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

    [SerializeField] private GameObject[] _muzzleFlahes;
    [SerializeField] private float _flashTime = 0.05f;

    [Header("총알 궤적")]

    [SerializeField] private LineRenderer _bulletTrail;
    [SerializeField] private Transform _muzzlePoint;
    [SerializeField] private float _trailSpeed = 500f; // 초당 이동 거리
    [SerializeField] private float _trailLength = 3f; // 궤적 꼬리 길이

    public event Action<int, int> OnBulletFire;

    private int _bulletCount;

    public int BulletCount => _bulletCount;
    public int MaxBulletCount => _maxBulletCount;

    private float _fireTimer;
    private Animator _animator;
    private Coroutine _reloadCoroutine;
    private Coroutine _trailCoroutine;
    private int _fire = Animator.StringToHash("Fire");
    private int _moveMotion = Animator.StringToHash("MoveMotion");
    private Player _player;


    private void Awake()
    {
        _bulletCount = _maxBulletCount;
        _bulletTrail.positionCount = 2;
        _bulletTrail.useWorldSpace = true;
        _bulletTrail.enabled = false;
    }

    private void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        _player = GetComponent<Player>();
    }

    private void Update()
    {
        if (_player.WeaponMode == WeaponMode.Melee) return;
        if (GameManager.Instance.State != GameState.Playing) return;
        // 0. 발사 타이머를 지난 시간만큼 줄인다.
        _fireTimer -= Time.deltaTime;

        // 1. 마우스 왼쪽 버튼 입력 받기
        bool isMouseVisible = Cursor.visible;
        if (!isMouseVisible && Input.GetButton("Fire1") && _fireTimer <= 0f && _bulletCount > 0)
        {
            _bulletCount--;
            _fireTimer = _fireInterval;
            Shoot();
            OnBulletFire?.Invoke(_bulletCount, _maxBulletCount);
            _animator.SetBool(_fire, true);

            // 이 방식은: 이 모노비헤이비어가 등록한 모든 코루틴을 실행 취소
            // StopAllCorotuine(); 

            if (_reloadCoroutine != null)
            {
                StopCoroutine(_reloadCoroutine);
                Debug.Log("재장전 취소!");
                _reloadCoroutine = null;
            }
        }
        else
        {
            _animator.SetBool(_fire, false);
        }

        // 2. R키 누르면 총알 재장전 시작
        if (Input.GetButtonDown("Reload") && _reloadCoroutine == null)
        {
            _reloadCoroutine = StartCoroutine(Reloading_Coroutine());
        }
    }

    private IEnumerator Reloading_Coroutine()
    {
        Debug.Log("재장전 시작!");

        yield return new WaitForSeconds(_reloadingTime);

        _bulletCount = _maxBulletCount;
        OnBulletFire?.Invoke(_bulletCount, _maxBulletCount);

        Debug.Log("재장전 완료!");
    }

    private void Shoot()
    {
        StartCoroutine(MuzzleFlash_Coroutine());
        // 2. 레이(광선)를 생성하고 발사될 위치와 진행 방향 설정하기
        var cameraTransform = Camera.main.transform;
        var ray = new Ray(cameraTransform.position, cameraTransform.forward);

        // 3. 레이가 부딛힌 대상의 정보를 저장할 변수 생성하기
        var hitInfo = new RaycastHit();

        // 4. 레이를 발사하고, 만일 부딛힌 물체가 있다면 이펙트 표시하기
        var isHit = Physics.Raycast(ray, out hitInfo, _range);

        // 맞지 않았으면 사거리 끝까지 궤적을 그린다.
        Vector3 endPoint = isHit ? hitInfo.point : ray.GetPoint(_range);
        PlayBulletTrail(endPoint);

        if (isHit)
        {
            // Debug.Log($"{hitInfo.collider.name}이 총알을 맞았다. 데미지: {_damage}");

            // 맞은 자리에 총알 자국 이펙트 만들기
            var effect = Instantiate(_hitEffectPrefab);
            effect.transform.position = hitInfo.point; // 맞은 자리
            effect.transform.forward = hitInfo.normal; // 수직 위치로 회전
            effect.Play();

            // 대미지 구조체 만들기
            DamageInfo damageInfo = new DamageInfo();
            damageInfo.Value = _damage;
            damageInfo.HitPoint = hitInfo.point;
            damageInfo.HitNormal = hitInfo.normal;

            hitInfo.collider.GetComponent<IDamageable>()?.TakeDamage(damageInfo);

            // 황유찬 :       Player
            // 김민수 :      Zombie, Boss
            // 박현서: Drum, Box, Bird
        }
    }

    private IEnumerator MuzzleFlash_Coroutine()
    {
        int randomIndex = UnityEngine.Random.Range(0, _muzzleFlahes.Length);
        GameObject flash = _muzzleFlahes[randomIndex];
        flash.SetActive(true);

        yield return new WaitForSeconds(_flashTime);

        flash.SetActive(false);
    }

    private void PlayBulletTrail(Vector3 endPoint)
    {
        // 궤적 하나를 재사용하므로 이전 궤적은 중단한다.
        if (_trailCoroutine != null) StopCoroutine(_trailCoroutine);
        _trailCoroutine = StartCoroutine(BulletTrail_Coroutine(_muzzlePoint.position, endPoint));
    }

    private IEnumerator BulletTrail_Coroutine(Vector3 start, Vector3 end)
    {
        float distance = Vector3.Distance(start, end);
        float traveled = 0f;
        _bulletTrail.enabled = true;

        // 머리(head)가 목표까지 날아가고, 꼬리(tail)가 _trailLength만큼 뒤따라온다.
        while (traveled < distance + _trailLength)
        {
            traveled += _trailSpeed * Time.deltaTime;
            float head = Mathf.Min(traveled, distance);
            float tail = Mathf.Clamp(traveled - _trailLength, 0f, distance);

            _bulletTrail.SetPosition(0, Vector3.MoveTowards(start, end, tail));
            _bulletTrail.SetPosition(1, Vector3.MoveTowards(start, end, head));
            yield return null;
        }

        _bulletTrail.enabled = false;
        _trailCoroutine = null;
    }
}
