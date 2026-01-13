# PowerDown Documentation

Welcome to the PowerDown documentation. This repository contains comprehensive documentation for developers and users.

## Documentation Index

- **[README.md](../README.md)** - Project overview, features, installation, and usage
- **[ARCHITECTURE.md](ARCHITECTURE.md)** - Technical architecture, component design, and data flow
- **[DESIGN_DECISIONS.md](DESIGN_DECISIONS.md)** - Architectural decisions and design rationale (ADR format)
- **[API.md](API.md)** - Public API documentation and extension points
- **[DEVELOPMENT.md](DEVELOPMENT.md)** - Development setup, coding standards, and contribution guide
- **[TESTING.md](TESTING.md)** - Testing strategy, running tests, and writing tests

## Quick Start

### For Users

1. [Build from source](../README.md#installation)
2. [Run with default settings](../README.md#basic-usage)
3. [Customize with command-line options](../README.md#command-line-options)

### For Developers

1. Read [ARCHITECTURE.md](ARCHITECTURE.md) to understand the system design
2. Follow [DEVELOPMENT.md](DEVELOPMENT.md) to set up your development environment
3. Review [TESTING.md](TESTING.md) to understand the testing approach
4. Check [DESIGN_DECISIONS.md](DESIGN_DECISIONS.md) for architectural rationale
5. Start contributing!

## Key Concepts

### Separation of Concerns

PowerDown is organized into four layers:

1. **Abstractions** - Platform-agnostic interfaces and models
2. **Core** - Shared business logic and orchestration
3. **Platform** - OS-specific implementations (Windows, Linux, macOS)
4. **CLI** - Command-line interface and user interaction

### Multi-Launcher Support

The system is designed to support multiple game launchers:

- **Steam** - Monitors logs, manifests, and download directories
- **Epic Games** - Monitors manifests and installation files
- **Future** - GOG, Battle.net, Xbox, PlayStation (planned)

### Cross-Platform Architecture

Platform-specific code is isolated in separate projects:

- `PowerDown.Platform.Windows` - Windows implementations
- `PowerDown.Platform.Linux` - Linux implementations (planned)
- `PowerDown.Platform.MacOS` - macOS implementations (planned)

Core logic remains platform-agnostic, enabling easy addition of new platforms.

## Getting Help

- **Documentation** - Check the relevant documentation file for your question
- **Issues** - Report bugs or request features on GitHub Issues
- **Discussions** - Ask questions or share ideas in GitHub Discussions

## Document Versions

This documentation is for PowerDown v1.0.0.

For older versions, check the Git tags and switch to the appropriate branch.
