
using Microsoft.AspNetCore.Mvc;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using subproj;
using System.Data.SqlTypes;
using System.Xml;

public class IsDisposedBooleanField : IDisposable
{
    private bool isDisposed;
    private CancellationTokenSource tok;

    public IsDisposedBooleanField()
    {
        tok = new CancellationTokenSource();
    }

    public IsDisposedBooleanField(bool input)
    {
        tok = new CancellationTokenSource();
        isDisposed = input;
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            isDisposed = true;
            tok.Dispose();
        }
    }
}

// Expect 3 TAINT_ERROR for SQL injection flows.
public class PulseTaintTests
{
    [HttpPost]
    static void sqlBadInt(int InputParameter)
    {
        subproj.WeatherForecast.runSqlCommandBad(InputParameter.ToString());
    }

    [HttpPost]
    static void sqlBadString(string InputParameter)
    {
        subproj.WeatherForecast.runSqlCommandBad(InputParameter);
    }

    [HttpPost]
    static void sqlParameterizedOk(int InputParameter)
    {
        subproj.WeatherForecast.runSqlCommandParameterized(InputParameter.ToString());
    }

    [HttpPost]
    static void sqlStoredProcedureOk(string InputParameter)
    {
        subproj.WeatherForecast.runSqlCommandStoredProcedure(InputParameter.ToString());
    }

    [HttpGet]
    public void SearchRawData(string query)
    {
        var queryPrefix = "prefix";
        using (var conn = new SqlConnection("readerConnectionString"))
        {
            using (var command = new SqlCommand(queryPrefix + query))
            {
                try
                {
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        Console.Write("Hello");
                    }
                }
                catch (Exception ex)
                {
                    Console.Write(ex.Message);
                    return;
                }
            }
        }
    }

}


// Expect a thread safety violation issue
public class ThreadSafety
{
    int mBalance = 0;
    private readonly object balanceLock = new object();

    public void deposit(int amount)
    {
        if (amount > 0)
        {
            lock (balanceLock)
            {
                mBalance += amount;
            }
        }
    }

    public int withdraw(int amount)
    {
        if (amount >= 0 && mBalance - amount >= 0)
        {
            mBalance -= amount;
            return mBalance;
        }
        else
        {
            return 0;
        }
    }

    /// <summary>
    /// An example with no null dereference expected.
    /// </summary>
    public static string NullDeReferenceOK()
    {
        return "abc";
    }

    /// <summary>
    /// An example with null dereference error expected.
    /// </summary>
    public static string NullDeReferenceBad()
    {
        return null;
    }
}

public class MainClass
{
    public static void Main(string[] args)
    {
        // FIXME: should close the global streams by calling p.Cleanup()
        // Null dereference error report expected.
        ThreadSafety.NullDeReferenceBad().GetHashCode();
        // No null dereference error report expected.
        ThreadSafety.NullDeReferenceOK().GetHashCode();
    }
}

// 17 reports expected (18 with --pulse-increase-leak-recall flag)
class InferResourceLeakTests
{
    private static byte[] myBytes = new byte[] { 10, 4 };

    /// <summary>
    /// This is a false positive that occurs when we return a class that owns IDisposable types.
    /// This occurs because the setter method at the end of the async MoveNext() method which
    /// retains the new object (and therefore the underlying IDisposable type) in memory is not
    /// modeled.
    /// </summary>
    public async Task<TakeAndDispose> ReturnFileStreamTaskFalsePositive()
    {
        var stream = new FileStream("", FileMode.Open);
        return new TakeAndDispose(stream);
    }

    /// <summary>
    /// Validates that default initializations of boolean fields i.e. indicating disposal to false
    /// is taken into account and applied to conditional disposal.
    /// </summary>
    public static void UsingOnCustomIDisposableWithBooleanFieldOK()
    {
        using (var custom = new IsDisposedBooleanField());
    }

    /// <summary>
    /// Validates that default initializations of boolean fields i.e. indicating disposal to false
    /// is taken into account and applied to conditional disposal. Should report because the bool
    /// is initialized to true, not false, and therefore disposal occurs.
    /// </summary>
    public static void UsingOnCustomIDisposableWithBooleanFieldTrueShouldReport()
    {
        using (var custom = new IsDisposedBooleanField(true));
    }

