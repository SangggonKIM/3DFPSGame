using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    private const float Gravity = -20f;

    [SerializeField] private float _moveSpeed = 7f;
    [SerializeField] private float _dashSpeed = 12f;
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private float _jumpPower = 10f;
    [SerializeField] private int _jumpCountLimit = 2;
    private int _jumpCount = 0;
    private float _currentSpeed = 0f;

    private bool _isJumping;
    private float _yVelocity; // 위아래 속도, 매 프레임마다 중력 가속도가 붙는다.

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // 1. 키보드 입력을 받는다.
        var h = Input.GetAxisRaw("Horizontal"); // -1, 0, 1
        var v = Input.GetAxisRaw("Vertical"); // -1, 0, 1

        // 2. 키보드 입력에 따라 방향을 구한다.
        var direction = new Vector3(h, 0f, v);
        direction.Normalize(); // 대각선이어도 방향의 길이 1로 바꾸기 위해
        direction = transform.TransformDirection(direction);
        // 로컬 좌표계: 한 대상을 중심
        // 월드 좌표계: 세상을 중심

        // 3. 땅에 서있지 않을때만 중력을 누적한다.
        if (!_characterController.isGrounded)
        {
            _yVelocity += Gravity * Time.deltaTime;
        }
        else
        {
            _yVelocity = -2f;
        }

        if (_isJumping && _characterController.isGrounded)
        {
            _isJumping = false;
            _jumpCount = 0;
        }

        if (Input.GetKey(KeyCode.LeftShift))
        {
            _currentSpeed = _dashSpeed;
        }
        else
        {
            _currentSpeed = _moveSpeed;
        }

        // 4. 만약에 점프키를 누르면 && 점프중이 아니라면 ... 점프를한다.
        if (Input.GetButtonDown("Jump") && !_isJumping) // 중재자 패턴
        {
            _yVelocity = _jumpPower;
            _jumpCount++;
            if (_jumpCount == _jumpCountLimit)
            {
                _isJumping = true;
            }
        }

        // 5. 속도를 구한다.
        var velocity = direction * _currentSpeed;
        velocity.y = _yVelocity;

        // 6. 속도(방향과 속력)에 따라 이동한다.
        // transform.position += direction * (_moveSpeed * Time.deltaTime);
        _characterController.Move(velocity * Time.deltaTime);
    }
}
