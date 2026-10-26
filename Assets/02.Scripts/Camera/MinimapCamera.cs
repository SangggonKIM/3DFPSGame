using System;
using UnityEngine;

// 미니맵 카메라: 플레이어 머리 위에서 아래를 내려다보며 따라다닌다.
public class MinimapCamera : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private float _height = 8f;
    [SerializeField] private float _zoomStep = 2f;
    [SerializeField] private float _minSize = 4f;
    [SerializeField] private float _maxSize = 16f;
    private Camera _camera;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        // 1. 대상의 머리 위로 위치를 옮긴다.
        transform.position = _target.position + (Vector3.up * _height);

        // 2. 대상이 쳐다보는 방향으로 카메라도 회전한다.
        transform.eulerAngles = new Vector3(90, _target.eulerAngles.y, 0f);
    }

    public void ZoomIn()
    {
        SetSize(_camera.orthographicSize - _zoomStep);
    }

    public void ZoomOut()
    {
        SetSize(_camera.orthographicSize + _zoomStep);
    }

    private void SetSize(float size)
    {
        _camera.orthographicSize = Mathf.Clamp(size, _minSize, _maxSize);
    }
}
