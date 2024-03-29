using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Studious
{
    public class PopupWindow : EditorWindow
    {
        private const string _packagePath = "Packages/com.studiousgames.studiousbackuppackage/Editor/Resources/Layouts/PopupWindow.uxml";
        private static PopupWindow _window;

        public void OnEnable()
        {
            _window = (PopupWindow)GetWindow(typeof(PopupWindow));
            _window.titleContent = new GUIContent(  "Studios Backup Package");
            _window.maxSize = new Vector2(480, 250);
            _window.minSize = new Vector2(480, 250);
        }

        void CreateGUI()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(_packagePath);
            VisualElement labelFromUXML = visualTree.Instantiate();
            rootVisualElement.Add(labelFromUXML);

            Label warning = rootVisualElement.Q<Label>("TextWarning");
            warning.text = "<color=red>Warning:</color> When the option for <B>Back up on Exit " +
                "is selected</b>, depending on the size of the project, Unity may appear to have hung." +
                "\n\n" +
                "Please let Unity finish what it is doing, Unity will close once the backup has " +
                "completed." +
                "\n\n" +
                "Interrupting this process will lead to a corrupt backup.";
        }

    }
}
