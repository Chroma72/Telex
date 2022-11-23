using System;
using System.Buffers;
using System.Net.Sockets;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Telex
{
    internal class Connection
    {
        private readonly TcpClient client;
        private readonly uint guid;
        private readonly Channel<Message> receiveChannel;   // messages received are fed directly to the server receive channel
        private readonly Channel<ArraySegment<byte>> sendChannel = Channel.CreateUnbounded<ArraySegment<byte>>(new UnboundedChannelOptions() { SingleReader = true });

        public Connection(TcpClient client, uint guid, Channel<Message> receiveChannel)
        {
            this.client = client;
            this.guid = guid;
            this.receiveChannel = receiveChannel;
            Task.Run(() => ReceiveAsync());
            Task.Run(() => SendAsync());
        }

        private async ValueTask ReceiveAsync()
        {
            int bytesRead, messageLength;
            NetworkStream stream = client.GetStream();
            byte[] messageLengthBuffer = new byte[2];
            byte[] buffer;

            while (true)
            {
                try
                {
                    bytesRead = await stream.ReadAsync(messageLengthBuffer).ConfigureAwait(false);
                    if (bytesRead == 0) break;

                    messageLength = BitConverter.ToUInt16(messageLengthBuffer);

                    buffer = ArrayPool<byte>.Shared.Rent(messageLength);
                    var segment = new ArraySegment<byte>(buffer, 0, messageLength);

                    bytesRead = await stream.ReadAsync(segment).ConfigureAwait(false);
                    if (bytesRead == 0) break;

                    await receiveChannel.Writer.WriteAsync(new Message(EventType.Receive, guid, segment)).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    break;
                }
            }
            // connection has closed or user disconnected 
            Console.WriteLine($"Connection from [{client.Client.RemoteEndPoint}] has disconnected.");
        }

        public void Send(ArraySegment<byte> segment)
        {
            sendChannel.Writer.TryWrite(segment);
        }

        private async ValueTask SendAsync()
        {
            NetworkStream stream = client.GetStream();

            while (true)
            {
                ArraySegment<byte> segment = await sendChannel.Reader.ReadAsync().ConfigureAwait(false);
                await stream.WriteAsync(segment).ConfigureAwait(false);
            }
        }

        public void SendSync(ArraySegment<byte> segment)
        {
            client.GetStream().Write(segment);
        }
    }
}
