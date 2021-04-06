// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Test.Assets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Cilsil.Test.Assets.Utils;

namespace Cilsil.Test.E2E
{
    [TestClass]
    public class InferSharpTest
    {
        public TestContext TestContext { get; set; }

        private TestRunManager TestRunManager;

        [TestInitialize]
        public void TestInitialize() => TestRunManager = new TestRunManager(TestContext.TestName);

        /// <summary>
        /// Validates that a dereference on a locally initialized null pointer is identified.
        /// </summary>
        /// <param name="state">The initial state of the TestClass object.</param>
        /// <param name="expectedError">The kind of error expected to be reported by Infer.</param>
        [DataRow(TestClassState.Initialized, InferError.None)]
        [DataRow(TestClassState.Null, InferError.NULL_DEREFERENCE)]
        [DataTestMethod]
        public void NullExceptionSimple(TestClassState state, InferError expectedError)
        {
            TestRunManager.Run(InitVars(state: state) +
                                   DerefObject(VarName.Tc),
                                GetString(expectedError));
        }

        /// <summary>
        /// Validates resource leak detection on a locally initialized stream, 
        /// where Close is the method for releasing the resource.
        /// </summary>
        /// <param name="closeStream"> If <c>true</c>, invokes the Close method; otherwise,
        /// does not.</param>
        /// <param name="expectedError">The kind of error expected to be reported by Infer.</param>
        [DataRow(true, InferError.None)]
        [DataRow(false, InferError.DOTNET_RESOURCE_LEAK)]
        [DataTestMethod]
        public void ResourceLeakIntraprocClose(bool closeStream, InferError expectedError)
        {
            TestRunManager.Run(InitVars(state: TestClassState.Initialized,
                                        firstLocalVarType: VarType.StreamReader,
                                        firstLocalVarValue: CallTestClassMethod(
                                            TestClassMethod.ReturnInitializedStreamReader,
                                            false)) +
                                   (closeStream ? CallMethod(
                                                    VarName.FirstLocal,
                                                    "Close")
                                                : string.Empty),
                               GetString(expectedError));

        }

        /// <summary>
        /// Validates resource leak detection on a locally initialized stream, 
        /// where Dispose is the method for releasing the resource.
        /// </summary>
        /// <param name="disposeStream"> If <c>true</c>, invokes the Dispose method; otherwise,
        /// does not.</param>
        /// <param name="expectedError">The kind of error expected to be reported by Infer.</param>
        [DataRow(true, InferError.None)]
        [DataRow(false, InferError.DOTNET_RESOURCE_LEAK)]
        [DataTestMethod]
        public void ResourceLeakIntraprocDispose(bool disposeStream, InferError expectedError)
        {
            TestRunManager.Run(InitVars(state: TestClassState.Initialized,
                                        firstLocalVarType: VarType.MemoryStream,
                                        firstLocalVarValue: CallTestClassMethod(
                                            TestClassMethod.ReturnInitializedMemoryStream,
                                            false)) +
                                   (disposeStream ? CallMethod(
                                                        VarName.FirstLocal,
                                                        "Dispose")
                                                  : string.Empty),
                              GetString(expectedError));

        }

        /// <summary>
        /// Validates that a resource leak on an initialized StreamReader is identified.
        /// </summary>
        /// <param name="closeStream"> If <c>true</c>, invokes the CloseStream method; otherwise,
        /// does not.</param>
        /// <param name="expectedError">The kind of error expected to be reported by Infer.</param>
        [DataRow(true, InferError.None)]
        [DataRow(false, InferError.DOTNET_RESOURCE_LEAK)]
        [DataTestMethod]
        public void ResourceLeakInterproc(bool closeStream, InferError expectedError)
        {
            TestRunManager.Run(InitVars(state: TestClassState.Initialized,
                                        firstLocalVarType: VarType.StreamReader,
                                        firstLocalVarValue: CallTestClassMethod(
                                            TestClassMethod.ReturnInitializedStreamReader,
                                            false)) +
                                   (closeStream ? CallTestClassMethod(
                                                    TestClassMethod.CloseStream,
                                                    true,
                                                    new string[]
                                                    {
                                                        GetString(VarName.FirstLocal)
                                                    })
                                                : string.Empty),
                               GetString(expectedError));

        }

