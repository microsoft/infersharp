# InferSharp

**InferSharp** (also referred to as Infer#) is an interprocedural and scalable static code analyzer for C#. Via the capabilities of Facebook's [Infer](https://fbinfer.com/), this tool detects null pointer dereferences and resource leak. Read more about our approach in the [Wiki page](https://github.com/microsoft/infersharp/wiki/InferSharp:-A-Scalable-Code-Analytics-Tool-for-.NET).

## Get Started
### GitHub Action
The instructions on how to run Infer# as a GitHub Action can be found here: [Infer# Action](https://github.com/marketplace/actions/infersharp).

### Azure DevOps
An example of how to run Infer# as an Azure DevOps task can be found [here](https://github.com/microsoft/infersharp/blob/main/.build/azure-pipelines-adoexample.yml).
If your existing pipeline runs on Windows or running a multi-stage job is desired, you can refer to the example [here](https://github.com/microsoft/infersharp/blob/main/.build/azure-pipelines-adoexample-multistage.yml).

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

We welcome contributions. Please follow [this guideline](https://github.com/microsoft/infersharp/blob/main/CONTRIBUTING.md).

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft trademarks or logos is subject to and must follow Microsoft's Trademark & Brand Guidelines. Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship. Any use of third-party trademarks or logos are subject to those third-party's policies.

## Security Reporting Instructions

**Please do not report security vulnerabilities through public GitHub issues.** Instead, please follow [this guideline](https://github.com/microsoft/infersharp/blob/main/SECURITY.md).
