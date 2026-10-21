using System.Collections.Generic;
using UnityEngine;

public class BombPool : MonoBehaviour
{
    [SerializeField] private Bomb _bombPrefab;
    [SerializeField, Min(1)] private int _poolSize = 10;

    private readonly Queue<Bomb> _availableBombs = new Queue<Bomb>();
    private bool _initialized;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (_initialized) return;
        if (_bombPrefab == null)
        {
            Debug.LogError("BombPool에 Bomb Prefab을 연결해주세요.", this);
            return;
        }

        _initialized = true;
        for (int i = 0; i < Mathf.Max(1, _poolSize); i++)
        {
            Bomb bomb = Instantiate(_bombPrefab, transform);
            bomb.Initialize(this);
            bomb.gameObject.SetActive(false);
            _availableBombs.Enqueue(bomb);
        }
    }

    public Bomb GetBomb()
    {
        return GetBomb(transform.position, Quaternion.identity);
    }

    public Bomb GetBomb(Vector3 position, Quaternion rotation)
    {
        // 다른 컴포넌트의 Awake가 먼저 실행되어도 초기화를 보장합니다.
        Initialize();
        if (!_initialized || !isActiveAndEnabled) return null;

        while (_availableBombs.Count > 0)
        {
            Bomb bomb = _availableBombs.Dequeue();
            if (bomb == null) continue;
            bomb.Activate(position, rotation);
            return bomb;
        }

        // 풀이 비었으면 추가 생성하지 않고 이번 발사를 취소합니다.
        return null;
    }

    public void ReturnBomb(Bomb bomb)
    {
        // 다른 풀의 폭탄이나 이미 반환된 폭탄이 중복 등록되지 않게 합니다.
        if (bomb == null || bomb.Owner != this || !bomb.IsInUse) return;
        bomb.Deactivate();
        _availableBombs.Enqueue(bomb);
    }
}
