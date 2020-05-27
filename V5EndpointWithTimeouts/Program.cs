using System;
using EndpointWithTimeouts;
using NServiceBus;
using NServiceBus.Config;
using NServiceBus.Config.ConfigurationSource;
using NServiceBus.Persistence;

namespace V5EndpointWithTimeouts
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new BusConfiguration();
            configuration.Transactions().DisableDistributedTransactions();

            var transport = configuration.UseTransport<MsmqTransport>();

            configuration.EndpointName("EndpointWithTimeouts");

            var persistence = configuration.UsePersistence<NHibernatePersistence>();
            persistence.ConnectionString(Environment.GetEnvironmentVariable("SQLServerConnectionString"));

            var bus = Bus.Create(configuration).Start();

            for (var i = 0; i < 100; i++)
            {
                var addMinutes = DateTime.UtcNow.AddMinutes(31);
                bus.Defer(addMinutes, new MyMessage()
                {
                    Payload = $"Bla {i} - {addMinutes.ToFileTimeUtc()}"
                });
            }

            Console.WriteLine("Done");
            Console.ReadLine();

            bus.Dispose();
        }
    }

    class ErrorConfig : IProvideConfiguration<MessageForwardingInCaseOfFaultConfig>
    {
        public MessageForwardingInCaseOfFaultConfig GetConfiguration()
        {
            return new MessageForwardingInCaseOfFaultConfig
            {
                ErrorQueue = "error"
            };
        }
    }
}