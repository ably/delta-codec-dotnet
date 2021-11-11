using System;
using System.IO;

namespace IO.Ably.DeltaCodec.Vcdiff
{
    /// <summary>
    /// Cache used for encoding/decoding addresses.
    /// </summary>
    internal sealed class AddressCache
    {
        private const byte SelfMode = 0;
        private const byte HereMode = 1;

        private readonly int _nearSize;
        private readonly int _sameSize;
        private readonly int[] _near;
        private int _nextNearSlot;
        private readonly int[] _same;

        private Stream _addressStream;

        internal AddressCache(int nearSize, int sameSize)
        {
            _nearSize = nearSize;
            _sameSize = sameSize;
            _near = new int[nearSize];
            _same = new int[sameSize*256];
        }

        internal void Reset(byte[] addresses)
        {
            _nextNearSlot = 0;
            Array.Clear(_near, 0, _near.Length);
            Array.Clear(_same, 0, _same.Length);

            _addressStream = new MemoryStream(addresses, false);
        }

        internal int DecodeAddress(int here, byte mode)
        {
            int ret;
            if (mode == SelfMode)
            {
                ret = IoHelper.ReadBigEndian7BitEncodedInt(_addressStream);
            }
            else if (mode == HereMode)
            {
                ret = here - IoHelper.ReadBigEndian7BitEncodedInt(_addressStream);
            }
            else if (mode - 2 < _nearSize) // Near cache
            {
                ret = _near[mode - 2] + IoHelper.ReadBigEndian7BitEncodedInt(_addressStream);
            }
            else // Same cache
            {
                int m = mode - (2 + _nearSize);
                ret = _same[(m * 256) + IoHelper.CheckedReadByte(_addressStream)];
            }

            Update (ret);
            return ret;
        }

        private void Update (int address)
        {
            if (_nearSize > 0)
            {
                _near[_nextNearSlot] = address;
                _nextNearSlot = (_nextNearSlot + 1) % _nearSize;
            }
            if (_sameSize > 0)
            {
                _same[address % (_sameSize * 256)] = address;
            }
        }
    }
}
