using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodePathScanner
{
    class Solution
    {
        string baseDir;

        private List<Project> projects = new List<Project>();

        public HashSet<string> modules = new HashSet<string>();
        public Solution(string path)
        {
            baseDir = Path.GetDirectoryName(path);
            try
            {
                TextReader tr = new StreamReader(path);
                string line;
                string section = null;
                while (null != (line = tr.ReadLine()))
                {
                    if (section == null)
                    {
                        if (line.StartsWith("Project(")) section = line;
                    }
                    else
                    {
                        section = section + " " + line;
                        if (line.StartsWith("EndProject"))
                        {
                            ParseProject(section);
                            section = null;
                        }
                    }
                }

            }
            catch (Exception e)
            {
                Program.Error($"Could not read solution file '{path}'", e);
            }

            foreach (Project project in projects)
            {
                if (project.assemblyPath != null)
                {
                    modules.Add(project.assemblyPath);
                }

                foreach (var reference in project.metadataReferences)
                {
                    modules.Add(reference);
                }
            }
        }

        private void ParseProject(string section)
        {
            var parts = section.Split(',');
            var path = parts[1].Trim();
            if (path[0] == '"') path = path.Substring(1, path.Length - 2);
            if (path.EndsWith(".csproj"))
            {
                var project = new Project(Path.Combine(baseDir, path));
                projects.Add(project);
            }
        }

    }
}
