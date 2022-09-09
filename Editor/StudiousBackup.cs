using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Studious
{

    public enum ZipModes
    {
        _7Zip = 1,
        //FastZip = 2
    }

    public class CronusBackupProvider : SettingsProvider
    {
        static CronusBackupProvider()
        {
            EditorApplication.quitting += Quit;

            EditorApplication.update += () => {
                if (DateTime.Now.Subtract(_lastBackup).Ticks > _backupTimeSpan.Ticks && CanBackup() && _autoBackuo)
                {
                    try
                    {
                        StartBackup();
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("Disabling auto backup, if the error persists contact the developer");
                        Debug.LogException(e);
                        _autoBackuo = false;
                    }
                }
            };
        }

        public const string _settingsPath = "Preferences/Studious Games/Studious Backup";
        private static CronusBackupProvider _instance;
        private static Vector2 _scroll;
        private static bool _backingUp = false;

        private static readonly GUIContent _zipModeContent = new GUIContent("Zip mode", "The application that will be used to Back Up this project.");
        //private static readonly GUIContent packLevelContent = new GUIContent("Pack level", "Zip-mode compression level, a higher value may decrease performance, while a lower value may increase the file size\n\n0=Store only, without compression.");
        //private static readonly GUIContent earlyOutContent = new GUIContent("Early out (%)", "The worst detected compression for switching to store.");
        //private static readonly GUIContent threadsContent = new GUIContent("Threads", "Worker threads count.");
        private static readonly GUIContent _useCustomSaveLocationContent = new GUIContent("Custom backups folder", "Specify the folder to store the backup\nIf enabled, backups from all projects will be store at this location, if disabled each backup will be store on its own project folder.");
        private static readonly GUIContent _customSaveLocationContent = new GUIContent("Backup folder location", "The folder to store the Back Ups.");
        private static readonly GUIContent _logToConsoleContent = new GUIContent("Log to console", "Log events to the console.");
        private static readonly GUIContent _autoBackupContent = new GUIContent("Auto backup", "Automatically Back Up in the specified time.");
        private static readonly GUIContent _backupOnExitContent = new GUIContent("Backup On Exit", "Automatically Back Up the project when exiting Editor.");

        public CronusBackupProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords)
        {
            label = "Studious Backup Package";
            _instance = this;
        }

        #region Properties
        private static ZipModes _mode
        {
            get { return (ZipModes)EditorPrefs.GetInt("SBS.ZipMode", 1 == 0 ? 2 : 1); }
            set { EditorPrefs.SetInt("SBS.ZipMode", (int)value); }
        }

        private static bool _autoBackuo
        {
            get { return EditorPrefs.GetBool("SBS.AutoBackup", false); }
            set { EditorPrefs.SetBool("SBS.AutoBackup", value); }
        }

        private static bool _logToConsole
        {
            get { return EditorPrefs.GetBool("SBS.LogToConsole", true); }
            set { EditorPrefs.SetBool("SBS.LogToConsole", value); }
        }

        private static bool _useCustomSaveLocation
        {
            get { return EditorPrefs.GetBool("SBS.UseCustomSave", false); }
            set { EditorPrefs.SetBool("SBS.UseCustomSave", value); }
        }

        private static string _customSaveLocation
        {
            get { return EditorPrefs.GetString("SBS.CustomSave", string.Empty); }
            set { EditorPrefs.SetString("SBS.CustomSave", value); }
        }

        private static string _saveLocation
        {
            get
            {
                if (!_useCustomSaveLocation || string.IsNullOrEmpty(_customSaveLocation))
                    return ($@"{Directory.GetCurrentDirectory()}\Backups");
                else
                    return _customSaveLocation;
            }
        }

        private static string _productNameForFile
        {
            get
            {
                string name = Application.productName;
                char[] chars = Path.GetInvalidFileNameChars();
                for (int i = 0; i < chars.Length; i++)
                    name = name.Replace(chars[i], '_');
                return name;
            }
        }

        private static TimeSpan _backupTimeSpan
        {
            get { return TimeSpan.FromSeconds(EditorPrefs.GetInt("SBS.TimeSpan", (int)TimeSpan.FromHours(8).TotalSeconds)); }
            set { EditorPrefs.SetInt("SBS.TimeSpan", (int)value.TotalSeconds); }
        }

        private static DateTime _lastBackup
        {
            get { return DateTime.Parse(PlayerPrefs.GetString("SBS.LastBackup", DateTime.MinValue.ToString())); }
            set { PlayerPrefs.SetString("SBS.LastBackup", value.ToString()); }
        }

        private static bool _backupOnExit
        {
            get { return EditorPrefs.GetBool("SBS.BackupOnExit", false); }
            set { EditorPrefs.SetBool("SBS.BackupOnExit", value); }
        }
        #endregion

        [SettingsProvider]
        public static SettingsProvider CreateInputSettingsProvider()
        {
            return new CronusBackupProvider(_settingsPath, SettingsScope.User);
        }

        public override void OnGUI(string searchContext)
        {
            if (!SevenZip.IsSupported /*&& !FastZip.isSupported*/)
            {
                EditorGUILayout.HelpBox("7Zip isn't supported, Zip Backup won't work", MessageType.Error);
                return;
            }
            //else if (!SevenZip.IsSupported)
            //    EditorGUILayout.HelpBox("7z.exe was not found, 7Zip won't work", MessageType.Warning);

            _scroll = EditorGUILayout.BeginScrollView(_scroll, false, false);
            GUI.enabled = SevenZip.IsSupported;

            EditorGUILayout.Space();
            _mode = (ZipModes)EditorGUILayout.EnumPopup(_zipModeContent, _mode);
            EditorGUILayout.Space();
            _logToConsole = EditorGUILayout.Toggle(_logToConsoleContent, _logToConsole);
            EditorGUILayout.Space();
            _backupOnExit = EditorGUILayout.Toggle(_backupOnExitContent, _backupOnExit);
            EditorGUILayout.Space();

            if (_useCustomSaveLocation = EditorGUILayout.Toggle(_useCustomSaveLocationContent, _useCustomSaveLocation))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(_customSaveLocationContent, EditorStyles.popup);
                if (GUILayout.Button(string.IsNullOrEmpty(_customSaveLocation) ? "Browse..." : _customSaveLocation, EditorStyles.popup, GUILayout.Width(150f)))
                {
                    string path = EditorUtility.OpenFolderPanel("Browse for backups folder", _customSaveLocation, "Backups");
                    if (path.Length > 0)
                        _customSaveLocation = path;
                }
                EditorGUILayout.EndHorizontal();
            }
            else
                _customSaveLocation = string.Empty;

            EditorGUILayout.Space();

            _autoBackuo = EditorGUILayout.ToggleLeft(_autoBackupContent, _autoBackuo);
            GUI.enabled = _autoBackuo;
            EditorGUI.indentLevel++;
            int days = EditorGUILayout.IntSlider("Days", _backupTimeSpan.Days, 0, 7);
            int hours = EditorGUILayout.IntSlider("Hours", _backupTimeSpan.Hours, 0, 23);
            int minutes = EditorGUILayout.IntSlider("Minutes", _backupTimeSpan.Minutes, 0, 59);

            if (days == 0 && hours == 0 && minutes < 5)
                minutes = 5;

            _backupTimeSpan = new TimeSpan(days, hours, minutes, 0);

            EditorGUI.indentLevel--;
            GUI.enabled = true;

            if (_lastBackup != DateTime.MinValue)
                EditorGUILayout.LabelField("Last backup: " + _lastBackup);
            else
                EditorGUILayout.LabelField("Last backup: Never backed Up");
            if (_backingUp)
                EditorGUILayout.LabelField("Next backup: Backing Up now...");
            else if (!_autoBackuo)
                EditorGUILayout.LabelField("Next backup: Disabled");
            else
                EditorGUILayout.LabelField("Next backup: " + _lastBackup.Add(_backupTimeSpan));

            EditorGUILayout.EndScrollView();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Use Defaults", GUILayout.Width(120f)))
            {
                EditorPrefs.DeleteKey("SBS.ZipMode");
                EditorPrefs.DeleteKey("SBS.AutoBackup");
                EditorPrefs.DeleteKey("SBS.LogToConsole");
                EditorPrefs.DeleteKey("SBS.UseCustomSave");
                EditorPrefs.DeleteKey("SBS.CustomSave");
                EditorPrefs.DeleteKey("SBS.TimeSpan");
                EditorPrefs.DeleteKey("SBS.BackupOnExit");
            }
            GUI.enabled = !_backingUp;
            if (GUILayout.Button("Backup now", GUILayout.Width(120f)))
                StartBackup();
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        [MenuItem("Tools/Studios Backup/Backup Now")]
        public static void StartBackup()
        {
            if (_backingUp && !EditorApplication.isPlaying)
                return;

            string path = string.Format("{0}/{1}_backup_{2}.zip", _saveLocation, _productNameForFile, DateTime.Now.ToString("yyyy-MM-dd-HH-mm"));
            string assetsPath = Application.dataPath;
            string projectSettingsPath = Application.dataPath.Replace("/Assets", "/ProjectSettings");
            double startTime = EditorApplication.timeSinceStartup;
            ZipProcess zip;

            //Only Supporting 7Zip for now.
            zip = new SevenZip(path, assetsPath, projectSettingsPath);

            zip.OnExit += (o, a) => {
                _backingUp = false;
                _lastBackup = DateTime.Now;

                if (zip.Process.ExitCode == 0)
                {
                    int zipSize = File.ReadAllBytes(path).Length;
                    string time = (EditorApplication.timeSinceStartup - startTime).ToString("0.00");

                    if (_logToConsole)
                        Debug.LogFormat("project has been backed up to {0} in {1} seconds.", EditorUtility.FormatBytes(zipSize), time);
                }
                else if (_logToConsole)
                    Debug.LogWarning("Something went wrong with the backup.");
            };

            _backingUp = zip.Start();

            if (_logToConsole)
                Debug.Log(_backingUp ? "Backing Up..." : "Error starting the Backup Process");
            if (!_backingUp)
                _lastBackup = DateTime.Now;
        }

        private static bool CanBackup()
        {
            return !_backingUp && (/*FastZip.isSupported ||*/ SevenZip.IsSupported) && !EditorApplication.isPlaying;
        }

        private static void Quit()
        {
            if(_backupOnExit)
                StartBackup();

            WaitForBackup();
        }

        private static async void WaitForBackup()
        {
            while(_backingUp)
            {
                await Task.Yield();
            }

        }
    }

}
