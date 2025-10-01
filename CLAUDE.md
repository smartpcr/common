# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.




```bash
# Restore dependencies
dotnet restore
# or
make restore

dotnet build
# or
make build

dotnet test --filter Category=unit_test
# or
make test

dotnet pack -c Release -o ./packages

```

## Testing











