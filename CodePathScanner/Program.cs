using Microsoft.Build.Logging.StructuredLogger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CodePathScanner
{
    class Program
    {
        
        static void Main(string[] args)
        {
            if (args.Count() == 2 && args[0] == "build")
            {
                var project = new Solution(args[1]);
                Scanner scanner = new Scanner(project.modules);
            }
            else if (args.Count() == 2 && args[0] == "test")
            {
                ScanFile(args[1], true);
            }
            else if (args.Count() == 2 && args[0] == "method")
            {
                ScanFile(args[1], false);

            }
            else if (args.Count() == 2 && args[0] == "playlist")
            {
                ScanFile(args[1], true, true);

            }
            else
            {
                Console.WriteLine("CodePathScanner.exe build <pathToSolutionFile>  // to build codepaths.tsv file in current directory");
                Console.WriteLine("CodePathScanner.exe test <methodname>  // to parse codepaths.tsv to find tests that cover a method");
                Console.WriteLine("CodePathScanner.exe method <methodname>  // to find methods that use a method");
                Console.WriteLine("CodePathScanner.exe playlist <methodname>  // same as 'test', but generates a playlist.xml file");
                Console.WriteLine("// <methodname> can be a partial string, if more than one methods match, then the choices will be shown");
            }
        }

        static void ScanFile(string methodname, bool test, bool playlist=false)
        {
            using (System.IO.TextReader tr = new System.IO.StreamReader("codepaths.tsv"))
            {
                string line;
                List<string> candidates = new List<string>();
                List<string> results = new List<string>();
                bool inResult = false;
                while (null!=(line=tr.ReadLine()))
                {
                    string[] parts = line.Split('\t');
                    if (parts.Length==1)
                    {
                        if (inResult=parts[0].Contains(methodname))
                        {
                            candidates.Add(parts[0]);
                        }
                    } else if (inResult)
                    {
                        if (!test || parts.Length == 3)
                        {
                            results.Add(line);
                        }
                    }
                }

                if (candidates.Count()==0)
                {
                    Console.WriteLine("no matches found");
                } else if (candidates.Count()>1)
                {
                    Console.WriteLine("multiple matches found");
                    foreach (var n in candidates)
                    {
                        Console.WriteLine("\t" + n);
                    }
                }
                else if (playlist)
                {
                    Console.WriteLine("<Playlist Version=\"1.0\">");
                    foreach (var n in results)
                    {
                        string[] parts = n.Split('\t');
                        var method = parts[1];
                        int pos1 = method.IndexOf("::");
                        int pos0 = method.LastIndexOf(" ", pos1);
                        int pos2 = method.IndexOf("(", pos1);
                        string result = method.Substring(pos0 + 1, pos1 - pos0 - 1) + "." + method.Substring(pos1 + 2, pos2 - pos1 - 2);
                        Console.WriteLine($"<Add Test=\"{result}\" />");
                    }

                    Console.WriteLine("</Playlist>");
                }
                else
                {
                    foreach (var n in candidates)
                    {
                        Console.WriteLine(n);
                    }

                    foreach (var n in results)
                    {
                        Console.WriteLine(n);
                    }

                    if (results.Count()==0)
                    {
                        Console.WriteLine("No Coverage found");
                    }
                }
            }
        }
        
        public static void Error(string message, Exception e = null)
        {
            Console.WriteLine(message);
            if (e!=null)
            {
                Console.WriteLine(e.ToString());
                Console.WriteLine(e.StackTrace.ToString());
            }
            System.Diagnostics.Debugger.Break();
            Environment.Exit(0);
        }
    }
}
