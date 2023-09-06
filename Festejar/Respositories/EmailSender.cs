using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net.Mail;
using System.Net;

namespace Festejar.Respositories
{
	public class EmailSender : IEmailSender
	{
		public Task SendEmailAsync(string email, string subject, string htmlMessage)
		{
			var smtpClient = new SmtpClient
			{
				Host = "smtp.ampernet.com.br",
				Port = 587,
				EnableSsl = false,
				Credentials = new NetworkCredential("marceloperuzzo@ampernet.com.br", "08891008966Mp@")
			};

			var mailMessage = new MailMessage
			{
				From = new MailAddress("marceloperuzzo@ampernet.com.br"),
				To = { email },
				Subject = subject,
				Body = htmlMessage,
				IsBodyHtml = true
			};

			return smtpClient.SendMailAsync(mailMessage);
		}
	}
}
