using System;
using UnityEngine;

public class KalTriggerEvent : MonoBehaviour
{
    private PlayerMeleeAttack _playerMeleeAttack;

    private void Start()
    {
        _playerMeleeAttack = GetComponentInParent<PlayerMeleeAttack>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_playerMeleeAttack != null)
        {
            _playerMeleeAttack.OnTriggerEntered(other);
        }
    }
}