# AWS Cognito Authorization Authentication MVP

Topics Covered
--------------

This is a MVP project on how we can migrate the asp.net Core Identity to AWS Cognito User Pools.
In this walk-through, you’ll build the following – An Amazon Cognito User Pool
to authenticate, store and manage users and configure a ASP.NET MVC .NET Core
Web App that can be hosted in AWS as well as how to do role based authentication
in Amazon Cognito using Cognito Groups.
For the migration process we will use the One-at-a-time user migration method.


In this project you will find:
------------------------------------------
1.  We create a project in .NET Core with local authentication and create authorization (permissions)

2.  Register Users & Roles locally in project.

3.  Manage Users Roles.

4.  Migrate current users to AWS Cognito.

5.  Integrate authentication using AWS Cognito on the same project

6.  Authorize in the project the authenticated users in Cognito

For the migration process we will use the One-at-a-time user migration method:
This approach requires more setup, but it allows users to continue using their existing passwords. Users are migrated into a user pool each time they sign in. When a user signs in, you first try to sign the user in to the user pool. If that sign-in fails, because the user does not exist in the user pool, you sign the user in through the existing user directory, capture the user name and password, and then silently sign them up in the user pool.

When a user is going to sign in, we will start the migration process.
We will create the user, create groups (roles), and add the user to the group.

More info about migration here : https://aws.amazon.com/blogs/mobile/migrating-users-to-amazon-cognito-user-pools/

**Pre-requisites**
----------------------------
1. Need to have an AWS account
2. Create a User Pool in cognito service
3. Create an App client. 
![image](https://user-images.githubusercontent.com/26839748/185515845-24dfb163-8308-449a-9d56-ae93e22f0aaa.png)
4. Create domain name. 
![image](https://user-images.githubusercontent.com/26839748/185515907-9c27b4f0-29e7-4d28-a921-aacce37f7533.png)
5. Auth flows configuration. 
![image](https://user-images.githubusercontent.com/26839748/185515991-9ba39c64-e6ac-46b9-a341-0c9f0a514d9d.png)
6. In the appsettings.json file, under the AuthenticationCognito section, provide values as below.




```

{
   "Logging":{
      "LogLevel":{
         "Default":"Warning"
      }
   },
   "AllowedHosts":"\\*",
   "Authentication":{
    "Cognito": {
      "ClientId": "\\<app client id from AWS Cognito\\>",
      "IncludeErrorDetails": true,
      "MetadataAddress": "https://cognito-idp.\\<your region\\>.amazonaws.com/\\<your-pool id\\>/.well-known/openid-configuration",
      "RequireHttpsMetadata": false,
      "ResponseType": "code",
      "SaveToken": true,
      "TokenValidationParameters": {
        "ValidateIssuer": true
      },
      "AppSignOutUrl": "\\<sign out relative url goes here \\>",
      "CognitoDomain": "\\<cognito domain goes here>\\"
    }
   }
}




```
Note: For the purpose of this lab, we are using the EU-CENTRAL-1 (Frankfurt)
region, please make a note of which region you are working in we creating Url’s
and configuring applications.

Make a note of the following:

-   App Client ID

-   Pool Id

-   Callback Url

-   Sign Out Url

-   Cognito Domain (fully qualified cognito domain url)
-   App client id from Amazon Cognito: This is your app client id which can be
    found by clicking App Clients under General Settings

-   Region: This is the aws region in which you configured amazon cognito
    resources

-   Pool Id: This is the pool id , can be found in the Cognito dashboard by
    clicking General Settings under the title Pool Id

Make sure you review these values for correctness.

Test the application
----------------------------
First start by creating a asp.net Core Identity account.
Then sign in. Go to manage Roles and create roles.
After creating roles go to update field where you can assign roles to the user.
Log out then try to sign in with AWS login form with the same account.


