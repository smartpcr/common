.PHONY: all restore build test clean

all: restore build test

restore:
	dotnet restore

build: restore
	dotnet build

test: build
	dotnet test --filter Category=unit_test

clean:
	dotnet clean