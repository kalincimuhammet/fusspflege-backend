namespace Fusspflege.Core.Interfaces;

public record OutgoingAttachment(string FileName, string ContentType, byte[] Content);

public record OutgoingEmail(
    string ToAddress,
    string ToName,
    string Subject,
    string HtmlBody,
    string PlainTextBody,
    OutgoingAttachment? Attachment = null);

public interface IEmailSender
{
    Task SendAsync(OutgoingEmail email, CancellationToken ct = default);
}