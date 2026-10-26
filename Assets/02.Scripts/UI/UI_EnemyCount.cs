using TMPro;
using UnityEngine;

public class UI_EnemyCount : MonoBehaviour
{
    private TextMeshProUGUI _textMesh;

    private void Awake()
    {
        _textMesh = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        EnemyManager.Instance.OnZombieDied += Refresh;
    }

    private void OnDisable()
    {
        if (EnemyManager.Instance == null) return;
        EnemyManager.Instance.OnZombieDied -= Refresh;
    }

    private void Start()
    {
        // 최초 표시는 한 번만 직접 갱신하고, 이후에는 좀비가 죽을 때만 갱신한다.
        Refresh(EnemyManager.Instance.RemainingCount, EnemyManager.Instance.MaxZombieCount);
    }

    private void Refresh(int remaining, int max)
    {
        _textMesh.text = $"남은 적: {remaining}/{max}마리";
    }
}
