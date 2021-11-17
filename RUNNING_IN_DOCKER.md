# How to Run Infer# Locally in a Docker Container

## Direct Mode

```
docker run -v <path_to_binary_folder>:/infersharp/binary_path --rm mcr.microsoft.com/infersharp:v1.2 /bin/bash -c "./run_infersharp.sh binary_path; cp infer-out/report.txt /infersharp/binary_path/report.txt"
```

The analysis result `report.txt` will appear at the root of the mounted volume, in this case, `path_to_binary_folder`.

## Interactive Mode

```
docker pull mcr.microsoft.com/infersharp:v1.2
docker run -it mcr.microsoft.com/infersharp:v1.2
./run_infersharp.sh Examples
```