    /// <summary>
    /// The "using" construct applies the generic IDisposable dispose in the bytecode, which is 
    /// problematic when it is applied to a custom IDisposable object -- if translated directly,
    /// this prevents the analysis from applying the spec it has for the custom IDisposable object
    /// which in turn can cause false positive alerts for underlying IDisposable fields.
    /// </summary>
    public static void UsingOnCustomIDisposableOK()
    {
        Stream stream = new FileStream("MyFile", FileMode.Open);
        using TakeAndDispose tad = new TakeAndDispose(stream);
    }

    /// <summary>
    /// Tests that the model of Write for throwing an exception (thus resource not getting
    /// disposed) works.
    /// </summary>
    public static void UsageCanThrowShouldReport()
    {
        FileStream fs = new FileStream("MyFile.txt", FileMode.Open, FileAccess.Read);
        fs.Write(myBytes, 0, 1); // this line can throw an exception, preventing future disposal
        fs.Dispose();
    }

    /// <summary>
    /// Tests that using (finally) correctly handles disposal despite possible exception.
    /// </summary>
    public static void UsageCanThrowOk()
    {
        using (var fs = new FileStream("MyFile.txt", FileMode.Open, FileAccess.Read))
        { // the using properly handles the exception.
            fs.Write(myBytes, 0, 1); // this line can throw an exception
        }
    }

    /// <summary>
    /// Tests that an exception getting thrown causes the leak to be reported.
    /// </summary>
    public static void ThrowBetweenConstructionAndDisposeShouldReport()
    {
        var stream1 = new FileStream("MyFile.txt", FileMode.Open);
        throw new FileNotFoundException();
        stream1.Dispose();
    }

    /// <summary>
    /// False negative: the throw that occurs in another method doesn't get propagated here.
    /// </summary>
    public static void CallThrowingMethodBetweenConstructionAndDisposeShouldReport()
    { // this doesn't report since the fact that doesThrow() doesn't get propageted here.
        var stream1 = new FileStream("MyFile.txt", FileMode.Open);
        doesThrow();
        stream1.Dispose();
    }

    /// <summary>
    /// Tests that conditional exception (with a boolean value inducing it) works. 
    /// </summary>
    public static void CallThrowingMethodBetweenConstructionAndDisposeShouldReport2()
    {
        var stream1 = new FileStream("MyFile.txt", FileMode.Open);
        doesThrowOrDispose(true, stream1);
    }

    public static void doesThrow()
    {
        throw new ArgumentException("Hi");
    }


    public static void doesThrowOrDispose(bool b, Stream s)
    {
        if (b)
        {
            throw new ArgumentException("Hi");
        }

        s.Dispose();
    }

    /// <summary>
    /// Basic allocation/dispose -- should be not report an issue.
    /// </summary>
    public static void ConstructingOneOk()
    {
        var stream1 = new FileStream("NotAFile.bad", FileMode.Open);
        stream1.Dispose();
    }

    /// <summary>
    /// Validates that delegation of the underlying stream to StreamReader works properly.
    /// </summary>
    public static void DelegatingToConstructorOk()
    {
        var stream1 = new FileStream("SomeFile.txt", FileMode.Open);
        var stream2 = new StreamReader(stream1);

        stream2.Dispose();
    }

    /// <summary>
    /// Validates that the first allocation of the resource is correctly reported as lost.
    /// </summary>
    public static void DelegatingResourceToResourceShouldReport()
    {
        // this first filestream is lost and never disposed, this is where we should report
        var fs = new FileStream("SomeFile.txt", FileMode.Open);
        // this filestream is the stream delegated to the bufferedstream and disposed with it
        fs = new FileStream("SomeFile.txt", FileMode.Open);
        var sr = new StreamReader(fs);
        sr.Dispose();
    }
    
    /// <summary>
    /// Validates that IDisposable resources which are exceptions that shouldn't be reported on
    /// indeed are not reported.
    /// </summary>
    public static void UsagesNotTrackedOk()
    {
        MemoryStream ms = new MemoryStream(10);
        ms.Write(myBytes, 0, 1);

        StringReader sr = new StringReader("MyString");
        sr.Read();

        StringWriter sw = new StringWriter();
        sw.Write("Hello");

        // there is no call to dispose/close, but MemoryStream has no resources, so we don't track it
        // If we did this with almost any other resource we should report.
    }

