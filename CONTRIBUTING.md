# Contribution Guidelines


## Contributing to Infer#

To contribute, fork InferSharp and file a [pull request](https://github.com/microsoft/infersharp/pulls).

When contributing to OCaml source code, please follow Infer's [contribution guidelines](https://github.com/facebook/infer/blob/master/CONTRIBUTING.md).

### Prerequisites

* [.NET Core 2.2](https://dotnet.microsoft.com/download/dotnet-core/2.2)

* Packages identified in our dockerfile [here](https://github.com/microsoft/infersharp/blob/main/Dockerfile)

### Installation and Build

Both the C# and the OCaml components must be separately built. Each set of build commands is assumed to be executed from the repository root.

**OCaml**

```bash
cd infer
./build-infer.sh ./autogen.sh
./build-infer.sh java
./autogen.sh
sudo make install 
```

**C#**

For the core translation pipeline:
```bash
dotnet build Infersharp.sln
```

For C# library models (optional but highly recommended for reducing false positive warnings):

```bash
./build_csharp_models.sh
``` 

### Debugging Infer#

To obtain an analysis on a directory tree of .NET binaries (comprised of both DLLs and PDBs), execute the following commands from the repository root:
```bash
# Extract CFGs from binary files.
dotnet Cilsil/bin/Debug/netcoreapp2.2/Cilsil.dll translate {directory_to_binary_files} \
                                                --outcfg {output_directory}/cfg.json \
                                                --outtenv {output_directory}/tenv.json \
                                                --cfgtxt {output_directory}/cfg.txt

# Run Infer on extracted CFGs.
infer capture
mkdir infer-out/captured
infer analyzejson --debug \
                  --cfg-json {output_directory}/cfg.json \
                  --tenv-json {output_directory}/tenv.json
```

Tips for debugging Infer# in your test:
- The CFG is expressed in a text format in {output_directory}/cfg.txt.
- Reported bugs are located at /infer-out/bugs.txt.
- Infer output is located at /infer-out/; detailed analysis information is located at /infer-out/captured/.

## Coding Style

### All Languages

* Line width limit is 100 characters.
* Follow the indentation and other stylistic aspects of the surrounding code.
 
### C#

Please conform to Microsoft's [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions).

### OCaml

Please conform to Infer's [Ocaml coding style guideline](https://github.com/facebook/infer/blob/master/CONTRIBUTING.md#ocaml).

## Testing your Changes

The testing framework first programmatically generating pieces of C# code for which the underlying bytecode isolates the instructions to be validated. Then, it builds the source code, runs the translation core on the resulting binaries in order to produce the CFG JSON, which in turn is passed to the Infer backend for analysis. The expected warnings are then validated. Utilities for generating test code are located [here](https://github.com/microsoft/infersharp/blob/main/Cilsil.Test/Assets/Utils.cs). The test execution is orchestrated [here](https://github.com/microsoft/infersharp/blob/main/Cilsil.Test/TestRunManager.cs).

When making a technical contribution to the codebase, please consider the following:

  - [Validate](https://github.com/microsoft/infersharp/CONTRIBUTING.md#building-infer#) the build. 

  - Add your unit tests in Cilsil.Test/E2E/NPETest.cs. 
  
  - Try to reuse existing utilities if necessary. Otherwise, add new utilities in "Cilsil.Test/Assets/Utils.cs".
  
  - Run the tests using the following command: `dotnet test`


## Reporting Issues

If you encounter any issues, please open an [issue](https://github.com/microsoft/infersharp/issues).


## Contributor License Agreement

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

