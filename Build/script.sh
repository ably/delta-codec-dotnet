#!/bin/bash

if [ "$DOTNETCORE" == 'true' ]
then
	dotnet test --framework netcoreapp2.0
else
	msbuild /p:Configuration=Release DeltaCodec.sln
	mono ./testrunner/NUnit.ConsoleRunner.3.10.0/tools/nunit3-console.exe ./DeltaCodec.Test/bin/Release/net46/DeltaCodec.Test.dll
fi