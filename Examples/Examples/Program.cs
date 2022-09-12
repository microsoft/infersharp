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
        /// Interprocedural resource usage example, leaks expected.
        /// </summary>
         public void ResourceLeakInterproceduralBad(){
            StreamWriter stream = AllocateStreamWriter();
            if (stream == null)
                return;

            try 
            {
                stream.WriteLine(12);
            } 
            finally 
            {
                // FIXME: should close the stream by calling stream.Close().
            }
        }

        /// <summary>
        /// Interprocedural resource usage example, no leaks expected.
        /// </summary>
        public void ResourceLeakInterproceduralOK(){
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
