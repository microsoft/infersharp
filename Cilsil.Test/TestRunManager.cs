// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Cilsil.Sil;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Cilsil.Test {
    public class TestRunManager : IDisposable {
        // All these properties are not thread-safe for parallel test execution, 
        // but it shouldn't be hard to append a UUID to make them thread-safe
        // otherwise, vstest provides test sandbox/relocation functionality which should make it 
        // thread-safe.
        // low pri TODO: explore one of the option above for parallel test execution
        private string AssetFolderPath => Path.Combine (
            Path.GetDirectoryName (Assembly.GetAssembly (typeof (Assets.TestClass)).Location),
            AssetFolderName);

        private string ProjectFilePath => Path.Combine (AssetFolderPath, TestProjectName);

        private string TestCodeFilePath => Path.Combine (AssetFolderPath, TestCodeFileName);

        private string TestBinaryFolder =>
            Path.Combine (AssetFolderPath, "bin", "Debug", "net5.0", "ubuntu.16.10-x64");

        private string CfgJsonPath => Path.Combine (TestBinaryFolder, "cfg.json");

        private string TenvJsonPath => Path.Combine (TestBinaryFolder, "tenv.json");

        private string InferOutFolder =>
            Path.Combine (Directory.GetCurrentDirectory (), "infer-out");

        private string InferReport => Path.Combine (InferOutFolder, "report.json");

        private readonly string TestName;

        private const string AssetFolderName = "Assets";

        private const string TestProjectName = "TestProject.csproj";

        private const string TestCodeFileName = "TestCode.cs";

        // A counter which increments upon each test case execution. Used for producing a unique
        // folder path to store the corresponding test case output.
        private static int TestCaseCount = 0;

        private Cfg Cfg;

        public TestRunManager (string testName) {
            TestName = testName;
        }

        /// <summary>
        /// This is a helper function that builds the input source code, returning the path to the
        /// output DLL and throwing an exception otherwise. It optionally decorates the code with 
        /// namespace, class, and method.
        /// </summary>
        /// <param name="code">The source code to be built.</param>
        /// <param name="returnType">The return type of TestMethod.</param>
        /// <param name="decorate">Namespace, class, and method signatures are added to enclose the
        /// source code if and only if this parameter is true.</param>
        /// <returns>The path to the testcode binaries produced by the build command, as well as 
        /// the path to the binaries for the test's core libraries.</returns>
        public string[] BuildCode (string code,
            String returnType,
            bool decorate = true) {
            if (decorate) {
                code =
                    $@"using System;
                       using System.IO;
                        namespace Cilsil.Test.Assets
                        {{
                            public class TestCode
                            {{
                                public {returnType} TestMethod()
                                {{
                                    {code}
                                }}

                                static void Main(string[] args)
                                {{
                                    Console.WriteLine();
                                }}   
                            }}
                        }}";
            }

            // C# core library DLL file path.
            var coreLibraryFilePath = Path.Combine (TestBinaryFolder, "publish", "System.Private.CoreLib.dll");

            File.WriteAllText (TestCodeFilePath, code);
            if (RunCommand ("dotnet", $"publish {ProjectFilePath} -c Debug -r ubuntu.16.10-x64", out var stdout, out _) != 0) {
                throw new ApplicationException (
                    $"Test code failed to build with error: \n{stdout}");
            }

            return new string[] { Path.Combine (TestBinaryFolder, "TestProject.dll"), coreLibraryFilePath };
        }

        public void Cleanup () {
            if (Directory.Exists (TestBinaryFolder)) {
                Directory.Delete (TestBinaryFolder, true);
            }
            if (File.Exists (TestCodeFilePath)) {
                File.Delete (TestCodeFilePath);
            }
            if (Directory.Exists (InferOutFolder)) {
                Directory.Delete (InferOutFolder, true);
            }
        }

        public void Dispose () => Cleanup ();

        public (string, string) RunCilsil (params string[] assemblies) {
            (var cfg, var tenv) = Program.ExecuteTranslation (assemblies);

            File.WriteAllText (CfgJsonPath, cfg.ToJson ());
            File.WriteAllText (TenvJsonPath, tenv.ToJson ());
            Cfg = cfg;

            if (Log.UnfinishedMethods.Count > 0) {
                var unfinishedMethods =
                    string.Join ("\n", Log.UnfinishedMethods.Select (kv => kv.Key));
                throw new AssertFailedException (
                    "CIL translation was not fully completed for the following methods:" +
                    $"\n{unfinishedMethods}");
            }
            return (CfgJsonPath, TenvJsonPath);
        }

        /// <summary>
        /// Helper method for calling infer's analysis on the CFG and TENV produced by the
        /// translation pipeline. If the 
        /// </summary>
        /// <param name="cfgJson"></param>
        /// <param name="tenvJson"></param>
        /// <param name="stdout"></param>
        /// <param name="stderr"></param>
        public void RunInfer (string cfgJson, string tenvJson, out string stdout, out string stderr) {
            RunCommand ("infer", "capture", out _, out _);
            Directory.CreateDirectory (Path.Combine (InferOutFolder, "captured"));
            RunCommand (
                "infer",
                $"analyzejson --debug --cfg-json {cfgJson} --tenv-json {tenvJson}",
                out stdout,
                out stderr);
            Trace.TraceError ($"\nInfer stderr: \n{stderr}");
        }

        /// <summary>
        /// Helper method for parsing the JSON bug report file produced by infer. It validates that
        /// the expected error type is present in the bug report produced for the given input 
        /// procedure name. If the expected error is not found, an exception is thrown.
        /// </summary>
        /// <param name="expectedErrorType">The error expected to appear in the bug report.</param>
        /// <param name="expectedProcName">The procedure in which the expected error should 
        /// appear.</param>
        public void CheckForInferError (string expectedErrorType,
            string expectedProcName = "Void TestCode.TestMethod()") {
            var jsonText = File.ReadAllText (InferReport);
            var inferReport = JToken.Parse (jsonText);
            foreach (var bug in inferReport) {
                var severity = bug.Value<string> ("severity");
                if (severity != "ERROR") {
                    continue;
                }

                var bugType = bug.Value<string> ("bug_type");
                if (bugType == expectedErrorType) {
                    var pname = bug.Value<string> ("procedure");
                    if (pname == expectedProcName) {
                        // Check could be more robust, there are more fields in the JSON that we 
                        // can validate against.
                        return;
                    }
                }
                throw new AssertFailedException ($"Unexpected issue found: {bugType}");
            }
            if (!string.IsNullOrEmpty (expectedErrorType)) {
                throw new AssertFailedException ($"Expected error type {expectedErrorType} was " +
                    "not found.");
            }
        }

        /// <summary>
        /// Method for executing infer on a piece of code, input as a string. Infer's output is
        /// validated against the given expected output. An assert exception occurs if the
        /// expected error is not found; the expected error type should be null if no error is
        /// expected.
        /// </summary>
        /// <param name="code">The source code to be analyzed by infer.</param>
        /// <param name="expectedErrorType">The infer error expected to be found in infer's bug 
        /// report. This should be null if no error is expected.</param>
        /// <param name="decorate">Determines whether to enclose the input code under a namespace,
        /// class, and method.</param>
        /// <param name="expectedProcName">The procedure name in which the infer error is expected
        /// to be found.</param>
        /// <param name="returnType">The return type of TestMethod.</param>
        public void Run (string code,
            string expectedErrorType,
            bool decorate = true,
            string expectedProcName = "Void TestCode.TestMethod()",
            string returnType = "void") {
            TestCaseCount++;
            try {
                var binary = BuildCode (code, returnType, decorate);
                (var cfgJson, var tenvJson) = RunCilsil (binary);

                RunInfer (cfgJson, tenvJson, out _, out _);

                CheckForInferError (expectedErrorType, expectedProcName);
            } finally {
                SaveArtifacts ();
                Trace.WriteLine (code);
                Cleanup ();
            }
        }

        /// <summary>
        /// This should be called in TestCleanup when the test fails to save the execution 
        /// artifacts for investigation.
        /// </summary>
        /// <remarks>Attachment on Linux machine will fail until this issue is resolved:
        /// https://github.com/microsoft/testfx/issues/619 </remarks>
        private void SaveArtifacts () {
            var artifactTestCaseFolder = Path.Combine (Directory.GetCurrentDirectory (),
                "artifacts",
                TestName,
                TestCaseCount.ToString ());

            if (!Directory.Exists (artifactTestCaseFolder)) {
                Directory.CreateDirectory (artifactTestCaseFolder);
            }

            if (File.Exists (CfgJsonPath)) {
                File.Copy (CfgJsonPath,
                    Path.Combine (artifactTestCaseFolder, "cfg.json"),
                    overwrite : true);
            }
            if (File.Exists (TenvJsonPath)) {
                File.Copy (TenvJsonPath,
                    Path.Combine (artifactTestCaseFolder, "tenv.json"),
                    overwrite : true);
            }
            if (File.Exists (TestCodeFilePath)) {
                File.Copy (TestCodeFilePath,
                    Path.Combine (artifactTestCaseFolder, TestCodeFileName),
                    overwrite : true);
            }

            var artifactInferOutFolder = Path.Combine (artifactTestCaseFolder, "infer-out");
            if (Directory.Exists (artifactInferOutFolder)) {
                Directory.Delete (artifactInferOutFolder, true);
            }
            if (Directory.Exists (InferOutFolder)) {
                Directory.Move (InferOutFolder, artifactInferOutFolder);
            }

            if (Cfg != null) {
                Cfg.GenerateDotFile (Path.Combine (artifactTestCaseFolder, "cfg.dot"));
                File.WriteAllText (Path.Combine (artifactTestCaseFolder, "cfg.txt"), Cfg.ToString ());
            }
        }

        private int RunCommand (string command, string arg, out string stdout, out string stderr) {
            var process = new Process ();
            var pInfo = new ProcessStartInfo {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = command,
                Arguments = arg,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            process.StartInfo = pInfo;
            process.Start ();

            stdout = process.StandardOutput.ReadToEnd ();
            stderr = process.StandardError.ReadToEnd ();

            process.WaitForExit ();

            return process.ExitCode;
        }
    }
}