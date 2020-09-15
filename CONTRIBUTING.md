# Contribution Guidelines


## Contributing to Infer#

We welcome contributions to Infer#. To do that, you'll want to fork a copy of Infer# to your account and contribute your hack via [pull requests on GitHub](https://github.com/microsoft/Infer#/pulls).

To hack infer bakcend, please follow [infer's contribution guideline](https://github.com/facebook/infer/blob/master/CONTRIBUTING.md).

### Building Infer#

infer backend is located under "infer/" directory. Run the following commands to get infer up and running:
```bash
cd infer
./build-infer.sh ./autogen.sh
./build-infer.sh java
./autogen.sh
sudo make install 
```

Optionally, build and install C# models for better C# language support:
```bash
./build_csharp_models.sh
``` 

Language agnostic translation module is located under "Cilsil/" directory. Run the following commands in the main directory to build:
```bash
dotnet build Infersharp.sln
```

### Debugging Infer#

Run Infer# on a target repository using the following commands:
```bash
# Extract CFGs from binary files.
dotnet Cilsil/bin/Debug/netcoreapp2.2/Cilsil.dll translate {directory_to_binary_files} \
                                                --outcfg {output_directory}/cfg.json \
                                                --outtenv {output_directory}/tenv.json \
                                                --cfgtxt {output_directory}/cfg.txt

# Run infer backend analysis
infer capture
mkdir infer-out/captured
infer analyzejson --debug \
                  --cfg-json {output_directory}/cfg.json \
                  --tenv-json {output_directory}/tenv.json
```

Tips for debugging Infer# in your test:
- Check "{output_directory}/cfg.txt" to confirm the correctness of CFG.
- Browse "infer-out/bugs.txt" to confirm bugs are identified as expected.
- It can be useful to look at the debug HTML output of Infer located in "infer-out/captured/" to see the detail of the symbolic execution.

## Coding Style

### C#

Please follow [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions).

### OCaml

Please follow [Infer's Ocaml coding style guideline](https://github.com/facebook/infer/blob/master/CONTRIBUTING.md#ocaml).

## Testing your Changes

- Make sure Infer# builds by following [this guidance](https://github.com/microsoft/infersharp/CONTRIBUTING.md#building-infer#). 

- Run the tests: `dotnet test`

- If relevant, add a test for your change.

- To add a test that Infer# finds (or does not find) a particular issue, add your test in
  "Cilsil.Test/E2E/NPETest.cs". 
  
- Try to reuse existing test modules if necessary. Otherwise, add new test modules in "Cilsil.Test/Assets/".


## Reporting Issues

If you encounter a problem when using Infer# or if you have any questions, please open a
[GitHub issue](https://github.com/microsoft/Infer#/issues).


## Contributor License Agreement

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

