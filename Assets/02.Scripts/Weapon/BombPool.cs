using Lean.Pool;
using UnityEngine;

[RequireComponent(typeof(LeanGameObjectPool))]
public class BombPool : MonoBehaviour
{
    private LeanGameObjectPool _pool;

    private void Awake()
    {
        _pool = GetComponent<LeanGameObjectPool>();
    }

    public Bomb GetBomb()
    {
        return GetBomb(transform.position, Quaternion.identity);
    }

    public Bomb GetBomb(Vector3 position, Quaternion rotation)
    {
        if (_pool == null) _pool = GetComponent<LeanGameObjectPool>();
        if (!isActiveAndEnabled || !_pool.isActiveAndEnabled) return null;
        if (_pool.Prefab == null || !_pool.Prefab.TryGetComponent<Bomb>(out var prefab))
        {
            Debug.LogError("Lean Game Object Pool의 Prefab에 Bomb 프리팹을 연결해주세요.", this);
            return null;
        }

        // Preload, Capacity 등 수량 설정은 LeanGameObjectPool에서 관리합니다.
        // Capacity에 도달하고 Recycle이 꺼져 있으면 null을 반환합니다.
        return LeanPool.Spawn(prefab, position, rotation);
    }

    public void ReturnBomb(Bomb bomb)
    {
        if (bomb == null || !bomb.IsInUse) return;
        LeanPool.Despawn(bomb.gameObject);
    }
}
