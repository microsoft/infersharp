// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;

namespace Cilsil.Test.Assets
{
    public class Utils
    {
        /// <summary>
        /// String representing the retrieval of the first element from the array.
        /// </summary>
        public const string ArrayFirstElement = "[0]";

        /// <summary>
        /// The various states that a TestClass object in the tests can be in.
        /// </summary>
        public enum TestClassState { None, Uninitialized, Null, Initialized };

        /// <summary>
        /// The various kinds of exception handling blocks that appear in the tests.
        /// </summary>
        public enum BlockKind { None, Using, TryCatchFinally, NestedTryCatchFinally, TryCatchWhenFinally };

        /// <summary>
        /// The various variable names used in the tests.
        /// </summary>
        public enum VarName
        {
            None, Tc, FirstLocal, SecondLocal, StaticObjectField,
            InstanceObjectField, StaticIntegerField
        };

        /// <summary>
        /// The different variable types used in the tests.
        /// </summary>
        public enum VarType
        {
            None, Boolean, Integer, UnsignedInteger, String, TestClass, StreamReader, MemoryStream,
            TestClassArrayOneDim, TestClassArrayTwoDim
        };

        /// <summary>
        /// The different types of boolean formulae evaluated in the tests.
        /// </summary>
        public enum BooleanTestType { Unary, Binary, Comparison };

        /// <summary>
        /// The various error types expected to be produced by Infer in the tests.
        /// </summary>
        public enum InferError
        {
            None, NULL_DEREFERENCE, DANGLING_POINTER_DEREFERENCE, DOTNET_RESOURCE_LEAK,
            THREAD_SAFETY_VIOLATION
        }

        /// <summary>
        /// The various class methods in TestClass which are called in the tests.
        /// </summary>
        public enum TestClassMethod
        {
            None,
            ExpectNonNullParam,
            ReturnNullOnFalse,
            IncrementRefParameter,
            IncrementStructFieldViaAddress,
            InitializeStaticObjectField,
            InitializeStaticObjectFieldViaReference,
            InitializeInstanceObjectField,
            InitializeStreamReaderObjectField,
            InitializeInstanceObjectFieldViaReference,
            ReturnElementFromInstanceArrayField,
            ReturnOneDimArray,
            ReturnTwoDimArray,
            TestBox,
            TestIsInst,
            TestStarg,
            CloseStream,
            ReturnInitializedStreamReader,
            ReturnInitializedMemoryStream
        }

        /// <summary>
        /// The different variable types used in the tests.
        /// </summary>
        public enum ModelMethod { String__IsNullOrWhiteSpace }

        /// <summary>
        /// Helper method for generating the string representation of a given ModelMethod.
        /// </summary>
        /// <param name="method">The ModelMethod for which to generate a string representation.</param>
        /// <returns>The string representation of the ModelMethod.</returns>
        public static string GetString(ModelMethod method, string[] args = null, bool withEnding = false)
        {
            var methodName = method.ToString().Replace("__", ".");
            var concatArgs = args == null ? string.Empty : string.Join(",", args);
            var outString = methodName + "(" + concatArgs + ")";
            if (withEnding) outString += ";\n";
            return outString;
        }

        /// <summary>
        /// Helper method for generating the string representation of a given VarName.
        /// </summary>
        /// <param name="name">The VarName for which to generate a string representation.</param>
        /// <returns>The string representation of the VarName.</returns>
        public static string GetString(VarName name)
        {
            switch (name)
            {
                case VarName.Tc:
                case VarName.FirstLocal:
                case VarName.SecondLocal:
                    return name.ToString();
                case VarName.StaticObjectField:
                    return GetString(VarType.TestClass) + "." +
                        nameof(TestClass.StaticObjectField);
                case VarName.StaticIntegerField:
                    return GetString(VarType.TestClass) + "." +
                        nameof(TestClass.StaticIntegerField);
                case VarName.InstanceObjectField:
                    return VarName.Tc.ToString() + "." +
                        nameof(TestClass.InstanceObjectField);
                default:
                    throw new NotImplementedException("Unhandled VarName");
            }
        }

