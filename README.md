# Visma.net token creator #

Have Visma.net create a token for you and send it by email. This way you can send your clients a link and get a token with a list of context by email.

## Parameters

You'll have to set these parameters as soon as you've installed the app in Azure.

| AppSetting | Description |
| ---------- | ------------ |
| client-id | The client Id provided by Visma | 
| client-secret | The client secret provided by Visma |
| callback-url | The url to https://[your-app-name].azurewebsites.net/api/callback |
| AzureWebJobsSendGridApiKey | API key from Sendgrid.com (It's a free service for 12k emails a month) |
| sendgrid-from | Who the email should be sent from |
| sendgrid-to | Who should receive the email |
| sendgrid-subject | Subject for the email |
