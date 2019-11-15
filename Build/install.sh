#!/bin/bash

if [ "$DOTNETCORE" == 'true' ]
then
	dotnet restore
else
	nuget restore DeltaCodec.sln
	nuget install NUnit.ConsoleRunner -Version 3.10.0 -OutputDirectory testrunner
fi