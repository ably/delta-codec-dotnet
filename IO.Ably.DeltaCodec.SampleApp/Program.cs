﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Diagnostics;

namespace IO.Ably.DeltaCodec.SampleApp
{
    class Program
    {
        public static string ChannelName = "simple-app-mqtt" + Guid.NewGuid().ToString().Split("-").First();

        static async Task Main(string[] args)
        {
            //HookupLogger();
            Console.WriteLine("Using channel: " + ChannelName);
            Console.WriteLine("Args: " + string.Join(",", args));
            bool isBinarySample = args.Length > 0 && args[0] == "binary";

            var factory = new MqttFactory();
            var consumer = new Consumer(factory);
            var producer = new Producer(factory, isBinarySample);

            await consumer.Start();
            Task.Run(() => producer.Start());

            Console.ReadLine();
        }

        private static void HookupLogger()
        {
            MqttNetGlobalLogger.LogMessagePublished += (s, e) =>
            {
                var trace = $">> [{e.TraceMessage.Timestamp:O}] [{e.TraceMessage.ThreadId}] [{e.TraceMessage.Source}] [{e.TraceMessage.Level}]: {e.TraceMessage.Message}";
                if (e.TraceMessage.Exception != null)
                {
                    trace += Environment.NewLine + e.TraceMessage.Exception.ToString();
                }

                Console.WriteLine(trace);
            };
        }
    }

    public class Producer
    {
        private IMqttClientOptions _options;
        private IMqttClient _client;

        private List<string> Messages => new List<string>()
        {
            "Message 1",
            new String('b', 10000), // Small messages don't have delta's calculated for them
            new String('d', 10000),
            "Message 2"
        };

        private List<byte[]> BinaryMessages => new List<byte[]>
        {
            new byte[]
            {
                76, 111, 114, 101, 109, 32, 105, 112, 115, 117, 109, 32, 100, 111, 108, 111, 114, 32, 115, 105, 116, 32,
                97, 109, 101, 116
            },
            new byte[]
            {
                214, 195, 196, 0, 0, 1, 26, 0, 40, 56, 0, 30, 4, 1, 44, 32, 99, 111, 110, 115, 101, 99, 116, 101, 116,
                117, 114, 32, 97, 100, 105, 112, 105, 115, 99, 105, 110, 103, 32, 101, 108, 105, 116, 46, 19, 26, 1, 30,
                0
            },
            new byte[]
            {
                214, 195, 196, 0, 0, 1, 56, 0, 69, 115, 0, 59, 4, 1, 32, 70, 117, 115, 99, 101, 32, 105, 100, 32, 110,
                117, 108, 108, 97, 32, 108, 97, 99, 105, 110, 105, 97, 44, 32, 118, 111, 108, 117, 116, 112, 97, 116,
                32, 111, 100, 105, 111, 32, 117, 116, 44, 32, 117, 108, 116, 114, 105, 99, 101, 115, 32, 108, 105, 103,
                117, 108, 97, 46, 19, 56, 1, 59, 0
            },
        };

        public Producer(MqttFactory factory, bool isBinary)
        {
            _options = new MqttClientOptionsBuilder()
                .WithClientId("producer")
                .WithTcpServer("mqtt.ably.io", 8883)
                .WithCredentials("oFpaLg.mB7oiw", "WP5kW-Mrk96MTaFq")
                .WithTls()
                .Build();
            _client = factory.CreateMqttClient();
            _client.UseConnectedHandler(async e =>
            {
                Console.WriteLine("### CONNECTED WITH SERVER ###");

                if (isBinary)
                {
                    foreach (var message in BinaryMessages)
                    {
                        await _client.PublishAsync(ChannelName, message);
                    }
                }
                else
                {
                    foreach (var message in Messages)
                    {
                        await _client.PublishAsync(ChannelName, message);
                    }
                }
            });
        }

        public async Task Start()
        {
            Console.WriteLine("Connecting Producer");
            var result = await _client.ConnectAsync(_options);
        }

        private string ChannelName => Program.ChannelName;

    }


    public class Consumer
    {
        private IMqttClientOptions _options;
        private IMqttClient _client;
        private DeltaDecoder _decoder = new DeltaDecoder();

        public Consumer(MqttFactory factory)
        {
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

                // Subscribe to a topic

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