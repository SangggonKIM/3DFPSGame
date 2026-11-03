using TMPro;
using UnityEngine;

// 로그인 입력을 검증하고 실패 시 흔들림/오류 문구 연출을 호출한다.
public class LoginManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField _idInputField;
    [SerializeField] private TMP_InputField _passwordInputField;
    [SerializeField] private UIErrorShake _idFieldEffect;
    [SerializeField] private UIErrorShake _passwordFieldEffect;

    private UIErrorShake _shownErrorEffect; // 현재 오류 문구를 띄운 연출 (같은 컴포넌트로 숨겨야 Fade가 겹치지 않는다)

    private void OnEnable()
    {
        _idInputField.onValueChanged.AddListener(HandleInputChanged);
        _passwordInputField.onValueChanged.AddListener(HandleInputChanged);
    }

    private void OnDisable()
    {
        _idInputField.onValueChanged.RemoveListener(HandleInputChanged);
        _passwordInputField.onValueChanged.RemoveListener(HandleInputChanged);
    }

    // 로그인 버튼 OnClick
    public void OnClickLogin()
    {
        string id = _idInputField.text;
        string password = _passwordInputField.text;

        if (string.IsNullOrWhiteSpace(id))
        {
            Fail(_idInputField, _idFieldEffect, "아이디를 입력해 주세요.");
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            Fail(_passwordInputField, _passwordFieldEffect, "비밀번호를 입력해 주세요.");
            return;
        }

        if (!TryLogin(id, password))
        {
            Fail(_passwordInputField, _passwordFieldEffect, "아이디 또는 비밀번호가 올바르지 않습니다.");
        }
    }

    private bool TryLogin(string id, string password)
    {
        // TODO: 계정 시스템 구현 전까지는 항상 실패로 처리한다.
        return false;
    }

    private void Fail(TMP_InputField inputField, UIErrorShake fieldEffect, string message)
    {
        fieldEffect.PlayErrorShake();
        fieldEffect.ShowError(message);
        _shownErrorEffect = fieldEffect;

        inputField.Select(); // 다시 입력하기 쉽도록 실패한 칸에 포커스를 준다.
    }

    private void HandleInputChanged(string text)
    {
        if (_shownErrorEffect == null) return;

        _shownErrorEffect.HideError();
        _shownErrorEffect = null;
    }
}
