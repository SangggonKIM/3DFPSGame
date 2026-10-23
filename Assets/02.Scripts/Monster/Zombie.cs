using UnityEngine;
using System.Collections;

// FSM(유한 상태 머신) : 유한개의 상태가 있고, 상태마다 행동을 다르게 함으로써 AI를 구현하는 유명한 방법
// - "상태": 객체가 가질 수 있는 어떤 조건이나 상황
// - "전이": 조건에 따라 현재 상태에서 다른 상태로 넘어가는 것
// - 그리고 위 방식에 따라 규칙을 세워둔 것을 "머신"


// 좀비가 가질 수 있는 상태

public enum ZombieState
{
    Idle, // 대기: 제자리에서 플레이어를 기다린다.
    Trace, // 추격: 플레이어를 쫓아간다.
    Comeback, // 복귀: 처음 자리로 돌아간다.
    Attack, // 공격: 일정 시간마다 플레이어를 때린다.
    Hit, // 피격: 공격(총알, 폭탄)을 받아 잠깐 멈칫한다.
    Death, // 죽음: 쓰러지고 잠시 뒤 사라진다.
    Patrol // 순찰: 처음 자리에서 좌우 5유닛을 왕복한다.
}

public class Zombie : MonoBehaviour, IDamageable
{
    [Header("스탯")]

    [SerializeField] private float _detectDistance = 8f; // 플레이어를 발견하는 거리
    [SerializeField] private float _comebackDistance = 12f; // 처음 자리에서 이만큼 멀어지면 돌아가는 거리
    [SerializeField] private float _attackDamage = 10f;
    [SerializeField] private float _attackRange = 1.5f;
    [SerializeField] private float _attackInterval = 1.2f;
    [SerializeField] private float _moveSpeed = 5f; // 이동속도
    [SerializeField] private float _maxHealth = 30f; // 최대 체력
    private float _health; // 체력

    [Header("이펙트")]

    [SerializeField] private GameObject _bloodSprayPrefab;

    private CharacterController _controller;
    private Vector3 _startPosition;
    private GameObject _player;
    private float _attackTimer = 0f;
    private const float PatrolDistance = 5f;
    private float _patrolTimer = 0f;

    public float Health => _health;
    public float MaxHealth => _maxHealth;


    private ZombieState _state = ZombieState.Patrol;

    private void Start()
    {
        _health = _maxHealth;
        _startPosition = transform.position;
        _controller = GetComponent<CharacterController>();

        // 게임을 시작하면 플레이어를 찾는다.
        _player = GameObject.FindGameObjectWithTag("Player");
    }

    private void Update()
    {
        switch (_state)
        {
            case ZombieState.Idle:
                Idle();
                break;
            case ZombieState.Patrol:
                Patrol();
                break;
            case ZombieState.Trace:
                Trace();
                break;
            case ZombieState.Comeback:
                Comeback();
                break;
            case ZombieState.Attack:
                Attack();
                break;
        }
    }

