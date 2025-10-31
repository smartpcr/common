.PHONY: all restore build test clean publish-monitoring-tools

all: restore build test

restore:
	dotnet restore

build: restore
	dotnet build

test: build
	dotnet test --filter Category=unit_test

pack: build
	dotnet pack -c Release -o ./packages

publish-monitoring-tools: build
	dotnet publish src/Common.Monitoring.Tools/Common.Monitoring.Tools.csproj \
		-c Release \
		-r win-x64 \
		--self-contained true \
		-p:PublishSingleFile=true \
		-p:IncludeNativeLibrariesForSelfContained=true \
		-p:PublishTrimmed=false \
		-p:PublishReadyToRun=true \
		-o ./publish/monitoring-tools-win-x64

clean:
	dotnet clean