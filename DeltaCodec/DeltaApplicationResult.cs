using System.Text;

namespace IO.Ably.DeltaCodec
{
    /// <summary>
    /// Contains and manages the result of delta application
    /// </summary>
    public class DeltaApplicationResult
    {
        private readonly byte[] data;

        internal DeltaApplicationResult(byte[] data)
        {
            this.data = data;
        }
        
        /// <summary>
        /// Exports the delta application result as byte[]
        /// </summary>
        /// <returns>byte[] representation of this delta application result</returns>
        public byte[] AsByteArray()
        {
            return this.data;
        }

        /// <summary>
        /// Exports the delta application result as string assuming the bytes in the result represent 
        /// an UTF-8 encoded string.
        /// </summary>
        /// <returns>The UTF-8 string representation of this delta application result</returns>
        public string AsUtf8String()
        {
            return Encoding.UTF8.GetString(this.data);
        }
    }
}