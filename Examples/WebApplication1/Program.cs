
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
        isDisposed = true;
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

// Expect 2 TAINT_ERROR for SQL injection flows.
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

// 15 reports expected (16 with --pulse-increase-leak-recall flag)
class InferResourceLeakTests
{
    private static byte[] myBytes = new byte[] { 10, 4 };

    public static void UsingOnCustomIDisposableWithBooleanFieldOK()
    {
        using (var custom = new IsDisposedBooleanField());
    }

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

    public static void UsageCanThrowShouldReport()
    {
        FileStream fs = new FileStream("MyFile.txt", FileMode.Open, FileAccess.Read);
        fs.Write(myBytes, 0, 1); // this line can throw an exception, preventing future disposal
        fs.Dispose();
    }

    public static void UsageCanThrowOk()
    {
        using (var fs = new FileStream("MyFile.txt", FileMode.Open, FileAccess.Read))
        { // the using properly handles the exception.
            fs.Write(myBytes, 0, 1); // this line can throw an exception
        }
    }

    public static void ThrowBetweenConstructionAndDisposeShouldReport()
    {
        var stream1 = new FileStream("MyFile.txt", FileMode.Open);
        throw new FileNotFoundException();
        stream1.Dispose();
    }

    public static void CallThrowingMethodBetweenConstructionAndDisposeShouldReport()
    { // this doesn't report since the fact that doesThrow() doesn't get propageted here.
        var stream1 = new FileStream("MyFile.txt", FileMode.Open);
        doesThrow();
        stream1.Dispose();
    }

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
    public static void ConstructingOneOk()
    {
        var stream1 = new FileStream("NotAFile.bad", FileMode.Open);
        stream1.Dispose();
    }

    public static void DelegatingToConstructorOk()
    {
        var stream1 = new FileStream("SomeFile.txt", FileMode.Open);
        var stream2 = new StreamReader(stream1);

        stream2.Dispose();
    }

    public static void DelegatingResourceToResourceShouldReport()
    {
        // this first filestream is lost and never disposed, this is where we should report
        var fs = new FileStream("SomeFile.txt", FileMode.Open);
        // this filestream is the stream delegated to the bufferedstream and disposed with it
        fs = new FileStream("SomeFile.txt", FileMode.Open);
        var sr = new StreamReader(fs);
        sr.Dispose();
    }

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

    public static void UsageNotTrackedDelegatedOk()
    {
        var stream1 = new MemoryStream();
        var streamReader = new StreamReader(stream1);

        streamReader.Dispose();
    }

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

    public static void UseMethodToCreateStreamAndDisposeOk()
    {
        var fs = CreateStreamOk();
        fs.Close();
    }

    public static void UseMethodToCreateStreamAndLeakShouldReport()
    {
        var fs = CreateStreamOk();
    }

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

    public static void LeakCustomDisposableShouldReport()
    {
        var tad = new TakeAndDispose(CreateStreamOk());
    }

    public static TakeAndDispose PassDisposableToCustomDisposableOk()
    {
        Stream stream = new FileStream("MyFile", FileMode.Open);
        return new TakeAndDispose(stream);
    }

    public static void PassDisposableToCustomDisposableAndDisposeOk()
    {
        Stream stream = new FileStream("MyFile", FileMode.Open);
        TakeAndDispose tad = new TakeAndDispose(stream);
        tad.Dispose();
    }

    public static void PassDisposableToCustomDisposableAndDisposeShouldReport()
    {
        Stream stream = new FileStream("MyFile", FileMode.Open);
        TakeAndDispose tad = new TakeAndDispose(stream);
        stream.Dispose();
    }

    public static TakeWithoutDispose PassDisposableToCustomDisposable2Ok()
    {
        Stream stream = new FileStream("MyFile", FileMode.Open);
        return new TakeWithoutDispose(stream);
    }

    public static void PassDisposableToCustomDisposableAndDispose2ShouldReport()
    {
        Stream stream = new FileStream("MyFile", FileMode.Open);
        TakeWithoutDispose twd = new TakeWithoutDispose(stream);
        twd.Dispose();
    }

    public static void PassDisposableToCustomClassWithDisposeAndDisposeOk()
    {
        Stream stream = new FileStream("MyFile", FileMode.Open);
        var tadnd = new TakeAndDisposeNotDisposable(stream);
        tadnd.Dispose();
    }

    public static TakeAndDisposeNotDisposable PassDisposableToCustomClassWithDisposeOk()
    {
        Stream stream = new FileStream("MyFile", FileMode.Open);
        return new TakeAndDisposeNotDisposable(stream);
    }

    public static IDisposable CastingOk()
    {
        FileStream myStream = new FileStream("MyFile", FileMode.Open);

        return myStream;
    }


    // out variables
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
}


internal class ArrayTest
{
    public static readonly int ArraySize = 5;

    public static void straightLineCodeOk()
    {
        FileStream[] streams = new FileStream[ArraySize];

        streams[0] = new FileStream("hi", FileMode.Create);

        streams[0].Close();
    }

    public static FileStream[] straightLineCodeReturnedOk()
    {
        FileStream[] streams = new FileStream[ArraySize];

        streams[0] = new FileStream("hi", FileMode.Create);

        return streams;
    }

    public static void straightLineCodeWithVariantOk()
    {
        FileStream[] streams = new FileStream[ArraySize];

        FileStream stream = new FileStream("hi", FileMode.Create);
        streams[0] = stream;

        stream.Close();
    }

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

    public static void straightLineCodeOk()
    { // reports as no model for ElementAt().
        List<FileStream> streams = new List<FileStream>();

        streams.Add(new FileStream("hi", FileMode.Create));

        streams.ElementAt(0).Close();
    }

    public static List<FileStream> straightLineCodeReturnedOk()
    {
        List<FileStream> streams = new List<FileStream>();

        streams.Add(new FileStream("hi", FileMode.Create));

        return streams;
    }

    public static void straightLineCodeWithVariantOk()
    {
        List<FileStream> streams = new List<FileStream>();

        FileStream stream = new FileStream("hi", FileMode.Create);
        streams.Add(stream);

        stream.Close();
    }

    public static void LoopingCodeOk()
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

    public static List<FileStream> LoopingCodeReturnedOk()
    {
        List<FileStream> streams = new List<FileStream>();

        for (int i = 0; i < ListSize; i++)
        {
            streams.Add(new FileStream("hi", FileMode.Create));
        }

        return streams;
    }
    public void AddToFieldOk()
    {
        FileStream stream = new FileStream("hi", FileMode.Create);

        _streamList.Add(stream);

        FileStream stream2 = new FileStream("hi", FileMode.Create);

        _streamDict.Add("mykey", stream2);
    }

    public async Task AddToFieldAsyncOk()
    {
        FileStream stream = new FileStream("hi", FileMode.Create);

        _streamList.Add(stream);

        FileStream stream2 = new FileStream("hi", FileMode.Create);

        _streamDict.Add("mykey", stream2);
    }
}