        /// <summary>
        /// Validates that a resource leak on a StreamReader initialized in another class is identified.
        /// </summary>
        /// <param name="closeStream" If <c>true</c>, invokes the CloseStream method; otherwise,
        /// does not.</param>
        /// <param name="expectedError">The kind of error expected to be reported by Infer.</param>
        [DataRow(true, InferError.None)]
        [DataRow(false, InferError.DOTNET_RESOURCE_LEAK)]
        [DataTestMethod]
        public void ResourceLeakInitInterproc(bool closeStream, InferError expectedError)
        {
            TestRunManager.Run(InitVars(state: TestClassState.None) +
                                   (closeStream ? CallTestClassMethod(
                                                    TestClassMethod.CloseStream,
                                                    true,
                                                    new string[]
                                                    {
                                                        CallTestClassMethod(
                                                            TestClassMethod.ReturnInitializedStreamReader,
                                                            false)
                                                    })
                                                : CallTestClassMethod(
                                                    TestClassMethod.ReturnInitializedStreamReader,
                                                    true)),
                               GetString(expectedError));
        }

        /// <summary>
        /// Validates that there is no resource leak identified on a returned resource.
        /// </summary>
        /// <param name="returnsResource" If <c>true</c>, returns the instantiated resource; otherwise,
        /// does not.</param>
        /// <param name="expectedError">The kind of error expected to be reported by Infer.</param>
        [DataRow(true, InferError.None)]
        [DataRow(false, InferError.DOTNET_RESOURCE_LEAK)]
        [DataTestMethod]
        public void ResourceLeakReturnedResource(bool returnsResource, InferError expectedError)
        {
            TestRunManager.Run(InitVars(state: TestClassState.Initialized,
                                        firstLocalVarType: VarType.MemoryStream,
                                        firstLocalVarValue: CallTestClassMethod(
                                                    TestClassMethod.ReturnInitializedMemoryStream,
                                                    false)) +
                                   (returnsResource ? ReturnVar(VarName.FirstLocal)
                                                     : string.Empty),
                               GetString(expectedError),
                               returnType: returnsResource ? GetString(VarType.MemoryStream)
                                                            : "void");
        }

        /// <summary>
        /// Validates that there is no resource leak identified on a static readonly singleton resource.
        /// </summary>
        /// <param name="staticSingleton" If <c>false</c>, instantiates a regular field resource; otherwise,
        /// does not.</param>
        /// <param name="expectedError">The kind of error expected to be reported by Infer.</param>
        [DataRow(true, InferError.None)]
        [DataRow(false, InferError.DOTNET_RESOURCE_LEAK)]
        [DataTestMethod]
        public void ResourceLeakStaticSingleton(bool staticSingleton, InferError expectedError)
        {
            TestRunManager.Run(InitVars(state: TestClassState.Initialized) +
                                   (staticSingleton ? string.Empty
                                                    : CallTestClassMethod(
                                                        TestClassMethod.InitializeStreamReaderObjectField,
                                                        true)),
                               GetString(expectedError));
        }

                /// <summary>
        /// Validates that a resource leak on a StreamReader initialized in exception handling block 
        /// is identified.
        /// </summary>
        /// <param name="blockKind">The kind of exception handling block expected to wrap the resource.</param>
        /// <param name="closeStream">If <c>true</c>, invokes the Close method; otherwise,
        /// does not.</param>
        /// <param name="expectedError">The kind of error expected to be reported by Infer.</param>
        [DataRow(BlockKind.Using, false, InferError.None)]
        [DataRow(BlockKind.TryCatchFinally, true, InferError.None)]
        [DataRow(BlockKind.TryCatchFinally, false, InferError.DOTNET_RESOURCE_LEAK)]
        [DataTestMethod]
        public void ResourceLeakExceptionHandling(BlockKind blockKind,
                                                  bool closeStream,
                                                  InferError expectedError)
        {
            TestRunManager.Run(InitBlock(resourceLocalVarType: VarType.StreamReader,
                                resourceLocalVarValue: CallTestClassMethod(
                                    TestClassMethod.ReturnInitializedStreamReader,
                                    false),
                                disposeResource: (closeStream ? CallMethod(
                                                    VarName.FirstLocal,
                                                    "Close")
                                                : string.Empty),
                                blockKind: blockKind),
                               GetString(expectedError));
        }

