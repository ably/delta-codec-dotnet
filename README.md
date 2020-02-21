# Dotnet Codec Library for the VCDIFF Delta Format

C# VCDiff decoder library used internally by the Ably client library. The implementation includes the Vcdiff code from "Miscellaneous Utility Library" authored by [Jon Skeet and Marc Gravell](https://jonskeet.uk/csharp/miscutil/) and forked by Ably.

## Supported platforms

The library is targeting netstandard 1.3.

## General Use

The `DeltaDecoder` class is an entry point to the public API. It provides a stateful way of applying a stream of `vcdiff` deltas.

`DeltaDecoder` can do the necessary bookkeeping in the scenario where a number of successive deltas/patches have to be applied where each of them represents the difference to the previous one (e.g. a sequence of messages each of which represents a set of mutations to a given object; i.e. sending only the mutations of an object instead the full object each time).

In order to benefit from the bookkeeping provided by the `DeltaDecoder` class, one has to first provide the base payload that the first delta would be generated against. That could be done using the `SetBase` method. `SetBase` method accepts a `byte[]` for the base payload. We have some helper methods you can use `DataHelpers.ConvertToByteArray` and `DataHelpers.TryConvertToDeltaByteArray`. These will convert object which can be either `byte[]`, `utf-8 string` or `base64 encoded string`. Usually the base payload comes from string or binary transport so it can be used easily.
The most simple flavor of `setBase` is:

```
var decoder = new DeltaDecoder();
byte[] basePayload = GetPayload(); // Get the first payload
decoder.SetBase(basePayload);
```

Once the decoder is initialized like this it can be used to apply a stream of deltas/patches each one resulting in a new full payload.

```
var result = decoder.ApplyDelta(vcdiffDelta);
```

`ApplyDelta` could be called as many times as needed. The `DeltaDecoder` will automatically retain the last delta application result and use it as a base for the next delta application. Thus it allows applying an infinite sequence of deltas.

`result` would be of type `DeltaApplicationResult`. That is a convenience class that allows interpreting the result in various data formats - string, byte[], etc.

`DeltaDecoder` also supports delta streams that have unique Ids. You can call `SetBase` and `ApplyDelta` passing these ids and the decoder will validate whether the supplied Id is the same as what is expected. E.g.


```
decoder.SetBase(payload, baseId);

var result = decoder.ApplyDelta(vcdiffDelta,
                deltaID,/*any unique identifier of the delta there might be*/
                baseID /*any unique identifier of the object this delta was generated against there might be */);
```

## Common Use Cases

### MQTT with Binary or String Payload

The samples are using [MQTTnet](https://www.nuget.org/packages/MQTTnet/) nuget package.

```
using System;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;

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
```

## Support, feedback and troubleshooting

Please visit http://support.ably.io/ for access to our knowledge base and to ask for any assistance.

You can also view the [community reported GitHub issues](https://github.com/ably/delta-codec-dotnet/issues).

To see what has changed in recent versions, see the [CHANGELOG](CHANGELOG.md).

## Contributing

1. Fork it
2. Create your feature branch (`git checkout -b my-new-feature`)
3. Commit your changes (`git commit -am 'Add some feature'`)
4. Ensure you have added suitable tests and the test suite is passing(`dotnet test`)
5. Push to the branch (`git push origin my-new-feature`)
6. Create a new Pull Request

## Release Process

- Make sure the tests are passing in ci for the branch you're building
- Update the CHANGELOG.md with any customer-affecting changes since the last release

## License

Copyright (c) 2019 Ably Real-time Ltd, Licensed under the Apache License, Version 2.0.  Refer to [LICENSE](LICENSE) for the license terms.
