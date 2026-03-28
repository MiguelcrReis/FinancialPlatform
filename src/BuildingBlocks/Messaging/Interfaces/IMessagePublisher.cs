using System;
using System.Collections.Generic;
using System.Text;

namespace BuildingBlocks.Messaging.Interfaces
{
    public interface IMessagePublisher
    {
        Task PublishAsync<T>(string routingKey, T message);
    }
}
