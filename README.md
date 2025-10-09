# Dotnet Codec Library for the VCDIFF Delta Format

C# VCDiff decoder library used internally by the Ably client library. The implementation includes the Vcdiff code from "Miscellaneous Utility Library" authored by [Jon Skeet and Marc Gravell](https://jonskeet.uk/csharp/miscutil/) and forked by Ably.

## Supported platforms

The library targets .NET Standard 2.0 which is compatible with cross-platform .Net ecosystem.

## Getting Started

### Cloning the Repository

This repository contains git submodules. To clone the repository with all submodules:

```bash
git clone --recurse-submodules https://github.com/ably/delta-codec-dotnet
```

If you've already cloned the repository without submodules, initialize and update them:

```bash
git submodule update --init --recursive
```

### Updating the Repository

To pull the latest changes including submodule updates:

```bash
git pull --recurse-submodules
```

Or update submodules separately:

```bash
git submodule update --remote --recursive
```

## Building and Testing

### Prerequisites
- .NET 6.0 SDK or later

### Build Commands

Clean the solution:
```bash
dotnet clean
```

Restore dependencies:
```bash
dotnet restore
```

Build the solution:
```bash
dotnet build --configuration Release
```

Run tests:
```bash
dotnet test
```

### Creating NuGet Package

To create a NuGet package:
```bash
dotnet pack --configuration Release
```

To create a NuGet package with a specific version:
```bash
dotnet pack --configuration Release -p:Version=1.0.0
```

The package will be created in `IO.Ably.DeltaCodec/bin/Release/` directory.

## Usage

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
```

## Support, feedback and troubleshooting

Please visit https://ably.com/support for access to our knowledge base and to ask for any assistance.

You can also view the [community reported GitHub issues](https://github.com/ably/delta-codec-dotnet/issues).

To see what has changed in recent versions, see the [CHANGELOG](CHANGELOG.md).

## License

Copyright (c) 2020 Ably Real-time Ltd, Licensed under the Apache License, Version 2.0.  Refer to [LICENSE](LICENSE) for the license terms.
