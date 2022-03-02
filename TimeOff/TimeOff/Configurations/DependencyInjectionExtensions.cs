using CrossCutting.IoC.Configurations;

namespace TimeOff.Configurations
{
    public static class DependencyInjectionExtensions
    {
        public static void AddDependencyInjection(this IServiceCollection services)
        {
            NativeInjectionBootStrapper.RegisterServices(services);
        }
    }
}
