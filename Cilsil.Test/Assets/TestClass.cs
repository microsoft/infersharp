// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.IO;

namespace Cilsil.Test.Assets
{
    public class TestClass
    {
        public TestClass InstanceObjectField;
        public static TestClass StaticObjectField;
        public TestClass[] InstanceArrayField;
        public static int StaticIntegerField;

        public record TestClassRecord { public string Name { get; init; } }

        /// <summary>
        /// Initializes a static readonly field resource. This kind of resource should not be disposed 
        /// as it will be reserved for singleton design pattern.
        /// </summary>
        public static readonly StreamReader StaticStreamReaderField = new StreamReader("whatever.txt");

        public StreamReader InstanceStreamReaderField;

        public struct InternalStruct
        {
            public int i;
        }

        public struct TestStruct
        {
            public InternalStruct internalStruct;
            public float floatField;
            public bool boolField;
            public char charField;
            public TestClass objectField;
        }

        public TestClass() { }

        public TestClass(string filename)
        {
            InstanceStreamReaderField = new StreamReader(filename);
        }

        /// <summary>
        /// Initializes the instance TestClass field. In test cases, this method provides coverage
        /// for stfld.
        /// </summary>
        /// param name="initializeToNull">if set to <c>true</c>, set the field to null. If set to
        /// <c>false</c>, initialize the field.</param>
        public void InitializeInstanceObjectField(bool initializeToNull)
        {
            InstanceObjectField = initializeToNull ? null : new TestClass();
        }

        /// <summary>
        /// Initializes the instance TestClass field via reference. In test cases, this method
        /// provides coverage for ldflda.
        /// </summary>
        /// <param name="initializeToNull">if set to <c>true</c>, set the field to null. If set to
        /// <c>false</c>, initialize the field.</param>
        public void InitializeInstanceObjectFieldViaReference(bool initializeToNull)
        {
            ref TestClass testClass = ref InstanceObjectField;
            testClass = initializeToNull ? null : new TestClass();
        }

        /// <summary>
        /// Initializes the instance StreamReader field.
        /// </summary>
        public void InitializeStreamReaderObjectField()
        {
            InstanceStreamReaderField = new StreamReader("whatever.txt");
        }

        /// <summary>
        /// Close the instance StreamReader field interprocedurally.
        /// </summary>
        public void CleanupStreamReaderObjectField()
        {
            InstanceStreamReaderField.Close();
        }

        /// <summary>
        /// Initializes the static TestClass field. In test cases, this method provides coverage for
        /// stsfld.
        /// </summary>
        /// param name="initializeToNull">if set to <c>true</c>, set the field to null. If set to
        /// <c>false</c>, initialize the field.</param>
        public static void InitializeStaticObjectField(bool initializeToNull)
        {
            StaticObjectField = initializeToNull ? null : new TestClass();
        }

        /// <summary>
        /// Initializes the static TestClass field via reference. In test cases, this method
        /// provides coverage for stsflda.
        /// </summary>
        /// <param name="initializeToNull">if set to <c>true</c>, set the field to null. If set to
        /// <c>false</c>, initialize the field.</param>
        public static void InitializeStaticObjectFieldViaReference(bool initializeToNull)
        {
            ref TestClass testClass = ref StaticObjectField;
            testClass = initializeToNull ? null : new TestClass();
        }

        /// <summary>
        /// Returns an initialized TestClass object on true input, and null on false input.
        /// </summary>
        /// <param name="b">If this parameter is true, the function returns an initialized
        /// TestClass object, and if it is false, the function returns null.</param>
        /// <returns>TestClass object if the input is true, and null if it is false.</returns>
        public static TestClass ReturnNullOnFalse(bool b) => b ? new TestClass() : null;

        /// <summary>
        /// This method closes the given StreamReader.
        /// </summary>
        /// <param name="stream">StreamReader to be closed.</param>
        public static void CloseStream(StreamReader stream) => stream.Close();

