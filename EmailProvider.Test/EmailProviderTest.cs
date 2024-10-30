using Moq;
using Azure.Messaging.ServiceBus;
using EmailProvider.Functions;
using EmailProvider.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using EmailProvider.Models;

namespace EmailProvider.Tests
{
    public class EmailSenderTests
    {
        private readonly Mock<ILogger<EmaiISender>> _mockLogger;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly EmaiISender _emailSender;

        public EmailSenderTests()
        {
            _mockLogger = new Mock<ILogger<EmaiISender>>();
            _mockEmailService = new Mock<IEmailService>();
            _emailSender = new EmaiISender(_mockLogger.Object, _mockEmailService.Object);
        }

        // 1. Test when a valid email request is received and the email is sent successfully
        [Fact]
        public async Task Run_ValidEmailRequest_CompletesMessage()
        {
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString("Test message"));
            var messageActionsMock = new Mock<ServiceBusMessageActions>();

            _mockEmailService.Setup(x => x.UnpackEmailRequest(It.IsAny<ServiceBusReceivedMessage>()))
                .Returns(new EmailRequest { To = "test@example.com" });
            _mockEmailService.Setup(x => x.SendEmail(It.IsAny<EmailRequest>())).Returns(true);

            await _emailSender.Run(message, messageActionsMock.Object);

            messageActionsMock.Verify(x => x.CompleteMessageAsync(message, It.IsAny<CancellationToken>()), Times.Once);
        }

        // 2. Test when the email request is invalid (null)
        [Fact]
        public async Task Run_InvalidEmailRequest_DoesNotCompleteMessage()
        {
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString("Test message"));
            var messageActionsMock = new Mock<ServiceBusMessageActions>();

            _mockEmailService.Setup(x => x.UnpackEmailRequest(It.IsAny<ServiceBusReceivedMessage>()))
                .Returns((EmailRequest)null);

            await _emailSender.Run(message, messageActionsMock.Object);

            messageActionsMock.Verify(x => x.CompleteMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // 3. Test when the email request has an empty recipient
        [Fact]
        public async Task Run_EmailRequestWithoutRecipient_DoesNotCompleteMessage()
        {
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString("Test message"));
            var messageActionsMock = new Mock<ServiceBusMessageActions>();

            _mockEmailService.Setup(x => x.UnpackEmailRequest(It.IsAny<ServiceBusReceivedMessage>()))
                .Returns(new EmailRequest { To = "" });

            await _emailSender.Run(message, messageActionsMock.Object);

            messageActionsMock.Verify(x => x.CompleteMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // 4. Test when SendEmail returns false, indicating failure in sending email
        [Fact]
        public async Task Run_EmailSendFailure_DoesNotCompleteMessage()
        {
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString("Test message"));
            var messageActionsMock = new Mock<ServiceBusMessageActions>();

            _mockEmailService.Setup(x => x.UnpackEmailRequest(It.IsAny<ServiceBusReceivedMessage>()))
                .Returns(new EmailRequest { To = "test@example.com" });
            _mockEmailService.Setup(x => x.SendEmail(It.IsAny<EmailRequest>())).Returns(false);

            await _emailSender.Run(message, messageActionsMock.Object);

            messageActionsMock.Verify(x => x.CompleteMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // 5. Test when UnpackEmailRequest throws an exception
        [Fact]
        public async Task Run_UnpackEmailRequestThrowsException_LogsError()
        {
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString("Test message"));
            var messageActionsMock = new Mock<ServiceBusMessageActions>();

            _mockEmailService.Setup(x => x.UnpackEmailRequest(It.IsAny<ServiceBusReceivedMessage>()))
                .Throws(new Exception("Unpack error"));

            await _emailSender.Run(message, messageActionsMock.Object);

            _mockLogger.Verify(x => x.LogError(It.IsAny<Exception>(), It.Is<string>(s => s.Contains("Unpack error"))), Times.Once);
        }

        // 6. Test when SendEmail throws an exception
        [Fact]
        public async Task Run_SendEmailThrowsException_LogsError()
        {
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString("Test message"));
            var messageActionsMock = new Mock<ServiceBusMessageActions>();

            _mockEmailService.Setup(x => x.UnpackEmailRequest(It.IsAny<ServiceBusReceivedMessage>()))
                .Returns(new EmailRequest { To = "test@example.com" });
            _mockEmailService.Setup(x => x.SendEmail(It.IsAny<EmailRequest>()))
                .Throws(new Exception("Send email error"));

            await _emailSender.Run(message, messageActionsMock.Object);

            _mockLogger.Verify(x => x.LogError(It.IsAny<Exception>(), It.Is<string>(s => s.Contains("Send email error"))), Times.Once);
        }

        // 7. Test with a null message input
        [Fact]
        public async Task Run_NullMessage_DoesNotThrowException()
        {
            var messageActionsMock = new Mock<ServiceBusMessageActions>();

            await _emailSender.Run(null, messageActionsMock.Object);

            messageActionsMock.Verify(x => x.CompleteMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // 8. Test with null message actions
        [Fact]
        public async Task Run_NullMessageActions_DoesNotThrowException()
        {
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString("Test message"));

            await _emailSender.Run(message, null);

            _mockLogger.Verify(x => x.LogError(It.IsAny<Exception>(), It.Is<string>(s => s.Contains("Message actions cannot be null"))), Times.Once);
        }

        // 9. Test when message body is empty
        [Fact]
        public async Task Run_EmptyMessageBody_DoesNotCompleteMessage()
        {
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString(""));
            var messageActionsMock = new Mock<ServiceBusMessageActions>();

            _mockEmailService.Setup(x => x.UnpackEmailRequest(It.IsAny<ServiceBusReceivedMessage>()))
                .Returns(new EmailRequest { To = "test@example.com" });

            await _emailSender.Run(message, messageActionsMock.Object);

            messageActionsMock.Verify(x => x.CompleteMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // 10. Test when email request has a valid recipient but invalid email structure
        [Fact]
        public async Task Run_EmailRequestWithInvalidRecipientFormat_DoesNotCompleteMessage()
        {
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString("Test message"));
            var messageActionsMock = new Mock<ServiceBusMessageActions>();

            _mockEmailService.Setup(x => x.UnpackEmailRequest(It.IsAny<ServiceBusReceivedMessage>()))
                .Returns(new EmailRequest { To = "invalid-email-format" });

            await _emailSender.Run(message, messageActionsMock.Object);

            messageActionsMock.Verify(x => x.CompleteMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
