.PHONY: all restore build test clean

all: restore build test

restore:
	dotnet restore

build: restore
	dotnet build

test: build
	dotnet test --filter Category=unit_test

pack: build
	dotnet pack -c Release -o ./packages

clean:
	dotnet clean