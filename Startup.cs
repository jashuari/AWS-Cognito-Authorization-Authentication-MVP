using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using AWS.Cognito.Authorization.Authentication.MVP.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AWS.Cognito.Authorization.Authentication.MVP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace AWS.Cognito.Authorization.Authentication.MVP
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));
            services.AddIdentity<User, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddDefaultUI()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();
            services.AddMvc(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser().
                Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            }).AddXmlSerializerFormatters();
            services.AddControllersWithViews();
            services.AddRazorPages();

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            }).AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddOpenIdConnect(options =>
            {
                options.SignInScheme = IdentityConstants.ApplicationScheme;
                options.ResponseType = Configuration["AWS:Authentication:Cognito:ResponseType"];
                options.MetadataAddress = Configuration["AWS:Authentication:Cognito:MetadataAddress"];

                options.ClientId = Configuration["AWS:Authentication:Cognito:ClientId"];
                options.Authority = "https://cognito-idp.eu-central-1.amazonaws.com/eu-central-1_fRGI7k6xQ/";
                options.Events = new OpenIdConnectEvents()
                {
                    OnRedirectToIdentityProviderForSignOut = OnRedirectToIdentityProviderForSignOut
                };

                //enable this code block if you want to leverage Role Based Authorization
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = options.TokenValidationParameters.ValidateIssuer,
                    RoleClaimType = "cognito:groups"
                };
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasClaim(c => c.Type == "cognito:groups" )));
            });

            
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        } 
        private Task OnRedirectToIdentityProviderForSignOut(RedirectContext context)
        {
            context.ProtocolMessage.Scope = "openid";
            context.ProtocolMessage.ResponseType = "code";

            var cognitoDomain = Configuration["AWS:Authentication:Cognito:CognitoDomain"];

            var clientId = Configuration["AWS:Authentication:Cognito:ClientId"];

            var logoutUrl = $"{context.Request.Scheme}://{context.Request.Host}{Configuration["AWS:Authentication:Cognito:AppSignOutUrl"]}";

            context.ProtocolMessage.IssuerAddress = $"{cognitoDomain}/logout?response_type=code&client_id={clientId}&logout_uri={logoutUrl}&redirect_uri={logoutUrl}";

            // delete cookies
            context.Properties.Items.Remove(CookieAuthenticationDefaults.AuthenticationScheme);
            // close openid session
            context.Properties.Items.Remove(OpenIdConnectDefaults.AuthenticationScheme);

            return Task.CompletedTask;
        }
    }
}