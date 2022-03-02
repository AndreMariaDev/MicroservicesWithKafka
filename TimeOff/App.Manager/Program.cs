// See https://aka.ms/new-console-template for more information
using Core.Interfaces;
using CrossCutting.IoC.Configurations;
using Microsoft.Extensions.DependencyInjection;
class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            var services = NativeInjectionBootStrapper.RegisterServices(serviceCollection);
            var serviceProvider = services.BuildServiceProvider();
            var manager = serviceProvider.GetService<IManager>();
            await manager.OnCreateTopic();
            var result = await manager.OnMessageConsumer();
            Console.WriteLine($"Result : {result}");
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
}