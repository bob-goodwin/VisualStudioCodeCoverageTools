using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Coverage
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("arguments: filename, part of methodname");
                return;
            }
            string fn = args[0];// @"C:\Users\bobgood\source\repos\UPL\UPLCompiler\TestResults\8536d3a3-b9d3-4c26-895e-22cbec248e70\bobgood_DESKTOP-7R87C3A 2019-12-21 21_28_21.coverage";
            string partialMethodName = args[1];
            var ci = Microsoft.VisualStudio.Coverage.Analysis.CoverageInfo.CreateFromFile(fn);
            var ds = ci.BuildDataSet();
            var lines = ds.Lines;
            var t = ds.Tables["SourceFileNames"];
            Dictionary<uint, string> sourcefilenames = new Dictionary<uint, string>();
            foreach (Microsoft.VisualStudio.Coverage.Analysis.CoverageDSPriv.SourceFileNamesRow sfn in t.Rows)
            {
                sourcefilenames[sfn.SourceFileID] = sfn.SourceFileName;
            }

            var methods = ds.Method;

            var lastMethodSeen = "";

            foreach (var line in lines)
            {
                var method = line.MethodRow;
                var class0 = method.ClassRow;
                var namespace0 = class0.NamespaceTableRow;
                var fullmethodname = $"{namespace0.NamespaceName}.{class0.ClassName}.{method.MethodName}";
                if (fullmethodname.Contains(partialMethodName))
                {
                    if (lastMethodSeen != fullmethodname)
                    {
                        Console.WriteLine(fullmethodname);
                        lastMethodSeen = fullmethodname;
                    }

                    var sourcefile = GetFile(sourcefilenames[line.SourceFileID]);
                    var sourceLine = sourcefile[(int)line.LnStart - 1];
                    string ok = (line.Coverage == 0) ? "." : "X";
                    Console.WriteLine($"{ok}{(int)line.LnStart:####}{sourceLine}");
                }
            }
        }

        static Dictionary<string, List<string>> source = new Dictionary<string, List<string>>();
        static List<string> GetFile(string filename)
        {
            if (!source.TryGetValue(filename, out var data))
            {
                using (TextReader tr = new StreamReader(filename))
                {
                    data = new List<string>();
                    string line;
                    while (null != (line = tr.ReadLine()))
                    {
                        data.Add(line);
                    }

                    source[filename] = data;
                }
            }

            return data;
        }
    }
}