        /// <summary>
        /// Helper method for generating the string representation of a given VarType.
        /// </summary>
        /// <param name="type">The VarType for which to generate a string representation.</param>
        /// <returns>The string representation of the VarType.</returns>
        public static string GetString(VarType type)
        {
            switch (type)
            {
                case VarType.Boolean:
                    return "bool";
                case VarType.Integer:
                    return "int";
                case VarType.UnsignedInteger:
                    return "uint";
                case VarType.String:
                    return "string";
                case VarType.StreamReader:
                    return "StreamReader";
                case VarType.MemoryStream:
                    return "MemoryStream";
                case VarType.TestClass:
                    return nameof(TestClass);
                case VarType.TestClassArrayOneDim:
                    return nameof(TestClass) + "[]";
                case VarType.TestClassArrayTwoDim:
                    return nameof(TestClass) + "[][]";
                case VarType.None:
                    return string.Empty;
                default:
                    throw new NotImplementedException("Unhandled VarType");
            }
        }

        /// <summary>
        /// Helper method for generating the string representation of a given InferError.
        /// </summary>
        /// <param name="error">The <see cref="InferError"/> for which to generate a string 
        /// representation.</param>
        /// <returns>The string representation of the InferError.</returns>
        public static string GetString(InferError error)
        {
            switch (error)
            {
                case InferError.DANGLING_POINTER_DEREFERENCE:
                case InferError.NULL_DEREFERENCE:
                case InferError.DOTNET_RESOURCE_LEAK:
                case InferError.THREAD_SAFETY_VIOLATION:
                    return error.ToString();
                case InferError.None:
                    return null;
                default:
                    throw new NotImplementedException("Unhandled Error");
            }
        }

        /// <summary>
        /// Returns a string representing the first array element for the input variable name. The
        /// variable must correspond to an array type.
        /// </summary>
        /// <param name="name">The variable name referencing the array.</param>
        /// <returns>The string representation for the first element of the array.</returns>
        public static string GetFirstArrayElement(VarName name) => GetString(name) + "[0]";

        /// <summary>
        /// Generates a string representation for variable declaration.
        /// </summary>
        /// <param name="type">The type of the variable being declared.</param>
        /// <param name="name">The name of the variable being declared.</param>
        /// <returns>The string representation of the variable declaration.</returns>
        public static string Declare(VarType type, VarName name) =>
            $"{GetString(type)} {GetString(name)};\n";

        /// <summary>
        /// Generates a string representation for object dereference via GetHashCode.
        /// </summary>
        /// <param name="name">The variable name for the object being dereferenced.</param>
        /// <returns>The string representation of the variable dereference.</returns>
        public static string DerefObject(VarName name) =>
            name == VarName.None ? string.Empty : $"_ = {GetString(name)}.GetHashCode();\n";

        /// <summary>
        /// Generates a string representation of value assignment to a variable.
        /// </summary>
        /// <param name="name">The name of the variable to be assigned to.</param>
        /// <param name="value">The string representation of the value to be assigned.</param>
        /// <returns>String representation of the value assignment statement.</returns>
        public static string Assign(VarName name, string value) =>
            $"{GetString(name)} = {value};\n";

        /// <summary>
        /// Generates a string representation of instantiating a new variable.
        /// </summary>
        /// <param name="type">The type of the variable being instantiated.</param>
        /// <param name="name">The name of the variable being instantiated.</param>
        /// <param name="value">The string representation of the value to be assigned.</param>
        /// <param name="withLineEnding">True if that call should have a semicolon ending.</param>
        /// <returns>String representation of the variable instantiation statement.</returns>
        private static string Instantiate(VarType type,
                                          VarName name,
                                          string value,
                                          bool withLineEnding) =>
            $"{GetString(type)} {GetString(name)} = {value}" + (withLineEnding ? ";\n" 
                                                                               : string.Empty);

