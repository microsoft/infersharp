# How to Run Infer# Locally on Windows via WSL2 (Windows Subsystem for Linux)

## Prerequisite
- [Enable WSL2](https://docs.microsoft.com/en-us/windows/wsl/install).

**Please use the _Ubuntu-20.04_ WSL distribution or the _Debian_ WSL distribution.**

## Setup
1. From Windows Command Prompt or PowerShell, enter `wsl.exe` to open your default Linux distribution.
2. Execute the following commands:

```
cd ~ && wget https://github.com/microsoft/infersharp/releases/download/v1.2/infersharp-linux64-v1.2.tar.gz && tar -xvzf infersharp-linux64-v1.2.tar.gz && cd infersharp
```

3. Run Infer# against your binaries, for example.

```
./run_infersharp.sh /mnt/c/Code/MyApp/bin/
```
