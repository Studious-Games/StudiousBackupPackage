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
        private static PopupWindow window;

        public PopupWindow()
        {
            Init();
        }

        static void Init()
        {
            window = (PopupWindow)EditorWindow.GetWindow(typeof(PopupWindow));
            window.titleContent = new GUIContent(  "Studios Backup Package");
            window.maxSize = new Vector2(480, 250);
            window.minSize = new Vector2(480, 250);
        }

        void CreateGUI()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(_packagePath);
            VisualElement labelFromUXML = visualTree.Instantiate();
            rootVisualElement.Add(labelFromUXML);

            Label warning = rootVisualElement.Q<Label>("TextWarning");
            warning.text = "<color=red>Warning:</color> A backup is currently running in the background, and " +
                "Unity will close as soon as it has finished." +
                "\n\n" +
                "By clsoing this Popup window you acknowldge that Unity is still running a backup of your project " +
                "in the backgroud.";
        }

    }
}
