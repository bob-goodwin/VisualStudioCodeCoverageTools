using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Coverage
{
    class Program
    {
        static void Main(string[] args)
        {
            //bobgood_MININT-6H5UCQS 2020-04-06 18_32_48.coverage
            var b = new Blame(Directory.GetCurrentDirectory());
            if (args.Length > 1) args = new string[] { string.Join(" ", args) };
            if (args.Length == 1 && args[0] == "build")
            {
                b.BlameAllCsProj();
            }
            else if (args.Length==1 && args[0].EndsWith(".coverage"))
            {
                b.BlameTestResults(args[0]);
            }
            else
            {
                Console.WriteLine("cd into root git root");
                Console.WriteLine("uncovered build");
                Console.WriteLine("   builds database of who last modified code");
                Console.WriteLine("   must be run after every git pull or code change to tested code");
                Console.WriteLine("uncovered <coverage file>.coverage");
                Console.WriteLine("   creates a uncovered report for that test coverage file");
                Console.WriteLine("   if full path is not provided, will search the directories for it");

            }
            b.BlameTestResults(@"C:\Users\bobgood\source\repos\UPL\UPLCompiler\TestResults\8536d3a3-b9d3-4c26-895e-22cbec248e70\bobgood_DESKTOP-7R87C3A 2019-12-21 21_28_21.coverage");

        }
    }
}
