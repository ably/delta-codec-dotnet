#!/bin/bash

if [ "$DOTNETCORE" == 'true' ]
then
	dotnet test
else
	msbuild /p:Configuration=Release MiscUtil.Compression.Vcdiff.sln
	mono ./testrunner/NUnit.ConsoleRunner.3.10.0/tools/nunit3-console.exe ./MiscUtil.Compression.Vcdiff.Test/bin/Release/MiscUtil.Compression.Vcdiff.Test.dll
fi