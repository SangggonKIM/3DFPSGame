using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMove : MonoBehaviour
{
    private const float GroundStickVelocity = -2f;

    [Header("이동")]

    [SerializeField, Min(0f)] private float _moveSpeed = 7f;
    [SerializeField, Min(0f)] private float _dashSpeed = 12f;
    [SerializeField, Min(0f)] private float _acceleration = 45f;
    [SerializeField, Min(0f)] private float _deceleration = 55f;
    [Tooltip("공중 가속·감속 비율. 0은 관성 유지, 1은 지상과 동일합니다.")]
    [SerializeField, Range(0f, 1f)]
    private float _airControl = 0.35f;
    [SerializeField] private CharacterController _characterController;

    [Header("점프")]

    [SerializeField, Min(0f)] private float _jumpPower = 10f;
    [Tooltip("1: 기본 점프, 2: 공중에서 한 번 더 점프")]
    [SerializeField, Min(1)]
    private int _jumpCountLimit = 2;
    [SerializeField, Min(0f)] private float _coyoteTime = 0.1f;
    [SerializeField, Min(0f)] private float _jumpBufferTime = 0.12f;
    [Tooltip("상승 중 점프 키를 놓았을 때 남길 상승 속도 비율")]
    [SerializeField, Range(0f, 1f)]
    private float _jumpCutMultiplier = 0.5f;

    [Header("중력")]

    [SerializeField, Min(0.01f)] private float _gravity = 20f;
    [SerializeField, Min(1f)] private float _fallGravityMultiplier = 2f;
    [SerializeField, Min(2f)] private float _maxFallSpeed = 30f;

    private Vector3 _horizontalVelocity;
    private float _yVelocity;
    private int _jumpCount;
    private float _lastGroundedTime = float.NegativeInfinity;
    private float _lastJumpPressedTime = float.NegativeInfinity;
    private bool _jumpCutApplied;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f) return;

        // isGrounded는 직전 Move 결과입니다. 점프 직후 남아 있는 바닥 판정으로
        // 상승 속도가 초기화되지 않도록 수직 속도도 함께 확인합니다.
        bool grounded = _characterController.isGrounded && _yVelocity <= 0f;
        if (grounded) RegisterGrounded();

        if (Input.GetButtonDown("Jump")) _lastJumpPressedTime = Time.time;
        bool jumpHeld = Input.GetButton("Jump");
        TryJump(grounded);
        ApplyJumpCut(jumpHeld);

        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f,
            Input.GetAxisRaw("Vertical"));
        input = Vector3.ClampMagnitude(input, 1f);
        Vector3 direction = transform.TransformDirection(input);
        direction.y = 0f;
        direction = direction.normalized * input.magnitude;

        float speed = Input.GetKey(KeyCode.LeftShift) ? _dashSpeed : _moveSpeed;
        Vector3 targetVelocity = direction * speed;
        float acceleration = input.sqrMagnitude > 0f ? _acceleration : _deceleration;
        // 월드 좌표 속도를 저장하므로 공중에서 몸을 돌려도 즉시 방향이 꺾이지 않습니다.
        bool movingOnGround = grounded && _yVelocity <= 0f;
        if (!movingOnGround) acceleration *= _airControl;
        _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity,
            targetVelocity, acceleration * deltaTime);

        if (!movingOnGround)
        {
            float gravityMultiplier = _yVelocity < 0f ? _fallGravityMultiplier : 1f;
            _yVelocity = Mathf.Max(_yVelocity - _gravity * gravityMultiplier * deltaTime,
                -_maxFallSpeed);
        }

        Vector3 velocity = _horizontalVelocity + Vector3.up * _yVelocity;
        CollisionFlags flags = _characterController.Move(velocity * deltaTime);

        // 이번 Move에서 천장에 닿았으면 상승 속도를 즉시 제거합니다.
        if ((flags & CollisionFlags.Above) != 0 && _yVelocity > 0f)
        {
            _yVelocity = 0f;
        }

        if ((flags & CollisionFlags.Below) != 0 && _yVelocity <= 0f)
        {
            RegisterGrounded();
            // 착지한 프레임에 버퍼를 소비하고 다음 이동부터 바로 상승합니다.
            TryJump(true);
            ApplyJumpCut(jumpHeld);
        }
    }

    private void RegisterGrounded()
    {
        _lastGroundedTime = Time.time;
        _jumpCount = 0;
        _yVelocity = GroundStickVelocity;
    }

    private void TryJump(bool grounded)
    {
        bool canUseGroundJump = grounded ||
                                (_jumpCount == 0 && Time.time - _lastGroundedTime <= _coyoteTime);

        // 낭떠러지에서 유예 시간이 지나면 지상 점프 기회는 소멸합니다.
        // 2단 점프 설정일 때에는 남은 공중 점프 한 번만 사용할 수 있습니다.
        if (!canUseGroundJump && _jumpCount == 0) _jumpCount = 1;

        if (Time.time - _lastJumpPressedTime > _jumpBufferTime) return;
        if (!canUseGroundJump && _jumpCount >= _jumpCountLimit) return;

        _yVelocity = _jumpPower;
        _jumpCount = canUseGroundJump ? 1 : _jumpCount + 1;
        _jumpCutApplied = false;
        // 입력과 유예 시간을 소비하여 길게 눌러도 자동 연속 점프하지 않습니다.
        _lastJumpPressedTime = float.NegativeInfinity;
        _lastGroundedTime = float.NegativeInfinity;
    }

    private void ApplyJumpCut(bool jumpHeld)
    {
        // 버퍼 입력 후 이미 키를 뗀 경우에도 짧은 점프가 됩니다.
        if (!jumpHeld && !_jumpCutApplied && _yVelocity > 0f)
        {
            _yVelocity *= _jumpCutMultiplier;
            _jumpCutApplied = true;
        }
    }
}
