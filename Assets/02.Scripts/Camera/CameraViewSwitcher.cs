using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class CameraViewSwitcher : MonoBehaviour
{
    public enum ViewMode
    {
        FirstPerson,
        ThirdPerson,
        Shoulder,
        Quarter,
        TopDown,
        Side
    }

    [Header("추적 대상")]

    [SerializeField] private Transform _player;
    [SerializeField] private Transform _cameraPosition;

    [Header("입력 — Input Manager (Old)")]

    [SerializeField]private KeyCode _switchKey = KeyCode.V;
    [Min(0f)][SerializeField] private float _mouseSensitivity = 2f;

    // 1인칭: 눈 위치로 이동하고, 설정한 방향에 마우스 상하 회전을 더합니다.
    // 최종 상하 각도는 -90~90도로 제한하며, 좌우 회전은 PlayerRotate를 따릅니다.
    [Header("1인칭")]

    [SerializeField]private ViewSettings _firstPerson = new(
        Vector3.zero, Vector3.zero, true);

    // 3인칭: 눈 위치보다 위쪽/뒤쪽에 배치하고, 약간 아래를 바라봅니다.
    [Header("3인칭")]

    [SerializeField]private ViewSettings _thirdPerson = new(
        new Vector3(0f, 1f, -4f), new Vector3(10f, 0f, 0f), true);

    // 숄더뷰: 오른쪽 어깨 뒤로 이동하고, 플레이어 앞쪽을 바라봅니다.
    [Header("숄더뷰")]

    [SerializeField]private ViewSettings _shoulder = new(
        new Vector3(0.7f, 0.3f, -2.5f), new Vector3(5f, 0f, 0f), true);

    // 쿼터뷰: 대각선 위쪽으로 이동하고, 눈 위치를 향해 비스듬히 내려다봅니다.
    // 기본값에서는 플레이어가 회전해도 카메라의 월드 기준 방향은 유지됩니다.
    [Header("쿼터뷰")]

    [SerializeField]private ViewSettings _quarter = new(
        new Vector3(6f, 6f, -6f), new Vector3(35.264f, -45f, 0f), false);

    // 탑다운: 눈 위치 바로 위로 이동하고, X축을 90도 회전해 아래를 봅니다.
    [Header("탑다운")]

    [SerializeField]private ViewSettings _topDown = new(
        new Vector3(0f, 10f, 0f), new Vector3(90f, 0f, 0f), false);

    // 사이드뷰: 월드 +X 방향으로 이동하고, -X 방향으로 플레이어 옆면을 봅니다.
    [Header("사이드뷰")]

    [SerializeField]private ViewSettings _side = new(
        new Vector3(8f, 0f, 0f), new Vector3(0f, -90f, 0f), false);
    private CameraFollow _cameraFollow;

    private CameraRotate _fpsCameraRotate;
    private bool _hasCapturedScriptStates;
    private float _mousePitch;
    private bool _wasCameraFollowEnabled;
    private bool _wasFpsCameraRotateEnabled;

    public ViewMode CurrentView { get; private set; } = ViewMode.FirstPerson;

    private void Awake()
    {
        _fpsCameraRotate = GetComponent<CameraRotate>();
        _cameraFollow = GetComponent<CameraFollow>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(_switchKey)) CurrentView = (ViewMode)(((int)CurrentView + 1) % 6);

        if (CurrentView == ViewMode.FirstPerson)
        {
            // Inspector의 기본 상하 각도와 마우스 회전을 합친 값에 제한을 적용합니다.
            var basePitch = _firstPerson.EulerAngles.x;
            _mousePitch -= Input.GetAxis("Mouse Y") * _mouseSensitivity;
            _mousePitch = Mathf.Clamp(_mousePitch, -90f - basePitch, 90f - basePitch);
        }
    }

    private void LateUpdate()
    {
        if (_player == null || _cameraPosition == null) return;

        var settings = GetCurrentSettings();
        var referenceRotation = settings.FollowPlayerYaw
            ? Quaternion.Euler(0f, _player.eulerAngles.y, 0f)
            : Quaternion.identity;

        // 위치 오프셋은 눈 위치 기준이며, 플레이어의 스케일 영향을 받지 않습니다.
        var position = _cameraPosition.position
                       + referenceRotation * settings.PositionOffset;
        var angles = settings.EulerAngles;

        if (CurrentView == ViewMode.FirstPerson) angles.x = Mathf.Clamp(angles.x + _mousePitch, -90f, 90f);

        // LookAt으로 덮어쓰지 않아 Inspector에서 지정한 방향을 그대로 사용합니다.
        var rotation = referenceRotation * Quaternion.Euler(angles);
        transform.SetPositionAndRotation(position, rotation);
    }

    private void OnEnable()
    {
        if (_player == null || _cameraPosition == null)
        {
            Debug.LogError("CameraViewSwitcher: Player와 CameraPosition을 연결하세요.", this);
            enabled = false;
            return;
        }

        // 카메라 Transform을 여러 스크립트가 동시에 변경하지 않도록 합니다.
        if (_fpsCameraRotate != null)
        {
            _wasFpsCameraRotateEnabled = _fpsCameraRotate.enabled;
            _fpsCameraRotate.enabled = false;
        }

        if (_cameraFollow != null)
        {
            _wasCameraFollowEnabled = _cameraFollow.enabled;
            _cameraFollow.enabled = false;
        }

        _hasCapturedScriptStates = true;
    }

    private void OnDisable()
    {
        if (!_hasCapturedScriptStates) return;

        if (_fpsCameraRotate != null) _fpsCameraRotate.enabled = _wasFpsCameraRotateEnabled;

        if (_cameraFollow != null) _cameraFollow.enabled = _wasCameraFollowEnabled;

        _hasCapturedScriptStates = false;
    }

    private ViewSettings GetCurrentSettings()
    {
        switch (CurrentView)
        {
            case ViewMode.ThirdPerson:
                return _thirdPerson;
            case ViewMode.Shoulder:
                return _shoulder;
            case ViewMode.Quarter:
                return _quarter;
            case ViewMode.TopDown:
                return _topDown;
            case ViewMode.Side:
                return _side;
            default:
                return _firstPerson;
        }
    }

    [Serializable]
    private class ViewSettings
    {
        [Tooltip("CameraPosition 기준 위치 오프셋. X: 오른쪽, Y: 위, Z: 앞")]
        [SerializeField]
        private Vector3 _positionOffset;

        [Tooltip("카메라 회전 각도. X: 상하, Y: 좌우, Z: 기울기")]
        [SerializeField]
        private Vector3 _eulerAngles;

        [Tooltip("켜면 위치와 방향이 Player의 Y축 회전을 따르고, 끄면 월드 축을 기준으로 합니다.")]
        [SerializeField]
        private bool _followPlayerYaw;

        public ViewSettings(Vector3 positionOffset, Vector3 eulerAngles, bool followPlayerYaw)
        {
            _positionOffset = positionOffset;
            _eulerAngles = eulerAngles;
            _followPlayerYaw = followPlayerYaw;
        }

        public Vector3 PositionOffset => _positionOffset;
        public Vector3 EulerAngles => _eulerAngles;
        public bool FollowPlayerYaw => _followPlayerYaw;
    }
}