    // 대미지를 받는다.
    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_state == ZombieState.Death)
        {
            return;
        }

        _health -= damageInfo.Value;

        StopAllCoroutines();

        if (_health > 0f)
        {
            // Todo. 맞은 부위에 피 이펙트를 실행하고 싶다.
            GameObject bloodSpray = Instantiate(_bloodSprayPrefab, transform);
            bloodSpray.transform.position = transform.position;
            bloodSpray.transform.forward = transform.forward;

            // 잠깐 멈칫 하고 싶다!
            Debug.Log($"{_state} -> Hit");
            _state = ZombieState.Hit;
            StartCoroutine(Hit_Coroutine());
        }
        else
        {
            Debug.Log($"{_state} -> Death");
            _state = ZombieState.Death;
            StartCoroutine(Death_Coroutine());
        }
    }

    private IEnumerator Hit_Coroutine()
    {
        yield return new WaitForSeconds(0.3f); // 잠깐 멈칫한다!

        // 맞았으니 쫓아온다.
        Debug.Log($"{_state} -> Trace");
        _state = ZombieState.Trace;
    }

    private IEnumerator Death_Coroutine()
    {
        _controller.enabled = false;
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
    }

    private void Idle()
    {
        // Todo. Idle 애니메이션 실행

        // 플레이어 가까워지면 Trace 상태로 전이한다.
        float distance = Vector3.Distance(transform.position, _player.transform.position);
        if (distance <= _detectDistance)
        {
            Debug.Log($"{_state} -> Trace");
            _state = ZombieState.Trace;
        }
    }

    private void Patrol()
    {
        // 순찰 중 플레이어를 발견하면 현재 위치에서 바로 추격한다.
        if (_player != null &&
            Vector3.Distance(transform.position, _player.transform.position) <= _detectDistance)
        {
            Debug.Log($"{_state} -> Trace");
            _state = ZombieState.Trace;
            return;
        }

        if (Time.deltaTime <= 0f)
        {
            return;
        }

        _patrolTimer += Time.deltaTime;
        // 중앙에서 오른쪽으로 출발하고, 양 끝에 도달하면 방향을 바꾼다.
        float offset = Mathf.PingPong(_patrolTimer * _moveSpeed + PatrolDistance,
            PatrolDistance * 2f) - PatrolDistance;
        Vector3 targetPosition = _startPosition + Vector3.right * offset;
        Vector3 movement = targetPosition - transform.position;
        movement.y = 0f;

        Vector3 velocity = Vector3.ClampMagnitude(movement / Time.deltaTime, _moveSpeed);
        if (velocity.sqrMagnitude > 0.0001f)
        {
            transform.forward = velocity.normalized;
        }

        _controller.SimpleMove(velocity);
    }

    // 추격: 플레이어를 쫓아간다.
    private void Trace()
    {
        // Todo. Run 애니메이션 실행

        // 처음 자리에서 너무 멀어지면 돌아간다.
        if (Vector3.Distance(transform.position, _startPosition) > _comebackDistance)
        {
            Debug.Log($"{_state} -> Comeback");
            _state = ZombieState.Comeback;
            return;
        }

        // 플레이어가 공격 거리 안이면 공격한다.
        if (Vector3.Distance(transform.position, _player.transform.position) <= _attackRange)
        {
            _attackTimer = 0f;
            Debug.Log($"{_state} -> Attack");
            _state = ZombieState.Attack;
            return;
        }

        Vector3 direction = _player.transform.position - transform.position;
        direction.y = 0f;
        direction.Normalize();

        transform.forward = direction;
        _controller.SimpleMove(direction * _moveSpeed);
    }

    private void Comeback()
    {
        if (Vector3.Distance(transform.position, _startPosition) < 1f)
        {
            _patrolTimer = 0f;
            Debug.Log($"{_state} -> Patrol");
            _state = ZombieState.Patrol;
            return;
        }

        Vector3 direction = _startPosition - transform.position;
        direction.y = 0f;
        direction.Normalize();

        transform.forward = direction;
        _controller.SimpleMove(direction * _moveSpeed);
    }

    // 공격: 일정 시간마다 플레이어를 때린다.
    private void Attack()
    {
        // 플레이어가 공격 사정 거리 밖으로 나가면 쫓아!
        float distance = Vector3.Distance(transform.position, _player.transform.position);
        if (distance > _attackRange)
        {
            Debug.Log($"{_state} -> Trace");
            _state = ZombieState.Trace;
        }

        _attackTimer += Time.deltaTime;
        if (_attackTimer >= _attackInterval)
        {
            // Todo. 공격 애니메이션 실행

            DamageInfo info = new DamageInfo()
            {
                Value = _attackDamage,
                HitPoint = _player.transform.position,
                HitNormal = (transform.position - _player.transform.position).normalized,
            };

            _player.GetComponent<IDamageable>().TakeDamage(info);

            _attackTimer = 0f;
        }
    }
}
