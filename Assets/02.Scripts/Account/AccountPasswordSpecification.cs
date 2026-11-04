using System.Text.RegularExpressions;

public class AccountPasswordSpecification
{
    // 비밀번호: 소문자, 숫자, 기호를 각각 하나 이상 포함한 10~30글자 (공백 제외 ASCII, 대문자 사용 가능)
    private static readonly Regex PasswordRegex =
        new Regex(@"^(?=.*[a-z])(?=.*[0-9])(?=.*[^A-Za-z0-9])[\x21-\x7E]{10,30}$");

    public string ErrorMesage => "비밀번호는 소문자, 숫자, 기호를 모두 포함한 10~30 글자여야 합니다.";

    public bool IsSatisfiedBy(string password)
    {
        return PasswordRegex.IsMatch(password);
    }
}
