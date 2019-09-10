using System;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

using IO.Ably;
using IO.Ably.Realtime;

using DeltaCodec;

namespace TestApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            AblyRealtime ably = new AblyRealtime("HG2KVw.AjZP_A:W7VXUG9yw1-Cza6u");
            IRealtimeChannel channel = ably.Channels.Get("[?delta=vcdiff]delta-sample-app");
            VcdiffDecoder channelDecoder = new VcdiffDecoder();
            channel.Subscribe(message =>
            {
                object data = message.Data;
                try
                {
                    if (VcdiffDecoder.IsDelta(data))
                    {
                        data = channelDecoder.ApplyDelta(data).AsObject();
                    }
                    else
                    {
                        channelDecoder.SetBase(data);
                    }
                }
                catch (Exception e)
                {
                    /* Delta decoder error */
                }

                /* Process decoded data */
                Console.WriteLine(JsonHelper.DeserializeObject<Data>(data as JObject));
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
}
