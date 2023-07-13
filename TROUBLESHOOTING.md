# Troubleshooting

- [Analysis Running Out of Memory](#analysis-running-out-of-memory)
  * [Docker Image](#docker-image)
  * [GitHub Action or Azure Pipelines](#github-action-or-azure-pipelines)
- [Analysis Running Out of Disk Space](#analysis-running-out-of-disk-space)
- [Analysis Times Out](#analysis-times-out) 
- [Nothing to compile. Try cleaning the build first](#nothing-to-compile)

## Analysis Running Out of Memory
Fixes depend on the usage method of Infer#.

### Docker Image
The default memory allocated by Docker is 2 GB, which can be inadequate for analyzing big projects. Increasing the memory limit would resolve this issue.

### GitHub Action or Azure Pipelines
The provided host machines may have inadequate memory. Consider running the analysis on a self-hosted machine with better hardware. 

## Analysis Running Out of Disk Space
Similar to the handling of memory issues, this can be resolved by increasing the allocated disk space limit on the docker image or host machine. 

## Analysis Times Out
The interprocedural analysis can be resource-intensive and can time out on big projects. Removing the binaries of upstream dependencies (such as external libraries and binaries from NuGet packages) from the analysis's target directory may mitigate this issue.
 
Note: Infer# ignores the .dll files if their corresponding .pdb files are not also present. For example, NuGet packages typically do not contain .pdb files and thus will not affect the overall length of the analysis. However, if the external dependencies do contain .pdb files or have .pdb embedded in the .dll files, removing those will help. 
 
Keep in mind that removing frequently-used upstream dependencies may reduce the capabilities of Infer#'s analysis, as it will no longer have knowledge of these dependencies. Therefore, consider removing binaries of libraries which are large but also less-frequently used in your project.

## Nothing to Compile
If you get this issue when attempting to run Infer# in WSL or via the VS extension, then check the version of WSL via `wsl -l -v`. You should be running WSL2, and running WSL1 will cause this error.
