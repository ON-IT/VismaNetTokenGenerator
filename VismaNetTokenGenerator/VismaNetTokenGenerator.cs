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

        private static ContentResult CreateTemplatedResult(string title, string content, HttpStatusCode status = HttpStatusCode.OK, string background = "blue-grey darken-1")
        {
            return new ContentResult()
            {
                Content = $"<html>" +
                          $"<head>" +
                          $"<link href=\"https://fonts.googleapis.com/icon?family=Material+Icons\" rel=\"stylesheet\">" +
                          $"<link rel=\"stylesheet\" href=\"https://cdnjs.cloudflare.com/ajax/libs/materialize/1.0.0/css/materialize.min.css\" integrity=\"sha256-OweaP/Ic6rsV+lysfyS4h+LM6sRwuO3euTYfr6M124g=\" crossorigin=\"anonymous\" />" +
                          $"<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"/>" +
                          $"<title>Visma.net Integrations Token Generator</title>" +
                          $"</head>" +
                          $"<body>" +
                          $"<div class='container'>" +
                          $"" +
                          $"<div class='row'>" +
                          $"<div class='col s12 m6 offset-m3'>" +
                          $"<div class='card {background} z-depth-5'>" +
                          $"<div class='card-content white-text'>" +
                          $"<span class='card-title'>{title}</span>" +
                          $"{content}" +
                          $"</div>" +
                          $"</div>" +
                          $"<a href='https://on-it.no' target='_blank'>" +
                          $"<img src=\"https://www.on-it.no/wp-content/themes/on-it/style/images/on_it_logo.png\" width=\"100px\" class='right' />" +
                          $"</a>" +
                          $"</div>" +
                          $"</div>" +
                          $"" +
                          $"</div>" +
                          $"</body>" +
                          $"</html>",
                ContentType = "text/html",
                StatusCode = (int) status
            };
        }

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

                    return CreateTemplatedResult("Thank you", "<p>Your token was generated and sent to us.</p>", background:"green darken-2");
                }
                return CreateTemplatedResult("Thank you", $"<p>{builder}</p>", background:"green darken-2");
            }
            catch (Exception e)
            {
                log.LogError(e, e.Message);
                return CreateTemplatedResult("<i class='material-icons medium right'>warning</i> Unable to create token", $"<p>{e.Message}</p>",
                    HttpStatusCode.InternalServerError, "red darken-2");
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