        /// <summary>
        /// Generates a string representation of variable initialization code.
        /// </summary>
        /// <param name="state">The  state of the TestClass used in the test case; for example,
        /// null or initialized.</param>
        /// <param name="firstLocalVarType">The type of the first local variable (separate from a
        /// local instance of TestClass) used in the test case.</param>
        /// <param name="secondLocalVarType">The type of the second local variable (separate from a
        /// local instance of TestClass) used in the test case.</param>
        /// <param name="firstLocalVarValue">The string representation of the value to be assigned
        /// to the first local variable.</param>
        /// <param name="secondLocalVarValue">The string representation of the value to be assigned
        /// to the second local variable.</param>
        /// <returns>String representing the set of statements needed to initialize the testcase
        /// variables.</returns>
        public static string InitVars(TestClassState state = TestClassState.None,
                                      VarType firstLocalVarType = VarType.None,
                                      VarType secondLocalVarType = VarType.None,
                                      string firstLocalVarValue = null,
                                      string secondLocalVarValue = null)
        {
            string output;
            switch (state)
            {
                case TestClassState.None:
                    output = string.Empty;
                    break;
                case TestClassState.Initialized:
                    output = Declare(VarType.TestClass, VarName.Tc) + Assign(VarName.Tc,
                                                                             "new TestClass()");
                    break;
                case TestClassState.Null:
                    output = Declare(VarType.TestClass, VarName.Tc) + Assign(VarName.Tc, "null");
                    break;
                case TestClassState.Uninitialized:
                    output = Declare(VarType.TestClass, VarName.Tc);
                    break;
                default:
                    throw new NotImplementedException("Unhandled TestClassState");
            }
            if (firstLocalVarType != VarType.None && firstLocalVarValue != null)
            {
                output += Declare(firstLocalVarType, VarName.FirstLocal) +
                          Assign(VarName.FirstLocal, firstLocalVarValue);
            }
            if (secondLocalVarType != VarType.None && secondLocalVarValue != null)
            {
                output += Declare(secondLocalVarType, VarName.SecondLocal) +
                          Assign(VarName.SecondLocal, secondLocalVarValue);
            }

            return output;
        }

        /// <summary>
        /// Method for decorating a code block with a lock statement.
        /// </summary>
        /// <param name="codeBlock">The code block to be decorated.</param>
        /// <returns>The code block enclosed in the lock statement.</returns>
        public static string EncloseInLock(string codeBlock) 
            => "lock(_object) { " + codeBlock + " }";

