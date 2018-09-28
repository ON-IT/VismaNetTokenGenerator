# Visma.net token creator #

Have Visma.net create a token for you and send it by email. This way you can send your clients a link and get a token with a list of context by email.

[![Deploy to Azure](http://azuredeploy.net/deploybutton.png)](https://azuredeploy.net/?repository=https://github.com/ON-IT/VismaNetTokenGenerator/)

## Parameters

You'll have to set these parameters as soon as you've installed the app in Azure.

| AppSetting | Description |
| ---------- | ------------ |
| VismaNetClientId | The client Id provided by Visma | 
| VismaNetClientSecret | The client secret provided by Visma |
| VismaNetCallbackUrl | The url to https://[your-app-name].azurewebsites.net/api/callback |
| AzureWebJobsSendGridApiKey | API key from Sendgrid.com (It's a free service for 12k emails a month) |
| SendGridFrom | Who the email should be sent from |
| SendGridTo | Who should receive the email |
| SendGridSubject | Subject for the email |
