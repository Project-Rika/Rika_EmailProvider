using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using EmailProvider.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;


namespace EmailProvider.Functions
{
    // Class for handling email sending functions triggered by Service Bus messages
    public class EmaiISender(ILogger<EmaiISender> logger, IEmailService emailService)
    {
        // Logger for recording runtime information and errors
        private readonly ILogger<EmaiISender> _logger = logger;

        // Service for handling email operations
        private readonly IEmailService _emailService = emailService;

        // Azure Function that triggers when a message is received on the 'email_request' queue
        [Function(nameof(EmaiISender))]
        public async Task Run([ServiceBusTrigger("email_request", Connection = "ServiceBusConnection")] ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions)
        {
            try
            {
                // Unpacks the message to retrieve email request data
                var emailRequest = _emailService.UnpackEmailRequest(message);

                // Checks if the email request is valid and contains a recipient
                if (emailRequest != null && !string.IsNullOrEmpty(emailRequest.To))
                {
                    // Sends the email and marks the message as complete if successful
                    if (_emailService.SendEmail(emailRequest))
                    {
                        await messageActions.CompleteMessageAsync(message);
                    }
                }
            }
            catch (Exception ex)
            {
                // Logs any errors that occur during the function execution
                _logger.LogError($"ERROR: EmailSender.Run :: {ex.Message}");
            }
        }
    }
}
