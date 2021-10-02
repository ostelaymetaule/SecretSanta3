// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;
using Serilog;

Console.WriteLine("Hello, World!");

//add wrapper logging as shown in https://blog.bitscry.com/2017/05/30/appsettings-json-in-net-core-console-app/
// Initialize serilog logger
Log.Logger = new LoggerConfiguration()
     .WriteTo.Console(Serilog.Events.LogEventLevel.Debug)
     .MinimumLevel.Debug()
     .Enrich.FromLogContext()
     .CreateLogger();

ServiceCollection serviceCollection = new ServiceCollection();
var serviceProvider = ContainerConfiguration.Configure(serviceCollection);


try
{
    Log.Information("Starting service");
    await serviceProvider.GetService<App>().Run();
    Log.Information("Ending service");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Error running service");
    throw ex;
}
finally
{
    Log.CloseAndFlush();
}