using System;
using System.Diagnostics;
using UnityEditor;

namespace Studious
{
    public abstract class ZipProcess
    {

        public delegate void DataReceivedCallback(object sender, DataReceivedEventArgs args);
        public delegate void ExitCallback(object sender, EventArgs args);

        public DataReceivedCallback OutputReceivedData = (o, a) => { };
        public DataReceivedCallback OutputDataReceivedThreaded = (o, a) => { };
        public DataReceivedCallback ErrorReceivedData = (o, a) => { };
        public DataReceivedCallback ErrorDataReceivedThreaded = (o, a) => { };
        public ExitCallback OnExit = (o, a) => { };
        public ExitCallback OnExitThreaded = (o, a) => { };

        protected string _output;
        protected string[] _sources;
        protected ProcessStartInfo _startInfo;
        //protected Process _process;
        protected bool _isSupported;
        protected bool _path;

        public Process Process { get; protected set; }

        public abstract bool Start();

        public bool Start(bool lockUnityThread)
        {
            bool started = Start();

            if (lockUnityThread && started)
                Process.WaitForExit();

            return started;
        }

        protected void OutputDataReceived(object sender, DataReceivedEventArgs args)
        {
            if (string.IsNullOrEmpty(args.Data))
                return;

            OutputDataReceivedThreaded(sender, args);

            EditorApplication.CallbackFunction update = new EditorApplication.CallbackFunction(() => { });
            update = () => {
                EditorApplication.update -= update;
                OutputReceivedData(sender, args);
            };
            EditorApplication.update += update;
        }

        protected void ErrorDataReceived(object sender, DataReceivedEventArgs args)
        {
            if (string.IsNullOrEmpty(args.Data))
                return;

            ErrorDataReceivedThreaded(sender, args);

            EditorApplication.CallbackFunction update = new EditorApplication.CallbackFunction(() => { });
            update = () => {
                EditorApplication.update -= update;
                ErrorReceivedData(sender, args);
            };
            EditorApplication.update += update;
        }

        protected void Exited(object sender, EventArgs args)
        {
            OnExitThreaded(sender, args);

            EditorApplication.CallbackFunction update = new EditorApplication.CallbackFunction(() => { });
            update = () => {
                EditorApplication.update -= update;
                OnExit(sender, args);
            };
            EditorApplication.update += update;
        }
    }
}