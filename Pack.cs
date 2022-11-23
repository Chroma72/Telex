// helper class to build outgoing packets

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace Telex
{
    public static class Pack
    {

        public static ArraySegment<byte> Create(ushort opcode, int count)
        {
            var array = ArrayPool<byte>.Shared.Rent(2 + 2 + count);
            BinaryPrimitives.WriteUInt16LittleEndian(array, (ushort)(2 + count));
            BinaryPrimitives.WriteUInt16LittleEndian(array.AsSpan(2), opcode);
            return new ArraySegment<byte>(array, 0, 2 + 2 + count);
        }

        public static ArraySegment<byte> Create(ushort opcode, string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var buffer = ArrayPool<byte>.Shared.Rent(2 + 2 + bytes.Length); // grab an array from the global array pool, must be put back into the pool (released)
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, (ushort)(2 + bytes.Length));
            BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(2), opcode);
            bytes.CopyTo(buffer, 4);
            return new ArraySegment<byte>(buffer, 0, 2 + 2 + bytes.Length);
        }

        public static void Recycle(ArraySegment<byte> segment)
        {
            if (segment.Array.IsNullOrEmpty()) return;

            ArrayPool<byte>.Shared.Return(segment.Array);
        }
    }
}
