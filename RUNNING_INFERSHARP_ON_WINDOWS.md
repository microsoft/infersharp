# How to Run Infer# Locally on Windows via WSL2 (Windows Subsystem for Linux)

## Prerequisite
- [Enable WSL2](https://docs.microsoft.com/en-us/windows/wsl/install).

## Getting Started
1. From Windows Command Prompt, download and set up the InferSharp custom distro by executing the following commands:

```
wget https://github.com/microsoft/infersharp/releases/download/v1.3/infersharp-wsl-distro-v1.3.tar.gz && wsl --import infersharp1.3 C:\wslDistroStorage\infersharp1.3 infersharp-wsl-distro-v1.3.tar.gz && rm infersharp-wsl-distro-v1.3.tar.gz
```

2. Launch the InferSharp custrom distro:
```
wsl ~ -d infersharp1.3
```

3. Go to the `infersharp` folder:
```
cd infersharp
```

3. Run Infer# against your binaries. For example, if the binaries are at `C:\Code\MyApp\bin`

```
./run_infersharp.sh /mnt/c/Code/MyApp/bin/
```
