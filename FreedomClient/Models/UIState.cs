using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreedomClient.Models
{
    [AddINotifyPropertyChangedInterface]
    public class UIOperation
    {
        const string NoOpOperationName = "NO_OP";

        [AlsoNotifyFor("IsNoOp")]
        public string Name { get; set; }
        public string Message { get; set; }
        public string ProgressReport { get; set; }
        public double Progress { get; set;}
        [AlsoNotifyFor("IsBusy")]
        public bool IsFinished { get; set; }
        [AlsoNotifyFor("IsBusy")]
        public bool IsCancelled { get; set; }
        public bool IsCancellable { get; set; }

        public bool IsBusy => !IsCancelled && !IsFinished;

        public CancellationTokenSource CancellationTokenSource { get; set; }

        public UIOperation() { 
            Name = string.Empty;
            Message= string.Empty;
            ProgressReport = string.Empty;
            Progress = 0;
            CancellationTokenSource = new CancellationTokenSource();
            IsCancellable= false;
            IsCancelled= false;
        }

        public static UIOperation NoOp => new() { Name = NoOpOperationName };

        public bool IsNoOp => Name == NoOpOperationName;
    }

    [AddINotifyPropertyChangedInterface]
    public class UIState
    {
        public UIOperation CurrentOperation;

        public UIState()
        {
            CurrentOperation = new UIOperation();
        }
    }
}