        /// <summary>
        /// This method returns an initialized StreamReader.
        /// </summary>
        public static StreamReader ReturnInitializedStreamReader()
        {
            return new StreamReader(string.Empty);
        }

        /// <summary>
        /// This method returns an initialized MemoryStream.
        /// </summary>
        public static MemoryStream ReturnInitializedMemoryStream()
        {
            return new MemoryStream(0);
        }

        /// <summary>
        /// This method dereferences the input object via a call to GetHashCode(). This method will
        /// produce a null dereference if and only if the input is null.
        /// </summary>
        /// <param name="t">The <see cref="TestClass"/> to dereference.</param>
        public static void ExpectNonNullParam(TestClass t) => t.GetHashCode();

        /// <summary>
        /// This method returns a one-dimensional TestClass array containing one element. That
        /// element is initialized if the input boolean is true, and is null otherwise.
        /// </summary>
        /// <param name="initializeArrayElement">If this is true, the array's element is
        /// initialized, and if it is not, the array's element is assigned null.</param>
        /// <param name="useBinaryLengthExpression">If this is true, the length expression for the
        /// array is a binary operation. Otherwise, it is a constant.</param>
        /// <returns>TestClass[] with one element that is initialized if the input bool is true
        /// and false otherwise.</returns>
        public static TestClass[] ReturnOneDimArray(bool initializeArrayElement,
                                                    bool useBinaryLengthExpression)
        {
            if (useBinaryLengthExpression)
            {
                var x = 0;
                var array = new TestClass[x + 1];
                array[0] = initializeArrayElement ? new TestClass() : null;
                return array;
            }
            return new TestClass[] { initializeArrayElement ? new TestClass() : null };
        }

        /// <summary>
        /// Returns the element from instance array.
        /// </summary>
        /// <param name="arrayElement">The element to place in the array.</param>
        /// <returns>The array.</returns>
        public static TestClass ReturnElementFromInstanceArrayField(TestClass arrayElement)
        {
            var testClass = new TestClass();
            testClass.InstanceArrayField = new TestClass[] { arrayElement };
            return testClass.InstanceArrayField[0];
        }

        /// <summary>
        /// This method returns a two-dimensional TestClass array containing one element. That
        /// element is initialized if the input boolean is true, and is null otherwise.
        /// </summary>
        /// <param name="initializeArrayElement"></param>
        /// <returns>TestClass[][] with one element that is initialized if the input bool is true
        /// and false otherwise.</returns>
        public static TestClass[][] ReturnTwoDimArray(bool initializeArrayElement) =>
            new TestClass[][] { initializeArrayElement ? new TestClass[] { } : null };

        /// <summary>
        /// Creates a <see cref="TestStruct"/> with default values and increments the integer field
        /// which is member to its struct field. If the incremented value is equal to the input
        /// integer, then a null value is returned. Otherwise, an initialized 
        /// <see cref="TestClass"/> is returned. This method CIL version refers to its fields by
        /// address, hence "ViaAddress" in this method's name.
        /// </summary>
        /// <param name="comparison">The value to compare the number to.</param>
        /// <returns>Null if the incremented field is equal to the input, and an initialized object
        /// otherwise.</returns>
        public static TestClass IncrementStructFieldViaAddress(int comparison)
        {
            // Struct is initialized with default values.
            var testStruct = new TestStruct();
            // Integer field has value 0; incremented value is 1.
            testStruct.internalStruct.i++;
            return testStruct.internalStruct.i == comparison ? null : new TestClass();
        }

        /// <summary>
        /// This method is used for validation of the correct translation of the starg instruction.
        /// Additionally, its usage in the test pipeline provides coverage for the usage of a 
        /// nullable variable as a boolean test expression.
        /// </summary>
        /// <param name="first">The default value to which second is set.</param>
        /// <param name="second">Remains the same if it is not null; else, it is set to 
        /// first.</param>
        /// <returns>The updated value of second.</returns>
        public static TestClass TestStarg(TestClass first, TestClass second)
        {
            // Second remains as is if it is non-null; if it is null, it is set to first.
            second = second ?? first;
            return second;
        }

