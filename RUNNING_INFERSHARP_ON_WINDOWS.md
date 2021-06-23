# How to Run Infer# Locally on Windows via WSL2 (Windows Subsystem for Linux)

## Prerequisite
- [Enable WSL2](https://docs.microsoft.com/en-us/windows/wsl/install-win10).

*We recommend using either the Ubuntu WSL distribution or the Debian WSL distribution.*

## Setup
1. Download the Infer# binaries from the [latest release page](https://github.com/microsoft/infersharp/releases).
2. From Windows Command Prompt or PowerShell, enter ```wsl.exe``` to open your default Linux distribution.
3. Copy the _infersharp_ folder to _/home/<YOUR_USERNAME>_ and create a symlink for future use.

**Important** - Because of a known performance issue on slow I/O between Windows and Linux file systems, copy the binaries only to Linux file system.

```
cp -r <FOLDER_PATH_TO_INFERSHARP_BINARIES> ~
cd /home/<YOUR_USERNAME>/infersharp/
sudo ln -s /home/<YOUR_USERNAME>/infersharp/infer/lib/infer/infer/bin/infer /usr/local/bin/infer
```
The folder structure should look like this:
```
└── home
    └── <YOUR_USERNAME>
        └── infersharp
            ├── Cilsil
            ├── Examples
            ├── infer
            └── run_infersharp.sh
```
4. Run Infer# against examples.
```
./run_infersharp.sh Examples/
```

## Using Infer# on Your Own Code
Navigate to the Infer# binaries:
```
cd /home/<YOUR_USERNAME>/infersharp/
```
Your own code/binaries, however, can be anywhere on the system. For example, if your binaries are at ```C:\Code\MyApp\bin```:
```
./run_infersharp.sh /mnt/c/Code/MyApp/bin/
```