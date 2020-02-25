using System;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;

namespace IO.Ably.DeltaCodec.SampleApp
{
    // This code can be found in the Main repo README file. 
    // It's here to make sure it compiles
    public class ConsumerSample
    {
        private IMqttClientOptions _options;
        private IMqttClient _client;
        private DeltaDecoder _decoder = new DeltaDecoder();

        public ConsumerSample()
        {
            var ablyApiKey = "REPLACE WITH API KEY";
            var keyParts = ablyApiKey.Split(":");
            var factory = new MqttFactory();
            _options = new MqttClientOptionsBuilder()
                .WithClientId("consumer")
                .WithTcpServer("mqtt.ably.io", 8883)
                .WithCredentials(keyParts[0], keyParts[1])
                .WithTls()
                .Build();
            _client = factory.CreateMqttClient();
            _client.UseApplicationMessageReceivedHandler(OnSubscriberMessageReceived);
            _client.UseConnectedHandler(e =>
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

        private void OnSubscriberMessageReceived(MqttApplicationMessageReceivedEventArgs messageArgs)
        {
            var message = DecodeStringMessage();
            var stats =
                $"Payload Size: {messageArgs.ApplicationMessage.Payload.Length}. Decoded Message Size: {message.Length}. Saving: {message.Length - messageArgs.ApplicationMessage.Payload.Length}";
            var item = $"Timestamp: {DateTime.Now:O} | Stats: {stats} | Topic: {messageArgs.ApplicationMessage.Topic} | Payload: {message} | QoS: {messageArgs.ApplicationMessage.QualityOfServiceLevel}";
            Console.WriteLine(item);

            string DecodeStringMessage()
            {
                try
                {
                    var payload = messageArgs.ApplicationMessage.Payload;
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