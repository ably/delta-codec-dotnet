# MiscUtil.Compression.Vcdiff

[![Build Status](https://travis-ci.org/ably-forks/MiscUtil.Compression.Vcdiff.svg?branch=master)](https://travis-ci.org/ably-forks/MiscUtil.Compression.Vcdiff)

C# VCDiff application library authored by [Jon Skeet and Marc Gravell](https://jonskeet.uk/csharp/miscutil/) and forked by Ably

# Release procedure

### Prerequisites
- `dotnet` in PATH
- `git` in PATH

### Checklist

1. .\pack.ps1 -version `(version)`
2. .\publish.ps1 -version `(version)` -apiKey `(ApiKey)`
3. Visit [https://github.com/ably-forks/MiscUtil.Compression.Vcdiff/tags](https://github.com/ably-forks/MiscUtil.Compression.Vcdiff/tags) and create release from the newly created tag