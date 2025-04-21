# Changelog

## [1.0.0] - Initial Release
### Added
- TCP client implementation with support for authentication, joining channels, and sending messages.
- Command-line argument parsing for protocol (`-t`), server address (`-s`), and port (`-p`).
- Graceful exit handling with `Ctrl+C`.
- State-based architecture (`StartState`, `AuthState`, `JoinState`, `OpenState`).
- Input validation for required arguments and port number.
- Manual and automated test cases for TCP functionality.
- Not functional UDP client, issues with recieving UDP packets, attempts to fix this resulted in breaking message sending as well.