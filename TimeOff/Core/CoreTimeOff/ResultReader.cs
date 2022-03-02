using System.Text.Json;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Models;
using System.Net;
using System.Text;
using Core.Interfaces;
using Core.Configuration;
using Microsoft.Extensions.Options;

namespace Core.CoreTimeOff
{
    public class ResultReader: IResultReader
    {

        private readonly AdminClientConfig _adminClientConfig;
        private readonly SchemaRegistryConfig _schemaRegistryConfig;
        private readonly ConsumerConfig _consumerConfig;
        private readonly ProducerConfig _producerConfig;
        private readonly Queue<KafkaMessage> _leaveApplicationReceivedMessages;
        private readonly StringBuilder _resultText;

        public ResultReader()
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
            this._consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers.BootstrapServers,
                GroupId = "manager",
                EnableAutoCommit = false, //We can turn off the automatic offset persistence process by setting the value of the EnableAutoCommit property to false for better control.
                EnableAutoOffsetStore = false, //We can safely turn off the database feature by setting the value of the property EnableAutoOffsetStore to false.
                // Read messages from start if no commit exists.
                AutoOffsetReset = AutoOffsetReset.Earliest, //We do not want to lose messages if our consumer crashes, so we will set the value of property AutoOffsetReset to AutoOffsetReset.Earliest.
                MaxPollIntervalMs = 10000,
                SessionTimeoutMs = 10000
            };
            this._leaveApplicationReceivedMessages = new Queue<KafkaMessage>();
            this._resultText = new StringBuilder();
        }

        public async Task<List<String>> OnMessageConsumer()
        {
            List<String> result = null;
            try
            {
                result = await StartManagerConsumer();

            }
            catch (System.Exception ex)
            {
                this._resultText.Append($"Non fatal error: {ex}");
            }
            return result;
        }


        private async Task<List<string>> StartManagerConsumer()
        {
            List<string> resultMessage = new List<string>();
            int count = 1;
            using var schemaRegistry = new CachedSchemaRegistryClient(this._schemaRegistryConfig);
            using var consumer = new ConsumerBuilder<string, LeaveApplicationProcessed>(this._consumerConfig)
                .SetKeyDeserializer(new AvroDeserializer<string>(schemaRegistry).AsSyncOverAsync())
                .SetValueDeserializer(new AvroDeserializer<LeaveApplicationProcessed>(schemaRegistry).AsSyncOverAsync())
                .SetErrorHandler((_, e) => Console.WriteLine($"Error: {e.Reason}"))
                .Build();
            {
                try
                {
                    Console.WriteLine("");
                    consumer.Subscribe(ApplicationConstants.LeaveApplicationResultsTopicName);
                    while (count < 100)
                    {
                        var result = consumer.Consume();
                        var leaveRequest = result.Message.Value;
                        resultMessage.Add(JsonSerializer.Serialize(leaveRequest));
                        consumer.Commit(result);
                        consumer.StoreOffset(result);
                        count++;
                    }
                }
                catch (ConsumeException e)
                {
                    Console.WriteLine($"Consume error: {e.Error.Reason}");
                }
                finally
                {
                    consumer.Close();
                }
            }
            return resultMessage;
        }
    }
}
