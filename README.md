# ZinC

The simplest 'C' build tool around.

## Overview

ZinC is a lightweight, cross-platform build tool for C projects that simplifies the build process without requiring complex configuration. Built with .NET 10 and compiled to native code using AOT, ZinC provides a modern CLI experience for managing C projects.

## Features

- **Simple Project Setup**: Initialize new C projects with a single command
- **Cross-Platform Support**: Build for Windows, Linux, macOS, and WebAssembly
- **Multiple Toolchains**: Support for GCC, Clang, MSVC, and more
- **IDE Integration**: Generate `compile_commands.json` for IntelliSense support
- **Build Modes**: Debug and release configurations
- **Customizable Toolchains**: Eject and customize toolchain configurations
- **Native Performance**: AOT-compiled for fast startup and execution

## Installation

Build from source:

```bash
dotnet publish ZinC.Cli/ZinC.Cli.csproj -c Release
```

The compiled `zinc` executable will be available in the publish output directory.

## Usage

### Setup a New Project

```bash
zinc setup <project-type> --name <artifact-name>
```

Create a new C project with the specified type and name.

### Build a Project

```bash
zinc build <toolchain> <platform> <mode> [options]
```

**Arguments:**
- `toolchain`: The toolchain to use (gcc, clang, msvc, etc.)
- `platform`: Target platform (windows, linux, macos, wasm, etc.)
- `mode`: Build mode (debug, release, etc.)

**Options:**
- `--run`, `-r`: Run the artifact after building
- `--compile-commands`, `-cc`: Generate compile_commands.json for IDE intellisense
- `--verbose`, `-v`: Show full compile and link commands

**Example:**
```bash
zinc build gcc windows debug --run
```

### Generate Compile Commands

```bash
zinc compile-commands <toolchain> <platform> <mode>
```

Generate a `compile_commands.json` file for IDE IntelliSense support.

### Configure Toolchains

```bash
zinc configure <toolchain>
```

Eject a toolchain configuration file for customization.

### List Toolchains

```bash
zinc toolchains [toolchain]
```

List all available toolchains or show details for a specific toolchain.

## Project Structure

After setup, a typical ZinC project contains:

- **zinc.json**: Project configuration file
- **src/**: Source code directory
- **build/**: Build output directory (generated)
- **compile_commands.json**: IDE integration file (optional)

## Requirements

- .NET 10.0 Runtime (for development)
- Appropriate toolchain installed (GCC, Clang, MSVC, etc.)

## License

[License information not specified - Yet]