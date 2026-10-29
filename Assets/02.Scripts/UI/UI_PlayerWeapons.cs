using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 현재 무기 모드에 맞는 아이콘과 남은 탄약/폭탄 개수를 표시한다.
public class UI_PlayerWeapons : MonoBehaviour
{
    [SerializeField] private Player _player;
    [SerializeField] private PlayerGunFire _gunFire;
    [SerializeField] private PlayerBombFire _bombFire;

    [Header("무기 아이콘")]
    [SerializeField] private Image _weaponIcon;
    [SerializeField] private Sprite _normalSprite;
    [SerializeField] private Sprite _sniperSprite;

    [Header("탄약 / 폭탄")]
    [SerializeField] private TextMeshProUGUI _ammoText;
    [SerializeField] private GameObject _bombRoot; // 일반 모드에서만 표시
    [SerializeField] private TextMeshProUGUI _bombText;

    private void OnEnable()
    {
        _player.OnWeaponChange += RefreshWeapon;
        _gunFire.OnBulletFire += RefreshAmmo;
        _bombFire.OnBombThrow += RefreshBomb;
    }

    private void OnDisable()
    {
        _player.OnWeaponChange -= RefreshWeapon;
        _gunFire.OnBulletFire -= RefreshAmmo;
        _bombFire.OnBombThrow -= RefreshBomb;
    }

    private void Start()
    {
        // 이벤트는 이후 변화만 알려주므로 초기값은 직접 읽는다.
        RefreshWeapon(_player.WeaponMode == WeaponMode.Sniper);
        RefreshAmmo(_gunFire.BulletCount, _gunFire.MaxBulletCount);
        RefreshBomb(_bombFire.BombCount);
    }

    private void RefreshWeapon(bool isSniper)
    {
        _weaponIcon.sprite = isSniper ? _sniperSprite : _normalSprite;
        _bombRoot.SetActive(!isSniper);
    }

    private void RefreshAmmo(int current, int max)
    {
        _ammoText.text = $"{current} / {max}";
    }

    private void RefreshBomb(int current)
    {
        _bombText.text = $"{current} / {_bombFire.MaxBombCount}";
    }
}
