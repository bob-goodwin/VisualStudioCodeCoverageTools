using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodePathScanner
{
    public class ScanDLL
    {
        public ScanDLL(Scanner data, string path)
        {
            Console.WriteLine($"scanning '{path}'");

            try
            {
                var module = ModuleDefinition.ReadModule(path);


                foreach (TypeDefinition type in module.Types)
                {
                    HashSet<string> inherits = new HashSet<string>();
                    if (type.BaseType != null) inherits.Add(type.BaseType.FullName);
                    foreach (var b in type.Interfaces)
                    {
                        inherits.Add(b.FullName);
                    }

                    data.inherits[type.FullName] = inherits;
                    data.inheritsFrom[type.FullName] = new HashSet<string>();

                    foreach (var method in type.Methods)
                    {
                        foreach (var attr in method.CustomAttributes)
                        {
                            var tt = attr.AttributeType.ToString();
                            if (tt.Contains("TestMethod"))
                            {
                                data.tests.Add(method.FullName);
                            }
                            else if (tt.Contains("TestAttribute"))
                            {
                                data.tests.Add(method.FullName);
                            }
                        }

                        data.invokes[method.FullName] = Scan(method);
                    }
                }
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed to scan '{path}'");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        private HashSet<string> Scan(MethodDefinition method)
        {
            HashSet<string> result = new HashSet<string>();
            if (method.Body != null)
            {
                foreach (var instr in method.Body.Instructions)
                {
                    switch (instr.OpCode.Code)
                    {
                        case Code.Callvirt:
                        case Code.Call:
                        case Code.Newobj:
                            Call(instr.Operand as MethodReference);
                            break;
                        case Code.Calli:
                            CallI(instr.Operand as CallSite);
                            break;
                        default:
                            break;
                    }
                }
            }

            return result;
            void Call(MethodReference other)
            {
                result.Add(other.FullName);
            }

            void CallI(CallSite other)
            {

            }
        }


    }
}
