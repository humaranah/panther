# Panther

A music player just the way I like it.

## Features

- Simple and intuitive interface
- Playlist management
- Support for multiple audio formats and sources
- Audio processing
- Lightweight and fast

## Getting Started

### Prerequisites

This application uses Bass.Net for audio playback. You'll need to provide your Bass.Net credentials during build.

### Build Instructions

1. Install dependencies:

    ```ps
    cd panther
    dotnet restore
    ```

2. Build the application with your Bass.Net credentials:

    ```ps
    dotnet build -p:BassEmail="your-email@example.com" -p:BassKey="your-bass-net-key"
    ```

    Or set environment variables and build:

    ```ps
    $env:BASS_EMAIL="your-email@example.com"
    $env:BASS_KEY="your-bass-net-key"
    dotnet build
    ```

    Note: The credentials are resolved at compile-time and embedded as constants in the compiled assembly.

## Screenshot

Current development status:

![Panther Screenshot](./screenshots/main_window.png)

## License

This project is licensed under the GPLv3 License.
