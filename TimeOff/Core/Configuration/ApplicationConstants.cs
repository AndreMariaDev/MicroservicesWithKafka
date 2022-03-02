using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using Microsoft.Extensions.Options;
using System.Configuration;
using Microsoft.Extensions.Hosting;

namespace Core.Configuration
{
    public static class ApplicationConstants
    {
        public const string LeaveApplicationsTopicName = "leave-applications";
        public const string LeaveApplicationResultsTopicName = "leave-applications-results";

        public static IConfiguration LoadConfiguration()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json").Build();

            return configuration;
        }
    }

    public class BootstrapServersData
    {
        public string BootstrapServers { get; set; } = default(string);
        public string Url { get; set; } = default(string);
    }
}