        /// <summary>
        /// Validates that a null dereference on a variable in exception handling blocks 
        /// is identified. 
        /// </summary>
        /// <param name="initVar">If <c>true</c>, instantiates the variable; otherwise,
        /// does not.</param>
        /// <param name="expectedError">The kind of error expected to be reported by Infer.</param>
        [DataRow(true, InferError.None)]
        [DataRow(false, InferError.NULL_DEREFERENCE)]
        [DataTestMethod]
        public void NullDereferenceExceptionHandling(bool initVar,
                                                     InferError expectedError)
        {
            TestRunManager.Run(InitBlock(resourceLocalVarType: VarType.StreamReader,
                                resourceLocalVarValue: (
                                    initVar ? CallTestClassMethod(
                                                TestClassMethod.ReturnInitializedStreamReader,
                                                false)
                                            : null),
                                disposeResource: CallMethod(
                                                    VarName.FirstLocal,
                                                    "Close"),
                                blockKind: BlockKind.TryCatchFinally),
                                GetString(expectedError));
        }

        /// <summary>
        /// Validates that a dereference on a string variable initialized to null is identified.
        /// </summary>
        /// <param name="input">The string representing the value to assign.</param>
        /// <param name="expectedError">The kind of error expected to be reported by Infer.</param>
        [DataRow("\"hello\"", InferError.None)]
        [DataRow("null", InferError.NULL_DEREFERENCE)]
        [DataTestMethod]
        public void NullExceptionLdstr(string input, InferError expectedError)
        {
            TestRunManager.Run(InitVars(firstLocalVarType: VarType.String,
                                        firstLocalVarValue: input) +
                                   DerefObject(VarName.FirstLocal),
                               GetString(expectedError));
        }

        /// <summary>
        /// Validates that a dereference on a null pointer returned by a different method is
        /// identified.
        /// </summary>
        /// <param name="testObjectShouldBeInitialized">Whether or not to initialize the object to
        /// be dereferenced.</param>
        /// <param name="expectedError">The kind of error expected to be reported by Infer.</param>
        [DataRow(true, InferError.None)]
        [DataRow(false, InferError.NULL_DEREFERENCE)]
        [DataTestMethod]
        public void NullExceptionInterproc(bool testObjectShouldBeInitialized,
                                           InferError expectedError)
        {
            TestRunManager.Run(Declare(VarType.TestClass, VarName.Tc) +
                                   Assign(VarName.Tc,
                                          CallTestClassMethod(TestClassMethod.ReturnNullOnFalse,
                                                     false,
                                                     new string[]
                                                     {
                                                         testObjectShouldBeInitialized
                                                         .ToString()
                                                         .ToLower()
                                                     })) +
                                   DerefObject(VarName.Tc),
                               GetString(expectedError));
        }

        /// <summary>
        /// Validates that a dereference on a null method parameter is identified.
        /// </summary>
        /// <param name="state">The value to initialize the method parameter to.</param>
        /// <param name="expectedError">The kind of error expected to be reported by Infer.</param>
        [DataRow(TestClassState.Initialized, InferError.None)]
        [DataRow(TestClassState.Null, InferError.NULL_DEREFERENCE)]
        [DataTestMethod]
        public void NullExceptionNullParam(TestClassState state, InferError expectedError)
        {
            TestRunManager.Run(InitVars(state: state) +
                                   CallTestClassMethod(TestClassMethod.ExpectNonNullParam,
                                              true,
                                              new string[] { GetString(VarName.Tc) }),
                               GetString(expectedError));
        }

