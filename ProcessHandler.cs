using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiProcessPairing
{
    public abstract class ProcessHandler<T> : IDisposable
    {

        protected readonly List<WrappedProcess<T>> _wrappedProcesses;
        public ProcessHandler()
        {
            _wrappedProcesses = new List<WrappedProcess<T>>();
        }

        protected async Task<(Guid?, T)> Run(string executable, string arguments, string initializationConfirmationText, T data)
        {
            // Set up the program to execute

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.FileName = executable;
            startInfo.Arguments = arguments;
            startInfo.UseShellExecute = false;
            // redirect the outputs so that we can capture and analyze it.
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            process.StartInfo = startInfo;

            var processWrapper = new WrappedProcess<T>(process, data);
            _wrappedProcesses.Add(processWrapper);

            process.Exited += new EventHandler((sender, e) =>
            {
                processWrapper.SetInitializationState(); // do not pass arguement, which will preserve previous setting
            });


            // please note that we process both standard and error output.
            // many programs output seemingly normal status updates to error output rather than standard output

            process.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
            {
                if (!String.IsNullOrEmpty(e.Data))
                {
                    // if so desired we could output the result with // Console.WriteLine("Output: " + e.Data);

                    // When the system detects the initializationConfirmationText string in the output it will mark the process wrapper as initialized
                    if (processWrapper.IsInitialized == false && e.Data.Contains(initializationConfirmationText))
                    {
                        processWrapper.SetInitializationState(true);
                    }
                        
                }
            });

            process.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
            {
                if (!String.IsNullOrEmpty(e.Data))
                {
                    // if so desired we could output the result with // Console.WriteLine("Output: " + e.Data);

                    // When the system detects the initializationConfirmationText string in the output it will mark the process wrapper as initialized
                    if (processWrapper.IsInitialized == false && e.Data.Contains(initializationConfirmationText))
                    {
                        processWrapper.SetInitializationState(true);
                    }

                }
            });

            try
            {
                // Start the process and enable the event handling.
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();


                Console.WriteLine("Waiting for process to be ready before returning...");  


                // await the task which will be marked as completed when the confirmation text is detected.
                // or will time out after 2.5 seconds
                await Task.WhenAny(processWrapper.InitializationTask, Task.Delay(2500));
                Console.WriteLine("Process initialized, exited or timed out...");



                if(process.HasExited)
                    Console.WriteLine("Process exited...");
                else if (processWrapper.InitializationTask.IsCompleted)
                    Console.WriteLine("Process initialized...");
                else
                    Console.WriteLine("Process timed out...");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Process error: {ex.Message}");
            }


            // assuming the process initialized then return the appropriate data
            if (processWrapper.IsInitialized)
                return (processWrapper.Id, processWrapper.Data);
            else
            {
                Kill(processWrapper);
                return (null, default(T));
            }
        }

        protected void PruneDead()
        {
            var deadWrappedProcesses = _wrappedProcesses.Where(p => p.IsRunning == false || p.IsInitialized == false);
            foreach(var deadWrappedProcess in deadWrappedProcesses)
            {
                Kill(deadWrappedProcess);
            }
        }

        public void Kill(WrappedProcess<T> wrappedProcess)
        {
            wrappedProcess.Kill();
            _wrappedProcesses.Remove(wrappedProcess);
        }

        public void Kill(Guid id)
        {
            var wrappedProcess = _wrappedProcesses.Find(p => p.Id == id);
            if (wrappedProcess != null)
                Kill(wrappedProcess);
        }

        public void Dispose()
        {
            _wrappedProcesses.ForEach((wrappedProcess) =>
            {
                wrappedProcess.Kill();
            });
        }
    }


    public class WrappedProcess<T>
    {
        private readonly Guid _id;
        private readonly Process _process;
        private readonly TaskCompletionSource _startupTask;
        private bool _isInitialized = false;
        private readonly T _data;
        public WrappedProcess(Process process, T data)
        {
            _id = Guid.NewGuid();
            _process = process;

            // this is a task that is triggered as completed by an event. Is used as an async completion helper.
            _startupTask = new TaskCompletionSource();
            _data = data;
        }



        public Guid Id
        {
            get
            {
                return _id;
            }
        }

        public T Data
        {
            get
            {
                return _data;
            }
        }

        public bool IsRunning
        {
            get
            {
                if (_process != null && _process.HasExited == false)
                    return true;

                return false;
                
            }
        }

        public bool IsInitialized { 
            get
            {
                return _isInitialized;
            }
        }


        // sets the state of the initialization task so that awaits waiting for it can then proceed.
        public void SetInitializationState(bool? succeeded = null)
        {
            _startupTask.TrySetResult();
            if(succeeded != null)
                _isInitialized = (bool)succeeded;
        }

        public Task InitializationTask { 
            get {
                return _startupTask.Task;
            } 
        }

        public void Kill()
        {
            // if the process exists kill it. This is safe with already exited processes.
            _process?.Kill(true);
            _process?.Dispose();
        }
    }
}
