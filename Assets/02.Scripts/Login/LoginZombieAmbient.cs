using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// 로그인 화면 배경 좀비 전용 배회 AI. (게임용 Zombie와 분리)
// 대기 → 시작 위치 반경 안 랜덤 지점으로 걷기 → 도착 후 대기를 반복한다.
[RequireComponent(typeof(NavMeshAgent))]
public class LoginZombieAmbient : MonoBehaviour
{
    private const int MaxSampleAttempts = 5;
    private const float SampleMaxDistance = 2f;

    [SerializeField] private Animator _animator;
    [SerializeField] private string _walkingParameter = "IsWalking";

    [Header("배회")]
    [SerializeField, Min(0.5f)] private float _wanderRadius = 4f;
    [SerializeField] private Vector2 _idleTimeRange = new Vector2(1.5f, 4f);
    [SerializeField] private Vector2 _walkSpeedRange = new Vector2(0.5f, 0.8f);
    [Tooltip("경로가 막혀 도착하지 못할 때 걷기를 포기하는 시간")]
    [SerializeField, Min(0.5f)] private float _maxWalkTime = 8f;

    private NavMeshAgent _agent;
    private Vector3 _homePosition;
    private int _walkingHash;
    private Coroutine _wanderRoutine;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (_animator == null) _animator = GetComponentInChildren<Animator>();
        _walkingHash = Animator.StringToHash(_walkingParameter);
        _homePosition = transform.position;
    }

    private void OnEnable()
    {
        _wanderRoutine = StartCoroutine(WanderRoutine());
    }

    private void OnDisable()
    {
        if (_wanderRoutine != null)
        {
            StopCoroutine(_wanderRoutine);
            _wanderRoutine = null;
        }

        StopWalking();
    }

    private IEnumerator WanderRoutine()
    {
        // Agent가 NavMesh에 올라갈 때까지 한 프레임 기다린다.
        yield return null;
        if (!_agent.isOnNavMesh)
        {
            Debug.LogWarning($"{name}: NavMesh 위에 있지 않아 배회하지 않습니다.", this);
            yield break;
        }

        while (true)
        {
            StopWalking();
            yield return new WaitForSeconds(Random.Range(_idleTimeRange.x, _idleTimeRange.y));

            if (!TryGetRandomPoint(out Vector3 destination)) continue;

            _agent.speed = Random.Range(_walkSpeedRange.x, _walkSpeedRange.y);
            _agent.SetDestination(destination);
            SetWalking(true);

            float walkTime = 0f;
            while (walkTime < _maxWalkTime)
            {
                if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance) break;

                walkTime += Time.deltaTime;
                yield return null;
            }
        }
    }

    private bool TryGetRandomPoint(out Vector3 point)
    {
        for (int i = 0; i < MaxSampleAttempts; i++)
        {
            Vector2 circle = Random.insideUnitCircle * _wanderRadius;
            Vector3 candidate = _homePosition + new Vector3(circle.x, 0f, circle.y);
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, SampleMaxDistance, NavMesh.AllAreas))
            {
                point = hit.position;
                return true;
            }
        }

        point = _homePosition;
        return false;
    }

    private void StopWalking()
    {
        if (_agent.isActiveAndEnabled && _agent.isOnNavMesh) _agent.ResetPath();
        SetWalking(false);
    }

    private void SetWalking(bool isWalking)
    {
        if (_animator == null || !_animator.isActiveAndEnabled) return;

        _animator.SetBool(_walkingHash, isWalking);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = Application.isPlaying ? _homePosition : transform.position;
        Gizmos.DrawWireSphere(center, _wanderRadius);
    }
}
