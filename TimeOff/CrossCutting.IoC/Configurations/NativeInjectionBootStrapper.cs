using Core.CoreTimeOff;
using Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CrossCutting.IoC.Configurations
{
    public static class NativeInjectionBootStrapper
    {
        public static IServiceCollection RegisterServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IEmployee, Employee>();
            serviceCollection.AddScoped<IManager, Manager>();
            serviceCollection.AddScoped<IResultReader, ResultReader>();
            return serviceCollection;
        }
    }
}
