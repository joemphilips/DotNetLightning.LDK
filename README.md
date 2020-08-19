
# NRustLightning: lightning network toolkit and daemon for .NET Core

powered by [rust-lightning](https://github.com/rust-bitcoin/rust-lightning)

# Features

* Security focused
  * No dependencies besides Microsoft OSS (e.g. AspNetCore) and [Nicolas Dorier](https://github.com/NicolasDorier)'s work (e.g. NBitcoin)
  * Extensive tests, including integration tests against other LN node implementation.
  * encrypt node master secret by user-provided password (pin) before storing it to the disk.
* Fully configurable
  * You can configure every settings for [rust-lightning configuration](https://docs.rs/lightning/0.0.11/lightning/util/config/index.html) as an CLI option or Environment variable.
  * For other configuration options, please can check the help message.
* Loosely coupled architecture.
  * You can install part of the package if you want to build your own LN wallet

## Compilation

Basically all you need to compile the server is

1. Latest Version of DotNet SDK
2. nightly version of the Cargo

check out [the Dockerfile](./src/NRustLightning.Server/Dockerfile) for the detail


## Code organization

from low level to high level...

* `src/NRustLightning`
  * library to interoperate with rust-lightning through abi.
* `src/NRustLightning.Net`
  * library to connect above package to the transport layer. primarily through TCP socket.
  * Probably this is the one you want to use if you are considering to build your own wallet.
* `src/NRustLightning.Infrastructure`
  * library which contains logic for both server and client.
* `src/NRustLightning.Server`
  * standalone LN daemon built with asp.net core server.
* `src/NRustLightning.Client`
  * C# client library for the server.
* `src/NRustLightning.CLI`
  * command-line application to work with the server. Which wraps the client.
  * This is still pretty much WIP


## How to configure the server

`NRustLightning.Server` takes configuration options by either

* cli argument (e.g. `--https.port=443`)
* Environment variable  (e.g. `NRUSTLIGHTNING_HTTPS_PORT=443`)
* configuration ini file (WIP)

You can check all configuration options by running the following command.

```
git clone --recursive <this repository url>
dotnet run --project src/NRustLightning.Server -- --help 
```

You must make sure to connect to your [nbxplorer](https://github.com/dgarage/NBXplorer) instance by options which starts from `nbx`

If you don't have any, you can try with regtest in docker-compose. See below.



## REST API

See [here for the swagger documentation for the server API](https://joemphilips.github.io/NRustLightning/dist/)



You can check the document in local if you are running in debug build. If you want to check it out quickly, first run regtest server by

```
source tests/NRustLightning.Server.Tests/env.sh
docker-compose -f tests/NRustLightning.Server.Tests/docker-compose.yml up
```

and access to `http://localhost:10320/swagger/`


## Single file executable

`NRustLightning.CLI` and `NRustLightning.Server` supports single file executable.
Run

```
dotnet publish -c Release -r <your RID>
```

for which RID to use, see [microsoft official](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog)

For command line parsing, it uses [System.CommandLine](https://github.com/dotnet/command-line-api/).
If you think parsing is not working as expected, Directives in `System.CommandLine` might be useful. for ecample invoking with '[parse]' directive will be like...

```
cd src/NRustLightning.Server
dotnet run -- '[parse]' --regtest <whatever-options-you-want-to-pass>
```

## Test organization

From low level to high level ...

* `NRustLightning.Tests.Common`
  * Common classes which used in more than two test packages.
* `NRustLightning.Tests`
  * basic tests for ABI.
* `NRustLightning.Net.Tests`
  * Tests which simulates P2P Network in memory. This cannot test the behavior with other lightning implementation.
* `NRustLightning.Server.Tests`
  * integration tests for the server and the client. See its own README.md for the detail.
  

## How to test the server

```
source tests/NRustLightning.Server.Tests/env.sh
docker-compose -f tests/NRustLightning.Server.Tests/docker-compose.yml up
./tests/NRustLightning.Server.Tests/docker-lnd-cli.sh
```

