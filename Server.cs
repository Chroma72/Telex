using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Telex
{
    public class Server
    {
        private readonly TcpListener listener;
        private uint guidCounter;
        private readonly ConcurrentDictionary<uint, Connection> connections = new();
        private readonly Channel<Message> receiveChannel = Channel.CreateUnbounded<Message>(new UnboundedChannelOptions() { SingleReader = true });
        private readonly Channel<ArraySegment<byte>> broadcastChannel = Channel.CreateUnbounded<ArraySegment<byte>>(new UnboundedChannelOptions() { SingleReader = true });

        private int messageCounter;
        public int totalMessages = 0;

        public Server(ushort port)
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Server.NoDelay = true;
        }

        public void Start()
        {
            listener.Start();
            _ = Task.Run(() => ListenAsync());
            _ = Task.Run(() => BroadcastAsync());
        } 

        private async ValueTask ListenAsync()
        {
            while (true)
            {
                var client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                client.NoDelay = true;
                uint guid = NextGuid();
                connections[guid] = new Connection(client, guid, receiveChannel);
                await receiveChannel.Writer.WriteAsync(new Message { Type = EventType.Connect, Guid = guid });
            }
        }

        uint NextGuid() => Interlocked.Increment(ref guidCounter);

        public void Broadcast(ArraySegment<byte> segment)
        {
            broadcastChannel.Writer.TryWrite(segment);
        }

        private async ValueTask BroadcastAsync()
        {
            while (true)
            {
                var segment = await broadcastChannel.Reader.ReadAsync().ConfigureAwait(false);
                foreach (Connection connection in connections.Values)
                {
                    connection.SendSync(segment);
                }

                Pack.Recycle(segment);
            }
        }

        public bool SendTo(uint guid, ArraySegment<byte> segment)
        {
            if (connections.ContainsKey(guid))
            {
                connections[guid].Send(segment);
                return true;
            }
            return false;
        }

        // implement throttling method for processing x amount of messages, default is 100 per cycle (might be overkill)
        public bool NextMessage(out Message message, int maxMessages = 100)
        {
            message = default;  // basically set message to null

            // only process maxMessages this cycle
            if (messageCounter >= maxMessages)
            {
                messageCounter = 0;
                return false;
            }

            // we have a valid message
            if (receiveChannel.Reader.TryRead(out message))
            {
                totalMessages++;
                messageCounter++;
                return true;
            }

            // no messages this cycle, reset everything
            messageCounter = 0;
            return false;
        }

        public void Tick(int numProcMessage)
        {

        }
    }
}
