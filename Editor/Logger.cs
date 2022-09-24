using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Studious
{
    public static class Logger
    {
        private static bool _showLogs
        {
            get { return EditorPrefs.GetBool("SBS.LogToConsole", true); }
            set { EditorPrefs.SetBool("SBS.LogToConsole", value); }
        }

        public static void Log(object message)
        {
            if (_showLogs)
                Debug.Log(message);
        }

        public static void Log(object message, Object context)
        {
            if (_showLogs)
                Debug.Log(message, context);
        }

        public static void LogFormat(string message, params object[] args)
        {
            if (_showLogs)
                Debug.LogFormat(message, args);
        }

        public static void LogWarning(string message)
        {
            if (_showLogs)
                Debug.LogWarning(message);
        }
    }
}
