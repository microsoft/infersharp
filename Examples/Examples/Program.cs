// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;

namespace Examples
{
    public class Program
    {
        private StreamReader SRGlobal;  
        // Instantiate a StreamWriter instance.
        StreamWriter SWGlobal = new StreamWriter("everwhat.txt");
        
        /// <summary>
        /// Constructor resource usage example, no leaks expected.
        /// </summary>
        public Program(string filename){
            SRGlobal = new StreamReader(filename);  
        }

        /// <summary>
        /// An example with no null dereference expected.
        /// </summary>
        public string NullDeReferenceOK(){
            return "abc";
        }

        /// <summary>
        /// An example with null dereference error expected.
        /// </summary>
        public string NullDeReferenceBad(){
            return null;
        }

        /// <summary>
        /// Intraprocedural resource usage example, no leaks expected.
        /// </summary>
        public void ResourceLeakIntraproceduralOK(){
            string data;
            StreamReader sr = new StreamReader("whatever.txt");            
            data = sr.ReadToEnd();
            sr.Close();
            Console.WriteLine(data);
        }

        /// <summary>
        /// Intraprocedural resource usage example, leaks expected.
        /// </summary>
        public void ResourceLeakIntraproceduralBad(){
            StreamWriter sw = new StreamWriter("everwhat.txt");
            sw.WriteLine("Guru99 - ASP.Net");
            // FIXME: should close the stream intraprocedurally by calling sw.Close()
        }

        /// <summary>
        /// Interprocedural resource usage example, leaks expected.
        /// </summary>
         public void ResourceLeakInterproceduralBad(){
            SRGlobal = new StreamReader("whatever.txt");  
            string data = SRGlobal.ReadToEnd();
            Console.WriteLine(data);
            // FIXME: should close the stream interprocedurally by calling Cleanup()
        }

        /// <summary>
        /// Interprocedural resource usage example, no leaks expected.
        /// </summary>
        public void ResourceLeakInterproceduralOK(){
            SRGlobal = new StreamReader("whatever.txt");  
            string data = SRGlobal.ReadToEnd();
            Console.WriteLine(data);
            CleanUp();
        }

        /// <summary>
        /// Returns a StreamWriter resource unless returns null with exception, no leaks expected.
        /// </summary>
        public StreamWriter AllocateStreamWriter() 
        {
            try
            {
                FileStream fs = File.Create("everwhat.txt");
                return new StreamWriter(fs);
            }
            catch(Exception e)
            {
                return null;
            }
        }

        /// <summary>
        /// Resource usage example with exception handling, leaks expected.
        /// </summary>
        public void ResourceLeakExcepHandlingBad() {
            StreamWriter stream = AllocateStreamWriter();
            if (stream == null)
                return;

            try 
            {
                stream.WriteLine(12);
            } 
            finally 
            {
                // FIXME: should close the stream by calling stream.Close() if stream is not null.
            }
        }

        /// <summary>
        /// Resource usage example with exception handling, no leak expected.
        /// </summary>
        public void ResourceLeakExcepHandlingOK() {
            StreamWriter stream = AllocateStreamWriter();
            if (stream == null)
                return;

            try 
            {
                stream.WriteLine(12);
            } 
            finally 
            {
                stream.Close();
            }
        }

        /// <summary>
        /// Two resources usage example with exception handling, one leak expected.
        /// </summary>
        public void TwoResourceLeakExcepHandlingBad() {
            StreamReader sr = null;
            StreamWriter sw = null;

            try 
            {
                sr = new StreamReader("whatever.txt");
                sw = new StreamWriter("everwhat.txt");
                sw.WriteLine(sr.ReadToEnd());
            } 
            finally 
            {
                if (sr != null) {
                    sr.Close();
                }
                if (sw != null) {
                    sw.Close();
                }
            }
        }

        /// <summary>
        /// Two resources usage example with exception handling, no leak expected.
        /// </summary>
        public void TwoResourceLeakExcepHandlingOK() {
            StreamReader sr = null;
            StreamWriter sw = null;

            try 
            {
                sr = new StreamReader("whatever.txt");
                var text = sr.ReadToEnd();
                try
                {
                    sw = new StreamWriter("everwhat.txt");
                    sw.WriteLine(text);
                }
                finally
                {   
                    if (sw != null) {
                        sw.Close();
                    }
                }
            } 
            finally 
            {
                if (sr != null) {
                    sr.Close();
                }
            }
        }

        /// <summary>
        /// Interprocedural close resource function.
        /// </summary>
        public void CleanUp(){
            SRGlobal.Close();
            SWGlobal.Close();
            Console.WriteLine("Close is called");
        }

    }

    public class MainClass {
        public static void Main(string[] args)
        {
            Program p = new Program("whatever.txt");
            // FIXME: should close the global streams by calling p.Cleanup()
            // Null dereference error report expected.
            p.NullDeReferenceBad().GetHashCode();
            // No null dereference error report expected.
            p.NullDeReferenceOK().GetHashCode();
        }
    }
}
