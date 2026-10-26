using System;
using UnityEngine;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    private static EnemyManager _instance;
    public static EnemyManager Instance => _instance;

    [Header("스폰")]
    [SerializeField] private ZombiePool _zombiePool;
    [SerializeField, Range(1, ZombiePool.MaxPoolSize)] private int _maxZombieCount = 40; // 이번 판에 등장하는 총 좀비 수
    [SerializeField, Min(0.1f)] private float _spawnInterval = 2f;
    [SerializeField] private Transform[] _spawnPoints;

    [Header("기즈모")]
    [SerializeField] private float _gizmoRadius = 0.5f;

    private readonly List<Zombie> _zombieList = new List<Zombie>();
    public List<Zombie> ZombieList => _zombieList;

    private int _spawnedCount;
    private int _killedCount;
    private float _spawnTimer;

    public int MaxZombieCount => Mathf.Min(_maxZombieCount, _zombiePool.PoolSize);
    public int RemainingCount => MaxZombieCount - _killedCount;

    // 좀비가 죽을 때만 발행된다. (남은 수, 최대 수)
    public event Action<int, int> OnZombieDied;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            return;
        }

        Destroy(gameObject);
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;
        if (_spawnedCount >= MaxZombieCount) return;

        _spawnTimer += Time.deltaTime;
        if (_spawnTimer < _spawnInterval) return;

        _spawnTimer = 0f;
        SpawnZombie();
    }

    private void SpawnZombie()
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0) return;

        Transform point = _spawnPoints[UnityEngine.Random.Range(0, _spawnPoints.Length)];
        Zombie zombie = _zombiePool.GetZombie(point.position, point.rotation);
        if (zombie == null) return;

        _spawnedCount++;
        _zombieList.Add(zombie);
        zombie.Died += HandleZombieDied;
    }

    private void HandleZombieDied(Zombie zombie)
    {
        zombie.Died -= HandleZombieDied;
        _zombieList.Remove(zombie);
        _killedCount++;
        OnZombieDied?.Invoke(RemainingCount, MaxZombieCount);
    }

    private void OnDrawGizmos()
    {
        if (_spawnPoints == null) return;

        Gizmos.color = Color.red;
        foreach (Transform point in _spawnPoints)
        {
            if (point == null) continue;
            Gizmos.DrawWireSphere(point.position, _gizmoRadius);
            Gizmos.DrawLine(point.position, point.position + point.forward * _gizmoRadius * 2f);
        }
    }
}
