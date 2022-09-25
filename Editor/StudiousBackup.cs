using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;
using Newtonsoft.Json;
using UnityEngine;
using System.Threading.Tasks;
using System.IO.Compression;
using System.ComponentModel;
using System.Threading;
using Newtonsoft.Json.Linq;
using UnityEditor.UIElements;
using UnityEditor.Search;

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
        private static Task<ZipResult> backupTask;

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

        private static bool _popupShown
        {
            get { return EditorPrefs.GetBool("SBS.ShowPopup", false); }
            set { EditorPrefs.SetBool("SBS.ShowPopup", value); }
        }

        private static int _backupNumber
        {
            get { return EditorPrefs.GetInt("SBS.BackupNumber", 10); }
            set { EditorPrefs.SetInt("SBS.BackupNumber", value); }
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
                        StartBackup();
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

                if (evt.newValue == true && !_popupShown)
                {
                    ScriptableObject.CreateInstance<PopupWindow>().ShowModal();
                    _popupShown = true;
                }
                if(!evt.newValue)
                    _popupShown = false;
            });

            Toggle backautoBackup = _rootElement.Q<Toggle>("AutoBackup");
            backautoBackup.value = _autoBackup;
            backautoBackup.RegisterValueChangedCallback(evt =>
            {
                _autoBackup = evt.newValue;
            });

            Label customLocation = _rootElement.Q<Label>("CustomLocation");
            customLocation.text = _customSaveLocation;

            IntegerField numberBackups = _rootElement.Q<IntegerField>("BackupNumber");
            numberBackups.value = _backupNumber;
            numberBackups.RegisterValueChangedCallback(evt =>
            {
                _backupNumber = evt.newValue;
                if (_backupNumber < 0)
                {
                    _backupNumber = 0;
                    numberBackups.value = 0;
                }

            });


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

                defaultButton.Blur();
            };

            Button backupNow = _rootElement.Q<Button>("BackupNow");
            backupNow.clicked += () =>
            {
                backupNow.Blur();
                StartBackup();
            };
        }

        [MenuItem("Tools/Studios Backup/Backup Now")]
        public static async void StartBackup()
        {
            if (!CanBackup())
                return;

            _backingUp = true;
            UpdateNextBackup();
            double startTime = EditorApplication.timeSinceStartup;
            string zipPath = _saveLocation;

            Logger.Log("Backing Up...");

            if (_useCustomSaveLocation)
            {
                zipPath = $"{zipPath}/{_productNameForFile}";
                if (!Directory.Exists(zipPath))
                {
                    Directory.CreateDirectory(zipPath);
                }
            }

            zipPath = string.Format("{0}/{1}_backup_{2}.zip", zipPath, _productNameForFile, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));

            backupTask = DoBackup(zipPath);
            await Task.WhenAll(backupTask);

            if(backupTask.Result.Success)
            {
                FileInfo fileInfo = new FileInfo(zipPath);
                string time = (EditorApplication.timeSinceStartup - startTime).ToString("0.00");
                Logger.LogFormat("Project backed up into {0} in {1} seconds", FormatFileSize(fileInfo.Length), time);

                _lastBackup = DateTime.Now;
                RemovePreviousBackups(Path.Combine(_saveLocation, _productNameForFile));
            }
            else
            {
                Logger.LogWarning(backupTask.Result.Message);
            }

            _backingUp = false;
            UpdateNextBackup();
        }

        private static void RemovePreviousBackups(string path)
        {
            DirectoryInfo directory = new DirectoryInfo(path);
            FileInfo[] backups = directory.GetFiles();

            foreach (var file in backups.OrderByDescending(file => file.CreationTime).Skip(_backupNumber))
            {
                file.Delete();
            }
        }

        private static Task<ZipResult> DoBackup(string zipPath)
        {
            bool isSuccess;
            string message = string.Empty;

            return Task.Run(() =>
            {
                ZipArchive archive;
                try
                {
                    using (archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                    {
                        foreach (string item in _items)
                        {
                            CompressFolder(item, archive);
                        }
                    }
                    isSuccess = true;
                }
                catch (Exception e)
                {
                    isSuccess = false;
                    message = e.Message;
                }

                return new ZipResult
                {
                    Success = isSuccess,
                    Message = message
                };
            });
        }

        private static void CompressFolder(string path, ZipArchive zipStream)
        {
            DirectoryInfo dir = new DirectoryInfo(path);

            foreach (FileInfo file in dir.AllFilesAndFolders().Where(o => o is FileInfo).Cast<FileInfo>())
            {
                string relPath = file.FullName.Substring(dir.Parent.FullName.Length + 1);
                zipStream.CreateEntryFromFile(file.FullName, relPath);
            }
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

        static ListView listView;

        private static void PopulateListView()
        {
            HelpBox help = _rootElement.Q<HelpBox>("FolderWarning");

            if (_items.Count > 0)
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

            listView = _rootElement.Q<ListView>();
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
               StartBackup();

            if (_backingUp)
                backupTask.Wait();
        }

        private static bool CanBackup()
        {
            return !_backingUp && !EditorApplication.isPlaying && _items.Count != 0;
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