using UnityEngine;

// 무언가에 부딛히면 폭발 이펙트를 남기고 사라지는 폭탄
[RequireComponent(typeof(Rigidbody))]
public class Bomb : MonoBehaviour
{
    [SerializeField] private ParticleSystem _explosionEffectPrefab;
    [SerializeField] private float _explosionRange = 4.5f;
    [SerializeField] private float _damage = 100f;


    private Rigidbody _rigidbody;
    public BombPool Owner { get; private set; }
    public bool IsInUse { get; private set; }

    internal void Initialize(BombPool owner)
    {
        Owner = owner;
        _rigidbody = GetComponent<Rigidbody>();
    }

    internal void Activate(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
        _rigidbody.position = position;
        _rigidbody.rotation = rotation;
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        IsInUse = true;
        gameObject.SetActive(true);
        _rigidbody.WakeUp();
    }

    public void Launch(Vector3 impulse)
    {
        if (IsInUse) _rigidbody.AddForce(impulse, ForceMode.Impulse);
    }

    internal void Deactivate()
    {
        IsInUse = false;
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.Sleep();
        gameObject.SetActive(false);
    }

    // 다른 콜라이더와 부딛힌 순간 실행되는 메서드
    private void OnCollisionEnter(Collision collision)
    {
        if (!IsInUse) return;
        Vector3 explosionPosition = transform.position;
        Owner.ReturnBomb(this);

        if (_explosionEffectPrefab != null)
        {
            var effect = Instantiate(_explosionEffectPrefab, explosionPosition, Quaternion.identity);
            effect.Play();
            ApplyAreaDamage();
        }
    }

    private void ApplyAreaDamage()
    {
        // 0. 주위에 있는 드럼통들을 모두 찾는다.
        // Physics.OverlapSpere(원점, 반지름, 찾을 레이어들)
        Collider[] colliders =
            Physics.OverlapSphere(transform.position, _explosionRange, LayerMask.GetMask("Drum", "Zombie"));
        DamageInfo damageInfo = new DamageInfo();
        damageInfo.Value = _damage;
        damageInfo.HitPoint = transform.position;
        damageInfo.HitNormal = transform.forward;

        // 1. 드럼통과 좀비들에게 대미지를 준다.
        foreach (Collider col in colliders)
        {
            col.GetComponent<IDamageable>()?.TakeDamage(damageInfo);
        }
    }
}
