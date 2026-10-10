using Azure;
using Azure.Communication.Email;
using Fusspflege.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace Fusspflege.Infrastructure.Email;

public class AcsEmailSender : IEmailSender
{
    private readonly EmailClient _client;
    private readonly string _senderAddress;

    public AcsEmailSender(IOptions<EmailOptions> options)
    {
        var o = options.Value;
        if (string.IsNullOrWhiteSpace(o.AcsConnectionString) || string.IsNullOrWhiteSpace(o.SenderAddress))
            throw new InvalidOperationException(
                "Email:AcsConnectionString und Email:SenderAddress müssen gesetzt sein.");

        _client = new EmailClient(o.AcsConnectionString);
        _senderAddress = o.SenderAddress;
    }

    public async Task SendAsync(OutgoingEmail email, CancellationToken ct = default)
    {
        var content = new EmailContent(email.Subject)
        {
            PlainText = email.PlainTextBody,
            Html = email.HtmlBody
        };

        var recipients = new EmailRecipients(new[] { new EmailAddress(email.ToAddress, email.ToName) });
        var message = new Azure.Communication.Email.EmailMessage(_senderAddress, recipients, content);

        if (email.Attachment is not null)
        {
            message.Attachments.Add(new EmailAttachment(
                email.Attachment.FileName,
                email.Attachment.ContentType,
                BinaryData.FromBytes(email.Attachment.Content)));
        }

        // Started = Azure hat die Mail angenommen, wir warten nicht auf die Zustellung
        await _client.SendAsync(WaitUntil.Started, message, ct);
    }
}