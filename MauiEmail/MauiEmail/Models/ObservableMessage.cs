using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using MauiEmail.Views;


namespace MauiEmail.Models
{
    public class ObservableMessage : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public UniqueId? UniqueId { get; set; }
        public DateTimeOffset Date { get; set; }
        public string Subject { get; set; }
        public string? Body { get; set; }
        public string? HtmlBody { get; set; }
        public MailboxAddress From { get; set; }
        public List<MailboxAddress> To { get; set; }
        public bool IsRead { get; set; }
        public bool IsFavorite { get; set; }

        private string _senderInitial;
        public string SenderInitial
        {
            get => _senderInitial;
            set
            {
                if (_senderInitial != value)
                {
                    _senderInitial = value;
                }
            }
        }
        public string Preview
        {
            get { return !string.IsNullOrEmpty(Body) ? Body.Substring(0, Math.Min(50, Body.Length)) + "..." : "No preview available"; }
        }
        public ObservableMessage(IMessageSummary message)
        {
            UniqueId = message.UniqueId != null && message.UniqueId.Id > 0 ? message.UniqueId : null;
            if (UniqueId == null)
            {
                Console.WriteLine("ObservableMessage: Warning! Email has invalid UniqueId.");
            }

            Date = message.Envelope.Date ?? DateTimeOffset.MinValue;
            Subject = !string.IsNullOrEmpty(message.Envelope.Subject) ? message.Envelope.Subject : "(No Subject)";

            var sender = message.Envelope.From?.Mailboxes?.FirstOrDefault();
            From = message.Envelope.From?.Mailboxes?.FirstOrDefault() ?? new MailboxAddress("Unknown", "unknown@example.com");

          
            To = (message.Envelope.To?.Mailboxes != null && message.Envelope.To.Mailboxes.Any())
                ? message.Envelope.To.Mailboxes.Select(m => (MailboxAddress)m).ToList()
                : new List<MailboxAddress>();

            IsRead = (message.Flags == MessageFlags.Seen);
            IsFavorite = (message.Flags == MessageFlags.Flagged);
            SenderInitial = !string.IsNullOrWhiteSpace(From.Name)
                ? From.Name.Trim()[0].ToString().ToUpper()
                : !string.IsNullOrWhiteSpace(From.Address)
                    ? From.Address.Trim()[0].ToString().ToUpper()
                    : "?";

            Console.WriteLine($"SenderInitial for {From.Name}: {SenderInitial}"); 
            Body = null;
            HtmlBody = null;

            Console.WriteLine($"LoadEmails: Email from {From.Name}, Initial: {SenderInitial}, UID: {UniqueId?.Id ?? 0}");
        }

      
        public ObservableMessage(MimeMessage mimeMessage, UniqueId uniqueId)
        {
            UniqueId = uniqueId;
            Date = mimeMessage.Date;
            Subject = mimeMessage.Subject;
            From = mimeMessage.From.Mailboxes.FirstOrDefault() ?? new MailboxAddress("Unknown", "unknown@example.com");
            To = mimeMessage.To.Mailboxes.Select(m => (MailboxAddress)m).ToList();
            Body = mimeMessage.TextBody;
            HtmlBody = mimeMessage.HtmlBody;
            IsRead = true;
            IsFavorite = false;
        }

        public MimeMessage ToMime()
        {
            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(From);
            mimeMessage.To.AddRange(To);
            mimeMessage.Subject = Subject;
            mimeMessage.Body = new TextPart("plain") { Text = Body ?? "" };
            return mimeMessage;
        }

        public MimeMessage GetForward()
        {
            var forwardMessage = new MimeMessage();
            forwardMessage.From.Add(From);
            forwardMessage.Subject = "FW: " + Subject;
            forwardMessage.Body = new TextPart("plain")
            {
                Text = $"\n----- Forwarded message -----\nFrom: {From}\nTo: {string.Join(", ", To)}\n\n{Body}"
            };
            return forwardMessage;
        }

        public void MarkAsRead(ImapClient imapClient, UniqueId uid)
        {
            var inbox = imapClient.Inbox;
            inbox.AddFlags(uid, MessageFlags.Seen, true);
            IsRead = true;
        }

        public void MarkAsFavorite(ImapClient imapClient, UniqueId uid)
        {
            var inbox = imapClient.Inbox;
            inbox.AddFlags(uid, MessageFlags.Flagged, true);
            IsFavorite = true;
        }

        public static IEnumerable<UniqueId> SearchMessages(ImapClient imapClient, string query)
        {
            var inbox = imapClient.Inbox;
            inbox.Open(FolderAccess.ReadOnly);
            return inbox.Search(SearchQuery.FromContains(query)
                .Or(SearchQuery.SubjectContains(query))
                .Or(SearchQuery.BodyContains(query)));
        }
    }
}
