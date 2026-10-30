using System;
using UnityEngine;

public class PlayerMeleeAttack : MonoBehaviour
{
    [SerializeField] private Collider _kalCollider;
    [SerializeField] private float _damage = 10f;
    private Player _player;
    private Animator _animator;
    private int _meleeLayerIndex;

    private void Awake()
    {
        _player = GetComponentInParent<Player>();
        _animator = GetComponentInChildren<Animator>();
        _meleeLayerIndex = _animator.GetLayerIndex("Melee Layer");
    }

    // 람다식을 활용한 함수
    public void ActivateCollider() => _kalCollider.enabled = true;
    public void DeactivateCollider() => _kalCollider.enabled = false;

    public void OnTriggerEntered(Collider other)
    {
        Debug.Log("부닥");
        if (other.TryGetComponent<IDamageable>(out IDamageable damageable))
        {
            DamageInfo damageInfo = new DamageInfo();
            damageInfo.Value = _damage;
            Vector3 hitPoint = other.ClosestPoint(_player.transform.position);
            damageInfo.HitPoint = hitPoint;
            damageInfo.HitNormal = (_player.transform.position - hitPoint).normalized;
            damageable.TakeDamage(damageInfo);
        }
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;
        if (_player.WeaponMode != WeaponMode.Melee) return;

        if (Input.GetButtonDown("Fire2"))
        {
            _animator.SetTrigger("Slash");
        }
    }
}