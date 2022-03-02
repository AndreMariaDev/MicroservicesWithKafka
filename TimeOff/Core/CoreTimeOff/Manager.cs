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
    public record KafkaMessage(string Key, int Partition, LeaveApplicationReceived Message);
    public class Manager : IManager
    {
        private readonly AdminClientConfig _adminClientConfig;
        private readonly SchemaRegistryConfig _schemaRegistryConfig;
        private readonly ConsumerConfig _consumerConfig;
        private readonly ProducerConfig _producerConfig;
        private readonly Queue<KafkaMessage> _leaveApplicationReceivedMessages;
        private readonly StringBuilder _resultText;

        public Manager()
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

        public async Task<String> OnMessageConsumer()
        {

            try
            {
                await Task.WhenAny(Task.Run(StartManagerConsumer), Task.Run(StartLeaveApplicationProcessor));
            }
            catch (System.Exception ex)
            {
                this._resultText.Append($"Non fatal error: {ex}");
            }

            return this._resultText.ToString();
        }

        private async Task<String> StartLeaveApplicationProcessor()
        {
            while (true)
            {
                if (!_leaveApplicationReceivedMessages.Any())
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                    continue;
                }

                var (key, partition, leaveApplication) = _leaveApplicationReceivedMessages.Dequeue();
                this._resultText.AppendLine($"Received message: {key} from partition: {partition} Value: {JsonSerializer.Serialize(leaveApplication)}");

                // Make decision on leave request.
                //var isApproved = StringComparison.OrdinalIgnoreCase;
                await SendMessageToResultTopicAsync(leaveApplication, true, partition);
            }

            // ReSharper disable once FunctionNeverReturns
        }

        private async Task StartManagerConsumer()
        {
            using var schemaRegistry = new CachedSchemaRegistryClient(this._schemaRegistryConfig);
            using var consumer = new ConsumerBuilder<string, LeaveApplicationReceived>(this._consumerConfig)
                .SetKeyDeserializer(new AvroDeserializer<string>(schemaRegistry).AsSyncOverAsync())
                .SetValueDeserializer(new AvroDeserializer<LeaveApplicationReceived>(schemaRegistry).AsSyncOverAsync())
                .SetErrorHandler((_, e) => Console.WriteLine($"Error: {e.Reason}"))
                .Build();
            {
                try
                {
                    //link a consumer to a topic
                    consumer.Subscribe("leave-applications");
                    this._resultText.AppendLine("Consumer loop started...\n");
                    while (true)
                    {
                        try
                        {
                            int maxPollIntervalMs = this._consumerConfig.MaxPollIntervalMs.HasValue ? this._consumerConfig.MaxPollIntervalMs.Value : 1000;
                            // We will give the process 1 second to commit the message and store its offset.
                            var result = consumer.Consume(TimeSpan.FromMilliseconds(maxPollIntervalMs - 1000));
                            var leaveRequest = result?.Message?.Value;
                            if (leaveRequest == null)
                            {
                                continue;
                            }

                            // Adding message to a list just for the demo.
                            // You should persist the message in database and process it later.
                            this._leaveApplicationReceivedMessages.Enqueue(new KafkaMessage(result.Message.Key, result.Partition.Value, result.Message.Value));

                            consumer.Commit(result);
                            consumer.StoreOffset(result);
                        }
                        catch (ConsumeException e) when (!e.Error.IsFatal)
                        {
                            //Console.WriteLine($"Non fatal error: {e}");
                            this._resultText.AppendLine($"Non fatal error: {e}");
                        }
                    }
                }
                catch (ConsumeException e) when (!e.Error.IsFatal)
                {
                    //Console.WriteLine($"Non fatal error: {e}");
                    this._resultText.AppendLine($"Non fatal error: {e}");
                }
                finally
                {
                    consumer.Close();
                }
            }
        }

        private async Task SendMessageToResultTopicAsync(LeaveApplicationReceived leaveRequest, bool isApproved, int partitionId)
        {
            CachedSchemaRegistryClient cachedSchemaRegistryClient = new CachedSchemaRegistryClient(_schemaRegistryConfig);
            using var schemaRegistry = new CachedSchemaRegistryClient(this._schemaRegistryConfig);
            using var producer = new ProducerBuilder<string, LeaveApplicationProcessed>(this._producerConfig)
                .SetKeySerializer(new AvroSerializer<string>(cachedSchemaRegistryClient))
                .SetValueSerializer(new AvroSerializer<LeaveApplicationProcessed>(cachedSchemaRegistryClient))
                .Build();
            {
                var leaveApplicationResult = new LeaveApplicationProcessed
                {
                    EmpDepartment = leaveRequest.EmpDepartment,
                    EmpEmail = leaveRequest.EmpEmail,
                    LeaveDurationInHours = leaveRequest.LeaveDurationInHours,
                    LeaveStartDateTicks = leaveRequest.LeaveStartDateTicks,
                    ProcessedBy = $"Manager #{partitionId}",
                    Result = isApproved
                        ? "Approved: Your leave application has been approved."
                        : "Declined: Your leave application has been declined."
                };

                var result = await producer.ProduceAsync(ApplicationConstants.LeaveApplicationResultsTopicName,
                    new Message<string, LeaveApplicationProcessed>
                    {
                        Key = $"{leaveRequest.EmpEmail}-{DateTime.UtcNow.Ticks}",
                        Value = leaveApplicationResult
                    });
                this._resultText.AppendLine($"\nMsg: Leave request processed and queued at offset {result.Offset.Value} in the Topic {result.Topic}");
            }
        }
    }
}