        /// <summary>
        /// Validates the correctness of translation for arithmetic operations. It uses a 
        /// translation of equality; therefore, the validity of this test depends on the
        /// correctness of that translation.
        /// </summary>
        [DataRow(VarType.Integer, BooleanTestType.Binary, "+", 5, 7, 12)]
        [DataRow(VarType.Integer, BooleanTestType.Binary, "-", 5, 7, -2)]
        [DataRow(VarType.Integer, BooleanTestType.Binary, "*", 5, 7, 35)]
        [DataRow(VarType.Integer, BooleanTestType.Binary, "/", 5, 7, 0)]
        [DataRow(VarType.Integer, BooleanTestType.Binary, "%", 5, 7, 5)]
        [DataRow(VarType.UnsignedInteger, BooleanTestType.Binary, "/", 14, 5, 2)]
        [DataRow(VarType.UnsignedInteger, BooleanTestType.Binary, "%", 14, 5, 4)]
        [DataRow(VarType.Integer, BooleanTestType.Binary, "&", 12, 10, 8)]
        [DataRow(VarType.Integer, BooleanTestType.Binary, "|", 12, 10, 14)]
        [DataRow(VarType.Integer, BooleanTestType.Binary, "^", 12, 10, 6)]
        [DataRow(VarType.Integer, BooleanTestType.Binary, "<<", 10, 1, 20)]
        [DataRow(VarType.Integer, BooleanTestType.Binary, ">>", 10, 1, 5)]
        [DataRow(VarType.Integer, BooleanTestType.Unary, "~", 12, null, -13)]
        [DataRow(VarType.Integer, BooleanTestType.Unary, "-", 12, null, -12)]
        [DataTestMethod]
        public void NullExceptionArithmetic(VarType type,
                                            BooleanTestType testType,
                                            string operatorToTest,
                                            int v1,
                                            int? v2,
                                            int v3)
        {
            RunTest("!=", InferError.None);
            RunTest("==", InferError.NULL_DEREFERENCE);

            void RunTest(string comparisonOperator, InferError expectedError)
            {
                var testCode = InitVars(
                                   state: TestClassState.Null,
                                   firstLocalVarType: type,
                                   secondLocalVarType: type,
                                   firstLocalVarValue: v1.ToString(),
                                   secondLocalVarValue: v2?.ToString()) +
                               GenerateSingleComparisonIfCondition(
                                   testType,
                                   operatorToTest,
                                   secondOperator: comparisonOperator,
                                   valueToBeComparedAgainst: v3.ToString()) +
                               DerefObject(VarName.Tc);

                TestRunManager.Run(testCode, GetString(expectedError));
            }
        }

        /// <summary>
        /// Validates the correctness of the translation of numerical comparison operations.
        /// </summary>
        /// <param name="type">The type of value to be compared.</param>
        /// <param name="operatorToTest">The numerical comparison operator to validate.</param>
        /// <param name="v1">The first operand.</param>
        /// <param name="v2">The second operand.</param>
        /// <param name="expectedError">The kind of error expected to be reported by Infer.</param>
        [DataRow(VarType.Integer, "<", 0, 0, InferError.None)]
        [DataRow(VarType.Integer, "<", 0, 1, InferError.NULL_DEREFERENCE)]
        [DataRow(VarType.UnsignedInteger, "<", 0, 0, InferError.None)]
        [DataRow(VarType.UnsignedInteger, "<", 0, 1, InferError.NULL_DEREFERENCE)]
        [DataRow(VarType.Integer, ">", 0, 0, InferError.None)]
        [DataRow(VarType.Integer, ">", 1, 0, InferError.NULL_DEREFERENCE)]
        [DataRow(VarType.UnsignedInteger, ">", 0, 0, InferError.None)]
        [DataRow(VarType.UnsignedInteger, ">", 1, 0, InferError.NULL_DEREFERENCE)]
        [DataRow(VarType.Integer, "==", 0, 1, InferError.None)]
        [DataRow(VarType.Integer, "==", 0, -1, InferError.None)]
        [DataRow(VarType.Integer, "==", 0, 0, InferError.NULL_DEREFERENCE)]
        [DataTestMethod]
        public void NullExceptionNumericalComparison(VarType type,
                                                     string operatorToTest,
                                                     int v1,
                                                     int v2,
                                                     InferError expectedError)
        {
            TestRunManager.Run(InitVars(state: TestClassState.Null,
                                        firstLocalVarType: type,
                                        secondLocalVarType: type,
                                        firstLocalVarValue: v1.ToString(),
                                        secondLocalVarValue: v2.ToString()) +
                                   GenerateSingleComparisonIfCondition(BooleanTestType.Comparison,
                                                                       operatorToTest) +
                                   DerefObject(VarName.Tc),
                               GetString(expectedError));
        }

