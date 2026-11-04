using System.Text.RegularExpressions;

public class AccountIdSpecification
{
    // ID: 이메일 (로컬부@도메인.최상위도메인 2글자 이상)
    private static readonly Regex EmailRegex =
        new Regex(@"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$");

    public string ErrorMesage => "아이디는 이메일 형식이어야 합니다.";

    public bool IsSatisfiedBy(string email)
    {
        return EmailRegex.IsMatch(email);
    }
}
