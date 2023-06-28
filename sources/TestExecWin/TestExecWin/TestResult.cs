using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TestExecWin
{
    public enum Result
    {
        Tentative,
        Success,
        Failed,
        Disabled
    }

    public class TestResult : INotifyPropertyChanged
    {
        private Result result;
        public bool MemLeaksDetected { get; set; }
        public TimeSpan ExecutionTime { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public string ProcessOutput { get; set; }

        public string GetFormattedProcessOutput()
        {
            return ProcessOutput?.Replace(": ", ":\r\n\t") ?? string.Empty;
        }

        public TestResult()
        {
            Result = Result.Tentative;
        }

        public Result Result 
        {
            get { return result; }
            set 
            { 
                result = value;
                OnPropertyChanged();
            }
        }
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            try
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
            catch
            {
                int a = 0;
            }
        }
    }
}