        /// Generates a string representation of exception handling block code.
        /// </summary>
        /// <param name="resourceLocalVarType">The type of the local resource variable used in the 
        /// test case.</param>
        /// <param name="resourceLocalVarValue">The string representation of the value to be 
        /// assigned to the local resource variable.</param>
        /// <param name="disposeResource">The string representation of disposing the instantiated 
        /// local resource.</param>
        /// <param name="blockKind">The kind of the exception handling block used in the test case; 
        /// for example, try-catch-finally or using.</param>
        /// <returns>String representing the set of statements in the exception handling 
        /// block.</returns>
        public static string InitBlock(VarType resourceLocalVarType = VarType.None,
                                       string resourceLocalVarValue = null,
                                       string disposeResource = null,
                                       BlockKind blockKind = BlockKind.None)
        {
            string output;
            var resourceInit = Declare(resourceLocalVarType, VarName.FirstLocal);
            switch (blockKind)
            {
                case BlockKind.None:
                    output = string.Empty;
                    break;
                case BlockKind.Using:
                    if (resourceLocalVarType != VarType.None && resourceLocalVarValue != null)
                    {
                        resourceInit = Instantiate(resourceLocalVarType,
                                                   VarName.FirstLocal,
                                                   resourceLocalVarValue,
                                                   false);
                    }
                    output =
                        $@"using({resourceInit})
                        {{

                        }}";
                    break;
                case BlockKind.TryCatchFinally:
                    if (resourceLocalVarType != VarType.None)
                    {
                        resourceInit += Assign(VarName.FirstLocal, "null");
                    }
                    if (disposeResource == null)
                    {
                        disposeResource = "";
                    }
                    var tryBlockCode =
                        resourceLocalVarValue == null ? string.Empty 
                                                      : Assign(VarName.FirstLocal,
                                                               resourceLocalVarValue);
                    output =
                        $@"{resourceInit}
                        try
                        {{
                            {tryBlockCode}
                        }}
                        catch(System.IO.IOException e)
                        {{
                            Console.WriteLine(e.Message);
                        }}
                        finally
                        {{
                            {disposeResource}
                        }}";
                    break;
                case BlockKind.NestedTryCatchFinally:
                    if (resourceLocalVarType != VarType.None)
                    {
                        resourceInit += Assign(VarName.FirstLocal, "null");
                    }
                    if (disposeResource == null)
                    {
                        disposeResource = "";
                    }
                    tryBlockCode =
                        resourceLocalVarValue == null ? string.Empty 
                                                      : Assign(VarName.FirstLocal,
                                                               resourceLocalVarValue);
                    output =
                        $@"{resourceInit}
                        try
                        {{
                            try
                            {{
                                {tryBlockCode}
                            }}
                            catch(System.IO.IOException e)
                            {{
                                Console.WriteLine(e.Message);
                            }}
                        }}
                        catch(System.IO.IOException e)
                        {{
                            Console.WriteLine(e.Message);
                        }}
                        finally
                        {{
                            {disposeResource}
                        }}";
                    break;
                case BlockKind.TryCatchWhenFinally:
                    if (resourceLocalVarType != VarType.None)
                    {
                        resourceInit += Assign(VarName.FirstLocal, "null");
                    }
                    if (disposeResource == null)
                    {
                        disposeResource = "";
                    }
                    tryBlockCode =
                        resourceLocalVarValue == null ? string.Empty 
                                                      : Assign(VarName.FirstLocal,
                                                               resourceLocalVarValue);
                    output =
                        $@"{resourceInit}
                        try
                        {{
                            try
                            {{
                                {tryBlockCode}
                            }}
                            catch(Exception e) when (e is System.IO.IOException)
                            {{
                                Console.WriteLine(e.Message);
                            }}
                        }}
                        catch(System.IO.IOException e)
                        {{
                            Console.WriteLine(e.Message);
                        }}
                        finally
                        {{
                            {disposeResource}
                        }}";
                    break;
                default:
                    throw new NotImplementedException("Unhandled BlockState");

            }

            return output;
        }

        /// <summary>
        /// Method for generating a string representation of a call to a non-TestClass method.
        /// </summary>
        /// <param name="callingVar">The variable that calls this method.</param>
        /// <param name="methodName">The method name to be called.</param>
        /// <param name="args">The list of arguments of the method.</param>
        /// <returns>A string representation of the method call.</returns>
        public static string CallMethod(VarName callingVar,
                                        string methodName,
                                        string[] args = null)
        {
            var concatArgs = args == null ? string.Empty : string.Join(",", args);
            return GetString(callingVar) + "." + methodName +
                        "(" + concatArgs + ");\n";
        }

        /// <summary>
        /// Method for generating a string representation of a returned variable.
        /// </summary>
        /// <param name="returnedVar">The variable that is returned in this statement.</param>
        /// <returns>A string representation of the return statement.</returns>
        public static string ReturnVar(VarName returnedVar)
        {
            return "return " + GetString(returnedVar) + ";\n";
        }