    /// <summary>
    /// Further validation of exceptions to disposing of IDisposables.
    /// </summary>
    public static void UsageNotTrackedDelegatedOk()
    {
        var stream1 = new MemoryStream();
        var streamReader = new StreamReader(stream1);

        streamReader.Dispose();
    }

    /// <summary>
    /// Validates that the exception getting thrown (preventing the dispose) is correctly interpreted.
    /// </summary>
    public static void UsageNotTrackedDelegatedShouldReport()
    {
        var stream1 = new MemoryStream();
        var streamReader = new StreamReader(stream1);

        // potential exception here means the stream reader may not be disposed.
        var fs = new FileStream("MyFile.txt", FileMode.Open);
        fs.Write(myBytes, 0, 1);
        fs.Dispose();

        streamReader.Dispose();
    }

    /// <summary>
    /// Validates that interprocedural allocation and disposal is correctly handled.
    /// </summary>
    public static void UseMethodToCreateStreamAndDisposeOk()
    {
        var fs = CreateStreamOk();
        fs.Close();
    }

    /// <summary>
    /// Validates that interprocedural allocation (no disposal) is correctly reported.
    /// </summary>
    public static void UseMethodToCreateStreamAndLeakShouldReport()
    {
        var fs = CreateStreamOk();
    }

    /// <summary>
    /// Validates that a returned StreamReader/underlying stream is not reported.
    /// </summary>
    public static StreamReader CreateStreamReaderAndReturnOk()
    {
        var fs = new FileStream("SomeFile.txt", FileMode.Append);
        var sr = new StreamReader(fs);
        return sr;
    }

    public static FileStream CreateStreamOk()
    {
        return new FileStream("MyFile.txt", FileMode.Create);
    }

    /// <summary>
    /// Validates that a user-defined IDisposable is reported.
    /// </summary>
    public static void LeakCustomDisposableShouldReport()
    {
        var tad = new TakeAndDispose(CreateStreamOk());
    }

    /// <summary>
    /// Validates that delegation to user-defined IDisposable is handled.
    /// </summary>
    public static TakeAndDispose PassDisposableToCustomDisposableOk()
    {
        Stream stream = new FileStream("MyFile", FileMode.Open);
        return new TakeAndDispose(stream);
    }

    /// <summary>
    /// Validates that delegation to and disposal of a user-defined IDisposable is handled.
    /// </summary>
    public static void PassDisposableToCustomDisposableAndDisposeOk()
    {
        Stream stream = new FileStream("MyFile", FileMode.Open);
        TakeAndDispose tad = new TakeAndDispose(stream);
        tad.Dispose();
    }

    /// <summary>
    /// Validates that failure to dispose of a user-defined IDisposable (with delegated resource)
    /// is reported.
    /// </summary>
    public static void PassDisposableToCustomDisposableAndDisposeShouldReport()
    {
        Stream stream = new FileStream("MyFile", FileMode.Open);
        TakeAndDispose tad = new TakeAndDispose(stream);
        stream.Dispose();
    }

    /// <summary>
    /// Validates that delegation to user-defined IDisposable (no Dispose of underlying) is 
    /// handled.
    /// </summary>
    public static TakeWithoutDispose PassDisposableToCustomDisposable2Ok()
    {
        Stream stream = new FileStream("MyFile", FileMode.Open);
        return new TakeWithoutDispose(stream);
    }

    /// <summary>
    /// Validates that delegation to user-defined IDisposable (no Dispose of underlying) is
    /// correctly handled.
    /// </summary>
    public static void PassDisposableToCustomDisposableAndDispose2ShouldReport()
    {
        Stream stream = new FileStream("MyFile", FileMode.Open);
        TakeWithoutDispose twd = new TakeWithoutDispose(stream);
        twd.Dispose();
    }

