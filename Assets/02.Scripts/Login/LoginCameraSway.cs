using UnityEngine;

// 로그인 화면 카메라가 사람이 서서 숨 쉬듯 아주 미세하게 흔들리게 한다.
// 축마다 다른 Perlin Noise 좌표를 써서 반복되는 느낌을 줄인다.
public class LoginCameraSway : MonoBehaviour
{
    private const float NoiseSeedRange = 100f;

    [SerializeField] private Vector3 _positionAmplitude = new Vector3(0.01f, 0.02f, 0f); // m
    [SerializeField] private Vector2 _rotationAmplitude = new Vector2(0.1f, 0.1f);       // X(Pitch), Y(Yaw) 도
    [SerializeField, Min(0f)] private float _frequency = 0.25f;                           // 노이즈 진행 속도

    private Vector3 _basePosition;
    private Quaternion _baseRotation;
    private float _time;
    private float _seedX;
    private float _seedY;
    private float _seedPitch;
    private float _seedYaw;

    private void Awake()
    {
        _seedX = Random.Range(0f, NoiseSeedRange);
        _seedY = Random.Range(0f, NoiseSeedRange);
        _seedPitch = Random.Range(0f, NoiseSeedRange);
        _seedYaw = Random.Range(0f, NoiseSeedRange);
    }

    private void OnEnable()
    {
        _basePosition = transform.localPosition;
        _baseRotation = transform.localRotation;
    }

    private void OnDisable()
    {
        transform.localPosition = _basePosition;
        transform.localRotation = _baseRotation;
    }

    private void LateUpdate()
    {
        _time += Time.deltaTime * _frequency;

        Vector3 positionOffset = new Vector3(
            Noise(_seedX) * _positionAmplitude.x,
            Noise(_seedY) * _positionAmplitude.y,
            0f);
        float pitch = Noise(_seedPitch) * _rotationAmplitude.x;
        float yaw = Noise(_seedYaw) * _rotationAmplitude.y;

        transform.localPosition = _basePosition + _baseRotation * positionOffset;
        transform.localRotation = _baseRotation * Quaternion.Euler(pitch, yaw, 0f);
    }

    // -1~1 범위 노이즈
    private float Noise(float seed)
    {
        return Mathf.Clamp(Mathf.PerlinNoise(_time, seed) * 2f - 1f, -1f, 1f);
    }
}
