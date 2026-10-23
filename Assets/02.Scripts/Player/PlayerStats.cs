using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour
{
    [SerializeField] private Slider[] _slider;
    private PlayerMove _playerMove;
    private Player _player;

    private void Awake()
    {
        _playerMove = GetComponent<PlayerMove>();
        _player = GetComponent<Player>();
    }

    private void LateUpdate()
    {
        _slider[0].value = _player.Health / _player.MaxHealth;
        _slider[1].value = _playerMove.Stamina / PlayerMove.MaxStamina;
    }
}