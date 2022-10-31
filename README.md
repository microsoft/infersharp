# ![InferSharp icon](/assets/Market_InferSharp_72.png) InferSharp

**InferSharp** (also referred to as Infer#) is an interprocedural and scalable static code analyzer for C#. Via the capabilities of Facebook's [Infer](https://fbinfer.com/), this tool detects race conditions, null pointer dereferences and resource leaks. It also performs [taint flow tracking](https://en.wikipedia.org/wiki/Taint_checking) to detect critical security vulnerabilities like SQL injections. Read more about our approach in the [Wiki page](https://github.com/microsoft/infersharp/wiki/InferSharp:-A-Scalable-Code-Analytics-Tool-for-.NET).

In addition to implementing the C# frontend, we contributed our [language-agnostic serialization layer](https://github.com/microsoft/infersharp/wiki/InferSharp:-A-Scalable-Code-Analytics-Tool-for-.NET#language-agnostic-representation-of-sil) ([Commit #1361](https://github.com/facebook/infer/commit/285ddb4a98f337a40d61e73b7a0867e44fa4f042)) to facebook/infer, which opens up opportunities for [additional language support](https://github.com/microsoft/infersharp/wiki/InferSharp:-A-Scalable-Code-Analytics-Tool-for-.NET#overview) in the future.

## Public Announcements
- .NET DevBlogs - [v1.4](https://devblogs.microsoft.com/dotnet/slaying-zombie-no-repo-crashes-with-infersharp/), [v1.2](https://devblogs.microsoft.com/dotnet/infer-v1-2-interprocedural-memory-safety-analysis-for-c/), [v1.0](https://devblogs.microsoft.com/dotnet/infer-interprocedural-memory-safety-analysis-for-c/)
- [Facebook Engineering Blog](https://engineering.fb.com/2020/12/14/open-source/infer/)
- [.NET Community Standup](https://youtu.be/cIB4gxqm6EY?list=PLdo4fOcmZ0oX-DBuRG4u58ZTAJgBAeQ-t&t=147)
- Visual Studio Toolbox - [YouTube](https://www.youtube.com/watch?v=yNSJv5wN4OA&feature=youtu.be), [Channel9](https://channel9.msdn.com/Shows/Visual-Studio-Toolbox/Analyzing-Code-with-Infer)

The latest version is ![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/v/release/microsoft/infersharp?include_prereleases). Please refer to the [release page](https://github.com/microsoft/infersharp/releases) for more information on the changes.

## Get Started

- [VS Extension](https://marketplace.visualstudio.com/items?itemName=matthew-jin.infersharp)
- [VSCode Extension](https://marketplace.visualstudio.com/items?itemName=matthew-jin.infersharp-ext)
- [Windows Subsystem for Linux](/RUNNING_INFERSHARP_ON_WINDOWS.md)
- [GitHub Action](https://github.com/marketplace/actions/infersharp)
- [Azure Pipelines](/.build/azure-pipelines-example-multistage.yml)
- [Docker](/RUNNING_IN_DOCKER.md)

## Build from Source
Use this [Dockerfile](/Dockerfile) to build images and binaries from source. It builds the latest code from `microsoft/infersharp:main` + `facebook/infer:main` by default.

## Troubleshooting
Please refer to the [troubleshooting guide](TROUBLESHOOTING.md).

## Contributing

We welcome contributions. Please follow [this guideline](CONTRIBUTING.md).

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft trademarks or logos is subject to and must follow Microsoft's Trademark & Brand Guidelines. Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship. Any use of third-party trademarks or logos are subject to those third-party's policies.

## Security Reporting Instructions

**Please do not report security vulnerabilities through public GitHub issues.** Instead, please follow [this guideline](SECURITY.md).
