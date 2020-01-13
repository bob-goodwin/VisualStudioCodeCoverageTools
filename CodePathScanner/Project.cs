using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buildalyzer.Construction;

namespace CodePathScanner
{
    class Project
    {
        public string projectDir;
        public string binDir;
        public List<string> metadataReferences = new List<string>();
        public string[] sourceFiles;
        public string assemblyPath = null;

        public Project(string projectPath)
        {
            StringWriter log = new StringWriter();
            Buildalyzer.AnalyzerResults results = null;

            try
            {
                var modifiedProjectPath = projectPath + ".tmp";
                MakeModifiedProject(projectPath, modifiedProjectPath);

                Buildalyzer.AnalyzerManagerOptions options = new Buildalyzer.AnalyzerManagerOptions
                {
                    LogWriter = log
                };


                Buildalyzer.AnalyzerManager manager = new Buildalyzer.AnalyzerManager(options);
                Buildalyzer.Environment.EnvironmentOptions buildOptions = new Buildalyzer.Environment.EnvironmentOptions();
                buildOptions.DesignTime = true;
                buildOptions.Restore = false;

                Buildalyzer.ProjectAnalyzer analyzer = manager.GetProject(modifiedProjectPath);
                results = analyzer.Build();
                File.Delete(modifiedProjectPath);

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(log);
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(Path.Combine(Directory.GetCurrentDirectory(), projectPath));
                Console.ForegroundColor = ConsoleColor.White;

                Buildalyzer.AnalyzerResult f = results.First();
                string[] references = f.References;
                sourceFiles = results.First().SourceFiles;

                this.projectDir = f.GetProperty("ProjectDir");
                this.binDir = Path.Combine(Path.GetDirectoryName(f.ProjectFilePath), "bin");
                for (int i = 0; i < references.Length; i++)
                {
                    if (File.Exists(references[i]))
                    {
                        this.AddReference(references[i]);
                    }
                    else
                    {
                        Console.WriteLine($"could not find '{references[i]}");
                    }
                }
                var properties = results.First().Properties;
                var assemblyName = properties["AssemblyName"];
                var outputPath = properties["OutputPath"];
                var outputType = properties["OutputType"];
                if (outputType == "Library") outputType = "dll";
                string filename = assemblyName + "." + outputType;
                this.assemblyPath = Path.Combine(this.projectDir, outputPath, filename);
                sourceFiles = results.First().SourceFiles;
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Could not read project file '{projectPath}'", e);
                Console.ForegroundColor = ConsoleColor.White;
            }

        }

        private void AddReference(string reference)
        {
            var dst = Path.Combine(this.binDir, Path.GetFileName(reference));
            if (!File.Exists(dst))
            {
                File.Copy(reference, dst, overwrite: true);
            }

            this.metadataReferences.Add(reference);
        }

        private void MakeModifiedProject(string before, string after)
        {
            using (TextWriter tw = new StreamWriter(after))
            {
                using (TextReader tr = new StreamReader(before))
                {
                    string line;
                    while (null != (line = tr.ReadLine()))
                    {
                        if (line.Trim().StartsWith("</Project>"))
                        {
                            tw.WriteLine(@" <Target Name=""IncrementalClean"" />");
                        }

                        tw.WriteLine(line);
                    }
                }
            }
        }

    }
}
