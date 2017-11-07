using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ConsoleProcessKiller
{
    internal class ProcInfo
    {
        public int BasePriority;
        public int Id;
        public long MemoryUsage;
        public string Name;
        public ProcessPriorityClass PriorityClass;
        public TimeSpan TotalProcTime;
        public double Weight;

        public void CalculateWeight()  // calculate weight of current process
        {
            Weight = (double) MemoryUsage / 1024 -
                     TotalProcTime.TotalMilliseconds /
                     Convert.ToDouble(Environment.ProcessorCount);  // memory/1024 - time of process/number of processors
        }
    }

    internal static class Program
    {
        private static readonly Process[] ListOfProcesses = Process.GetProcesses();

        // get all process on the machine
        private static List<ProcInfo> GetAllProcessesInfo()
        {
            var processesToSort = new List<ProcInfo>();

            foreach (var proc in ListOfProcesses)
            {
                var tmpProcInfo = new ProcInfo();
                //  filling info about processes
                try  // if you want to get info about system process - you will get Exception
                {
                    tmpProcInfo.Name = proc.ProcessName;
                    tmpProcInfo.Id = proc.Id;
                    tmpProcInfo.MemoryUsage = proc.WorkingSet64;
                    tmpProcInfo.BasePriority = proc.BasePriority;
                    tmpProcInfo.PriorityClass = proc.PriorityClass;
                    tmpProcInfo.TotalProcTime = proc.TotalProcessorTime;
                }
                catch (Exception)
                {
                    tmpProcInfo.PriorityClass = ProcessPriorityClass.AboveNormal;
                    tmpProcInfo.TotalProcTime = TimeSpan.Zero;
                }
                // if the process is system's:
                if (tmpProcInfo.TotalProcTime == TimeSpan.Zero ||
                    tmpProcInfo.PriorityClass == ProcessPriorityClass.AboveNormal ||
                    tmpProcInfo.PriorityClass == ProcessPriorityClass.High ||
                    tmpProcInfo.PriorityClass == ProcessPriorityClass.RealTime)
                    continue;
                // else - calculate weight of this process
                tmpProcInfo.CalculateWeight();
                // add it to common list
                processesToSort.Add(tmpProcInfo);
            }
            return processesToSort;
        }

        private static void KillProcessWithId(int idToKill)
        {
            // find and kill the necessary process
            foreach (var proc in ListOfProcesses)
            {
                if (proc.Id != idToKill) continue;
                proc.Kill();
                break;
            }
        }

        private static void KillSomeProcesses(List<ProcInfo> processesToSort)
        {
            // sort list by calculated weights
            processesToSort.Sort((pr2, pr1) => pr1.Weight.CompareTo(pr2.Weight));
            // output on display all available to kill processes
            foreach (var t in processesToSort)
            Console.WriteLine("Name: {0}, memory: {1}, procTime: {2}, weight: {3}", t.Name, t.MemoryUsage,
                    t.TotalProcTime, t.Weight);

            // kill 5 of the most bad processes
            for (var i = 0; i < 5; ++i)
            {
                Console.WriteLine("I'LL KILL PROCESS {0}!!!", processesToSort[i].Name);
                KillProcessWithId(processesToSort[i].Id);
            }

        }
        public static int Main()
        {
            var tmpList = GetAllProcessesInfo();
            KillSomeProcesses(tmpList);
            Console.ReadLine();
            return 0;
        }
    }
}