using TMPro;
using UnityEngine;

public class UI_EnemyCount : MonoBehaviour
{
    private TextMeshProUGUI _textMesh;
    private EnemyManager _enemyManager;

    private void Awake()
    {
        _textMesh = GetComponent<TextMeshProUGUI>();
    }

    // EnemyManager.Awake가 끝난 뒤에 구독하기 위해 Start에서 연결한다.
    private void Start()
    {
        _enemyManager = EnemyManager.Instance;
        _enemyManager.OnZombieDied += Refresh;

        // 최초 표시는 한 번만 직접 갱신하고, 이후에는 좀비가 죽을 때만 갱신한다.
        Refresh(_enemyManager.RemainingCount, _enemyManager.MaxZombieCount);
    }

    private void OnDestroy()
    {
        if (_enemyManager != null)
        {
            _enemyManager.OnZombieDied -= Refresh;
        }
    }

    private void Refresh(int remaining, int max)
    {
        _textMesh.text = $"남은 적: {remaining}/{max}마리";
    }
}