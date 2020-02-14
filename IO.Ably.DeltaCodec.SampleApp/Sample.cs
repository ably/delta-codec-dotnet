using System;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;

namespace IO.Ably.DeltaCodec.SampleApp
{
    public class ConsumerSample
    {
        private IMqttClientOptions _options;
        private IMqttClient _client;
        private DeltaDecoder _decoder = new DeltaDecoder();

        public ConsumerSample()
        {
            var factory = new MqttFactory();
            _options = new MqttClientOptionsBuilder()
                .WithClientId("consumer")
                .WithTcpServer("mqtt.ably.io", 8883)
                .WithCredentials("oFpaLg.mB7oiw", "WP5kW-Mrk96MTaFq")
                .WithTls()
                .Build();
            _client = factory.CreateMqttClient();
            _client.UseApplicationMessageReceivedHandler(OnSubscriberMessageReceived);
            _client.UseConnectedHandler(async e =>
            {
                Console.WriteLine("### CONNECTED WITH SERVER ###");
            });
        }

        private string ChannelName => $"[?delta=vcdiff]{Program.ChannelName}";

        public async Task Start()
        {
            Console.WriteLine("Connecting Consumer");
            await _client.ConnectAsync(_options);
            await _client.SubscribeAsync(new TopicFilterBuilder().WithTopic(ChannelName).Build());
            Console.WriteLine("### SUBSCRIBED ###");
        }

        private void OnSubscriberMessageReceived(MqttApplicationMessageReceivedEventArgs x)
        {
            var message = DecodeStringMessage();
            var stats =
                $"Payload Size: {x.ApplicationMessage.Payload.Length}. Decoded Message Size: {message.Length}. Saving: {message.Length - x.ApplicationMessage.Payload.Length}";
            var item = $"Timestamp: {DateTime.Now:O} | Stats: {stats} | Topic: {x.ApplicationMessage.Topic} | Payload: {message} | QoS: {x.ApplicationMessage.QualityOfServiceLevel}";
            Console.WriteLine(item);

            string DecodeStringMessage()
            {
                try
                {
                    var payload = x.ApplicationMessage.Payload;
                    if (DeltaDecoder.IsDelta(payload))
                    {
                        var result = _decoder.ApplyDelta(payload);
                        return result.AsUtf8String();
                    }

                    _decoder.SetBase(payload);

                    return Encoding.UTF8.GetString(payload);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return "";
                }
            }
        }
    }
}