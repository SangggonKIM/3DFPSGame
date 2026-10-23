using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour
{
    [SerializeField] private Slider[] _slider;
    private PlayerMove _playerMove;
    private PlayerHealth _playerHealth;

    private void Awake()
    {
        _playerMove = GetComponent<PlayerMove>();
        _playerHealth = GetComponent<PlayerHealth>();
    }

    private void LateUpdate()
    {
        _slider[0].value = _playerHealth.Health / _playerHealth.MaxHealth;
        _slider[1].value = _playerMove.Stamina / PlayerMove.MaxStamina;
    }
}