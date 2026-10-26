using System;
using TMPro;
using UnityEngine;

public class UI_EnemyCount : MonoBehaviour
{
    private TextMeshProUGUI _textMesh;

    private void Awake()
    {
        _textMesh = GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        _textMesh.text = $"남은 적: {EnemyManager.Instance.GetEnemyCount()}마리";
    }
}