using EmailConsoleApp;
namespace MauiEmail
{
    public partial class App : Application
    {
        public static MailConfig MailConfig { get; private set; }
        public static EmailService EmailService { get; private set; }
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
            MailConfig = new MailConfig();
            EmailService = new EmailService(MailConfig);
        }
    }
}
