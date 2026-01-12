# PowerDown

Automatically shut down your PC when Steam or Epic Games finish downloading and installing games.

## Features

- **Multi-Launcher Support** - Monitor Steam and Epic Games simultaneously
- **Smart Detection** - Tracks both download and installation progress
- **Verification Polling** - Confirms downloads are truly complete before shutting down
- **Cross-Platform Architecture** - Designed for Windows, Linux, and macOS (Windows currently supported)
- **Configurable Delays** - Adjust verification time, polling interval, and required checks
- **Safe Mode** - Dry-run option to test without actual shutdown
- **Graceful Cancellation** - Cancel with CTRL+C during verification period

## Installation

### Build from Source

```bash
git clone https://github.com/yourusername/PowerDown.git
cd PowerDown
dotnet build PowerDown.sln
```

### Run

```bash
cd src/PowerDown.Cli
dotnet run
```

## Usage

### Basic Usage

Monitor both Steam and Epic Games with default settings:

```bash
PowerDown.exe
```

### Advanced Usage

Monitor only Steam:

```bash
PowerDown.exe --steam-only
```

Monitor only Epic Games:

```bash
PowerDown.exe --epic-only
```

Custom verification delay (120 seconds):

```bash
PowerDown.exe --delay 120
```

Verbose logging:

```bash
PowerDown.exe --verbose
```

Custom Steam installation path:

```bash
PowerDown.exe --steam-path "D:\Steam"
```

Custom Epic Games installation path:

```bash
PowerDown.exe --epic-path "E:\Epic Games"
```

Dry-run mode (test without actual shutdown):

```bash
PowerDown.exe --dry-run
```

### Command-Line Options

| Option | Short | Description | Default |
|---------|--------|-------------|----------|
| `--delay` | `-d` | Verification delay in seconds | 60 |
| `--interval` | `-i` | Polling interval in seconds | 10 |
| `--checks` | `-c` | Required consecutive idle checks | 3 |
| `--steam-only` | `-s` | Monitor Steam only | Both |
| `--epic-only` | `-e` | Monitor Epic Games only | Both |
| `--steam-path` | | Custom Steam install directory | Auto-detected |
| `--epic-path` | | Custom Epic Games install directory | Auto-detected |
| `--dry-run` | `-r` | Test mode without actual shutdown | false |
| `--verbose` | `-v` | Enable verbose logging | false |
| `--help` | `-h` | Show help message | |

## How It Works

1. **Detection** - Monitors Steam logs and Epic manifests for download activity
2. **Tracking** - Shows active downloads with progress percentages
3. **Completion** - Detects when all downloads and installations are complete
4. **Verification** - Polls for 60 seconds (configurable) to ensure no new downloads start
5. **Shutdown** - Schedules Windows shutdown after verification passes

## Configuration

### Auto-Detection

PowerDown automatically detects installation paths:

**Steam:**
- Reads from: `HKCU\Software\Valve\Steam\SteamPath`
- Falls back to: `%ProgramFiles%\Steam`

**Epic Games:**
- Parses: `%ProgramData%\Epic\UnrealEngineLauncher\LauncherInstalled.dat`
- Falls back to: `%ProgramFiles%\Epic Games`

### Manual Path Override

If auto-detection fails, provide custom paths:

```bash
PowerDown.exe --steam-path "D:\Steam" --epic-path "E:\Epic Games"
```

## Supported Platforms

### Current

- **Windows** - Full support for Steam and Epic Games

### Future

- **Linux** - Planned support
- **macOS** - Planned support

## Supported Launchers

### Current

- **Steam** - Full detection and monitoring
- **Epic Games** - Full detection and monitoring

### Future

- **GOG Galaxy** - Planned
- **Battle.net** - Planned
- **Xbox Game Pass** - Planned
- **PlayStation** - Evaluated

## Development

See [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md) for development setup and guidelines.

## Architecture

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for technical architecture details.

## Design Decisions

See [docs/DESIGN_DECISIONS.md](docs/DESIGN_DECISIONS.md) for architectural decisions and trade-offs.

## Testing

See [docs/TESTING.md](docs/TESTING.md) for testing strategy and guidelines.

## Roadmap

- [ ] Linux platform support
- [ ] macOS platform support
- [ ] GOG Galaxy integration
- [ ] Battle.net integration
- [ ] Configuration file support
- [ ] GUI application
- [ ] System tray integration
- [ ] Remote control via HTTP API
- [ ] Web dashboard

## Contributing

Contributions are welcome! Please see [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md) for guidelines.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- Steam detection inspired by various Steam community tools
- Epic Games detection using Epic's JSON manifest format
- Built with .NET 8 and System.CommandLine
