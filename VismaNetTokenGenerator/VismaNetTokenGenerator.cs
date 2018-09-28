using System;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ONIT.VismaNetApi;
using ONIT.VismaNetApi.Exceptions;
using SendGrid.Helpers.Mail;

namespace VismaNetTokenGenerator
{
    public static class VismaNetTokenGenerator
    {
        private const string SendGridQueue = nameof(SendGridQueue);

        private static readonly IConfigurationRoot Config = new ConfigurationBuilder()
            .AddJsonFile("local.settings.json", true, true)
            .AddEnvironmentVariables()
            .Build();

        [FunctionName("Callback")]
        public static async Task<IActionResult> Callback(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "callback")]
            HttpRequest req,
            [Queue(SendGridQueue)] IAsyncCollector<string> emailQueue,
            ILogger log)
        {
            var clientId = Config.GetValue<string>("client-id") ??
                           throw new InvalidArgumentsException("client-id is missing");
            var clientSecret = Config.GetValue<string>("client-secret") ??
                               throw new InvalidArgumentsException("client-secret is missing");
            var callbackUrl = Config.GetValue<string>("callback-url") ??
                              throw new InvalidArgumentsException("callback-url is missing");
            var code = req.GetQueryParameterDictionary()["code"];
            try
            {
                var token = await VismaNet.GetTokenUsingOAuth(clientId, clientSecret, code, callbackUrl);
                var contexts = await VismaNet.GetContextsForToken(token);
                if (!string.IsNullOrEmpty(Config.GetValue<string>("AzureWebJobsSendGridApiKey")))
                {
                    var builder = new StringBuilder();
                    builder.AppendLine($"Token: {token}");
                    builder.AppendLine("Available contexts:");
                    foreach (var ctx in contexts)
                    {
                        builder.AppendLine($"{ctx.name} ({ctx.id})");
                    }

                    await emailQueue.AddAsync(builder.ToString());

                    return new ContentResult
                    {
                        Content = "<h1>Thank you</h1><p>Your token was generated and sent to us.</p>",
                        ContentType = "text/html",
                        StatusCode = 200
                    };
                }
                else
                {
                    var builder = new StringBuilder();
                    builder.AppendLine("<h1>Thank you</h1>");
                    builder.AppendLine($"<p><strong>Token:</strong> {token}</p>");
                    builder.AppendLine("<p><strong>Available contexts:</strong></p><ul>");
                    foreach (var ctx in contexts)
                    {
                        builder.AppendLine($"<li>{ctx.name} ({ctx.id})</li>");
                    }

                    builder.AppendLine("</ul>");

                    return new ContentResult
                    {
                        Content = builder.ToString(),
                        ContentType = "text/html",
                        StatusCode = 200
                    };
                }
            }
            catch (Exception e)
            {
                log.LogError(e, e.Message);
                return new InternalServerErrorResult();
            }
        }

        [FunctionName("Init")]
        public static IActionResult Init([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "init")]
            HttpRequest req, ILogger log)
        {
            var clientId = Config.GetValue<string>("client-id") ??
                           throw new InvalidArgumentsException("client-id is missing");
            var callbackUrl = Config.GetValue<string>("callback-url") ??
                              throw new InvalidArgumentsException("callback-url is missing");
            var redirectUrl = VismaNet.GetOAuthUrl(clientId, callbackUrl);
            return new RedirectResult(redirectUrl);
        }

        [FunctionName("SendTokenByMail")]
        public static void SendTokenByMail(
            [QueueTrigger(SendGridQueue)] string emailContent,
            [SendGrid(From = "%sendgrid-from%", Subject = "%sendgrid-subject%",
                To = "%sendgrid-to%")]
            out SendGridMessage message)
        {
            message = new SendGridMessage
            {
                PlainTextContent = emailContent
            };
        }
    }
}