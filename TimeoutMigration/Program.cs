using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Linq;
using NHibernate.Mapping.ByCode;
using NServiceBus;
using NServiceBus.DelayedDelivery;
using NServiceBus.DeliveryConstraints;
using NServiceBus.Extensibility;
using NServiceBus.Raw;
using NServiceBus.Routing;
using NServiceBus.Transport;
using Environment = NHibernate.Cfg.Environment;

namespace TimeoutMigration
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            var nhConfig = new Configuration();
            nhConfig.SetProperty(Environment.ConnectionProvider, "NHibernate.Connection.DriverConnectionProvider");
            nhConfig.SetProperty(Environment.ConnectionDriver, "NHibernate.Driver.Sql2008ClientDriver");
            nhConfig.SetProperty(Environment.Dialect, "NHibernate.Dialect.MsSql2008Dialect");
            nhConfig.SetProperty(Environment.ConnectionString, System.Environment.GetEnvironmentVariable("SQLServerConnectionString"));

            var mapper = new ModelMapper();
            mapper.AddMapping<TimeoutEntityMap>();
            var mappings = mapper.CompileMappingForAllExplicitlyAddedEntities();
            nhConfig.AddMapping(mappings);

            var sessionFactory =
                nhConfig.BuildSessionFactory();

            var senderConfig = RawEndpointConfiguration.CreateSendOnly("DUMMY");
            var transport = senderConfig.UseTransport<SqsTransport>();
            transport.UnrestrictedDurationDelayedDelivery();

            var sender = await RawEndpoint.Start(senderConfig)
                .ConfigureAwait(false);

            var ignoreMachineName = true; // Must be true for Sqs
            var now = DateTime.UtcNow;

            using (var session = sessionFactory.OpenStatelessSession())
            {
                var timeoutEntities = await session.Query<TimeoutEntity>().Take(100).ToListAsync();
                var transportOperations = new List<TransportOperation>();

                foreach (var timeout in timeoutEntities)
                {
                    await Console.Out.WriteLineAsync($"Id:{timeout.Id} Time:{timeout.Time:s} Destination:{timeout.Destination} {timeout.Endpoint}").ConfigureAwait(false);

                    var body = timeout.State;
                    var headers = ConvertStringToDictionary(timeout.Headers);
                    var timestamp = timeout.Time;
                    var destination = timeout.Destination;
                    var id = headers["NServiceBus.MessageId"];

                    if (ignoreMachineName)
                    {
                        var at = destination.LastIndexOf("@", StringComparison.InvariantCulture);

                        if (at != -1)
                        {
                            destination = destination.Substring(0, at);
                        }
                    }

                    var age = now - timestamp;

                    var request = new OutgoingMessage(
                        messageId: timeout.Id.ToString(),
                        headers: headers,
                        body: body
                    );

                    var operation = new TransportOperation(
                        request,
                        new UnicastAddressTag(destination)
                    , DispatchConsistency.Default, new List<DeliveryConstraint>
                        {
                            new DoNotDeliverBefore(timestamp)
                        });

                    if (age.Ticks>0)
                    {
                        await Console.Out.WriteLineAsync($"Warning: Message {id} was scheduled for {timestamp} which passed {age} ago.")
                            .ConfigureAwait(false);
                    }

                    transportOperations.Add(operation);


                }
                await sender.Dispatch(
                        outgoingMessages: new TransportOperations(transportOperations.ToArray()),
                        transaction: new TransportTransaction(),
                        context: new ContextBag()
                    )
                    .ConfigureAwait(false);
            }
        }

        static Dictionary<string, string> ConvertStringToDictionary(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return new Dictionary<string, string>();
            }

            // with a hack to replace assembly version
            return ObjectSerializer.DeSerialize<Dictionary<string, string>>(data.Replace("V5EndpointWithTimeouts", "V7EndpointWithTimeouts"));
        }
    }
}