        /// <summary>
        /// Validates the correctness of the translation of bge and ble.
        /// </summary>
        /// <param name="operatorToTest">The operator to validate.</param>
        /// <param name="v1">The first operand.</param>
        /// <param name="expectedError">The kind of error expected to be reported by Infer.</param>
        // This configuration creates a bge.s instruction.
        [DataRow("<", 1, InferError.None)]
        [DataRow("<", 0, InferError.None)]
        [DataRow("<", -1, InferError.NULL_DEREFERENCE)]
        // This configuration creates a ble.s instruction.
        [DataRow(">", -1, InferError.None)]
        [DataRow(">", 0, InferError.None)]
        [DataRow(">", 1, InferError.NULL_DEREFERENCE)]
        [DataTestMethod]
        public void NullExceptionBgeBle(string operatorToTest, int v1, InferError expectedError)
        {
            TestRunManager.Run(InitVars(state: TestClassState.Null,
                                        firstLocalVarType: VarType.Integer,
                                        secondLocalVarType: VarType.Integer,
                                        firstLocalVarValue: v1.ToString(),
                                        secondLocalVarValue: "0") +
                                   $@"if ({GetString(VarName.FirstLocal)} {operatorToTest} 0 && 
                                          {GetString(VarName.SecondLocal)} == 0)" +
                                   DerefObject(VarName.Tc),
                               GetString(expectedError));
        }

        /// <summary>
        /// Validates that logical operators are correctly translated.
        /// </summary>
        /// <param name="operatorToTest">The operator to validate.</param>
        /// <param name="v1">The first operand.</param>
        /// <param name="v2">The second operand.</param>
        /// <param name="expectedError">The kind of error expected to be reported by Infer.</param>
        [DataRow("||", true, true, InferError.NULL_DEREFERENCE)]
        [DataRow("||", true, false, InferError.NULL_DEREFERENCE)]
        [DataRow("||", false, true, InferError.NULL_DEREFERENCE)]
        [DataRow("||", false, false, InferError.None)]
        [DataRow("&&", true, true, InferError.NULL_DEREFERENCE)]
        [DataRow("&&", true, false, InferError.None)]
        [DataRow("&&", false, true, InferError.None)]
        [DataRow("&&", false, false, InferError.None)]
        [DataRow("^", true, true, InferError.None)]
        [DataRow("^", true, false, InferError.NULL_DEREFERENCE)]
        [DataRow("^", false, true, InferError.NULL_DEREFERENCE)]
        [DataRow("^", false, false, InferError.None)]
        [DataTestMethod]
        public void NullExceptionLogical(string operatorToTest,
                                         bool v1,
                                         bool v2,
                                         InferError expectedError)
        {
            TestRunManager.Run(InitVars(state: TestClassState.Null,
                                        firstLocalVarType: VarType.Boolean,
                                        secondLocalVarType: VarType.Boolean,
                                        firstLocalVarValue: v1.ToString().ToLower(),
                                        secondLocalVarValue: v2.ToString().ToLower()) +
                                   GenerateSingleComparisonIfCondition(BooleanTestType.Comparison,
                                                                       operatorToTest) +
                                   DerefObject(VarName.Tc),
                               GetString(expectedError));
        }

        /// <summary>
        /// Validates that a dereference on a null static object field is identified.
        /// </summary>
        /// <param name="initializationMethod">The method by which to initialize the field.</param>
        /// <param name="initializeToNull">If <c>true</c>, initialize the field to null; otherwise,
        /// initialize the field to an object.</param>
        /// <param name="expectedError">The kind of error expected to be reported by Infer.</param>
        [DataRow(TestClassMethod.InitializeStaticObjectField,
                 false,
                 InferError.None)]
        [DataRow(TestClassMethod.InitializeStaticObjectField,
                 true,
                 InferError.NULL_DEREFERENCE)]
        [DataRow(TestClassMethod.InitializeStaticObjectFieldViaReference,
                 false,
                 InferError.None)]
        [DataRow(TestClassMethod.InitializeStaticObjectFieldViaReference,
                 true,
                 InferError.NULL_DEREFERENCE)]
        [DataTestMethod]
        public void NullExceptionStaticFieldDeref(TestClassMethod initializationMethod,
                                                  bool initializeToNull,
                                                  InferError expectedError)
        {
            TestRunManager.Run(CallTestClassMethod(initializationMethod,
                                          true,
                                          args: new string[]
                                          {
                                              initializeToNull.ToString().ToLower()
                                          }) +
                                   DerefObject(VarName.StaticObjectField),
                               GetString(expectedError));

        }

