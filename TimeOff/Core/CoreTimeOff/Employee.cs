using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Core.Configuration;
using Core.Interfaces;
using Microsoft.Extensions.Options;
using Models;
using System.Net;

namespace Core.CoreTimeOff
{
    public class Employee: IEmployee
    {
        private readonly AdminClientConfig _adminClientConfig;
        private readonly SchemaRegistryConfig _schemaRegistryConfig;
        private readonly ProducerConfig _producerConfig;
        public Employee()
        {
            var config = ApplicationConstants.LoadConfiguration();
            var _bootstrapServers = new BootstrapServersData();


            new ConfigureFromConfigurationOptions<BootstrapServersData>(
                config.GetSection("BootstrapServersData")
            ).Configure(_bootstrapServers);

            //this._adminClientConfig = new AdminClientConfig() { BootstrapServers = "127.0.0.1:9092" };
            //this._schemaRegistryConfig = new SchemaRegistryConfig() { Url = "http://127.0.0.1:8081" };

            this._adminClientConfig = new AdminClientConfig() { BootstrapServers = _bootstrapServers.BootstrapServers };
            this._schemaRegistryConfig = new SchemaRegistryConfig() { Url = _bootstrapServers.Url };
            _producerConfig = new ProducerConfig
            {
                BootstrapServers = _bootstrapServers.BootstrapServers,
                // Guarantees delivery of message to topic.
                EnableDeliveryReports = true,
                ClientId = Dns.GetHostName()
            };
        }

        public async Task OnCreateTopic()
        {
            using var adminClient = new AdminClientBuilder(this._adminClientConfig).Build();
            try
            {
                await adminClient.CreateTopicsAsync(new[]
                {
                    new TopicSpecification
                    {
                        Name = "leave-applications",
                        ReplicationFactor = 1,
                        NumPartitions = 3
                    }
                });
            }
            catch (CreateTopicsException e) when (e.Results.Select(r => r.Error.Code)
                .Any(el => el == ErrorCode.TopicAlreadyExists))
            {
                Console.WriteLine($"Topic {e.Results[0].Topic} already exists");
            }
        }

        public async Task<String> OnMessage(Message item)
        {
            using var schemaRegistry = new CachedSchemaRegistryClient(this._schemaRegistryConfig);
            using var producer = new ProducerBuilder<string, LeaveApplicationReceived>(this._producerConfig)
                .SetKeySerializer(new AvroSerializer<string>(schemaRegistry))
                .SetValueSerializer(new AvroSerializer<LeaveApplicationReceived>(schemaRegistry))
                .Build();
            while (true)
            {

                var leaveApplication = new LeaveApplicationReceived
                {
                    EmpDepartment = item.Department.ToString(),
                    EmpEmail = item.Email,
                    LeaveDurationInHours = item.LeaveDurationInHours,
                    LeaveStartDateTicks = item.LeaveStartDate.Ticks
                };
                var partition = new TopicPartition(
                    ApplicationConstants.LeaveApplicationsTopicName,
                    new Partition((int)Enum.Parse<Departments>(item.Department.ToString())));
                var result = await producer.ProduceAsync
                (
                    partition,
                    new Message<string, LeaveApplicationReceived>
                    {
                        Key = $"{item.Email}-{DateTime.UtcNow.Ticks}",
                        Value = leaveApplication
                    }
                );
                return $"\nMsg: Your leave request is queued at offset {result.Offset.Value} in the Topic {result.Topic}:{result.Partition.Value}\n\n";
            }
        }
    }
}
