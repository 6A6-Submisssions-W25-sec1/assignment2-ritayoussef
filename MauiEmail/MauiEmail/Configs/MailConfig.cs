using MailKit.Security;

namespace EmailConsoleApp;

public class MailConfig : IMailConfig
{
    private const int IMAP_PORT = 993;
    private const int SMTP_PORT_TLS = 587;
    public string EmailAddress { get; set; } = "appdevrita@gmail.com";
    public string Password { get; set; } = "bdqs svom dddi urtz";

    public string ReceiveHost { get; set; } = "imap.gmail.com";
    public SecureSocketOptions ReceiveSocketOptions { get; set; } = SecureSocketOptions.SslOnConnect;
    public int ReceivePort { get; set; } = IMAP_PORT;

    public string SendHost { get; set; } = "smtp.gmail.com";
    public int SendPort { get; set; } = SMTP_PORT_TLS;
    public SecureSocketOptions SendSocketOptions { get; set; } = SecureSocketOptions.StartTls;

    public string OAuth2ClientId { get; set; } = "";
    public string OAuth2ClientSecret { get; set; } = "";
    public string OAuth2RefreshToken { get; set; } = "";
}