        /// <summary>
        /// Validates that a dereference on a null instance object field is identified.
        /// </summary>
        /// <param name="initializationMethod">The method by which to initialize the field.</param>
        /// <param name="initializeToNull">If <c>true</c>, initialize the field to null; otherwise,
        /// initialize the field to an object.</param>
        /// <param name="expectedError">The kind of error expected to be reported by Infer.</param>
        [DataRow(TestClassMethod.InitializeInstanceObjectField,
                 false,
                 InferError.None)]
        [DataRow(TestClassMethod.InitializeInstanceObjectField,
                 true,
                 InferError.NULL_DEREFERENCE)]
        [DataRow(TestClassMethod.InitializeInstanceObjectFieldViaReference,
                 false,
                 InferError.None)]
        [DataRow(TestClassMethod.InitializeInstanceObjectFieldViaReference,
                 true,
                 InferError.NULL_DEREFERENCE)]
        [DataTestMethod]
        public void NullExceptionInstanceFieldDeref(TestClassMethod initializationMethod,
                                                    bool initializeToNull,
                                                    InferError expectedError)
        {
            TestRunManager.Run(InitVars(state: TestClassState.Initialized) +
                                   CallTestClassMethod(initializationMethod,
                                              true,
                                              args: new string[]
                                              {
                                                  initializeToNull.ToString().ToLower()
                                              }) +
                                   DerefObject(VarName.InstanceObjectField),
                               GetString(expectedError));
        }

        /// <summary>
        /// Validates that a dereference on a null element from a one-dimensional array is
        /// identified.
        /// </summary>
        /// <param name="initializeArrayElement">True if array element should be initialized, false
        /// if it should be set to null.</param>
        /// <param name="expectedError">The kind of error expected to be reported by Infer.</param>
        [DataRow(true, true, InferError.None)]
        [DataRow(true, false, InferError.None)]
        [DataRow(false, true, InferError.NULL_DEREFERENCE)]
        [DataRow(false, false, InferError.NULL_DEREFERENCE)]
        [DataTestMethod]
        public void NullExceptionArrayOneDim(bool initializeArrayElement,
                                             bool useBinaryLengthExpression,
                                             InferError expectedError)
        {
            TestRunManager.Run(
                InitVars(firstLocalVarType: VarType.TestClassArrayOneDim,
                         secondLocalVarType: VarType.TestClass,
                         firstLocalVarValue: CallTestClassMethod(
                             TestClassMethod.ReturnOneDimArray,
                             false,
                             args: new string[] {
                                                    initializeArrayElement.ToString().ToLower(),
                                                    useBinaryLengthExpression.ToString().ToLower()
                                                }),
                         secondLocalVarValue: GetFirstArrayElement(VarName.FirstLocal)) +
                    DerefObject(VarName.SecondLocal),
                GetString(expectedError));
        }

        /// <summary>
        /// Validates that a dereference on a null element from a one-dimensional array instance 
        /// field is identified.
        /// </summary>
        /// <param name="initializeArrayElement">True if array element should be initialized, false
        /// if it should be set to null.</param>
        /// <param name="expectedError">The kind of error expected to be reported by Infer.</param>
        [DataRow(true, InferError.None)]
        [DataRow(false, InferError.NULL_DEREFERENCE)]
        public void NullExceptionArrayField(bool initializeArrayElement, InferError expectedError)
        {
            TestRunManager.Run(
                InitVars(state: initializeArrayElement ? TestClassState.Initialized
                                                       : TestClassState.Null,
                         firstLocalVarType: VarType.TestClass,
                         firstLocalVarValue: CallTestClassMethod(
                             TestClassMethod.ReturnNullOnFalse,
                             false,
                             args: new string[] {
                                                    initializeArrayElement.ToString()
                                                }),
                         secondLocalVarType: VarType.TestClass,
                         secondLocalVarValue: CallTestClassMethod(
                             TestClassMethod.ReturnElementFromInstanceArrayField,
                             false,
                             args: new string[] {
                                                    VarName.FirstLocal.ToString()
                                                })) +
                    DerefObject(VarName.SecondLocal),
                GetString(expectedError));
        }

        /// <summary>
        /// Validates that a dereference on a null array element is identified in a one-dimensional
        /// array.
        /// </summary>
        /// <param name="initializeArrayElement">True if array element should be initialized, false
        /// if it should be set to false.</param>
        /// <param name="expectedError">The kind of error expected to be reported by Infer.</param>
        [DataRow(true, InferError.None)]
        [DataRow(false, InferError.NULL_DEREFERENCE)]
        [DataTestMethod]
        public void NullExceptionArrayTwoDim(bool initializeArrayElement,
                                             InferError expectedError)
        {
            TestRunManager.Run(
                InitVars(firstLocalVarType: VarType.TestClassArrayTwoDim,
                         secondLocalVarType: VarType.TestClassArrayOneDim,
                         firstLocalVarValue: CallTestClassMethod(
                             TestClassMethod.ReturnTwoDimArray,
                             false,
                             args: new string[] { initializeArrayElement.ToString().ToLower() }),
                         secondLocalVarValue: GetFirstArrayElement(VarName.FirstLocal)) +
                    DerefObject(VarName.SecondLocal),
                GetString(expectedError));
        }

