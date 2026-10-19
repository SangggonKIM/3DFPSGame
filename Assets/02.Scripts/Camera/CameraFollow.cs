using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform _target;

    private void Awake()
    {
        if (_target == null) Debug.LogWarning("[CameraFollow] 카메라 타겟이 없습니다!!");
    }

    // Monobehaviour 생명주기
    // A, B
    // A -> Update
    // B -> Update
    // A -> LateUpdate
    // B -> LateUpdate
    private void LateUpdate()
    {
        transform.position = _target.position;
    }
}
