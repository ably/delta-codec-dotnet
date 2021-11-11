using System.IO;

namespace IO.Ably.DeltaCodec.Vcdiff
{
    /// <summary>
    /// A few IO routines to make life easier. Most are basically available
    /// in EndianBinaryReader, but having them separately here makes VcdiffDecoder
    /// more easily movable to other places - and no endianness issues are involved in
    /// the first place.
    /// </summary>
    internal static class IoHelper
    {
        internal static byte[] CheckedReadBytes(Stream stream, int size)
        {
            byte[] ret = new byte[size];
            int index=0;
            while (index < size)
            {
                int read = stream.Read(ret, index, size-index);
                if (read == 0)
                {
                    throw new EndOfStreamException
                        ($"End of stream reached with {size - index} byte{(size - index == 1 ? "s" : "")} left to read.");
                }
                index += read;
            }
            return ret;
        }

        internal static byte CheckedReadByte (Stream stream)
        {
            int b = stream.ReadByte();
            if (b == -1)
            {
                throw new IOException ("Expected to be able to read a byte.");
            }
            return (byte)b;
        }

        internal static int ReadBigEndian7BitEncodedInt(Stream stream)
        {
            int ret = 0;
            for (int i = 0; i < 5; i++)
            {
                int b = stream.ReadByte();
                if (b == -1)
                {
                    throw new EndOfStreamException();
                }
                ret = (ret << 7) | (b&0x7f);
                if ((b & 0x80) == 0)
                {
                    return ret;
                }
            }
            // Still haven't seen a byte with the high bit unset? Dodgy data.
            throw new IOException("Invalid 7-bit encoded integer in stream.");
        }
    }
}
