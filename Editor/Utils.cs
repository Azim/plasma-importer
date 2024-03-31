using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Azim.PlasmaImporter.Editor
{
    public class Utils
    {
        private readonly static string[] DllsToCopy = new[] {      // /managed
            "AIToASE.dll",
            "AmplifyImpostors.dll",
            // "Assembly-CSharp.dll", 
            "Assembly-CSharp-firstpass.dll",
            "com.rlabrecque.steamworks.net.dll",
            "DemiLib.dll",
            "DOTween.dll",
            "DOTweenPro.dll",
            "FMODUnity.dll",
            "FMODUnityResonance.dll",
            "GlitchLibraryAssembly.dll",
            "jianglong.library.gif-player.dll",
            "mcs.dll",
            //"Newtonsoft.Json.dll",
            "PlasmaLibrary.dll",
            "QFSW.QC.dll",
            "QFSW.QC.Extras.dll",
            "QFSW.QC.Grammar.dll",
            "QFSW.QC.Parsers.dll",
            "QFSW.QC.ScanRules.dll",
            "QFSW.QC.Serializers.dll",
            "QFSW.QC.Suggestors.dll",
            "QFSW.QC.UI.dll",
            "Rewired_Core.dll",
            "Rewired_Windows.dll",
            "Sirenix.OdinInspector.Attributes.dll",
            "Sirenix.Serialization.Config.dll",
            "Sirenix.Serialization.dll",
            "Sirenix.Utilities.dll",
            "Tayx.Graphy.dll",
            "ZFBrowser.dll",
        };
        public static void CopyFilesRecursively(string sourceFolder, string targetFolder)
        {
            Directory.CreateDirectory(targetFolder);

            var paths = Directory.GetDirectories(sourceFolder, "*", SearchOption.AllDirectories);
            for (var i = 0; i < paths.Length; i++)
            {
                var dirPath = paths[i];
                EditorUtility.DisplayProgressBar("Copying files", $"Creating directory {dirPath}", (float)i / paths.Length);
                Directory.CreateDirectory(dirPath.Replace(sourceFolder, targetFolder));
            }

            var files = Directory.GetFiles(sourceFolder, "*.*", SearchOption.AllDirectories);
            for (var i = 0; i < files.Length; i++)
            {
                var newPath = files[i];
                EditorUtility.DisplayProgressBar("Copying files", $"Copying {newPath}", (float)i / files.Length);
                File.Copy(newPath, newPath.Replace(sourceFolder, targetFolder), overwrite: true);
            }

            EditorUtility.ClearProgressBar();
        }


        public static string assetRipperOutputPath => Path.GetFullPath(".temp/AssetRipperOutput");
        public readonly static string AssetRipperDownloadPath = "https://github.com/Azim/AssetRipper/releases/download/v1.0.0/AssetRipper.SourceGenerated.zip";
        public static string assetRipperExePath => Path.GetFullPath("Packages/icu.azim.plasma-importer/Editor/Libs/.AssetRipper/AssetRipper.Tools.SystemTester.exe");
        public static void AssetRipperCheck()
        {
            var dllLocation = Path.Combine(Path.GetDirectoryName(Utils.assetRipperExePath), "AssetRipper.SourceGenerated.dll");
            if (!File.Exists(dllLocation))
            {
                var dllUrl = Utils.AssetRipperDownloadPath;
                var zipLocation = $"{dllLocation}.zip";
                EditorUtility.DisplayProgressBar("Downloading AssetRipper DLL", $"Downloading from {dllUrl}", 0.5f);
                Debug.Log($"Started download");

                using (var client = new System.Net.WebClient())
                {
                    client.DownloadFile(dllUrl, zipLocation);
                    //client.DownloadFileTaskAsync(dllUrl, zipLocation).Wait();
                    Debug.Log("Downloaded");
                }

                EditorUtility.ClearProgressBar();

                if (!File.Exists(zipLocation))
                {
                    throw new Exception("Failed to download AssetRipper DLL");
                }

                Debug.Log($"Extracting {zipLocation} to {dllLocation}");

                try
                {
                    System.IO.Compression.ZipFile.ExtractToDirectory(zipLocation, Path.GetDirectoryName(dllLocation));
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    throw;
                }
                Debug.Log("Extracted");

                Debug.Log($"Deleting {zipLocation}");
                try
                {
                    File.Delete(zipLocation);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }

                if (!File.Exists(dllLocation))
                {
                    throw new Exception("Failed to extract AssetRipper DLL");
                }
                Debug.Log("Deleted");
            }
        }
        public static void AssetRip(string gamePath)
        {
            var outputPath = Utils.assetRipperOutputPath;
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, recursive: true);
            }
            Debug.Log($"Running AssetRipper at \"{Utils.assetRipperExePath}\" with \"{gamePath}\" and outputting into \"{outputPath}\"");
            Debug.Log($"Using data folder at \"{gamePath}\"");
            Debug.Log($"Outputting ripped assets at \"{outputPath}\"");

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = Utils.assetRipperExePath,
                    Arguments = $"\"{gamePath}\" \"{outputPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            try
            {
                process.Start();

                var elapsed = 0f;
                while (!process.StandardOutput.EndOfStream)
                {
                    var line = process.StandardOutput.ReadLine();
                    //? time estimation
                    elapsed += Time.deltaTime / (60f * 30);
                    EditorUtility.DisplayProgressBar("Running AssetRipper", line, elapsed);
                }
                EditorUtility.ClearProgressBar();
                process.WaitForExit();

                var errorOutput = process.StandardError.ReadToEnd();

                // check for any errors
                if (process.ExitCode != 0)
                {
                    throw new Exception($"AssetRipper failed to run with exit code {process.ExitCode}. Error: {errorOutput}");
                }

            }
            catch (Exception e)
            {
                Debug.LogError($"Error running AssetRipper: {e}");
                throw;
            }
        }

        public static void CopyDlls(string gameDataDir)
        {
            string targetDir = Path.GetFullPath("Assets/Plugins");
            if (Directory.Exists(targetDir))
            {
                Directory.Delete(targetDir, true);
            }
            Directory.CreateDirectory(targetDir);

            string sourceDir = Path.Combine(gameDataDir, "Managed");
            foreach(string name in DllsToCopy)
            {
                string source = Path.Combine(sourceDir, name);
                string target = Path.Combine(targetDir, name);
                File.Copy(source, target, true);
            }
        }

        private static void RemoveConstructor(string path, string name)
        {
            string text = File.ReadAllText(path);
            while (text.Contains($"public {name}("))
            {
                int start = text.IndexOf($"public {name}(");
                int end = text.IndexOf("}", start);
                text = text.Remove(start, end + 1 - start);
            };

            File.WriteAllText(path, text);
        }
        public static void ScrubScripts(string scriptsPath)
        {

            Debug.Log("Deleting UnitySourceGeneratedAssemblyMonoScriptTypes_v1.cs");
            try
            {
                File.Delete(Path.Combine(scriptsPath, "UnitySourceGeneratedAssemblyMonoScriptTypes_v1.cs"));
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            var allScripts = Directory.GetFiles(scriptsPath, "*.cs", SearchOption.AllDirectories);
            for (int i = 0; i < allScripts.Length; i++)
            {
                string file = allScripts[i];
                EditorUtility.DisplayProgressBar("Fixing compile errors", $"checking {file}", (float)i / (float)allScripts.Length);

                if (file.Contains("TheraBytes")) //weird default constructors
                {
                    string text = File.ReadAllText(file);
                    text = text.Replace("(_00210)", "");
                    File.WriteAllText(file, text);
                    continue;
                }
                if (file.EndsWith("QTransform.cs")) //wants too much so eh just nuke it
                {
                    RemoveConstructor(file, "QTransform");
                    continue;
                }
                if (file.EndsWith("Basis.cs"))
                {
                    RemoveConstructor(file, "Basis");
                    continue;
                }
                if (file.EndsWith("LuaNativeTable.cs"))
                {
                    RemoveConstructor(file, "LuaNativeTable");
                    continue;
                }
                if (file.EndsWith("LuaTableContent.cs"))
                {
                    RemoveConstructor(file, "Key");
                    continue;
                }
                if (file.EndsWith("ToLuaString.cs"))
                {
                    RemoveConstructor(file, "ToLuaString");
                    continue;
                }
                if (file.EndsWith("WireframeComponentListener.cs"))
                {
                    RemoveConstructor(file, "CollisionPair");
                    continue;
                }
                if (file.EndsWith("VFXComponent.cs"))
                {
                    //public Dictionary<MeshRenderer, SpecialMaterial> specialMaterials
                    //[OdinSerialize] public MyDictionary specialMaterials;

                    string text = File.ReadAllText(file);
                    text = text.Replace(
                        "public Dictionary<MeshRenderer, SpecialMaterial> specialMaterials",
                        "[OdinSerialize] public MyDictionary specialMaterials"
                    );
                    text = "using Sirenix.Serialization;\n" + text;
                    File.WriteAllText(file, text);
                    continue;
                }

            }
        }
    }
}