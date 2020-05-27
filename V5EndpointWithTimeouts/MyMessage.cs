using NServiceBus;

namespace EndpointWithTimeouts
{
    public class MyMessage : ICommand
    {
        public string Payload { get; set; }
    }
}