        /// <summary>
        /// Method for generating a string representation of a call to a method of TestClass.
        /// </summary>
        /// <param name="method">The method to be called.</param>
        /// <param name="withLineEnding"><c>true</c> if that call should have a semicolon ending,
        /// <c>false</c> otherwise.</param>
        /// <param name="args">The list of arguments of the method.</param>
        /// <returns>A string representation of the method call.</returns>
        public static string CallTestClassMethod(TestClassMethod method,
                                                 bool withLineEnding,
                                                 string[] args = null)
        {
            switch (method)
            {
                case TestClassMethod.ReturnInitializedMemoryStream:
                    if (args != null)
                    {
                        throw new ArgumentException(
                            "ReturnInitializedMemoryStream requires no argument.");
                    }
                    return GetMethodCall(true);
                case TestClassMethod.ReturnInitializedStreamReader:
                    if (args != null)
                    {
                        throw new ArgumentException(
                            "ReturnInitializedStreamReader requires no argument.");
                    }
                    return GetMethodCall(true);
                case TestClassMethod.CloseStream:
                    if (args == null || args.Length != 1)
                    {
                        throw new ArgumentException(
                            "CloseStream requires one argument.");
                    }
                    return GetMethodCall(true);
                case TestClassMethod.InitializeStreamReaderObjectField:
                    if (args != null)
                    {
                        throw new ArgumentException(
                            "InitializeStreamReaderObjectField requires no argument.");
                    }
                    return GetMethodCall(false);
                case TestClassMethod.None:
                    return string.Empty;
                case TestClassMethod.ExpectNonNullParam:
                    if (args == null || args.Length != 1)
                    {
                        throw new ArgumentException(
                            "ExpectNonNullParam requires 1 argument.");
                    }
                    return GetMethodCall(true);
                case TestClassMethod.ReturnNullOnFalse:
                    if (args == null || args.Length != 1)
                    {
                        throw new ArgumentException(
                            "ReturnNullOnFalse requires 1 arguments.");
                    }
                    return GetMethodCall(true);
                case TestClassMethod.IncrementRefParameter:
                    if (args == null || args.Length != 1)
                    {
                        throw new ArgumentException(
                            "IncrementRefParameter requires 1 argument.");
                    }
                    return GetMethodCall(true);
                case TestClassMethod.IncrementStructFieldViaAddress:
                    if (args == null || args.Length != 1)
                    {
                        throw new ArgumentException(
                            "IncrementStructFieldViaAddress requires 1 argument.");
                    }
                    return GetMethodCall(true);
                case TestClassMethod.InitializeStaticObjectField:
                    if (args == null || args.Length != 1)
                    {
                        throw new ArgumentException(
                            "InitializeStaticObjectField requires 1 argument.");
                    }
                    return GetMethodCall(true);
                case TestClassMethod.InitializeStaticObjectFieldViaReference:
                    if (args == null || args.Length != 1)
                    {
                        throw new ArgumentException(
                            "InitializeStaticObjectFieldViaReference requires 1 argument.");
                    }
                    return GetMethodCall(true);
                case TestClassMethod.InitializeInstanceObjectField:
                    if (args == null || args.Length != 1)
                    {
                        throw new ArgumentException(
                            "InitializeInstanceObjectField requires 1 argument.");
                    }
                    return GetMethodCall(false);
                case TestClassMethod.InitializeInstanceObjectFieldViaReference:
                    if (args == null || args.Length != 1)
                    {
                        throw new ArgumentException(
                            "InitializeInstanceObjectFieldViaReference requires 1 argument.");
                    }
                    return GetMethodCall(false);
                case TestClassMethod.ReturnElementFromInstanceArrayField:
                    if (args == null || args.Length != 1)
                    {
                        throw new ArgumentException(
                            "ReturnElementFromInstanceArrayField requires 1 argument.");
                    }
                    return GetMethodCall(false);
                case TestClassMethod.ReturnOneDimArray:
                    if (args == null || args.Length != 2)
                    {
                        throw new ArgumentException("ReturnOneDimArray requires 2 arguments.");
                    }
                    return GetMethodCall(true);
                case TestClassMethod.ReturnTwoDimArray:
                    if (args == null || args.Length != 1)
                    {
                        throw new ArgumentException("ReturnTwoDimArray requires 1 argument.");
                    }
                    return GetMethodCall(true);
                case TestClassMethod.TestBox:
                    if (args == null || args.Length != 1)
                    {
                        throw new ArgumentException("TestBox requires 1 argument.");
                    }
                    return GetMethodCall(true);
                case TestClassMethod.TestIsInst:
                    if (args == null || args.Length != 1)
                    {
                        throw new ArgumentException("TestIsInst requires 1 argument.");
                    }
                    return GetMethodCall(true);
                case TestClassMethod.TestStarg:
                    if (args == null || args.Length != 2)
                    {
                        throw new ArgumentException("TestStarg requires 2 arguments");
                    }
                    return GetMethodCall(true);
                default:
                    throw new NotImplementedException("Unhandled TestClassMethod.");
            }
            string GetMethodCall(bool isStatic)
            {
                var enclosingObject = isStatic ?
                    GetString(VarType.TestClass) : GetString(VarName.Tc);
                var concatArgs = args == null ? string.Empty : string.Join(",", args);
                var callEnding = withLineEnding ? ";\n" : string.Empty;
                return enclosingObject + "." + method.ToString() +
                        "(" + concatArgs + ")" + callEnding;
            }
        }

