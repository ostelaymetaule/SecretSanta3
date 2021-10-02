using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SecretSanta.Bot.Repository;
using SecretSanta.Bot.Helpers;
using Microsoft.OpenApi.Models;
using SecretSanta3.Extensions;

namespace SecretSanta3
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

            var basepath2 = Directory.GetCurrentDirectory();
            var basepath = Environment.GetEnvironmentVariable("basepath") ?? basepath2;

            var messagesFilePath = Path.Combine(basepath, "SecretSanta", "secretSanta.json");
            var userInfosFilePath = Path.Combine(basepath, "SecretSanta", "secretSantaUserInfos.json");

            IFileRepository rep = new SimpleJsonRepository(messagesFilePath, userInfosFilePath);
            services.AddSingleton<IFileRepository>(rep);

            var token =  Environment.GetEnvironmentVariable("bottoken"); //TODO: not forget insert bot token
            var tgClient = new Telegram.Bot.TelegramBotClient(token);
            var botCaller = new BotCaller(rep, tgClient);
            services.AddSingleton(tgClient);

            services.AddSingleton(botCaller);

            var issuer = Environment.GetEnvironmentVariable("issuer") ?? "cloud.yellowwardrobe.com";
            var jwtToken = Environment.GetEnvironmentVariable("token") ?? "-";
            var key = Encoding.ASCII.GetBytes(jwtToken);

            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Secretsanta Api", Version = "v1" });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    Description = "Authorization header using the Bearer scheme",
                    Name = "Authorization",
                    In = ParameterLocation.Header
                });

                c.DocumentFilter<SwaggerSecurityRequirementsDocumentFilter>();

            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                //c.RoutePrefix = string.Empty;
                //important: the path is fucked up 
                c.SwaggerEndpoint("./v1/swagger.json", "Secretsanta Api V1");
                //string swaggerJsonBasePath = string.IsNullOrWhiteSpace(c.RoutePrefix) ? "." : "..";
                //c.SwaggerEndpoint($"{swaggerJsonBasePath}/swagger/v1/swagger.json", "My API");

            });

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

        }
    }
}
