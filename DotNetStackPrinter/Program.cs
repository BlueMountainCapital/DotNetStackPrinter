using CommandLine;
using Microsoft.Samples.Debugging.MdbgEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetStackPrinter {
    internal class Program {
        private class CommandLineOptions {
            [Option('v', "verbose", Default = false, HelpText = "When true, prints module names and argument values")]
            public bool Verbose { get; set; }

            [Option('d', "depth", Default = 0, HelpText = "Limit stacktraces to a certain depth. 0 means do not limit.")]
            public int Depth { get; set; }

            [Option('s', "keep-stacks-separate", Default = false, HelpText = "By default, this program will combine common prefixes of stack traces. To see each individual thread separately, pass in this option.")]
            public bool KeepStacksSeparate { get; set; }

            [Value(0, MetaName = "pid", HelpText = "Target process ID", Required = true)]
            public int Pid { get; set; }
        }

        private class StackTrace {
            public int ThreadId;
            public List<(bool IsCurrentlyExecutingFrame, string FrameDescription)> Frames;
            public bool WasCutOff;
        }
        
        private class MergedStackTraceNode {
            public string Frame;
            public int Count;
            public List<MergedStackTraceNode> Children;
        }

        public static void Main(string[] args)
            => Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsed(opts => RunOptionsAndReturnExitCode(opts));

        private static int RunOptionsAndReturnExitCode(CommandLineOptions opts) {
            var stackTraces = GetStackTracesFromProcess(opts.Pid, opts.Depth, opts.Verbose);

            if (opts.KeepStacksSeparate)
                PrintStackTraces(stackTraces, opts.Depth);
            else
                MergeAndPrintStackTraces(stackTraces);
            
            return 0;
        }
        
        private static void MergeAndPrintStackTraces(List<StackTrace> stackTraces) {
            var mergedStackTraces = MergeStackTraces(stackTraces.Where(st => st.Frames.Count > 0).Select(st => new ArraySegment<string>(st.Frames.Select(f => f.FrameDescription).ToArray())));
            foreach (var mergedStackTrace in mergedStackTraces.OrderByDescending(n => n.Count))
                PrintMergedStackTrace(mergedStackTrace);
        }
        
        private static void PrintMergedStackTrace(MergedStackTraceNode node, string tabs = "") {
            Console.WriteLine($"{tabs}[{node.Count}]{node.Frame}");
            while (node.Children.Count == 1) {
                Console.WriteLine($"\t{tabs}{node.Frame}");
                node = node.Children[0];
            }

            foreach (var child in node.Children.OrderByDescending(c => c.Count)) {
                PrintMergedStackTrace(child, tabs + "\t");
            }
        }

        private static List<MergedStackTraceNode> MergeStackTraces(IEnumerable<ArraySegment<string>> stackTraces)
            => stackTraces.GroupBy(st => st.First())
                .Select(gr => new MergedStackTraceNode {
                    Frame = gr.Key,
                    Count = gr.Count(),
                    Children = MergeStackTraces(gr.Where(st => st.Count > 1).Select(st => new ArraySegment<string>(st.Array, st.Offset + 1, st.Count - 1)).ToList())
                })
                .ToList();

        private static void PrintStackTraces(IEnumerable<StackTrace> stackTraces, int depth) {
            foreach (var stackTrace in stackTraces) {
                Console.WriteLine($"Thread #:{stackTrace.ThreadId}");
                var i = 0;
                foreach (var frame in stackTrace.Frames) {
                    Console.WriteLine($"{(frame.IsCurrentlyExecutingFrame ? "*" : " ")}{i}. {frame.FrameDescription}");
                    i++;
                }
                if (stackTrace.WasCutOff)
                    Console.WriteLine($"displayed only the first {depth} frames, use the --depth parameter to control the depth.");
            }
        }

        private static List<StackTrace> GetStackTracesFromProcess(int pid, int depth, bool verbose) {
            var eng = new MDbgEngine();
            var version = MdbgVersionPolicy.GetDefaultAttachVersion(pid);
            var process = eng.Attach(pid, version);
            process.Go().WaitOne();

            try {
                var result = new List<StackTrace>();
                foreach (MDbgThread thread in eng.Processes.Active.Threads)
                    result.Add(GetStackTraceFromThread(thread, depth, verbose));
                return result;
            }
            finally {
                process.Detach().WaitOne();
            }
        }

        private static StackTrace GetStackTraceFromThread(MDbgThread thread, int depth, bool verbose) {
            var stackLines = new List<(bool IsCurrentlyExecutingFrame, string FrameDescription)>();

            // more or less copied from InternalWhereCommand in MDbg\debugger\mdbg\mdbgCommands.cs
            var currentlyExecutingFrame = thread.HaveCurrentFrame ? thread.CurrentFrame : null;
            var currentlyInspectingFrame = thread.BottomFrame;
            var i = 0;
            while (currentlyInspectingFrame != null && (depth == 0 || i < depth)) {
                if (currentlyInspectingFrame.IsInfoOnly) {
                    stackLines.Add((false, currentlyInspectingFrame.ToString()));
                }
                else {
                    stackLines.Add((currentlyInspectingFrame.Equals(currentlyExecutingFrame), currentlyInspectingFrame.ToString(verbose ? "v" : null)));
                    ++i;
                }
                currentlyInspectingFrame = currentlyInspectingFrame.NextUp;
            }

            stackLines.Reverse();

            return new StackTrace {
                ThreadId = thread.Id,
                Frames = stackLines,
                WasCutOff = currentlyInspectingFrame != null && depth != 0
            };
        }
    }
}
