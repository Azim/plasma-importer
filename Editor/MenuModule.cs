using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Threading;
using System.Drawing.Drawing2D;

namespace Azim.PlasmaImporter.Editor
{
    public class MenuModule : MonoBehaviour
    {

        // Add a menu item named "Do Something" to MyMenu in the menu bar.
        [MenuItem("PlasmaImporter/Generate Project")]
        static void CreateFacades()
        {
            PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Standalone, ApiCompatibilityLevel.NET_Unity_4_8);

            string path = EditorUtility.OpenFolderPanel("Choose Plasma_Data folder", "", "");
            if (path.Length == 0)
            {
                Debug.Log("Choose Plasma_Data folder");
                return;
            }

            Utils.AssetRipperCheck();

            Utils.AssetRip(path);

            Utils.CopyDlls(path);

            string outputPath = Utils.assetRipperOutputPath;

            string scriptsPath = Path.Combine(outputPath, "ExportedProject", "Assets", "Scripts", "Assembly-CSharp");
            string targetScriptsPath = Path.GetFullPath("Assets/Assembly-CSharp");
            Utils.ScrubScripts(scriptsPath);

            if (Directory.Exists(targetScriptsPath))
            {
                Directory.Delete(targetScriptsPath, true);
            }
            Utils.CopyFilesRecursively(scriptsPath, targetScriptsPath);

            string tagManagerSource = Path.Combine(outputPath, "ExportedProject", "ProjectSettings", "TagManager.asset");
            string tagManagerDest = Path.GetFullPath("ProjectSettings/TagManager.asset");
            File.Copy(tagManagerSource, tagManagerDest, true);

            string utilsFolderSource = Path.GetFullPath("Packages/icu.azim.plasma-importer/.Scripts");
            string utilsFolderDest = Path.GetFullPath("Assets/Plasma-Util");
            if (!Directory.Exists(utilsFolderDest))
            {
                Directory.CreateDirectory(utilsFolderDest);
            }
            string utilsEditorFolderDest = Path.Combine(utilsFolderDest, "Editor");

            if (!Directory.Exists(utilsEditorFolderDest))
            {
                Directory.CreateDirectory(utilsEditorFolderDest);
            }

            File.Copy(
                Path.Combine(utilsFolderSource, "SerializableDictionary.cs"),
                Path.Combine(utilsFolderDest, "SerializableDictionary.cs"),
                true);
            File.Copy(
                Path.Combine(utilsFolderSource, "DictionaryDrawer.cs"),
                Path.Combine(utilsEditorFolderDest, "DictionaryDrawer.cs"),
                true);


        }
    }
}