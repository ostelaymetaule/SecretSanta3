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
            var token = Environment.GetEnvironmentVariable("bottoken");
            var tgClient = new Telegram.Bot.TelegramBotClient(token);
            var botCaller = new BotCaller(rep, tgClient);
            services.AddSingleton(tgClient);

            services.AddSingleton(botCaller);

            var issuer = Environment.GetEnvironmentVariable("issuer");
            var jwtToken = Environment.GetEnvironmentVariable("token");
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
              .AddJwtBearer(cfg =>
              {
                  cfg.RequireHttpsMetadata = false;
                  cfg.SaveToken = true;

                  cfg.TokenValidationParameters = new TokenValidationParameters()
                  {
                      ValidIssuer = issuer,
                      ValidAudience = issuer,
                      IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtToken))
                  };
              });




            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

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