    /// <summary>
    /// Validates that delegation to user-defined non-IDisposable is correctly handled.
    /// </summary>
    public static void PassDisposableToCustomClassWithDisposeAndDisposeOk()
    {
        Stream stream = new FileStream("MyFile", FileMode.Open);
        var tadnd = new TakeAndDisposeNotDisposable(stream);
        tadnd.Dispose();
    }

    /// <summary>
    /// Validates that return of delegated stream to non-IDisposable is correctly handled.
    /// </summary>
    public static TakeAndDisposeNotDisposable PassDisposableToCustomClassWithDisposeOk()
    {
        Stream stream = new FileStream("MyFile", FileMode.Open);
        return new TakeAndDisposeNotDisposable(stream);
    }

    /// <summary>
    /// Should be no leak, as all resources are allocated via using.
    /// </summary>
    public static string NestedUsingWithThrownException()
    {
        using (var x = new StreamReader(""))
        {
            Console.Write("in first using");
            using (var y = new StreamReader("2"))
            {
                Console.Write("hello world again");
                if (y != null)
                {
                    throw new Exception();
                }
            }
        }
        return "done";
    }

    /// <summary>
    /// Validates that upward casting of stream to IDisposable is not reported on.
    /// </summary>
    public static IDisposable CastingOk()
    {
        FileStream myStream = new FileStream("MyFile", FileMode.Open);

        return myStream;
    }


    /// <summary>
    /// Validates that initialization of out variable is correctly not reported on.
    /// </summary>
    public static void OutStreamOk(out FileStream myStream)
    {
        myStream = new FileStream("myFile.txt", FileMode.OpenOrCreate);
    }

    // unknown function behavior test
    public static void UnknownCallIsNopReport()
    { // reports if the unknown functions do *not* havoc arguments.
        FileStream myStream = new FileStream("MyFile", FileMode.OpenOrCreate);
        myStream.ToString();
    }

    private static SqlXml AddRbioProtocolSlimShouldReport(SqlXml remoteReplicas)
    { // only reports with flag --pulse-increase-leak-recall.
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(remoteReplicas.Value);
        var nodeReader = new XmlNodeReader(xmlDoc.DocumentElement);

        // SqlXml is unknown, so nodeReader should be leaked here.
        return new SqlXml(nodeReader);
    }
}


public class TakeAndDispose : IDisposable
{
    private bool disposedValue;
    private Stream stream;

    public TakeAndDispose(Stream stream)
    {
        this.disposedValue = false;
        this.stream = stream;
    }

    public virtual void Dispose()
    {
        if (!disposedValue)
        {

            stream.Dispose();



            disposedValue = true;
        }
    }
}

public class TakeWithoutDispose : IDisposable
{
    private bool disposedValue;
    private Stream stream;

    public TakeWithoutDispose(Stream stream)
    {
        this.disposedValue = false;
        this.stream = stream;
    }

    public virtual void Dispose()
    {
        if (!disposedValue)
        {
            //stream.Dispose();
            disposedValue = true;
        }
    }
}

public class TakeAndDisposeNotDisposable
{
    private Stream stream;

    public TakeAndDisposeNotDisposable(Stream stream)
    {
        this.stream = stream;
    }

    public void Dispose()
    {
        stream.Dispose();
    }
}


public class MultipleConstructors
{
    private readonly FileStream _myStream;

    public MultipleConstructors(String filename, Boolean c) : this(new FileStream(filename, FileMode.Open), c)
    { // should report, as if c=true, we the FileStream is never stored.

    }

    public MultipleConstructors(FileStream myStream, Boolean c)
    {
        if (c)
        {
            throw new ArgumentException("Just because");
        }

        _myStream = myStream;
    }

    public MultipleConstructors(String filename) : this(new FileStream(filename, FileMode.Open))
    { // should report as the FileStream is never stored.

    }

    public MultipleConstructors(FileStream myStream)
    {
        throw new ArgumentException("Just because");

        _myStream = myStream;
    }

    /// <summary>
    /// Although the field is indeed stored, the object goes out of scope and leaks the field. 
    /// </summary>
    public void InvokeConstructorFalseBooleanShouldReport()
    {
        var x = new MultipleConstructors("test", false);
    }
}


internal class ArrayTest
{
    public static readonly int ArraySize = 5;