        /// <summary>
        /// This method is used for validating the increment of an integer that is passed by a
        /// reference.
        /// </summary>
        /// <param name="input">The integer to be incremented.</param>
        public static void IncrementRefParameter(ref int input)
        {
            input++;
        }

        /// <summary>
        /// This method is used for the validation of box and unbox support. It boxes and unboxes
        /// the input integer and compares it to 1, returning null if comparison is true and false
        /// otherwise.
        /// </summary>
        /// <param name="input">The value to be boxed and unboxed, and compared to 1.</param>
        /// <returns>Null if input is equal to 1, and an initialized TestClass object
        /// otherwise.</returns>
        public static TestClass TestBox(int input)
        {
            object boxedValue = input;
            if ((int)boxedValue == 1)
            {
                return null;
            }
            else
            {
                return new TestClass();
            }
        }

        /// <summary>
        /// This method is used for the validation of isinst support. It checks if an object is an
        /// instance of <see cref="TestClass"/>, returning null if so, and an initialized 
        /// <see cref="TestClass"/> otherwise. NB: due to the limitations of static verification,
        /// our translation only captures the null verification component of instance validation.
        /// </summary>
        /// <param name="input">The object whose type is to be tested.</param>
        /// <returns>Null if the input is an instance of <see cref="TestClass"/>, and an
        /// instantiated <see cref="TestClass"/> instance otherwise.</returns>
        public static TestClass TestIsInst(object input)
        {
            if (input is TestClass)
            {
                return null;
            }
            else
            {
                return new TestClass();
            }
        }

        private static void ThrowsFileNotFoundException()
        {
            throw new FileNotFoundException();
        }

        private static void ThrowsIOException()
        {
            throw new IOException();
        }

        /// <summary>
        /// If input <c>true</c>, a certain exception is thrown which when caught results in a null 
        /// object being returned; otherwise, an instantiated object is returned.
        /// </summary>
        /// <param name="input">if <c>true</c>, a null object is returned; otherwise, an
        /// instantiated object is returned.</param>
        /// <returns>A possibly null <see cref="TestClass"/> instance.</returns>
        public static TestClass CatchReturnsNullIfTrue(bool input)
        {
            try
            {
                if (input)
                {
                    ThrowsIOException();
                }
                else
                {
                    ThrowsFileNotFoundException();
                }
            }
            catch (FileNotFoundException)
            {
                return new TestClass();
            }
            catch (IOException)
            {
                return null;
            }
            return new TestClass();
        }

        /// <summary>
        /// Analogous to <see cref="CatchReturnsNullIfTrue(bool)"/>, but with a finally handler.
        /// </summary>
        /// <param name="input">if <c>true</c>, a null object is returned; otherwise, an
        /// instantiated object is returned.</param>
        /// <returns>A possibly null <see cref="TestClass"/> instance.</returns>
        public static TestClass FinallyReturnsNullIfTrue(bool input)
        {
            TestClass output = new TestClass();
            TestClass returnValue = new TestClass();
            try
            {
                if (input)
                {
                    ThrowsIOException();
                }
                else
                {
                    ThrowsFileNotFoundException();
                }
            }
            catch (FileNotFoundException)
            {
                output = new TestClass();
            }
            catch (IOException)
            {
                output = null;
            }
            finally
            {
                returnValue = output;
            }
            return returnValue;
        }

        /// <summary>
        /// No resource leak should be reported, as the allocated stream is closed in finally.
        /// </summary>
        public static void TryFinallyResourceLeak()
        {
            var stream = new StreamReader("file.txt");
            try
            {
                ThrowsIOException();
            }
            finally
            {
                stream.Close();
            }
        }

        /// <summary>
        /// Identical to <see cref="TryFinallyResourceLeak"/>.
        /// </summary>
        public static void TryFinallyResourceLeakUsing()
        {
            using (var stream = new StreamReader("file.txt"))
            {
                ThrowsIOException();
            }
        }
    }
}