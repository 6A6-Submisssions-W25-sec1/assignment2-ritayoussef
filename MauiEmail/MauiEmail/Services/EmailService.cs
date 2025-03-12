using MimeKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit;
using MailKit.Search;
using MauiEmail.Models;

namespace EmailConsoleApp;

public class EmailService : IEmailService
{
    private readonly IMailConfig _mailConfig;
    private SmtpClient _smtpClient;
    private ImapClient _imapClient;

    public EmailService(IMailConfig mailConfig)
    {
        _mailConfig = mailConfig;
        _smtpClient = new SmtpClient();
        _imapClient = new ImapClient();
    }

    public async Task StartSendClientAsync()
    {
        try
        {
            if (!_smtpClient.IsConnected)
            {
                await _smtpClient.ConnectAsync(_mailConfig.SendHost, _mailConfig.SendPort, _mailConfig.SendSocketOptions);
            }
            if (!_smtpClient.IsAuthenticated)
            {
                await _smtpClient.AuthenticateAsync(_mailConfig.EmailAddress, _mailConfig.Password);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting SMTP client: {ex.Message}");
        }
    }

    public async Task DisconnectSendClientAsync()
    {
        if (_smtpClient.IsConnected)
        {
            await _smtpClient.DisconnectAsync(true);
        }
    }
    public async Task StartRetreiveClientAsync()
    {
        try
        {
            if (!_imapClient.IsConnected)
            {
                await _imapClient.ConnectAsync(_mailConfig.ReceiveHost, _mailConfig.ReceivePort, _mailConfig.ReceiveSocketOptions);
            }
            if (!_imapClient.IsAuthenticated)
            {
                await _imapClient.AuthenticateAsync(_mailConfig.EmailAddress, _mailConfig.Password);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting IMAP client: {ex.Message}");
            throw; 
        }
    }

    public async Task DisconnectRetreiveClientAsync()
    {
        if (_imapClient.IsConnected)
        {
            await _imapClient.DisconnectAsync(true);
        }
    }

    public async Task SendMessageAsync(MimeMessage message)
    {
        try
        {
            await StartSendClientAsync();
            await _smtpClient.SendAsync(message);
            Console.WriteLine("Email sent successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending email: {ex.Message}");
        }
        finally
        {
            await DisconnectSendClientAsync();
        }
    }

    public async Task<IEnumerable<MimeMessage>> DownloadAllEmailsAsync()
    {
        try
        {
            await StartRetreiveClientAsync();
            var inbox = _imapClient.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadOnly);

            var emails = new List<MimeMessage>();
            for (int i = 0; i < inbox.Count; i++)
            {
                emails.Add(await inbox.GetMessageAsync(i));
            }
            return emails;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving emails: {ex.Message}");
            return Enumerable.Empty<MimeMessage>();
        }
        finally
        {
            await DisconnectRetreiveClientAsync();
        }
    }

    public async Task DeleteMessageAsync(int index)
    {
        try
        {
            await StartRetreiveClientAsync();
            var inbox = _imapClient.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadWrite);

            if (index < 0 || index >= inbox.Count)
            {
                Console.WriteLine("Invalid email index.");
                return;
            }

            var messageSummary = await inbox.FetchAsync(new[] { index }, MessageSummaryItems.UniqueId);
            if (messageSummary.Count > 0)
            {
                var uid = messageSummary[0].UniqueId;
                var trashFolder = _imapClient.GetFolder("[Gmail]/Trash");
                await inbox.MoveToAsync(uid, trashFolder);

                Console.WriteLine("Email moved to Trash successfully.");
            }
            else
            {
                Console.WriteLine("Email not found.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting email: {ex.Message}");
        }
        finally
        {
            await DisconnectRetreiveClientAsync();
        }
    }
    public async Task<IEnumerable<ObservableMessage>> FetchAllMessages()
    {
        await StartRetreiveClientAsync();
        var inbox = _imapClient.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadOnly);

        var messages = await inbox.FetchAsync(0, -1, MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope | MessageSummaryItems.Flags);

        return messages
            .Where(m => m.UniqueId != null && m.UniqueId.Id > 0) 
            .Select(m => new ObservableMessage(m));
    }


    public async Task<ObservableMessage> DownloadEmailAsync(UniqueId uid)
    {
        await StartRetreiveClientAsync();
        var inbox = _imapClient.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadOnly);
        
        var mimeMessage = await inbox.GetMessageAsync(uid);
        return new ObservableMessage(mimeMessage, uid);
    }

    public async Task DeleteMessageAsync(UniqueId uid)
    {
        Console.WriteLine($"DeleteMessageAsync: Deleting email UID {uid}");

        await StartRetreiveClientAsync();
        var inbox = _imapClient.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadWrite);

        await inbox.StoreAsync(uid, new StoreFlagsRequest(StoreAction.Add, MessageFlags.Deleted) { Silent = true });
        await inbox.ExpungeAsync(); 

        Console.WriteLine("DeleteMessageAsync: Email deleted successfully.");
    }



    public async Task MarkAsReadAsync(UniqueId uid)
    {
        await StartRetreiveClientAsync();
        var inbox = _imapClient.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadWrite);
        
        await inbox.StoreAsync(uid, new StoreFlagsRequest(StoreAction.Add, MessageFlags.Seen) { Silent = true });
    }

    public async Task MarkAsFavoriteAsync(UniqueId uid)
    {
        await StartRetreiveClientAsync();
        var inbox = _imapClient.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadWrite);
    
        await inbox.StoreAsync(uid, new StoreFlagsRequest(StoreAction.Add, MessageFlags.Flagged) { Silent = true });
        await inbox.ExpungeAsync();
    }

    public async Task<IEnumerable<UniqueId>> SearchMessagesAsync(string query)
    {
        await StartRetreiveClientAsync();
        var inbox = _imapClient.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadOnly);
        
        return await inbox.SearchAsync(
            SearchQuery.FromContains(query)
            .Or(SearchQuery.SubjectContains(query))
            .Or(SearchQuery.BodyContains(query))
        );
    }

}