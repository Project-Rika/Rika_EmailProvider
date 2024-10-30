namespace EmailProvider.Models;

// Represents an email request with necessary details to send an email
public class EmailRequest
{
    // Recipient's email address  - Hassan
    public string To { get; set; } = null!;

    // Subject of the email
    public string Subject { get; set; } = null!;

    // HTML content of the email body
    public string HtmlBody { get; set; } = null!;

    // Plain text version of the email body for clients that do not support HTML
    public string PlainText { get; set; } = null!;
}




