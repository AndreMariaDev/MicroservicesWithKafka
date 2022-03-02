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
            var resultReader = serviceProvider.GetService<IResultReader>();
            Console.WriteLine("Start App: ResultReader");
            var result = await resultReader.OnMessageConsumer();
            Console.WriteLine($"Result : {result}");

        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
}