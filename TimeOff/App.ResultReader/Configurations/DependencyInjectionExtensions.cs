using CrossCutting.IoC.Configurations;
using Microsoft.Extensions.DependencyInjection;

namespace App.ResultReader.Configurations
{
    public static class DependencyInjectionExtensions
    {
        public static void AddDependencyInjection(this IServiceCollection services)
        {
            NativeInjectionBootStrapper.RegisterServices(services);
        }
    }
}