        /// <summary>
        /// Validates the null-coalesce operator in C#, which uses a nullable reference type as the
        /// test expression for a boolean branch condition. Additionally, provides coverage over
        /// the starg instruction.
        /// </summary>
        /// <param name="initFirst">If <c>true</c>, initializes the first argument to 
        /// <see cref="TestClassMethod.TestStarg"/>; otherwise, sets it to null.</param>
        /// <param name="initSecond">If <c>true</c>, initializes the first argument to 
        /// <see cref="TestClassMethod.TestStarg"/>; otherwise, sets it to null.</param>
        /// <param name="expectedError">The kind of error expected to be reported by Infer.</param>
        [DataRow(true, true, InferError.None)]
        [DataRow(true, false, InferError.None)]
        [DataRow(false, true, InferError.None)]
        [DataRow(false, false, InferError.NULL_DEREFERENCE)]
        [DataTestMethod]
        public void NullExceptionTestStarg(bool initFirst,
                                           bool initSecond,
                                           InferError expectedError)
        {
            TestRunManager.Run(
                InitVars(firstLocalVarType: VarType.TestClass,
                         firstLocalVarValue: CallTestClassMethod(
                             TestClassMethod.TestStarg,
                             false,
                             args: new string[]
                             {
                                 CallTestClassMethod(
                                     TestClassMethod.ReturnNullOnFalse,
                                     false,
                                     args: new string[]
                                     {
                                         initFirst.ToString().ToLower()
                                     }
                                 ),
                                 CallTestClassMethod(
                                   TestClassMethod.ReturnNullOnFalse,
                                   false,
                                   args: new string[]
                                   {
                                       initSecond.ToString().ToLower()
                                   }
                                 )
                             })) +
                    DerefObject(VarName.FirstLocal),
                GetString(expectedError));
        }

        /// <summary>
        /// Validates translation for address loading and storing instructions within a struct.
        /// Provides coverage over initobj. If the incremented default value (0, for the integer
        /// field) is equivalent to the input value, a null value is dereferenced.
        /// </summary>
        /// <param name="testCounterValue">The value against which to compare the field 
        /// value.</param>
        /// <param name="expectedError">The expected error.</param>
        [DataRow(0, InferError.None)]
        [DataRow(1, InferError.NULL_DEREFERENCE)]
        [DataTestMethod]
        public void NullExceptionReferenceStruct(int testCounterValue, InferError expectedError)
        {
            TestRunManager.Run(
                InitVars(firstLocalVarType: VarType.TestClass,
                         firstLocalVarValue: CallTestClassMethod(
                             TestClassMethod.IncrementStructFieldViaAddress,
                             false,
                             args: new string[] { testCounterValue.ToString() })) +
                    DerefObject(VarName.FirstLocal),
                GetString(expectedError));
        }

        /// <summary>
        /// Validates translation of parameters passed via reference. In this case, an integer is
        /// passed via reference to a method which increments it, and the new value is checked
        /// within the calling context. 
        /// </summary>
        /// <param name="comparison">The value against which to compare the incremented 
        /// variable.</param>
        /// <param name="expectedError">The expected error.</param>
        [DataRow(0, InferError.None)]
        [DataRow(1, InferError.NULL_DEREFERENCE)]
        [DataTestMethod]
        public void NullExceptionReferenceParameter(int comparison, InferError expectedError)
        {
            TestRunManager.Run(
                InitVars(state: TestClassState.Null,
                         firstLocalVarType: VarType.Integer,
                         firstLocalVarValue: 0.ToString(),
                         secondLocalVarType: VarType.Integer,
                         secondLocalVarValue: comparison.ToString()) +
                    CallTestClassMethod(TestClassMethod.IncrementRefParameter,
                                        true,
                                        args: new string[]
                                        {
                                            "ref " + VarName.FirstLocal.ToString()
                                        }) +
                    GenerateSingleComparisonIfCondition(BooleanTestType.Comparison, "==") +
                DerefObject(VarName.Tc), GetString(expectedError));
        }

