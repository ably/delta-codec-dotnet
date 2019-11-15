using System;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

using IO.Ably;
using IO.Ably.Realtime;

using DeltaCodec;
using System.Text;
using Newtonsoft.Json;

namespace TestApp
{
    class Program
    {
        

        static async Task Main(string[] args)
        {
            AblyRealtime ably = new AblyRealtime("HG2KVw.AjZP_A:W7VXUG9yw1-Cza6u");
            IRealtimeChannel channel = ably.Channels.Get("[?delta=vcdiff]delta-sample-app");
            var channelDecoder = new DeltaDecoder();
            channel.Subscribe(message =>
            {
                var data =  message.Data;
                try
                {
                    if(message.Encoding.Contains("vcdiff"))
                    {
                        var bytes = Helpers.ConvertToByteArray(data);
                        Console.WriteLine("Processing delta - Message size: " + bytes.Length);
                        if(DeltaDecoder.IsDelta(bytes) == false)
                            throw new Exception("Something went wrong");

                        var decodedData = channelDecoder.ApplyDelta(bytes).AsByteArray();
                        data = JObject.Parse(UTF8Encoding.UTF8.GetString(decodedData));
                    }
                    else 
                    {
                        var serialisedObject = JsonConvert.SerializeObject(data);
                        channelDecoder.SetBase(serialisedObject.GetBytes());
                    }
                }
                catch (Exception e)
                {
                    /* Delta decoder error */
                    Console.WriteLine(e.Message);
                }

                /* Process decoded data */
                Console.WriteLine(((JObject)data).ToObject<Data>());
            });
            ably.Connection.On(ConnectionEvent.Connected, change =>
            {
                Data data = new Data()
                {
                    foo = "bar",
                    count = 1,
                    status = "active"
                };
                channel.Publish("data", data);
                data.count++;
                channel.Publish("data", data);
                data.status = "inactive";
                channel.Publish("data", data);
            });

            await Task.Run(() =>
            {
                Console.ReadLine();
            });
        }

        class Data
        {
            public string foo;
            public int count;
            public string status;

            public override string ToString()
            {
                return $"foo = {this.foo}; count = {this.count}; status = {this.status}";
            }
        }
    }

    public static class Helpers
    {
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
                throw new ArgumentException();
            }
        }

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
