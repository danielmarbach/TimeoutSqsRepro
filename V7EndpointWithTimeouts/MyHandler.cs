using System;
using System.Threading.Tasks;
using EndpointWithTimeouts;
using NServiceBus;

namespace V7EndpointWithTimeouts
{
    class MyHandler : IHandleMessages<MyMessage>
    {
        public Task Handle(MyMessage message, IMessageHandlerContext context)
        {
            return Console.Out.WriteLineAsync(message.Payload);
        }
    }
}