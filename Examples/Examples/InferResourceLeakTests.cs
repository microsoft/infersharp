using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class InferResourceLeakTests
{
    private static byte[] myBytes = new byte[] {10, 4};

    public static void UsageCanThrowShouldReport()
    { 
        FileStream fs = new FileStream("MyFile.txt", FileMode.Open, FileAccess.Read);
        fs.Write(myBytes); // this line can throw an exception, preventing future disposal
        fs.Dispose();
    }

    public static void UsageCanThrowOk()
    {
        using (var fs = new FileStream("MyFile.txt", FileMode.Open, FileAccess.Read))
        { // the using properly handles the exception.
            fs.Write(myBytes); // this line can throw an exception
        }
    }

    public static void ConstructionCanThrowShouldReport()
    {
        // bad because stream2 could throw, preventing stream 1 from closing
        var stream1 = new FileStream("NotAFile.bad", FileMode.Open);
        var stream2 = new FileStream("NotAFile.bad", FileMode.Append);

        stream1.Dispose();
        stream2.Dispose();
    }

    public static void ConstructingOnePotentialThrowOk()
    {
        // if new FileStream throw, the resource should not be allocated and does not need to be disposed?
        var stream1 = new FileStream("NotAFile.bad", FileMode.Open);
        stream1.Dispose();
    }

    public static void DelegatingToThrowingConstructorShouldReport()
    { 
        var stream1 = new FileStream("SomeFile.txt", FileMode.Open);
        // if new StreamReader throws, then stream1 has no place to close.
        var stream2 = new StreamReader(stream1);

        stream2.Dispose();
    }

    public static void DelegatingResourceToResourceOk()
    { // rework
        // this filestream is managed by the bufferedstream and disposed with it
        var fs = new FileStream("SomeFile.txt", FileMode.Open);
        try
        {
            var sr = new StreamReader(fs);
            sr.Dispose();
        } catch (ArgumentException e)
        {
            fs.Dispose();
        }
    }

    public static void DelegatingResourceToResourceShouldReport()
    {   
        // this first filestream is lost and never disposed, this is where we should report
        var fs = new FileStream("SomeFile.txt", FileMode.Open);
        // this filestream is the stream delegated to the bufferedstream and disposed with it
        fs = new FileStream("SomeFile.txt", FileMode.Open);
        try
        {
            var sr = new StreamReader(fs);
            sr.Dispose();
        }
        catch (ArgumentException e)
        {
            fs.Dispose();
        }
    }

    public static void UsagesNotTrackedOk ()
    {
        MemoryStream ms = new MemoryStream(10);
        ms.Write(myBytes);

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
        // if new StreamReader throws, then stream1 has no place to close.
        // but stream1 is a MemoryStream, so that's not a problem.
        var streamReader = new StreamReader(stream1);

        streamReader.Dispose();
    }

    public static void UsageNotTrackedDelegatedShouldReport ()
    {
        var stream1 = new MemoryStream();
        // if new StreamReader throws, then stream1 has no place to close.
        // but stream1 is a MemoryStream, so that's not a problem.
        var streamReader = new StreamReader(stream1);

        // potential exception here means the stream reader may not be disposed.
        var fs = new FileStream("MyFile.txt", FileMode.Open);
        fs.Dispose();

        streamReader.Dispose();
    }

    public static void UseMethodToCreateStreamAndDisposeOk ()
    {
        var fs = CreateStreamOk();
        fs.Close();
    }

    public static void UseMethodToCreateStreamAndLeakShouldReport()
    {
        var fs = CreateStreamOk();
    }

    public static StreamReader CreateStreamReaderLeakInnerShouldReport()
    { // if the stream reader throws an exception, the fs is leaked
        var fs = new FileStream("SomeFile.txt", FileMode.Append);
        var sr = new StreamReader(fs);
        return sr;
    }


    public static StreamReader CreateStreamReaderLeakInnerOk ()
    { // if the stream reader throws an exception, the fs is leaked
        var fs = new FileStream("SomeFile.txt", FileMode.Append);
        StreamReader sr = null;
        try
        {
            sr = new StreamReader(fs);            
        } catch (ArgumentException e)
        {
            fs.Close ();
            throw;
        }
        return sr;
    }

    public static FileStream CreateStreamOk ()
    {
        return new FileStream("MyFile.txt", FileMode.Create);
    }

    public static Object LoseIDisposableShouldReport ()
    {
        return new InheritTwoDisposable() as MyFavoriteClass;
    }
}


public class MyFavoriteClass
{

}

public class InheritTwoDisposable : MyFavoriteClass , IDisposable
{
    public void Dispose()
    {
        //throw new NotImplementedException();
    }
}




// Should get 7 reports