using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;
using Newtonsoft.Json;
using UnityEngine;
using System.Threading.Tasks;
using Studious;

namespace Studious
{
    class CronusBackupProvider : SettingsProvider
    {
        private static CronusBackupProvider _instance;

        private const string _packagePath = "Packages/com.studiousgames.studiousbackuppackage/Editor/Resources/Layouts/SettingsLayout.uxml";
        private static readonly Version pluginVersion = new Version(1, 0, 3);
        private static readonly DateTime pluginDate = new DateTime(2022, 10, 31);
        private static VisualElement _rootElement;
        private static List<string> _defaultFolders = new List<string> { "Assets", "Packages", "ProjectSettings", "UserSettings" };
        private static List<string> _items = new List<string>();
        private static bool _backingUp = false;

        public const string _settingsPath = "Preferences/Studious Games/Studious Backup";

        #region Properties
        private static int _mode
        {
            get { return EditorPrefs.GetInt("SBS.ZipMode", 1 == 0 ? 2 : 1) - 1; }
            set { EditorPrefs.SetInt("SBS.ZipMode", (int)value); }
        }

        private static bool _logToConsole
        {
            get { return EditorPrefs.GetBool("SBS.LogToConsole", true); }
            set { EditorPrefs.SetBool("SBS.LogToConsole", value); }
        }

        private static bool _backupOnExit
        {
            get { return EditorPrefs.GetBool("SBS.BackupOnExit", false); }
            set { EditorPrefs.SetBool("SBS.BackupOnExit", value); }
        }

