using Lean.Pool;
using UnityEngine;

// 충돌하면 폭발 이펙트를 남기고 LeanPool에 반환되는 폭탄입니다.
[RequireComponent(typeof(Rigidbody), typeof(LeanPooledRigidbody))]
public class Bomb : MonoBehaviour, IPoolable
{
    [SerializeField] private ParticleSystem _explosionEffectPrefab;

    private Rigidbody _rigidbody;
    public bool IsInUse { get; private set; }

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    // 풀의 Notification = IPoolable 설정으로 호출됩니다.
    public void OnSpawn()
    {
        if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();
        IsInUse = true;
        _rigidbody.WakeUp();
    }

    public void OnDespawn()
    {
        IsInUse = false;
        // 이동·회전 속도 초기화는 LeanPooledRigidbody가 처리합니다.
    }

    public void Launch(Vector3 impulse)
    {
        if (IsInUse) _rigidbody.AddForce(impulse, ForceMode.Impulse);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsInUse) return;
        IsInUse = false; // 같은 폭탄의 중복 충돌 처리를 방지합니다.
        Vector3 explosionPosition = transform.position;
        LeanPool.Despawn(gameObject);

        if (_explosionEffectPrefab != null)
        {
            var effect = Instantiate(_explosionEffectPrefab, explosionPosition, Quaternion.identity);
            effect.Play();
        }
    }
}
