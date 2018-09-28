using System;
using System.Net;
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
            var clientId = Config.GetValue<string>("VismaNetClientId") ??
                           throw new InvalidArgumentsException("VismaNetClientId is missing");
            var clientSecret = Config.GetValue<string>("VismaNetClientSecret") ??
                               throw new InvalidArgumentsException("VismaNetClientSecret is missing");
            var callbackUrl = Config.GetValue<string>("VismaNetCallbackUrl") ??
                              throw new InvalidArgumentsException("VismaNetCallbackUrl is missing");
            var code = req.GetQueryParameterDictionary()["code"];
            try
            {
                var token = await VismaNet.GetTokenUsingOAuth(clientId, clientSecret, code, callbackUrl);
                var contexts = await VismaNet.GetContextsForToken(token);
                var builder = new StringBuilder();
                builder.AppendLine($"<p><strong>Token:</strong> {token}</p>");
                builder.AppendLine("<p><strong>Available contexts:</strong></p>");
                builder.AppendLine("<ul>");
                foreach (var ctx in contexts)
                {
                    builder.AppendLine($"<li>{ctx.name} ({ctx.id})</li>");
                }

                builder.AppendLine("</ul>");
                try
                {
                    dynamic vismaNet = new VismaNet(0, token);
                    var userInfo = await vismaNet.context.userdetails.Get();
                    var user = userInfo[0];
                    builder.AppendLine($"<p>Created by {user.firstName} {user.lastName} (<a href='mailto:{user.emailAddress}'>{user.emailAddress}</a>)</p>");
                }
                catch (Exception e)
                {
                    log.LogError(e, "Could not fetch user details");
                }

                if (!string.IsNullOrEmpty(Config.GetValue<string>("AzureWebJobsSendGridApiKey")))
                {
                    await emailQueue.AddAsync(builder.ToString());

                    return new ContentResult
                    {
                        Content = "<h1>Thank you</h1><p>Your token was generated and sent to us.</p>",
                        ContentType = "text/html",
                        StatusCode = 200
                    };
                }

                return new ContentResult
                {
                    Content = $"<h1>Token created</h1>{builder}",
                    ContentType = "text/html",
                    StatusCode = 200
                };
            }
            catch (Exception e)
            {
                log.LogError(e, e.Message);
                return new ContentResult
                {
                    Content = $"<h1>Unable to create token</h1><p>{e.Message}</p>",
                    ContentType = "text/html",
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }
        }

        [FunctionName("Init")]
        public static IActionResult Init([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "init")]
            HttpRequest req, ILogger log)
        {
            var clientId = Config.GetValue<string>("VismaNetClientId") ??
                           throw new InvalidArgumentsException("client-id is missing");
            var callbackUrl = Config.GetValue<string>("VismaNetCallbackUrl") ??
                              throw new InvalidArgumentsException("VismaNetCallbackUrl is missing");
            var redirectUrl = VismaNet.GetOAuthUrl(clientId, callbackUrl);
            return new RedirectResult(redirectUrl);
        }

        [FunctionName("SendTokenByMail")]
        public static void SendTokenByMail(
            [QueueTrigger(SendGridQueue)] string emailContent,
            [SendGrid(From = "%SendgridFrom%", Subject = "%SendgridSubject%",
                To = "%SendgridTo%")]
            out SendGridMessage message)
        {
            message = new SendGridMessage
            {
                HtmlContent = emailContent
            };
        }
    }
}