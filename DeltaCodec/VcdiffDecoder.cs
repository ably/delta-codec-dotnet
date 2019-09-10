using System;
using System.IO;
using System.Text;

using Newtonsoft.Json;

namespace DeltaCodec
{
    /// <summary>
    /// VCDIFF decoder capable of processing continuous sequences of consecutively generated VCDIFFs
    /// </summary>
    public class VcdiffDecoder
    {
        private byte[] @base;
        private string baseId;

        /// <summary>
        /// Checks if <paramref name="data"/> contains valid VCDIFF
        /// </summary>
        /// <param name="data">The data to be checked (byte[] or Base64-encoded string)</param>
        /// <returns>True if <paramref name="data"/> contains valid VCDIFF, false otherwise</returns>
        public static bool IsDelta(object data)
        {
            if (TryConvertToDeltaByteArray(data, out byte[] delta))
            {
                return HasVcdiffHeader(delta);
            }

            return false;
        }

        /// <summary>
        /// Applies the <paramref name="delta"/> to the result of applying the previous delta or to the base data if no previous delta has been applied yet.
        /// Base data has to be set by <see cref="SetBase(object, string)"/> before calling this method for the first time. 
        /// </summary>
        /// <param name="delta">The delta to be applied</param>
        /// <param name="deltaId">(Optional) Sequence ID of the current delta application result. If set, it will be used for sequence continuity check during the next delta application</param>
        /// <param name="baseId">(Optional) Sequence ID of the expected previous delta application result. If set, it will be used to perform sequence continuity check agains the last preserved sequence ID</param>
        /// <returns><see cref="DeltaApplicationResult"/> instance</returns>
        /// <exception cref="InvalidOperationException">The decoder is not initialized by calling <see cref="SetBase(object, string)"/></exception>
        /// <exception cref="SequenceContinuityException">The provided <paramref name="baseId"/> does not match the last preserved sequence ID</exception>
        /// <exception cref="ArgumentException">The provided <paramref name="delta"/> is not a valid VCDIFF</exception>
        /// <exception cref="MiscUtil.Compression.Vcdiff.VcdiffFormatException"></exception>
        public DeltaApplicationResult ApplyDelta(object delta, string deltaId = null, string baseId = null)
        {
            if (this.@base == null)
            {
                throw new InvalidOperationException($"Uninitialized decoder - {nameof(SetBase)}() should be called first");
            }
            if (this.baseId != baseId)
            {
                throw new SequenceContinuityException(baseId, this.baseId);
            }
            if (!TryConvertToDeltaByteArray(delta, out byte[] deltaAsByteArray) || !HasVcdiffHeader(deltaAsByteArray))
            {
                throw new ArgumentException($"The provided {nameof(delta)} is not a valid VCDIFF delta");
            }
            using (MemoryStream baseStream = new MemoryStream(@base))
            using (MemoryStream deltaStream = new MemoryStream(deltaAsByteArray))
            using (MemoryStream decodedStream = new MemoryStream())
            {
                MiscUtil.Compression.Vcdiff.VcdiffDecoder.Decode(baseStream, deltaStream, decodedStream);
                this.@base = decodedStream.ToArray();
                this.baseId = deltaId;
                // Return a copy to avoid future delta application failures if the returned array is modified
                return new DeltaApplicationResult(decodedStream.ToArray());
            }
        }

        /// <summary>
        /// Sets the base object used for the next delta application (see <see cref="ApplyDelta(object, string, string)"/>).
        /// </summary>
        /// <param name="newBase">The base object to be set</param>
        /// <param name="newBaseId">(Optional) The <paramref name="newBase"/>'s sequence ID, to be used for sequence continuity checking when delta is applied using <see cref="ApplyDelta(object, string, string)"/></param>
        /// <exception cref="ArgumentNullException">The provided <paramref name="newBase"/> parameter is null.</exception>
        public void SetBase(object newBase, string newBaseId = null)
        {
            if (newBase == null)
            {
                throw new ArgumentNullException($"{nameof(newBase)} cannot be null");
            }

            this.@base = ConvertToByteArray(newBase);
            this.baseId = newBaseId;
        }

        private static bool HasVcdiffHeader(byte[] delta)
        {
            return delta[0] == 0xd6 &&
                   delta[1] == 0xc3 &&
                   delta[2] == 0xc4 &&
                   delta[3] == 0;
        }

        private static byte[] ConvertToByteArray(object data)
        {
            if (data is byte[])
            {
                return data as byte[];
            }
            else if (data is string)
            {
                string dataAsString = data as string;
                return TryConvertFromBase64String(dataAsString, out byte[] result) ? result : Encoding.UTF8.GetBytes(dataAsString);
            }
            else
            {
                return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
            }
        }

        private static bool TryConvertToDeltaByteArray(object obj, out byte[] delta)
        {
            byte[] dataAsByteArray = obj as byte[];
            string dataAsString = obj as string;
            if (dataAsByteArray != null || (dataAsString != null && TryConvertFromBase64String(dataAsString, out dataAsByteArray)))
            {
                delta = dataAsByteArray;
                return true;
            }

            delta = null;
            return false;
        }

        private static bool TryConvertFromBase64String(string str, out byte[] result)
        {
            result = null;
            try
            {
                result = Convert.FromBase64String(str);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
