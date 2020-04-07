using LibGit2Sharp;
using Microsoft.Build.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Coverage
{
    public class MethodBlame
    {
        public string name;
        public string file;
        public Dictionary<int, string> blames = new Dictionary<int, string>();
        public int strikes = 0;
        public void Write(TextWriter tw, string name)
        {
            tw.WriteLine(name);
            tw.WriteLine(file);
            foreach (var n in blames)
            {
                tw.Write(n.Key + "," + n.Value + ",");
            }
            tw.WriteLine();
        }

        public MethodBlame()
        {

        }

        public MethodBlame(TextReader tr)
        {
            name = tr.ReadLine();
            if (name == "<end>") return;

            file = tr.ReadLine();
            string[] parts = tr.ReadLine().Split(',');
            try
            {
                for (int i = 0; i < parts.Count() - 1; i += 2)
                {
                    blames[int.Parse(parts[i])] = parts[i + 1];
                }
            }
            catch (Exception)
            { }
        }
    }

    public class ProjectBlame
    {
        public string name;
        public HashSet<string> users = new HashSet<string>();
        public Dictionary<string, MethodBlame> methods = new Dictionary<string, MethodBlame>();
        public void Write(TextWriter tw)
        {
            tw.WriteLine(name);
            tw.WriteLine(string.Join(",", users));
            foreach (var n in methods.Keys)
            {
                methods[n].Write(tw, n);
            }
            tw.WriteLine("<end>");
        }

        public ProjectBlame() { }
        public ProjectBlame(TextReader tr)
        {
            name = tr.ReadLine();
            string[] parts = tr.ReadLine().Split(',');
            foreach (var p in parts)
            {
                users.Add(p);
            }

            for (; ; )
            {
                var b = new MethodBlame(tr);
                if (b.name == "<end>") return;
                methods[b.name] = b;
            }
        }
    }

    public class FileBlame
    {
        public Dictionary<string, MethodBlame> methods = new Dictionary<string, MethodBlame>();
    }

    public class Blame
    {
        public bool GetBlame(string repo, string path, out Dictionary<int, string> blame)
        {
            blame = new Dictionary<int, string>();
            Process process = new Process();
            // Configure the process using the StartInfo properties.
            process.StartInfo.FileName = "git.exe";
            process.StartInfo.Arguments = "--no-pager blame " + path + " -e";
            process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
            process.StartInfo.WorkingDirectory = repo;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();
            string line;
            while (null != (line = process.StandardOutput.ReadLine()))
            {
                int pos0 = line.IndexOf("(");
                if (pos0 < 0) continue;
                int pos1 = line.IndexOf(")", pos0);
                if (pos0 > 0 && pos1 > 0)
                {
                    string[] parts = line.Substring(pos0 + 1, pos1 - pos0 - 1).Split(' ');
                    var email = parts[0];
                    if (email[0] == '<') email = email.Substring(1, email.Length - 2);
                    int epos = email.IndexOf("@");
                    if (epos > 0) email = email.Substring(0, epos);
                    int lineNo = int.Parse(parts[parts.Length - 1]);
                    blame[lineNo] = email;
                }
            }
            var err = process.StandardError.ReadToEnd();
            if (err != "")
            {
                //                Console.WriteLine(err);
                return false;
            }

            return true;
        }

        string repo;
        public Blame(string repo)
        {
            this.repo = repo;
        }

        public void BlameAllCsProj()
        {
            foreach (var path in AllCsproj(repo))
            {
                BlameCsproj(path);
            }
        }

        public void BlameTestResults(string fn)
        {
            ReadAllCoverage();
            if (!ReadWhoFile()) return;

            if (!File.Exists(fn))
            {
                var f = FindFile(repo, fn).ToList();
                if (f.Count() == 1) BlameTestResults1(f[0]);
                else if (f.Count() == 0)
                {
                    Console.WriteLine($"Cant find file '{fn}");
                    return;
                }
                else
                {
                    Console.WriteLine($"Multiple matches for file '{fn}");
                    foreach (var x in f)
                    {
                        Console.WriteLine($"'{x}");
                        BlameTestResults1(x);
                    }
                }
            }

            List<MethodBlame> sorted = new List<MethodBlame>();
            foreach (var b in blameSet)
            {
                sorted.Add(b);
            }

            sorted.Sort((y, x) => x.strikes.CompareTo(y.strikes));
            using (TextWriter tw = new StreamWriter("uncovered.tsv"))
            {
                foreach (var s in sorted)
                {
                    tw.Write(s.file + "\t" + s.name);
                    HashSet<string> hs = new HashSet<string>();
                    foreach (var n in s.blames.Values)
                    {
                        hs.Add(n);
                    }
                    foreach (var n in hs)
                    {
                        tw.Write("\t" + n);

                    }
                    tw.WriteLine();
                }
            }
        }

        public void BlameTestResults1(string fn)
        { 

            var ci = Microsoft.VisualStudio.Coverage.Analysis.CoverageInfo.CreateFromFile(fn);
            var ds = ci.BuildDataSet();
            var lines = ds.Lines;
            var t = ds.Tables["SourceFileNames"];

            Dictionary<uint, string> sourcefilenames = new Dictionary<uint, string>();
            foreach (Microsoft.VisualStudio.Coverage.Analysis.CoverageDSPriv.SourceFileNamesRow sfn in t.Rows)
            {
                sourcefilenames[sfn.SourceFileID] = sfn.SourceFileName;
            }

            foreach (var line in lines)
            {
                var method = line.MethodRow;
                var class0 = method.ClassRow;
                var namespace0 = class0.NamespaceTableRow;
                var fullmethodname = $"{namespace0.NamespaceName}.{class0.ClassName}.{method.MethodName}";
                if (TryGetMethodBlame(sourcefilenames[line.SourceFileID],(int)line.LineID+1,out var mb, out var who))
                {
                    if (whoset.Contains(who))
                    {
                        mb.strikes++;
                        blameSet.Add(mb);
                    }
                }
            }

         }

        HashSet<string> whoset = new HashSet<string>();
        HashSet<MethodBlame> blameSet = new HashSet<MethodBlame>();
        bool ReadWhoFile()
        {
            if (!File.Exists("who.txt"))
            {
                Console.WriteLine("place a file named who.txt in the root of your enlistment");
                Console.WriteLine("add a line for each member of your team, or whos changes you want to track");
                Console.WriteLine("place the email name of each person, with the @microsoft removed");
                Console.WriteLine("look at the second line of each .csv file in the coverage directory to see who has modified each project in the solution");
                    return false;
            }

            using (TextReader tr = new StreamReader("who.txt"))
            {
                string line;
                while (null != (line = tr.ReadLine()))
                {
                    whoset.Add(line);
                }
            }

            return true ;
        }

        List<ProjectBlame> projectBlames = new List<ProjectBlame>();

        Dictionary<string, FileBlame> files = new Dictionary<string, FileBlame>();

        bool TryGetMethodBlame(string file, int line, out MethodBlame methodBlame, out string who)
        {
            if (files.TryGetValue(file, out var fb))
            {
                foreach (var m in fb.methods)
                {
                    if (m.Value.blames.TryGetValue(line, out var b))
                    {
                        who = b;
                        methodBlame = m.Value;
                        return true;
                    }
                }
            }

            who = null;
            methodBlame = null;
            return false;
        }

        public void ReadAllCoverage()
        {
            foreach (var file in Directory.EnumerateFiles(repo + "\\coverage", "*.csv"))
            {
                using (TextReader tr = new StreamReader(file))
                {
                    projectBlames.Add(new ProjectBlame(tr));
                }
            }

            foreach (var b in projectBlames)
            {
                foreach (var mb in b.methods)
                {
                    if (!files.TryGetValue(mb.Value.file, out var file))
                    {
                        file = new FileBlame();
                        files[mb.Value.file] = file;
                    }

                    file.methods[mb.Key] = mb.Value;
                }
            }
        }

        public void BlameCsproj(string projectPath)
        {
            Console.WriteLine(projectPath);
            StringWriter log = new StringWriter();
            Buildalyzer.AnalyzerManagerOptions options = new Buildalyzer.AnalyzerManagerOptions
            {
                LogWriter = log
            };

            Buildalyzer.AnalyzerResults results = null;
            try
            {
                Buildalyzer.AnalyzerManager manager = new Buildalyzer.AnalyzerManager(options);
                Buildalyzer.ProjectAnalyzer analyzer = manager.GetProject(projectPath);
                results = analyzer.Build();
            }
            catch (Exception)
            {
                Console.WriteLine($"not project file format {projectPath}");
                return;
            }
            if (results.Count() == 0)
            {
                Console.WriteLine($"project has no results {projectPath}");
                return;
            }
            var proj = results.First();

            ProjectBlame blame = new ProjectBlame();
            blame.name = Path.GetFileNameWithoutExtension(projectPath);

            foreach (var sourcePath in proj.SourceFiles)
            {
                if (sourcePath.Contains("\\obj\\")) continue;
                if (!sourcePath.EndsWith(".cs")) continue;

                if (!GetBlame(repo, sourcePath, out var dict)) continue;
                Console.WriteLine(" " + sourcePath);
                string code;
                using (TextReader reader = new StreamReader(sourcePath))
                {
                    code = reader.ReadToEnd();
                }

                Dictionary<int, int> chars = new Dictionary<int, int>();
                int line = 1;
                for (int i = 0; i < code.Length; i++)
                {
                    chars[i] = line;
                    if (code[i] == '\n') line++;
                }

                SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
                foreach (var n in tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    var line0 = chars[n.Span.Start];
                    var line1 = chars[n.Span.End];
                    var name = n.Identifier.Text;
                    var b1 = n.WithBody(null).WithReturnType(IdentifierName("")).WithModifiers(new SyntaxTokenList());
                    b1 = b1.WithAttributeLists(new SyntaxList<AttributeListSyntax>());
                    var sig = b1.ToString();
                    sig = sig.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ").Replace("  ", " ").Replace("  ", " ").Replace("  ", " ").Replace("  ", " ").Replace("  ", " ").Replace("( ", "(");

                    var method = new MethodBlame();
                    method.file = sourcePath;
                    for (int i = line0; i <= line1; i++)
                    {
                        if (dict.TryGetValue(i, out var email))
                        {
                            blame.users.Add(email);
                            method.blames[i] = email;
                        }
                        blame.methods[sig] = method;
                    }
                }
            }

            Directory.CreateDirectory(repo + "\\coverage\\");
            using (TextWriter tw = new StreamWriter(repo + "\\coverage\\" + blame.name + ".csv"))
            {
                blame.Write(tw);
            }
        }

        public static IEnumerable<string> FindFile(string path, string match)
        {
            foreach (var dir in Directory.EnumerateDirectories(path))
            {
                foreach (var s in FindFile(dir, match))
                {
                    yield return s;
                }
            }

            foreach (var file in Directory.EnumerateFiles(path, "*.coverage"))
            {
                if (file.EndsWith(match))
                    yield return file;
            }
        }

        public static IEnumerable<string> AllCsproj(string path)
        {
            foreach (var dir in Directory.EnumerateDirectories(path))
            {
                foreach (var s in AllCsproj(dir))
                {
                    yield return s;
                }
            }

            foreach (var file in Directory.EnumerateFiles(path, "*.csproj"))
            {
                yield return file;
            }
        }
    }
}
