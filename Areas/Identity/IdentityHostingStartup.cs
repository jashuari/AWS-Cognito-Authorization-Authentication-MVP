using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AWS.Cognito.Authorization.Authentication.MVP.Data;
using AWS.Cognito.Authorization.Authentication.MVP.Models;

[assembly: HostingStartup(typeof(AWS.Cognito.Authorization.Authentication.MVP.Areas.Identity.IdentityHostingStartup))]
namespace AWS.Cognito.Authorization.Authentication.MVP.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => {
            });
        }
    }
}