        /// <summary>
        /// Helper method for generating boolean formula test code for validating that the
        /// translation of an operator is correctly understood by Infer.
        /// It handles three types of formulae, where v1, v2, valueToBeComparedAgainst are
        /// primitive values:
        ///     1. Validate unary operator: op1 x op2 y, i.e. for operator "-", -3 == -3
        ///     2. Validate binary operator: (x op1 y) op2 y, i.e. for operator "+", (3 + 5) == 8
        ///     3. Validate comparison operator: x op1 y, i.e. for operator ">", 4 > 3
        /// In cases (1) and (2), both op2 and valueToBeComparedAgainst should be provided.
        /// </summary>
        /// <param name="type">The expression type, among the three options.</param>
        /// <param name="firstOperator">The string representation of the first operator.</param>
        /// <param name="secondOperator">The string representation of the second operator.</param>
        /// <param name="valueToBeComparedAgainst">For cases (1) and (2), this is the value
        /// against which the expression is compared. For case 3, it should not be
        /// provided.</param>
        public static string GenerateSingleComparisonIfCondition(
            BooleanTestType type,
            string firstOperator,
            string secondOperator = null,
            string valueToBeComparedAgainst = null)
        {
            string booleanFormula;
            switch (type)
            {
                case BooleanTestType.Binary:
                    if (secondOperator == null || valueToBeComparedAgainst == null)
                    {
                        throw new ArgumentException(
                            "op2 and valueToBeComparedAgainst must be provided.");
                    }
                    booleanFormula =
                        $@"({GetString(VarName.FirstLocal)} {firstOperator} 
                            {GetString(VarName.SecondLocal)}) {secondOperator}
                            {valueToBeComparedAgainst}";
                    break;
                case BooleanTestType.Comparison:
                    booleanFormula = $@"{GetString(VarName.FirstLocal)} 
                                        {firstOperator}
                                        {GetString(VarName.SecondLocal)}";
                    break;
                case BooleanTestType.Unary:
                    if (secondOperator == null || valueToBeComparedAgainst == null)
                    {
                        throw new ArgumentException(
                            "op2 and valueToBeComparedAgainst must be provided.");
                    }

                    booleanFormula = $@"{firstOperator} {GetString(VarName.FirstLocal)}
                                        {secondOperator}
                                        {valueToBeComparedAgainst}";
                    break;
                default:
                    throw new NotImplementedException("Unhandled BooleanTestType");
            }
            return "if (" + booleanFormula + ")";
        }
    }
}