    /// <summary>
    /// Should not report, stream gets closed.
    /// </summary>
    public static void straightLineCodeOk()
    {
        FileStream[] streams = new FileStream[ArraySize];

        streams[0] = new FileStream("hi", FileMode.Create);

        streams[0].Close();
    }

    /// <summary>
    /// Should not report, stream is returned in array.
    /// </summary>
    public static FileStream[] straightLineCodeReturnedOk()
    {
        FileStream[] streams = new FileStream[ArraySize];

        streams[0] = new FileStream("hi", FileMode.Create);

        return streams;
    }

    /// <summary>
    /// Should not report, underlying stream gets closed.
    /// </summary>
    public static void straightLineCodeWithVariantOk()
    {
        FileStream[] streams = new FileStream[ArraySize];

        FileStream stream = new FileStream("hi", FileMode.Create);
        streams[0] = stream;

        stream.Close();
    }

    /// <summary>
    /// Should not report, stream gets allocated/freed in loop.
    /// </summary>
    public static void LoopingCodeOk()
    {
        FileStream[] streams = new FileStream[ArraySize];

        for (int i = 0; i < ArraySize; i++)
        {
            streams[i] = new FileStream("hi", FileMode.Create);
        }

        for (int i = 0; i < ArraySize; i++)
        {
            streams[i].Close();
        }
    }

    /// <summary>
    /// Should not report, streams allocated in loop and returned.
    /// </summary>
    public static FileStream[] LoopingCodeReturnedOk()
    {
        FileStream[] streams = new FileStream[ArraySize];

        for (int i = 0; i < ArraySize; i++)
        {
            streams[i] = new FileStream("hi", FileMode.Create);
        }

        return streams;
    }
}

internal class ListTest
{
    private readonly List<FileStream> _streamList = new List<FileStream>();
    private readonly Dictionary<String, FileStream> _streamDict = new Dictionary<string, FileStream>();

    public static readonly int ListSize = 5;

    /// <summary>
    /// Should not report, stream is closed (false positive -- ElementAt model)
    /// </summary>
    public static void straightLineCodeFalsePositive()
    { // reports as no model for ElementAt().
        List<FileStream> streams = new List<FileStream>();

        streams.Add(new FileStream("hi", FileMode.Create));

        streams.ElementAt(0).Close();
    }

    /// <summary>
    /// Should not report, stream is returned in list.
    /// </summary>
    public static List<FileStream> straightLineCodeReturnedOk()
    {
        List<FileStream> streams = new List<FileStream>();

        streams.Add(new FileStream("hi", FileMode.Create));

        return streams;
    }

    /// <summary>
    /// Should not report, underlying stream closed.
    /// </summary>
    public static void straightLineCodeWithVariantOk()
    {
        List<FileStream> streams = new List<FileStream>();

        FileStream stream = new FileStream("hi", FileMode.Create);
        streams.Add(stream);

        stream.Close();
    }

    /// <summary>
    /// Should not report, streams allocated/closed in loop (false positive -- ElementAt model).
    /// </summary>
    public static void LoopingCodeFalsePositive()
    { // reports as no model for ElementAt()
        List<FileStream> streams = new List<FileStream>();

        for (int i = 0; i < ListSize; i++)
        {
            streams.Add(new FileStream("hi", FileMode.Create));
        }

        for (int i = 0; i < ListSize; i++)
        {
            streams.ElementAt(i).Close();
        }
    }

    /// <summary>
    /// Should not report, streams allocated/stored in loop, returned.
    /// </summary>
    /// <returns></returns>
    public static List<FileStream> LoopingCodeReturnedOk()
    {
        List<FileStream> streams = new List<FileStream>();

        for (int i = 0; i < ListSize; i++)
        {
            streams.Add(new FileStream("hi", FileMode.Create));
        }

        return streams;
    }

    /// <summary>
    /// Should not report, streams added to instance collection.
    /// </summary>
    public void AddToFieldOk()
    {
        FileStream stream = new FileStream("hi", FileMode.Create);

        _streamList.Add(stream);

        FileStream stream2 = new FileStream("hi", FileMode.Create);

        _streamDict.Add("mykey", stream2);
    }
}