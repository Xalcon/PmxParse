using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PmxParse
{
    class PmxUtils
    {
        public static unsafe string ReadString(PmxTextEncoding textEncoding, byte* pointer, out byte* nextPointer)
        {
            var len = *((int*)pointer);
            var str = GetEncoding(textEncoding).GetString(pointer + sizeof(int), len);
            nextPointer = pointer + sizeof(int) + len;
            return str;
        }

        public static unsafe byte* ReadAndIncrement(ref byte* pointer, int length)
        {
            var p = pointer;
            pointer += length;
            return p;
        }
        
        public static unsafe int ReadVarInt(ref byte* pointer, int length)
        {
            switch (length)
            {
                case sizeof(sbyte):
                    return *((sbyte*)PmxUtils.ReadAndIncrement(ref pointer, sizeof(sbyte)));
                case sizeof(ushort):
                    return *((ushort*)PmxUtils.ReadAndIncrement(ref pointer, sizeof(ushort)));
                case sizeof(int):
                    return *((int*)PmxUtils.ReadAndIncrement(ref pointer, sizeof(int)));
                default:
                    throw new NotSupportedException();
            }
        }

        private static Encoding GetEncoding(PmxTextEncoding textEncoding)
        {
            switch (textEncoding)
            {
                case PmxTextEncoding.UTF16:
                    return Encoding.Unicode;
                case PmxTextEncoding.UTF8:
                    return Encoding.UTF8;
                default:
                    throw new ArgumentOutOfRangeException(nameof(textEncoding), textEncoding, null);
            }
        }
    }
}
