# How to Run Infer# Locally in a Docker Container

## Direct Mode

Use Windows Command Prompt or PowerShell:
```
docker run -v <path_to_binary_folder>:/infersharp/binary_path --rm mcr.microsoft.com/infersharp:v1.4 /bin/bash -c "./run_infersharp.sh binary_path; cp infer-out/report.txt /infersharp/binary_path/report.txt"
```

For example, `path_to_binary_folder` is `C:\Project1\bin`, the analysis result `report.txt` will appear at the root of the mounted volume - `C:\Project1\bin` in this case.

## Interactive Mode

```
docker pull mcr.microsoft.com/infersharp:v1.4
docker run -it mcr.microsoft.com/infersharp:v1.4
./run_infersharp.sh Examples
```

You can copy your binaries to the container and run `./run_infersharp.sh Project1/bin` for example.
