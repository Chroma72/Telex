using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Telex
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var server = new Server(47000);
            server.Start();

            _ = Task.Run(async () =>
            {
                await Task.Delay(10);
                var client = new TcpClient("127.0.0.1", 47000);

                await Task.Delay(100);

                //while (true)
                //{
                    for (int i = 0; i < 100; i++)
                    {
                        var seg = Pack.Create(0x0100, DateTime.Now.ToString());
                        await client.GetStream().WriteAsync(seg);
                        Pack.Recycle(seg);
                        await Task.Delay(1);
                    }
                //}

                client.Close();
            });

            while (true)
            {

                while (server.NextMessage(out Message message, 200))
                {
                    switch (message.Type)
                    {
                        case EventType.Connect:
                            Console.WriteLine($"Client [{message.Guid}] connected. {message.Data.Count}");
                            break;

                        case EventType.Disconnect:
                            break;

                        case EventType.Receive:
                            Console.WriteLine($"Opcode: {message.Opcode} Message: {Encoding.UTF8.GetString(message.Data)}");
                            message.Recycle();
                            break;

                        case EventType.Timeout:
                            break;
                    }
                }

                server.Tick(100);

                //Console.WriteLine($"{server.totMsg}");

                //byte[] bytes = Pack.FromOpSize(0x00ff, 38);
                //server.Broadcast(bytes);

                System.Threading.Thread.Sleep(1);
            }
        }
    }
}