using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace Studious
{
    public class SevenZip : ZipProcess
    {
        public static bool IsSupported
        {
            get
            {
                if (string.IsNullOrEmpty(Path))
                    return false;
                return SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows;
            }
        }

        public static string Path
        {
            get
            {
                string path = EditorApplication.applicationContentsPath + "/Tools/7z.exe";
                if (File.Exists(path))
                    return path;
                return string.Empty;
            }
        }

        public SevenZip(string output, params string[] sources)
        {
            if (string.IsNullOrEmpty(output))
                throw new ArgumentException("Invalid output file path");

            if (sources.Length < 1)
                throw new ArgumentException("Need at least one source file");

            _output = output;
            _sources = sources;
        }

        public override bool Start()
        {
            _startInfo = new ProcessStartInfo();
            _startInfo.FileName = Path;
            _startInfo.CreateNoWindow = true;
            _startInfo.UseShellExecute = false;
            _startInfo.RedirectStandardOutput = true;
            _startInfo.RedirectStandardError = true;
            _startInfo.Arguments += string.Format("a -tzip -bd \"{0}\" ", _output);

            for (int i = 0; i < _sources.Length; i++)
                if (Directory.Exists(_sources[i]) || File.Exists(_sources[i]))
                    _startInfo.Arguments += string.Format("\"{0}\" ", _sources[i]);

            if (File.Exists(_output))
                File.Delete(_output);
            if (!Directory.Exists(System.IO.Path.GetDirectoryName(_output)))
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_output));

            Process = new Process();
            Process.StartInfo = _startInfo;
            Process.EnableRaisingEvents = true;
            Process.OutputDataReceived += OutputDataReceived;
            Process.ErrorDataReceived += ErrorDataReceived;
            Process.Exited += Exited;

            bool started = Process.Start();
            Process.BeginOutputReadLine();
            Process.BeginErrorReadLine();

            return started;
        }

    }
}