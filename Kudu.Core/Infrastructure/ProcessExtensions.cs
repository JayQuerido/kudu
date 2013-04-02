﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Kudu.Contracts.Tracing;

namespace Kudu.Core.Infrastructure
{
    // http://blogs.msdn.com/b/bclteam/archive/2006/06/20/640259.aspx
    public static class ProcessExtensions
    {
        public static void Kill(this Process process, bool includesChildren, ITracer tracer)
        {
            try
            {
                if (includesChildren)
                {
                    foreach (Process child in process.GetChildren())
                    {
                        SafeKillProcess(child, tracer);
                    }
                }
            }
            catch (Exception ex)
            {
                tracer.TraceError(ex);
            }
            finally
            {
                SafeKillProcess(process, tracer);
            }
        }

        public static IEnumerable<Process> GetChildren(this Process process)
        {
            int pid = process.Id;
            Dictionary<int, List<int>> tree = GetProcessTree();
            return GetChildren(pid, tree).Select(cid => SafeGetProcessById(cid)).Where(p => p != null);
        }

        // recursively get children.
        // return depth-first (leaf child first).
        private static IEnumerable<int> GetChildren(int pid, Dictionary<int, List<int>> tree)
        {
            List<int> children;
            if (tree.TryGetValue(pid, out children))
            {
                List<int> result = new List<int>();
                foreach (int id in children)
                {
                    result.AddRange(GetChildren(id, tree));
                    result.Add(id);
                }
                return result;
            }
            return Enumerable.Empty<int>();
        }

        /// <summary>
        /// Calculates the sum of TotalProcessorTime for the current process and all its children.
        /// </summary>
        public static TimeSpan GetTotalProcessorTime(this Process process, ITracer tracer)
        {
            try
            {
                var processes = process.GetChildren().Concat(new[] { process }).Select(p => new { Name = p.ProcessName, Id = p.Id, Cpu = p.TotalProcessorTime });
                var totalTime = TimeSpan.FromTicks(processes.Sum(p => p.Cpu.Ticks));
                var info = String.Join("+", processes.Select(p => String.Format("{0}({1},{2:0.000}s)", p.Name, p.Id, p.Cpu.TotalSeconds)).ToArray());
                tracer.Trace("Cpu: {0}=total({1:0.000}s)", info, totalTime.TotalSeconds);
                return totalTime;
            }
            catch (Exception ex)
            {
                tracer.TraceError(ex);
            }

            return process.TotalProcessorTime;
        }

        private static Process SafeGetProcessById(int pid)
        {
            try
            {
                return Process.GetProcessById(pid);
            }
            catch (ArgumentException)
            {
                // Process with an Id is not running.
                return null;
            }
        }

        private static void SafeKillProcess(Process process, ITracer tracer)
        {
            try
            {
                string processName = process.ProcessName;
                int pid = process.Id;
                process.Kill();
                tracer.Trace("Abort Process '{0}({1})'.", processName, pid);
            }
            catch (Exception)
            {
                if (!process.HasExited)
                {
                    throw;
                }
            }
        }

        private static Dictionary<int, List<int>> GetProcessTree()
        {
            var tree = new Dictionary<int, List<int>>();
            foreach (var proc in Process.GetProcesses().ToDictionary(p => p.Id, p => p.ProcessName))
            {
                string indexedProcessName = FindIndexedProcessName(proc.Key, proc.Value);
                if (String.IsNullOrEmpty(indexedProcessName))
                {
                    continue;
                }

                int? parentId = FindPidFromIndexedProcessName(indexedProcessName);
                if (!parentId.HasValue)
                {
                    // We encountered an unauthorized access exception when trying to use perf counters when the account Kudu AppDomain is executing in doesn't have sufficient privileges. 
                    // Skip when this happens.
                    continue;
                }
                List<int> children = null;
                if (!tree.TryGetValue(parentId.Value, out children))
                {
                    tree[parentId.Value] = children = new List<int>();
                }

                children.Add(proc.Key);
            }

            return tree;
        }

        private static string FindIndexedProcessName(int pid, string processName)
        {
            string processIndexedName = null;
            Process[] processesByName = Process.GetProcessesByName(processName);
            for (var index = 0; index < processesByName.Length; index++)
            {
                processIndexedName = index == 0 ? processName : processName + "#" + index;
                int? processId = SafeGetPerfCounter("Process", "ID Process", processIndexedName);
                if (processId.HasValue && processId.Value == pid)
                {
                    return processIndexedName;
                }
            }

            return processIndexedName;
        }

        private static int? FindPidFromIndexedProcessName(string indexedProcessName)
        {
            return SafeGetPerfCounter("Process", "Creating Process ID", indexedProcessName);
        }

        private static int? SafeGetPerfCounter(string category, string counterName, string key)
        {
            using (var counter = new PerformanceCounter(category, counterName, key, readOnly: true))
            {
                return (int)counter.NextValue();
            }
        }
    }
}