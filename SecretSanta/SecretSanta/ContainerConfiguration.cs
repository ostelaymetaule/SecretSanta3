﻿// See https://aka.ms/new-console-template for more information
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SecretSanta.Data;
using SecretSanta.Helper;
using Serilog;


public class ContainerConfiguration
{
    public static IServiceProvider Configure(IServiceCollection serviceCollection)
    {
        // The Microsoft.Extensions.DependencyInjection.ServiceCollection
        // has extension methods provided by other .NET Core libraries to
        // regsiter services with DI.
        serviceCollection = new ServiceCollection();

        //TODO validate the sinks settings and move config to a json file
        // Add logging
        serviceCollection.AddSingleton(LoggerFactory.Create(builder =>
        {
            builder
                .AddSerilog(dispose: true);
        }));
        serviceCollection.AddHttpClient();

        // The Microsoft.Extensions.Logging package provides this one-liner
        // to add logging services.
        serviceCollection.AddLogging();

        var containerBuilder = new ContainerBuilder();

        // Once you've registered everything in the ServiceCollection, call
        // Populate to bring those registrations into Autofac. This is
        // just like a foreach over the list of things in the collection
        // to add them to Autofac.
        containerBuilder.Populate(serviceCollection);



        // Make your Autofac registrations. Order is important!
        // If you make them BEFORE you call Populate, then the
        // registrations in the ServiceCollection will override Autofac
        // registrations; if you make them AFTER Populate, the Autofac
        // registrations will override. You can make registrations
        // before or after Populate, however you choose.
        //containerBuilder.RegisterType<MessageHandler>().As<IHandler>();

        // Build configuration
        IConfigurationRoot configuration = new ConfigurationBuilder()

            .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
            .AddJsonFile("appsettings.json", false)
            .Build();
        // Add access to generic IConfigurationRoot
        containerBuilder.RegisterInstance(configuration).As<IConfigurationRoot>();
        // Add app 
        containerBuilder.RegisterType<App>().AsSelf();
        containerBuilder.RegisterType<Repository>().AsSelf();
        var token = Environment.GetEnvironmentVariable("bottoken") ?? ""; //TODO: not forget insert bot token
        containerBuilder.RegisterType<Telegram.Bot.TelegramBotClient>().WithParameter("token", token).AsSelf();
        containerBuilder.Register(c => c.Resolve<IHttpClientFactory>().CreateClient()).As<HttpClient>();
        containerBuilder.RegisterType<Assigner>().AsSelf();
        containerBuilder.RegisterType<ClientTG>().AsSelf();

        //containerBuilder.RegisterType<Telegram.Bot.TelegramBotClient>()

        // Creating a new AutofacServiceProvider makes the container
        // available to your app using the Microsoft IServiceProvider
        // interface so you can use those abstractions rather than
        // binding directly to Autofac.
        var container = containerBuilder.Build();
        var serviceProvider = new AutofacServiceProvider(container);
         
        return serviceProvider;
    }
}