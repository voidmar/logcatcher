using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.IO;
using CommandLine;

namespace LogCatcherInstaller
{
    class Program
    {
        class Options
        {
            [Option('i', "input", Required = true, HelpText = "Path to target assembly")]
            public string Assembly { get; set; }

            [Option("namespace", HelpText = "Target LogCatcher class namespace")]
            public string LogCatcherNamespace { get; set; }

            [Option('n', "name", Required = true, HelpText = "Target LogCatcher class name")]
            public string LogCatcherName { get; set; }

            [HelpOption]
            public string GetUsage()
            {
                var help = new CommandLine.Text.HelpText("LogCatcherInstaller");
                help.AddDashesToOption = true;
                help.AddOptions(this);
                return help.ToString();
            }

            public Options()
            {
                LogCatcherNamespace = "";
            }
        }

        static void Main(string[] args)
        {
            var options = new Options();

            var arg_parser = new CommandLine.Parser();
            if (!arg_parser.ParseArguments(args, options))
            {
                Console.Write(options.GetUsage());
                return;
            }

            var dest_path = Path.GetFullPath(options.Assembly);
            var backup_path = GetBackupPath(dest_path);
            File.Copy(dest_path, backup_path);

            MergeLogCatcher(dest_path, options.LogCatcherNamespace, options.LogCatcherName);

            var module = ModuleDefinition.ReadModule(dest_path);
            var logcatcher = module.Types.First(t => t.Name == options.LogCatcherName && t.Namespace == options.LogCatcherNamespace);

            var unity_engine = ModuleDefinition.ReadModule("UnityEngine.dll");
            var debug_class = unity_engine.Types.First(t => t.Name == "Debug");

            var replacements = new Dictionary<string, MethodDefinition>();
            foreach (var method in debug_class.Methods.Where(m => m.Name.StartsWith("Log")))
            {
                var replacement = logcatcher.Methods.FirstOrDefault(m => m.Name == method.Name && m.Parameters.Count == method.Parameters.Count);

                if (replacement == null)
                    continue;

                replacements.Add(method.FullName, replacement);
            }

            foreach (var type in module.Types)
            {
                foreach (var method in type.Methods.Where(m => m.HasBody))
                {
                    foreach (var instruction in method.Body.Instructions.Where(i => i.OpCode == OpCodes.Call))
                    {
                        var method_ref = (MethodReference)instruction.Operand;

                        MethodDefinition replacement;
                        if (replacements.TryGetValue(method_ref.FullName, out replacement))
                        {
                            instruction.Operand = replacement;
                        }
                    }
                }
            }

            module.Write(dest_path);
        }

        static string GetBackupPath(string path)
        {
            int count = 0;
            var backup_path = path + ".bak";
            while (File.Exists(backup_path))
            {
                ++count;
                backup_path = path + "." + count + ".bak";
            }
            return backup_path;
        }

        static string MakeLogCatcher(string namespace_name, string name)
        {
            var source_path = typeof(LogCatcher.LogCatcher).Assembly.Location;
            var dest_path = Path.Combine(Path.GetDirectoryName(source_path), Path.GetRandomFileName());

            var module = ModuleDefinition.ReadModule(source_path);

            var logcatcher_class = module.Types.First(t => t.Name == "LogCatcher");
            logcatcher_class.Name = name;
            logcatcher_class.Namespace = namespace_name;

            module.Write(dest_path);
            return dest_path;
        }

        static void MergeLogCatcher(string target_assembly, string namespace_name, string name)
        {
            var logcatcher_path = MakeLogCatcher(namespace_name, name);

            try
            {
                var il_repack = new ILRepacking.ILRepack();

                il_repack.UnionMerge = true;
                il_repack.XmlDocumentation = true;
                il_repack.DebugInfo = true;
                il_repack.OutputFile = target_assembly;
                il_repack.InputAssemblies = new string[]
                {
                    target_assembly,
                    logcatcher_path
                };

                il_repack.Repack();
            }
            finally
            {
                File.Delete(logcatcher_path);
            }
        }
    }
}
