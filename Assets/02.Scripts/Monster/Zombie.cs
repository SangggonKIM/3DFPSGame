using System;
using UnityEngine;
using System.Collections;
using UnityEngine.AI;

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

[RequireComponent(typeof(NavMeshAgent))]
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
    [SerializeField] private NavMeshAgent _agent;
    private float _health; // 체력

    [Header("이펙트")]

    [SerializeField] private GameObject _bloodSprayPrefab;

    public event Action OnHit;
    private Vector3 _startPosition;
    private GameObject _player;
    private float _attackTimer = 0f;
    private const float PatrolDistance = 5f;
    private float _patrolTimer = 0f;
    private Animator _animator;
    private readonly int _idleToMove = Animator.StringToHash("IdleToMove");
    private readonly int _moveToIdle = Animator.StringToHash("MoveToIdle");
    private readonly int _moveToAttackDelay = Animator.StringToHash("MoveToAttackDelay");
    private readonly int _startAttack = Animator.StringToHash("StartAttack");
    private readonly int _attackToMove = Animator.StringToHash("AttackToMove");
    private readonly int _death = Animator.StringToHash("Death");
    private readonly int _hit = Animator.StringToHash("Hit");

    public float Health => _health;
    public float MaxHealth => _maxHealth;


    private ZombieState _state = ZombieState.Patrol;

    public event Action<Zombie> Died;
    public ZombiePool Owner { get; private set; }
    public bool IsInUse { get; private set; }

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
    }

    internal void Initialize(ZombiePool owner)
    {
        Owner = owner;
    }

    // 풀에서 꺼낼 때마다 상태를 초기화한다.
    internal void Activate(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
        _health = _maxHealth;
        _startPosition = position;
        _attackTimer = 0f;
        _patrolTimer = 0f;
        _state = ZombieState.Patrol;

        if (_player == null)
        {
            _player = GameObject.FindGameObjectWithTag("Player");
        }

        IsInUse = true;
        gameObject.SetActive(true);
    }

    internal void Deactivate()
    {
        IsInUse = false;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;
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
            OnHit?.Invoke();
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
            Died?.Invoke(this);
            _animator.SetTrigger(_death);
            StartCoroutine(Death_Coroutine());
        }
    }

    private IEnumerator Hit_Coroutine()
    {
        StopAgent();
        _animator.SetTrigger(_hit);
        yield return new WaitForSeconds(0.5f); // 잠깐 멈칫한다!

        // 맞았으니 쫓아온다.
        Debug.Log($"{_state} -> Trace");
        _state = ZombieState.Trace;
    }


    private IEnumerator Death_Coroutine()
    {
        StopAgent();
        yield return new WaitForSeconds(0.5f);
        Owner.ReturnZombie(this);
    }

    private void Idle()
    {
        StopAgent();

        // 플레이어 가까워지면 Trace 상태로 전이한다.
        float distance = Vector3.Distance(transform.position, _player.transform.position);
        if (distance <= _detectDistance)
        {
            Debug.Log($"{_state} -> Trace");
            _animator.SetTrigger(_idleToMove);
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
    }

    // 추격: 플레이어를 쫓아간다.
    private void Trace()
    {
        _animator.SetTrigger(_idleToMove);
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
            _animator.SetTrigger(_moveToAttackDelay);
            _state = ZombieState.Attack;
            return;
        }

        // SetDestination은 목적지 설정
        // - 호출할 때마다 목적지를 향해 조금씩 이동 (X)
        // - 한 번 세팅하면 내부에서 계속해서 이동 (O)
        _agent.SetDestination(_player.transform.position); // 한줄로 알아서 회전하고 플레이어 바라보며 따라간다.
    }

    private void Comeback()
    {
        if (Vector3.Distance(transform.position, _startPosition) < _agent.stoppingDistance)
        {
            _patrolTimer = 0f;
            Debug.Log($"{_state} -> Patrol");
            _state = ZombieState.Patrol;
            return;
        }

        _agent.SetDestination(_startPosition);
    }

    // 공격: 일정 시간마다 플레이어를 때린다.
    private void Attack()
    {
        StopAgent();
        // 플레이어가 공격 사정 거리 밖으로 나가면 쫓아!
        float distance = Vector3.Distance(transform.position, _player.transform.position);
        if (distance > _attackRange)
        {
            Debug.Log($"{_state} -> Trace");
            _animator.SetTrigger(_attackToMove);
            _state = ZombieState.Trace;
            return;
        }

        _attackTimer += Time.deltaTime;
        if (_attackTimer >= _attackInterval)
        {
            _animator.SetTrigger(_startAttack);

            _attackTimer = 0f;
        }
    }

    public void AttackAction()
    {
        DamageInfo info = new DamageInfo()
        {
            Value = _attackDamage,
            HitPoint = _player.transform.position,
            HitNormal = (transform.position - _player.transform.position).normalized,
        };

        _player.GetComponent<IDamageable>().TakeDamage(info);
    }

    private void StopAgent()
    {
        _agent.ResetPath(); // 에이전트의 목적지 삭제
        _agent.velocity = Vector3.zero; // 미끄럼 방지를 위해 가속도 제로!
    }
}