        /// <summary>
        /// Validates translation of box and unbox. Note that if a variable is returned
        /// interprocedurally and unbox is attempted, the translation won't have kept track of the 
        /// underlying value and method translation will terminate early. 
        /// </summary>
        /// <param name="comparison">The value which gets boxed. If it is of value 1, a null
        /// the method should return a null value; else, it should return an instantiated 
        /// <see cref="TestClass"/>.</param>
        /// <param name="expectedError">The expected error.</param>
        [DataRow(0, InferError.None)]
        [DataRow(1, InferError.NULL_DEREFERENCE)]
        [DataTestMethod]
        public void NullExceptionBoxIntegers(int comparison, InferError expectedError)
        {
            TestRunManager.Run(
                InitVars(firstLocalVarType: VarType.Integer,
                         firstLocalVarValue: comparison.ToString(),
                         secondLocalVarType: VarType.TestClass,
                         secondLocalVarValue: CallTestClassMethod(TestClassMethod.TestBox,
                                                                  false,
                                                                  args: new string[]
                                                                  {
                                                                      comparison.ToString()
                                                                  })) +
                    DerefObject(VarName.SecondLocal), GetString(expectedError));
        }

        /// <summary>
        /// Validates our (limited) translation of isinst. The only type-checking scenarios that
        /// can be statically supported are trivial. We therefore treat them just as null-checks:
        /// this is captured in the translation by simply treating isinst itself as a nop, as the 
        /// bytecode separately captures the null-check component of instance type-checking. We 
        /// capture the fact that our translation amounts to a null-check behavior by passing in 
        /// both an actual instance of TestClass as well as a non-TestClass object, and validating 
        /// that in both cases the null value is returned by the method, while the null input
        /// results in the instantiated TestClass being returned. 
        /// </summary>
        /// <param name="testInputCode">Defines the object to be input to the type-checking
        /// TestClass method; 0 for instantiated TestClass, 1 for generic object (non-TestClass), 
        /// 2 for null.</param>
        /// <param name="expectedError">The expected error.</param>
        [DataRow(0, InferError.NULL_DEREFERENCE)]
        [DataRow(1, InferError.NULL_DEREFERENCE)]
        [DataRow(2, InferError.None)]
        [DataTestMethod]
        public void NullExceptionIsInst(int testInputCode, InferError expectedError)
        {
            string inputObjectString;
            switch (testInputCode)
            {
                case 0:
                    inputObjectString = "new TestClass()";
                    break;
                case 1:
                    inputObjectString = "new object()";
                    break;
                case 2:
                    inputObjectString = "null";
                    break;
                default:
                    return;
            }
            TestRunManager.Run(
                InitVars(firstLocalVarType: VarType.TestClass,
                         firstLocalVarValue: CallTestClassMethod(TestClassMethod.TestIsInst,
                                                                  false,
                                                                  args: new string[]
                                                                  {
                                                                      inputObjectString
                                                                  })) +
                    DerefObject(VarName.FirstLocal), GetString(expectedError));
        }

        /// <summary>
        /// Validates the use of Infer models for pre-compiled code during analysis. The model 
        /// tested here is String.IsNullOrWhiteSpace, but the purpose of the test is to verify
        /// that any model can be used in analysis.
        /// </summary>
        /// <param name="modelOperator">Defines whether the model's null check is logically 
        /// negated.</param>
        /// <param name="expectedError">The expected error.</param>
        [DataRow("!", InferError.None)]
        [DataRow("", InferError.NULL_DEREFERENCE)]
        [DataTestMethod]
        public void ModelIsNullOrWhitespace(string modelOperator, InferError expectedError)
        {
            TestRunManager.Run(
                InitVars(state: TestClassState.Null,
                         firstLocalVarType: VarType.Boolean,
                         firstLocalVarValue: GetString(
                            ModelMethod.String__IsNullOrWhiteSpace,
                            args: new string[]
                            {
                                "null"
                            },
                         withEnding: true)) +
                    GenerateSingleComparisonIfCondition(BooleanTestType.Unary,
                            firstOperator: modelOperator,
                            secondOperator: "==",
                            "true") +
                    DerefObject(VarName.Tc),
                GetString(expectedError));
        }
    }
}