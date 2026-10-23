using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float _maxHealth = 100f; // 최대 체력
    private float _health; // 체력

    public float Health => _health;
    public float MaxHealth => _maxHealth;

    public void Awake()
    {
        _health = _maxHealth;
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        _health -= damageInfo.Value;
    }
}