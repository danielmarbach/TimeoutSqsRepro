using System;
using System.Threading.Tasks;
using Amazon.Util;
using NServiceBus;

namespace V7EndpointWithTimeouts
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            var configuration = new EndpointConfiguration("EndpointWithTimeouts");
            configuration.EnableInstallers();
            configuration.AuditProcessedMessagesTo("EndpointWithTimeouts.Audits");

            var transport = configuration.UseTransport<SqsTransport>();
            transport.UnrestrictedDurationDelayedDelivery();

            var persistence = configuration.UsePersistence<InMemoryPersistence>();


            await Endpoint.Start(configuration);

            Console.ReadLine();
        }
    }
}