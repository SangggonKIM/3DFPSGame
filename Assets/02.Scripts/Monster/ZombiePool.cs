using System.Collections.Generic;
using UnityEngine;

public class ZombiePool : MonoBehaviour
{
    public const int MaxPoolSize = 40;

    [SerializeField] private Zombie _zombiePrefab;
    [SerializeField, Range(1, MaxPoolSize)] private int _poolSize = MaxPoolSize;

    private readonly Queue<Zombie> _availableZombies = new Queue<Zombie>();
    private bool _initialized;

    public int PoolSize => _poolSize;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (_initialized) return;
        if (_zombiePrefab == null)
        {
            Debug.LogError("ZombiePool에 Zombie Prefab을 연결해주세요.", this);
            return;
        }

        _initialized = true;
        for (int i = 0; i < _poolSize; i++)
        {
            Zombie zombie = Instantiate(_zombiePrefab, transform);
            zombie.Initialize(this);
            zombie.gameObject.SetActive(false);
            _availableZombies.Enqueue(zombie);
        }
    }

    public Zombie GetZombie(Vector3 position, Quaternion rotation)
    {
        Initialize();
        if (!_initialized || !isActiveAndEnabled) return null;

        while (_availableZombies.Count > 0)
        {
            Zombie zombie = _availableZombies.Dequeue();
            if (zombie == null) continue;
            zombie.Activate(position, rotation);
            return zombie;
        }

        // 풀이 비었으면 추가 생성하지 않고 이번 스폰을 건너뛴다.
        return null;
    }

    public void ReturnZombie(Zombie zombie)
    {
        // 다른 풀의 좀비나 이미 반환된 좀비가 중복 등록되지 않게 한다.
        if (zombie == null || zombie.Owner != this || !zombie.IsInUse) return;
        zombie.Deactivate();
        _availableZombies.Enqueue(zombie);
    }
}
