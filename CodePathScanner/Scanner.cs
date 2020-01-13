using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodePathScanner
{
    public class Scanner
    {
        public Dictionary<string, HashSet<string>> invokes = new Dictionary<string, HashSet<string>>();
        public HashSet<string> tests = new HashSet<string>();
        public Dictionary<string, HashSet<string>> inherits = new Dictionary<string, HashSet<string>>();
        public Dictionary<string, HashSet<string>> inheritsFrom = new Dictionary<string, HashSet<string>>();
        public Dictionary<string, Dictionary<string, int>> path = new Dictionary<string, Dictionary<string, int>>();
        public Dictionary<string, Dictionary<string, int>> reversePath = new Dictionary<string, Dictionary<string, int>>();

        public Scanner(IEnumerable<string> modules)
        {
            foreach (var dll in modules)
            {
                new ScanDLL(this, dll);
            }

            ExpandInherits();
            ReverseInherits();
            Follow();
            ReversePath();
            Dump();
        }

        private void ReversePath()
        {
            foreach (var orig in path.Keys)
            {
                foreach (var dstv in path[orig])
                {
                    var dst = dstv.Key;
                    var dist = dstv.Value;
                    if (!reversePath.TryGetValue(dst, out var dict))
                    {
                        dict = new Dictionary<string, int>();
                        reversePath[dst] = dict;
                    }

                    dict[orig] = dist;
                }
            }
        }

        private void Dump()
        {
            using (System.IO.TextWriter tw = new System.IO.StreamWriter("codepaths.tsv"))
            {
                foreach (var dst in reversePath.Keys)
                {
                    tw.WriteLine(dst);
                    List<(string, int, bool)> list = new List<(string, int, bool)>();
                    foreach (var n in reversePath[dst])
                    {
                        list.Add((n.Key, n.Value, tests.Contains(n.Key)));
                    }

                    list.Sort((x, y) => x.Item2.CompareTo(y.Item2));
                    foreach (var n in list)
                    {
                        tw.WriteLine($"{n.Item2}\t{n.Item1}"+(n.Item3?"\tTest":""));
                    }
                }
            }
        }

        private void Follow()
        {
            foreach (var orig in invokes.Keys) 
            {
                Stack<string> stack = new Stack<string>();
                Follow(orig, stack);
            }
        }

        private Dictionary<string, int> Follow(string orig, Stack<string> stack)
        {
            if (orig .Contains("Examples_Unsafe"))
            {

            }
            foreach (var n in stack)
            {
                if (n == orig) return new Dictionary<string, int>();
            }

            if (path.ContainsKey(orig)) return path[orig];

            Dictionary<string, int> result = new Dictionary<string, int>();

            stack.Push(orig);
            if (this.invokes.ContainsKey(orig))
            {
                foreach (var n in this.invokes[orig])
                {
                    foreach (var n2 in Inheritable(n))
                    {
                        result[n2] = 1;
                        foreach (var kvp in Follow(n2, stack))
                        {
                            if (!result.ContainsKey(kvp.Key) || result[kvp.Key] > (kvp.Value + 1))
                            {
                                result[kvp.Key] = kvp.Value + 1;
                            }
                        }
                    }
                }
            }

            stack.Pop();
            path[orig] = result;

            if (path.Count() % 1000 == 0)
            {
                Console.WriteLine($"Following {path.Count()} of {invokes.Count()}");
            }

            return result;
        }

        private IEnumerable<string> Inheritable(string method)
        {
            yield return method;
            int pos1 = method.IndexOf("::");
            if (method.IndexOf("::", pos1 + 2) >= 0) yield break;
            int pos0 = method.LastIndexOf(" ", pos1);
            var type = method.Substring(pos0);
            var className = method.Substring(pos0 + 1, pos1 - pos0 - 1);
            var methodName = method.Substring(pos1 + 2);


            if (this.inheritsFrom.TryGetValue(className, out var candidates))
            {
                foreach (var candidate in candidates)
                {
                    var replacement = $"{type} {candidate}::{methodName}";
                    if (this.invokes.ContainsKey(replacement))
                    {
                        yield return replacement;
                    }
                }

            }
        }

        private void Follow(string orig, string method, Stack<string> stack)
        {
            foreach (var n in stack)
            {
                if (n == method) return;
            }

            stack.Push(method);

            foreach (var next in invokes[method])
            {
                foreach (var next2 in Inheritable(method))
                {
                    var path1 = this.path[next2];
                    if (!path1.TryGetValue(orig, out int value) || value > stack.Count)
                    {
                        path1[orig] = stack.Count;
                    }

                    Follow(orig, next2, stack);
                }
            }

            stack.Pop();
        }

        private void ExpandInherits()
        {
            Dictionary<string, HashSet<string>> inheritsRepl = new Dictionary<string, HashSet<string>>();
            foreach (var orig in inherits.Keys)
            {
                HashSet<string> final = new HashSet<string>();
                foreach (var inh in inherits[orig])
                    Expand(final, inh);
                inheritsRepl[orig] = final;
            }

            inherits = inheritsRepl;
        }

        private void Expand(HashSet<string> final, string inh)
        {
            if (final.Contains(inh)) return;
            final.Add(inh);
            if (inherits.ContainsKey(inh))
            {
                foreach (var inh2 in inherits[inh])
                {
                    Expand(final, inh2);
                }
            }
        }

        private void ReverseInherits()
        {
            foreach (var orig in inherits.Keys)
            {
                foreach (var inh in inherits[orig])
                {
                    if (!inheritsFrom.ContainsKey(inh))
                    {
                        inheritsFrom[inh] = new HashSet<string>();
                    }
                    inheritsFrom[inh].Add(orig);
                }
            }
        }

        public Scanner()
        {

        }
    }
}
