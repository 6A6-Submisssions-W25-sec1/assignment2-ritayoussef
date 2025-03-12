using MailKit.Security;
namespace EmailConsoleApp;

public interface IMailConfig
{
    string EmailAddress { get; }
    string Password { get; }
    string ReceiveHost { get; }
    SecureSocketOptions ReceiveSocketOptions { get; }
    int ReceivePort { get; }
    string SendHost  { get; }
    int SendPort { get; }
    SecureSocketOptions SendSocketOptions { get; }
    string OAuth2ClientId { get; }
    string OAuth2ClientSecret { get; }
    public string OAuth2RefreshToken { get; }
}