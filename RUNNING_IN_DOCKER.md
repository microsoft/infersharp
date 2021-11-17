# How to Run Infer# Locally in a Docker Container

## Direct Mode

Use Windows Command Prompt or PowerShell:
```
docker run -v <path_to_binary_folder>:/infersharp/binary_path --rm mcr.microsoft.com/infersharp:v1.2 /bin/bash -c "./run_infersharp.sh binary_path; cp infer-out/report.txt /infersharp/binary_path/report.txt"
```

The analysis result `report.txt` will appear at the root of the mounted volume - `path_to_binary_folder` in this case.

## Interactive Mode

```
docker pull mcr.microsoft.com/infersharp:v1.2
docker run -it mcr.microsoft.com/infersharp:v1.2
./run_infersharp.sh Examples
```