        private static bool _autoBackup
        {
            get { return EditorPrefs.GetBool("SBS.AutoBackup", false); }
            set { EditorPrefs.SetBool("SBS.AutoBackup", value); }
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

        private static string _backFolders
        {
            get { return EditorPrefs.GetString("SBS.BackupFolders", string.Empty); }
            set { EditorPrefs.SetString("SBS.BackupFolders", value); }
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
        #endregion


        static CronusBackupProvider()
        {
            EditorApplication.quitting += Quit;

            EditorApplication.update += () =>
            {
                if (DateTime.Now.Subtract(_lastBackup).Ticks > _backupTimeSpan.Ticks && CanBackup() && _autoBackup)
                {
                    try
                    {
                        DoBackup();
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("Disabling auto backup, if the error persists contact the developer");
                        Debug.LogException(e);
                        _autoBackup = false;
                    }
                }
            };
        }

        public CronusBackupProvider(string path, SettingsScope scope = SettingsScope.User) : base(path, scope)
        {
            label = "Studious Backup Package";
            _instance = this;
        }

        public override void OnActivate(string searchContext, VisualElement root)
        {
            _rootElement = root;
            InitializeEditor();
        }

        private static void InitializeEditor()
        {
            _rootElement.Clear();

            VisualTreeAsset template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{_packagePath}");
            TemplateContainer temple = template.CloneTree();

            _items = JsonConvert.DeserializeObject<List<string>>(_backFolders);

            if (_items == null)
                _items = new List<string>(_defaultFolders);

            _rootElement.Add(temple);

            PopulateDropdown();
            PopulateListView();
            DisplayBackupTime(false);

            Toggle autoBackup = _rootElement.Q<Toggle>("AutoBackup");
            autoBackup.RegisterValueChangedCallback(HandleAutoBackup);

            Toggle consoleLog = _rootElement.Q<Toggle>("ConsoleLog");
            consoleLog.value = _logToConsole;
            consoleLog.RegisterValueChangedCallback(evt =>
            {
                _logToConsole = evt.newValue;
            });

            Toggle backupExit = _rootElement.Q<Toggle>("BackupExit");
            backupExit.value = _backupOnExit;
            backupExit.RegisterValueChangedCallback(evt =>
            {
                _backupOnExit = evt.newValue;
            });

            Toggle backautoBackup = _rootElement.Q<Toggle>("AutoBackup");
            backautoBackup.value = _autoBackup;
            backautoBackup.RegisterValueChangedCallback(evt =>
            {
                _autoBackup = evt.newValue;
            });

            Label customLocation = _rootElement.Q<Label>("CustomLocation");
            customLocation.text = _customSaveLocation;


            SliderInt daySlider = _rootElement.Q<SliderInt>("DaySlider");
            SliderInt hourSlider = _rootElement.Q<SliderInt>("HourSlider");
            SliderInt minSlider = _rootElement.Q<SliderInt>("MinSlider");

            int days = daySlider.value = _backupTimeSpan.Days;
            int hours = hourSlider.value = _backupTimeSpan.Hours;
            int minutes = minSlider.value = _backupTimeSpan.Minutes;

            daySlider.RegisterValueChangedCallback(evt =>
            {
                days = evt.newValue;
                UpdateMinuteSlider();
            });

            hourSlider.RegisterValueChangedCallback(evt =>
            {
                hours = evt.newValue;
                UpdateMinuteSlider();
            });

            minSlider.RegisterValueChangedCallback(evt =>
            {
                minutes = evt.newValue;
                UpdateMinuteSlider();
            });

            UpdateMinuteSlider();

            void UpdateMinuteSlider()
            {
                if (days == 0 && hours == 0 && minutes < 5)
                {
                    minutes = 5;
                    minSlider.value = minutes;
                }
                _backupTimeSpan = new TimeSpan(days, hours, minutes, 0);
                UpdateNextBackup();
            }

            Toggle customBackup = _rootElement.Q<Toggle>("CustomFolder");
            customBackup.value = _useCustomSaveLocation;
            customBackup.RegisterValueChangedCallback(evt =>
            {
                _useCustomSaveLocation = evt.newValue;
                ShowCustomBackupSelector(evt.newValue);
            });

            ShowCustomBackupSelector(_useCustomSaveLocation);

            Button button = _rootElement.Q<Button>("Browse");
            button.clicked += () =>
            {
                string path = EditorUtility.OpenFolderPanel("Browse for folder", Directory.GetCurrentDirectory(), "Backups");
                if (path.Length > 0)
                {
                    _customSaveLocation = path;
                    customLocation.text = path;
                }
            };

            UpdateNextBackup();

            Label version = _rootElement.Q<Label>("Version");
            version.text = $"{pluginVersion} - ({pluginDate:d})";

            Button defaultButton = _rootElement.Q<Button>("UseDefaults");
            defaultButton.clicked += () =>
            {
                EditorPrefs.DeleteKey("SBS.ZipMode");
                EditorPrefs.DeleteKey("SBS.AutoBackup");
                EditorPrefs.DeleteKey("SBS.LogToConsole");
                EditorPrefs.DeleteKey("SBS.UseCustomSave");
                EditorPrefs.DeleteKey("SBS.CustomSave");
                EditorPrefs.DeleteKey("SBS.TimeSpan");
                EditorPrefs.DeleteKey("SBS.BackupOnExit");
                EditorPrefs.DeleteKey("SBS.BackupFolders");

                InitializeEditor();
            };

            Button backupNow = _rootElement.Q<Button>("BackupNow");
            backupNow.clicked += () =>
            {
                DoBackup();
            };
        }

        [MenuItem("Tools/Studios Backup/Backup Now 2")]
        public static void DoBackup()
        {
            if (_backingUp && !EditorApplication.isPlaying && _items.Count == 0)
                return;

            string path = _saveLocation;

            double startTime = EditorApplication.timeSinceStartup;
            ZipProcess zip;

            if(_useCustomSaveLocation)
            {
                path = $"{path}\\{_productNameForFile}";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }

            path = string.Format("{0}/{1}_backup_{2}.zip", path, _productNameForFile, DateTime.Now.ToString("yyyy-MM-dd-HH-mm"));

            //Only Supporting 7Zip for now.
            zip = new SevenZip(path, _items.ToArray());

            zip.OnExit += (o, a) => {
                _backingUp = false;
                _lastBackup = DateTime.Now;

                if (zip.Process.ExitCode == 0)
                {
                    FileInfo fileInfo = new FileInfo(path);
                    string time = (EditorApplication.timeSinceStartup - startTime).ToString("0.00");

                    if (_logToConsole)
                        Debug.LogFormat("Project backed up into {0} in {1} seconds", FormatFileSize(fileInfo.Length), time);
                }
                else if (_logToConsole)
                    Debug.LogWarning("Something went wrong with the backup.");

                InitializeEditor();
            };

            _backingUp = zip.Start();

            if (_logToConsole)
                Debug.Log(_backingUp ? "Backing Up..." : "Error starting the Backup Process");
            if (!_backingUp)
                _lastBackup = DateTime.Now;

            InitializeEditor();
        }

        private static void ShowCustomBackupSelector(bool visible)
        {
            VisualElement backupLocation = _rootElement.Q<VisualElement>("BackupSelector");
            backupLocation.visible = visible;

            if (visible)
                backupLocation.style.display = DisplayStyle.Flex;
            else
                backupLocation.style.display = DisplayStyle.None;
        }

        private static void PopulateDropdown()
        {
            var dropDown = _rootElement.Q<DropdownField>();
            dropDown.choices = Enum.GetNames((typeof(ZipModes))).ToList();
            dropDown.index = (int)_mode;
            dropDown.RegisterValueChangedCallback(evt =>
            {
                _mode = dropDown.index + 1;
            });
        }

        private static void PopulateListView()
        {
            HelpBox help = _rootElement.Q<HelpBox>("FolderWarning");

            if(_items.Count > 0)
                help.style.display = DisplayStyle.None;
            else
                help.style.display = DisplayStyle.Flex;


            Func<VisualElement> makeItem = () =>
            {
                VisualElement listContainer = new VisualElement();
                listContainer.name = "ListContainer";
                listContainer.AddToClassList("ListContainer");

                Label listLabel = new Label("");
                listLabel.name = "ListLabel";
                listLabel.AddToClassList("ListLabel");
                listContainer.Add(listLabel);

                return listContainer;
            };

            Action<VisualElement, int> bindItem = (e, i) => BindItem(e, i);
            VisualElement BindItem(VisualElement ve, int i)
            {
                ve.Q<Label>("ListLabel").text = _items[i];
                return ve;
            }

            ListView listView = _rootElement.Q<ListView>();
            _rootElement.AddToClassList("listview-item");
            listView.makeItem = makeItem;
            listView.bindItem = bindItem;
            listView.itemsSource = _items;

            listView.itemsAdded += ItemAdded;
            listView.itemsRemoved += ItemsRemoved;

            listView.selectionType = SelectionType.Single;
        }

        private static void ItemAdded(IEnumerable<int> item)
        {
            string path = EditorUtility.OpenFolderPanel("Browse for folder", Directory.GetCurrentDirectory(), "Backups");
            if (path.Length > 0)
            {
                path = new DirectoryInfo(path).Name;
                _items[item.ElementAt(0)] = path;
                _backFolders = JsonConvert.SerializeObject(_items);
            }
            else
            {
                _items.RemoveAt(item.ElementAt(0));
            }

            InitializeEditor();
        }

        private static void ItemsRemoved(IEnumerable<int> item)
        {
            _backFolders = JsonConvert.SerializeObject(_items);
            InitializeEditor();
        }

        private static void HandleAutoBackup(ChangeEvent<bool> evt)
        {
            _autoBackup = evt.newValue;

            DisplayBackupTime(evt.newValue);
            UpdateNextBackup();
        }

        private static void UpdateNextBackup()
        {
            Label nextBackup = _rootElement.Q<Label>("NextBackup");
            if (_backingUp)
                nextBackup.text = "Backing Up now...";
            else if (!_autoBackup)
                nextBackup.text = "Disabled";
            else
                nextBackup.text = _lastBackup.Add(_backupTimeSpan).ToString();

            Label lastBackup = _rootElement.Q<Label>("LastBackup");

            if (_lastBackup != DateTime.MinValue)
                lastBackup.text = _lastBackup.ToString();
            else
                lastBackup.text = "Never backed Up";
        }

        private static void DisplayBackupTime(bool newValue)
        {
            SliderInt daySlider = _rootElement.Q<SliderInt>("DaySlider");
            SliderInt hourSlider = _rootElement.Q<SliderInt>("HourSlider");
            SliderInt minSlider = _rootElement.Q<SliderInt>("MinSlider");

            daySlider.SetEnabled(newValue);
            hourSlider.SetEnabled(newValue);
            minSlider.SetEnabled(newValue);
        }

        private static void Quit()
        {
            if (_backupOnExit)
                DoBackup();

            WaitForBackup();
        }

        private static async void WaitForBackup()
        {
            while (_backingUp)
            {
                await Task.Yield();
            }
        }

        private static bool CanBackup()
        {
            return !_backingUp && (/*FastZip.isSupported ||*/ SevenZip.IsSupported) && !EditorApplication.isPlaying;
        }

        private static string FormatFileSize(long bytes)
        {
            var unit = 1024;
            if (bytes < unit) { return $"{bytes} B"; }

            var exp = (int)(Math.Log(bytes) / Math.Log(unit));
            return $"{bytes / Math.Pow(unit, exp):F2} {("KMGTPE")[exp - 1]}B";
        }

        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            var provider = new CronusBackupProvider(_settingsPath, SettingsScope.User);

            //provider.keywords = GetSearchKeywordsFromGUIContentProperties<Styles>();
            return provider;
        }
    }
}

public enum ZipModes
{
    SevenZip = 1,
    //FastZip = 2
}