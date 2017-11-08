using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;

namespace ProcessesKiller
{
    /// <summary>
    ///     Логика взаимодействия для MainWindow.xaml
    /// </summary>
    internal class ProcInfo
    {
        public int BasePriority;
        public int Id;
        public long MemoryUsage;
        public string Name;
        public ProcessPriorityClass PriorityClass;
        public TimeSpan TotalProcTime;
        public int Weight;

        public void CalculateWeight() // calculate weight of current process
        {
            Weight = (int) ((double) MemoryUsage / 1024 -
                            TotalProcTime.TotalMilliseconds /
                            Convert.ToDouble(Environment.ProcessorCount)
            ); // memory/1024 - time of process/number of processors
        }
    }

    public class StringsToShow
    {
        public StringsToShow(string name, string weight, string id, string priority)
        {
            Name = name;
            Weight = weight;
            Id = id;
            Priority = priority;
        }

        public string Name { get; }
        public string Weight { get; }
        public string Id { get; }
        public string Priority { get; }
    }

    public partial class MainWindow
    {
        private static readonly List<ProcInfo> ProcessesToSort = new List<ProcInfo>();
        private static Process[] _listOfProcesses;

        public MainWindow()
        {
            InitializeComponent();
            ItemsToTable = new ObservableCollection<StringsToShow>();
            DataGrid.DataContext = this;
            ReloadTable();
        }

        public ObservableCollection<StringsToShow> ItemsToTable { get; set; }

        private static void KillProcessWithId(int idToKill)
        {
            // find and kill the necessary process
            foreach (var proc in _listOfProcesses)
            {
                if (proc.Id != idToKill) continue;
                try
                {
                    proc.Kill();
                    MessageBox.Show("You've killed him: " + proc.ProcessName + "\n Congratulations!");
                }
                catch (Exception)
                {
                    MessageBox.Show("You'd got exception, when you tried to kill process:" + proc.ProcessName);
                }
                break;
            }
        }

        private void ReloadTable()
        {
            ItemsToTable.Clear();
            GetAllProcessesInfo();
            foreach (var v in ProcessesToSort)
                ItemsToTable.Add(new StringsToShow(v.Name, v.Weight.ToString(), v.Id.ToString(),
                    v.PriorityClass.ToString()));
        }

        // get all process on the machine
        private static void GetAllProcessesInfo()
        {
            _listOfProcesses = Process.GetProcesses();
            ProcessesToSort.Clear();
            foreach (var proc in _listOfProcesses)
            {
                var tmpProcInfo = new ProcInfo();
                //  filling info about processes
                try // if you want to get info about system process - you will get Exception
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
                    tmpProcInfo.PriorityClass == ProcessPriorityClass.RealTime ||
                    tmpProcInfo.PriorityClass == ProcessPriorityClass.Idle)
                    continue;
                // else - calculate weight of this process
                tmpProcInfo.CalculateWeight();
                // add it to common list
                ProcessesToSort.Add(tmpProcInfo);
            }
            ProcessesToSort.Sort((pr2, pr1) => pr1.Weight.CompareTo(pr2.Weight));
        }

        private void KillSelectedProcess(object sender, RoutedEventArgs e)
        {
            var d = DataGrid.SelectedItem as StringsToShow;
            if (d == null) return;
            KillProcessWithId(int.Parse(d.Id));
            ReloadTable();
        }

        private void KillTheWorst(object sender, RoutedEventArgs e)
        {
            KillProcessWithId(ProcessesToSort[0].Id);
            ReloadTable();
        }

        private void ReloadT(object sender, RoutedEventArgs e)
        {
            ReloadTable();
        }
    }
}