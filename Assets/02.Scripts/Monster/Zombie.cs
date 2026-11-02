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
    Patrol, // 순찰: 처음 자리에서 좌우 5유닛을 왕복한다.
    Jump // 점프: NavMeshLink 구간을 포물선으로 건넌다.
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
    [SerializeField] private float _jumpHeight = 2f; // 포물선 최고점 높이
    [SerializeField] private float _jumpSpeed = 6f; // 점프 중 수평 이동 속도
    [SerializeField] private float _jumpWindupTime = 0.5f; // 도약 전 웅크리는 시간 (애니메이션의 발이 떨어지는 시점에 맞춘다)
    [SerializeField] private float _repathInterval = 0.2f; // 경로 재계산 주기 (매 프레임 SetDestination 방지)
    [SerializeField] private NavMeshAgent _agent;
    private float _health; // 체력

    [Header("이펙트")]

    [SerializeField] private GameObject _bloodSprayPrefab;

    public event Action OnHit;
    private Vector3 _startPosition;
    private Transform _player;
    private IDamageable _playerDamageable;
    private float _attackTimer = 0f;
    private const float PatrolDistance = 5f;
    private const float HitStunTime = 0.5f;
    private const float DeathDelay = 0.5f;
    private float _patrolTimer = 0f;
    private float _repathTimer = 0f;

    // 코루틴 대기 객체는 매번 new 하지 않고 재사용한다.
    private WaitForSeconds _hitStunWait;
    private WaitForSeconds _deathWait;
    private WaitForSeconds _jumpWindupWait;

    // 피 이펙트는 좀비마다 하나만 만들어 재생만 반복한다.
    private Transform _bloodSpray;
    private ParticleSystem[] _bloodSprayParticles;
    private Animator _animator;
    private readonly int _idleToMove = Animator.StringToHash("IdleToMove");
    private readonly int _moveToIdle = Animator.StringToHash("MoveToIdle");
    private readonly int _moveToAttackDelay = Animator.StringToHash("MoveToAttackDelay");
    private readonly int _startAttack = Animator.StringToHash("StartAttack");
    private readonly int _attackToMove = Animator.StringToHash("AttackToMove");
    private readonly int _death = Animator.StringToHash("Death");
    private readonly int _hit = Animator.StringToHash("Hit");
    private readonly int _jump = Animator.StringToHash("Jump");

    public float Health => _health;
    public float MaxHealth => _maxHealth;


    private ZombieState _state = ZombieState.Patrol;

    public event Action<Zombie> Died;
    public ZombiePool Owner { get; private set; }
    public bool IsInUse { get; private set; }

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        // 링크 구간은 에이전트가 직선으로 건너지 않고 코드에서 직접 점프시킨다.
        _agent.autoTraverseOffMeshLink = false;

        _hitStunWait = new WaitForSeconds(HitStunTime);
        _deathWait = new WaitForSeconds(DeathDelay);
        _jumpWindupWait = new WaitForSeconds(_jumpWindupTime);

        CreateBloodSpray();
    }

    // 원본 프리팹은 무한 반복(loop) 파티클이라, 맞을 때마다 생성하면 파괴되지 않고 계속 쌓인다.
    // 한 번만 만들고 반복을 끈 뒤, 피격 시 처음부터 다시 재생한다.
    private void CreateBloodSpray()
    {
        _bloodSpray = Instantiate(_bloodSprayPrefab, transform).transform;
        _bloodSprayParticles = _bloodSpray.GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem particle in _bloodSprayParticles)
        {
            ParticleSystem.MainModule main = particle.main;
            main.loop = false;
            main.playOnAwake = false;
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void PlayBloodSpray()
    {
        _bloodSpray.SetPositionAndRotation(transform.position, transform.rotation);
        foreach (ParticleSystem particle in _bloodSprayParticles)
        {
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Play(true);
        }
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
        _repathTimer = 0f;
        _state = ZombieState.Patrol;

        if (_player == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            _player = player.transform;
            _playerDamageable = player.GetComponent<IDamageable>();
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
        OnHit?.Invoke();

        // 점프 중에는 멈칫하지 않는다. 코루틴이 끊기면 링크 위에 갇히기 때문이다.
        bool isJumping = _state == ZombieState.Jump;
        if (!isJumping || _health <= 0f)
        {
            StopAllCoroutines();
        }

        if (_health > 0f)
        {
            // Todo. 맞은 부위에 피 이펙트를 실행하고 싶다.
            PlayBloodSpray();

            if (isJumping) return;

            // 잠깐 멈칫 하고 싶다!
            ChangeState(ZombieState.Hit);
            StartCoroutine(Hit_Coroutine());
        }
        else
        {
            ChangeState(ZombieState.Death);
            Died?.Invoke(this);
            _animator.SetTrigger(_death);
            StartCoroutine(Death_Coroutine());
        }
    }

    private IEnumerator Hit_Coroutine()
    {
        StopAgent();
        _animator.SetTrigger(_hit);
        yield return _hitStunWait; // 잠깐 멈칫한다!

        // 맞았으니 쫓아온다.
        ChangeState(ZombieState.Trace);
    }


    private IEnumerator Death_Coroutine()
    {
        StopAgent();
        yield return _deathWait;
        Owner.ReturnZombie(this);
    }

    private void Idle()
    {
        StopAgent();

        // 플레이어 가까워지면 Trace 상태로 전이한다.
        if (IsWithin(_player.position, _detectDistance))
        {
            _animator.SetTrigger(_idleToMove);
            ChangeState(ZombieState.Trace);
        }
    }

    private void Patrol()
    {
        // 순찰 중 플레이어를 발견하면 현재 위치에서 바로 추격한다.
        if (_player != null && IsWithin(_player.position, _detectDistance))
        {
            ChangeState(ZombieState.Trace);
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
        if (TryStartJump()) return;

        _animator.SetTrigger(_idleToMove);
        // 처음 자리에서 너무 멀어지면 돌아간다.
        if (!IsWithin(_startPosition, _comebackDistance))
        {
            ChangeState(ZombieState.Comeback);
            return;
        }

        // 플레이어가 공격 거리 안이면 공격한다.
        if (IsWithin(_player.position, _attackRange))
        {
            _attackTimer = 0f;
            _animator.SetTrigger(_moveToAttackDelay);
            ChangeState(ZombieState.Attack);
            return;
        }

        // SetDestination은 목적지 설정
        // - 호출할 때마다 목적지를 향해 조금씩 이동 (X)
        // - 한 번 세팅하면 내부에서 계속해서 이동 (O)
        MoveTo(_player.position); // 한줄로 알아서 회전하고 플레이어 바라보며 따라간다.
    }

    private void Comeback()
    {
        if (TryStartJump()) return;

        if (IsWithin(_startPosition, _agent.stoppingDistance))
        {
            _patrolTimer = 0f;
            ChangeState(ZombieState.Patrol);
            return;
        }

        MoveTo(_startPosition);
    }

    // 경로 계산은 비싸므로 _repathInterval마다 한 번만 목적지를 갱신한다.
    private void MoveTo(Vector3 destination)
    {
        _repathTimer -= Time.deltaTime;
        if (_repathTimer > 0f && _agent.hasPath) return;

        _repathTimer = _repathInterval;
        _agent.SetDestination(destination);
    }

    // Vector3.Distance의 제곱근 계산 없이 거리를 비교한다.
    private bool IsWithin(Vector3 target, float range)
    {
        return (target - transform.position).sqrMagnitude <= range * range;
    }

    private void ChangeState(ZombieState next)
    {
        GameLogger.Log($"{_state} -> {next}");
        _state = next;
    }

    // 에이전트가 링크 시작점에 도착했으면 점프를 시작한다.
    private bool TryStartJump()
    {
        if (!_agent.isOnOffMeshLink) return false;

        ZombieState returnState = _state;
        _animator.SetTrigger(_jump);
        ChangeState(ZombieState.Jump);
        StartCoroutine(Jump_Coroutine(_agent.currentOffMeshLinkData, returnState));
        return true;
    }

    // 링크 시작점에서 끝점까지 포물선을 그리며 이동한다.
    private IEnumerator Jump_Coroutine(OffMeshLinkData link, ZombieState returnState)
    {
        Vector3 start = transform.position;
        Vector3 end = link.endPos + Vector3.up * _agent.baseOffset;

        Vector3 lookDirection = end - start;
        lookDirection.y = 0f;
        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            transform.forward = lookDirection.normalized;
        }

        // 애니메이션의 준비 동작이 끝날 때까지 제자리에서 기다린다.
        yield return _jumpWindupWait;

        float duration = Vector3.Distance(start, end) / _jumpSpeed;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float clampedT = Mathf.Clamp01(t);

            // 직선 보간 + 4t(1-t) 곡선: t=0.5일 때 _jumpHeight만큼 가장 높이 뜬다.
            Vector3 position = Vector3.Lerp(start, end, clampedT);
            position.y += _jumpHeight * 4f * clampedT * (1f - clampedT);
            transform.position = position;
            yield return null;
        }

        _agent.CompleteOffMeshLink(); // 링크 통과 완료 → 에이전트가 다시 경로를 따라 이동한다.

        ChangeState(returnState);
    }

    // 공격: 일정 시간마다 플레이어를 때린다.
    private void Attack()
    {
        StopAgent();
        // 플레이어가 공격 사정 거리 밖으로 나가면 쫓아!
        if (!IsWithin(_player.position, _attackRange))
        {
            _animator.SetTrigger(_attackToMove);
            ChangeState(ZombieState.Trace);
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
            HitPoint = _player.position,
            HitNormal = (transform.position - _player.position).normalized,
        };

        _playerDamageable.TakeDamage(info);
    }

    private void StopAgent()
    {
        _agent.ResetPath(); // 에이전트의 목적지 삭제
        _agent.velocity = Vector3.zero; // 미끄럼 방지를 위해 가속도 제로!
    }
}
