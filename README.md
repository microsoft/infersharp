# Infer#

**Infer# (InferSharp)** is an interprocedural and scalable static code analyzer for C#. Via the capabilities of Facebook's [Infer](https://fbinfer.com/), this tool detects null pointer dereferences and resource leak. Read more about our approach in the [Wiki page](https://github.com/microsoft/infersharp/wiki/InferSharp).

## Get Started
### GitHub Action
The instructions on how to run Infer# as a GitHub Action can be found here: [C# Code Analyzer](https://github.com/microsoft/CSharpCodeAnalyzer).

### Docker Image
Alternatively, use our Docker image:
```shell
docker pull mcr.microsoft.com/infersharp:latest
```
Start a container in interactive mode, then run the following command in the container:
```shell
sh run_infersharp.sh Examples output
```
To view the bug report:
```shell
cat output/filtered_bugs.txt
```

## Contributing

Please follow [this guideline](https://github.com/microsoft/infersharp/contributing.md)
