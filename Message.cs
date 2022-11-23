// incoming packets are put into a Message which contains the EventType, player id (Guid), Opcode, and the Data

using System;
using System.Buffers;
using System.Buffers.Binary;

namespace Telex
{
    public struct Message
    {
        public EventType Type { get; init; }
        public uint Guid { get; init; }
        public ushort Opcode { get; init; }
        public ArraySegment<byte> Data { get; init; }

        public Message(EventType type, uint guid, ArraySegment<byte> Data)
        {
            Type = type;
            Guid = guid;
            Opcode = BinaryPrimitives.ReadUInt16LittleEndian(Data.Slice(0, 2)); // extract opcode
            this.Data = Data.Slice(2);                                          // exclude opcode in Data
        }

        public void Recycle()
        {
            if (Data.Array.IsNullOrEmpty()) return;

            ArrayPool<byte>.Shared.Return(Data.Array);
        }
    }

    public enum EventType
    {
        Connect,
        Disconnect,
        Receive,
        Timeout
    }
}
