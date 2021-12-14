# Visma.net token creator #

Have Visma.net create a token for you and send it by email. This way you can send your clients a link and get a token with a list of context by email.

If you want to create a copy of this service you can fork this repository, create a new Azure Functions App and set it to deploy from Github. Set the Application Settings as described below.



[![Deploy to Azure](https://azuredeploy.net/deploybutton.svg)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FON-IT%2FVismaNetTokenGenerator%2Fmaster%2Fazuredeploy.json)


## Application settings

You'll have to set these application settings as soon as you've installed the app in Azure.

| AppSetting | Description |
| ---------- | ------------ |
| VismaNetClientId | The client Id provided by Visma | 
| VismaNetClientSecret | The client secret provided by Visma |
| VismaNetCallbackUrl | The url to https://[your-app-name].azurewebsites.net/api/callback |
| AzureWebJobsSendGridApiKey | API key from Sendgrid.com (It's a free service for 12k emails a month) |
| SendGridFrom | Who the email should be sent from |
| SendGridTo | Who should receive the email |
| SendGridSubject | Subject for the email |
