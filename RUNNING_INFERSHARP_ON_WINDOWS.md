# How to Run Infer# locally on Windows via WSL2 (Windows Subsystem for Linux)

## Prerequisite
- [Enable WSL2](https://docs.microsoft.com/en-us/windows/wsl/install-win10).

## Setup
1. Download the binaries from [Infer# - v1.1](TBA).
2. From Windows Command Prompt or PowerShell, open your default Linux distribution and enter: ```wsl.exe```.
3. Copy the _infersharp_ folder to _/home/_ and create symlink for future use:
```
cp -r <FOLDER_PATH_TO_INFERSHARP_BINARIES> ~
cd /home/<YOUR_USERNAME>/infersharp/
sudo ln -s /home/<YOUR_USERNAME>/infersharp/infer/lib/infer/infer/bin/infer /usr/local/bin/infer
```
The folder structure should look like this:
```
├── home
    ├── <YOUR_USERNAME>
        ├── infersharp
            ├── Cilsil
            ├── Examples
            ├── infer
            └── run_infersharp.sh
```
4. Run Infer# against examples.
```
./run_infersharp.sh Examples/
```

## Use Infer# on your own code
**Important** - Because of a known performance issue on slow I/O between Windows and Linux file systems, please make sure you are always at _/home/<YOUR_USERNAME>/infersharp/_ before running the analysis.
```
cd /home/<YOUR_USERNAME>/infersharp/
```
Your own code/binaries, however, can be anywhere on your system. For example, if your binaries are at ```C:\Code\MyApp\bin```:
```
./run_infersharp.sh /mnt/c/Code/MyApp/bin/
```