using System;
using System.Text;

namespace IO.Ably.DeltaCodec
{
    /// <summary>
    /// Helpers for converting string and base64 data to byte[] which can be consumed by the <see cref="DeltaDecoder"/>
    /// </summary>
    public static class DataHelpers
    {
        /// <summary>
        /// Converts an object which can be `byte[]`, `utf-8 string` or `base64 encoded string`
        /// to a `byte[]`
        /// </summary>
        /// <param name="data">object which can be `byte[]` or `string`</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">the object is not `byte[]` or `string`</exception>
        public static byte[] ConvertToByteArray(object data)
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
                throw new ArgumentException("data parameter can only be of type `byte[]` or `string`");
            }
        }

        /// <summary>
        /// Try to convert an object of type `byte[]` or `string` to a `byte[]` which can be used for delta calculations
        /// Similar to <see cref="ConvertToByteArray"/> but doesn't throw exception if the incorrect type is passed.
        /// </summary>
        /// <param name="obj">object to be converted.</param>
        /// <param name="delta">resulting `byte[]`.</param>
        /// <returns>`true` or `false` depending on whether the conversion succeeded.</returns>
        public static bool TryConvertToDeltaByteArray(object obj, out byte[] delta)
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

        /// <summary>
        /// Try to convert a `base64` string to `byte[]`.
        /// </summary>
        /// <param name="str">base64 encoded string</param>
        /// <param name="result">resulting byte[]</param>
        /// <returns>`true` or `false` depending on whether the conversion succeeded.</returns>
        public static bool TryConvertFromBase64String(string str, out byte[] result)
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
