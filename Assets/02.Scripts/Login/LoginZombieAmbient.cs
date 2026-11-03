using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// 로그인 화면 배경 좀비 전용 배회 AI. (게임용 Zombie와 분리)
// MoveArea Collider 영역 안의 랜덤 지점을 멈추지 않고 계속 이어서 걷는다.
[RequireComponent(typeof(NavMeshAgent))]
public class LoginZombieAmbient : MonoBehaviour
{
    private const int MaxSampleAttempts = 10;
    private const float InsideTolerance = 0.01f;
    private const float NavMeshSampleMargin = 2f;
    private const float ArriveDistance = 0.5f; // 이 거리 안에 들어오면 멈추기 전에 다음 목적지를 정한다.

    [SerializeField] private Animator _animator;
    [SerializeField] private string _walkingParameter = "IsWalking";

    [Header("배회")]
    [Tooltip("좀비가 돌아다닐 영역. Box/Sphere/Capsule/Convex Mesh Collider를 지원한다.")]
    [SerializeField] private Collider _moveArea;
    [SerializeField] private Vector2 _walkSpeedRange = new Vector2(0.5f, 0.8f);
    [Tooltip("경로가 막혀 도착하지 못할 때 다음 목적지로 넘어가는 시간")]
    [SerializeField, Min(0.5f)] private float _maxWalkTime = 8f;

    private NavMeshAgent _agent;
    private int _walkingHash;
    private Coroutine _wanderRoutine;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.autoBraking = false; // 목적지 근처에서 감속하지 않고 다음 목적지로 이어서 걷는다.
        if (_animator == null) _animator = GetComponentInChildren<Animator>();
        _walkingHash = Animator.StringToHash(_walkingParameter);
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

        if (_agent.isActiveAndEnabled && _agent.isOnNavMesh) _agent.ResetPath();
        SetWalking(false);
    }

    private IEnumerator WanderRoutine()
    {
        // Agent가 NavMesh에 올라갈 때까지 한 프레임 기다린다.
        yield return null;
        if (_moveArea == null)
        {
            Debug.LogWarning($"{name}: MoveArea가 지정되지 않아 배회하지 않습니다.", this);
            yield break;
        }
        if (!_agent.isOnNavMesh)
        {
            Debug.LogWarning($"{name}: NavMesh 위에 있지 않아 배회하지 않습니다.", this);
            yield break;
        }

        SetWalking(true);
        while (true)
        {
            if (!TryGetRandomPoint(out Vector3 destination))
            {
                yield return null; // 이번 프레임에 못 찾으면 다음 프레임에 다시 시도한다.
                continue;
            }

            _agent.speed = Random.Range(_walkSpeedRange.x, _walkSpeedRange.y);
            _agent.SetDestination(destination);

            float walkTime = 0f;
            while (walkTime < _maxWalkTime)
            {
                if (!_agent.pathPending && _agent.remainingDistance <= ArriveDistance) break;

                walkTime += Time.deltaTime;
                yield return null;
            }
        }
    }

    // MoveArea 안에 있으면서 NavMesh 위인 랜덤 지점을 찾는다.
    private bool TryGetRandomPoint(out Vector3 point)
    {
        Bounds bounds = _moveArea.bounds;
        for (int i = 0; i < MaxSampleAttempts; i++)
        {
            Vector3 candidate = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                bounds.center.y,
                Random.Range(bounds.min.z, bounds.max.z));
            if (!IsInsideArea(candidate)) continue;

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, bounds.extents.y + NavMeshSampleMargin, NavMesh.AllAreas)) continue;

            // NavMesh로 보정된 위치가 영역 밖으로 밀려났으면 버린다. (높이는 영역 중심으로 맞춰 검사)
            Vector3 flattened = new Vector3(hit.position.x, bounds.center.y, hit.position.z);
            if (!IsInsideArea(flattened)) continue;

            point = hit.position;
            return true;
        }

        point = Vector3.zero;
        return false;
    }

    private bool IsInsideArea(Vector3 position)
    {
        return (_moveArea.ClosestPoint(position) - position).sqrMagnitude < InsideTolerance * InsideTolerance;
    }

    private void SetWalking(bool isWalking)
    {
        if (_animator == null || !_animator.isActiveAndEnabled) return;

        _animator.SetBool(_walkingHash, isWalking);
    }

    private void OnDrawGizmosSelected()
    {
        if (_moveArea == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(_moveArea.bounds.center, _moveArea.bounds.size);
    }
}
