using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
#if UNITY_EDITOR
using UnityEditor.PackageManager;
#endif

namespace Megumin
{
    /// <summary>
    /// 快速检查和更新本地包的依赖的包的版本。
    /// 如果多个本地包互相依赖时，修改包版本时特别好用
    /// </summary>
    public class PackageDependenciesUpdater : ScriptableObject
    {
        [UnityPackageName(Source = new string[] { "Embedded", "Local" })]
        public List<string> Packages;

        [ContextMenu("Check Dependencies")]
        public void CheckDependencies()
        {

#if UNITY_EDITOR
            foreach (var packageName in Packages)
            {
                if (TryGetNeedUpdateList(packageName, out var packageInfo, out var needUpdate))
                {
                    UpdatePackageJsonFile(packageInfo, needUpdate);
                    foreach (var item in needUpdate)
                    {
                        Debug.Log($"{packageName} depend {item.dName}:{item.oldVersion}, but {item.dName} already {item.newVersion}");
                    }
                }
            }
#endif

        }

        [ContextMenu("Update Dependencies")]
        public void UpdateDependencies()
        {
#if UNITY_EDITOR

            foreach (var packageName in Packages)
            {
                if (TryGetNeedUpdateList(packageName, out var packageInfo, out var needUpdate))
                {
                    UpdatePackageJsonFile(packageInfo, needUpdate);
                }
            }

            UnityEditor.PackageManager.Client.Resolve();
#endif
        }

#if UNITY_EDITOR

        private bool TryGetNeedUpdateList(string packageName,
            out PackageInfo packageInfo,
            out List<(string dName, string oldVersion, string newVersion)> needUpdate)
        {
            if (TryGetPackageInfo(packageName, out packageInfo))
            {
                needUpdate = new();
                foreach (var item in packageInfo.dependencies)
                {
                    if (TryGetPackageInfo(item.name, out var dependenciesPackage))
                    {
                        if (item.version != dependenciesPackage.version)
                        {
                            needUpdate.Add((item.name, item.version, dependenciesPackage.version));
                        }
                    }
                }

                return true;

            }

            needUpdate = null;
            return false;
        }

        public static void UpdatePackageJsonFile(PackageInfo package,
                                                 List<(string dName, string oldVersion, string newVersion)> needUpdate)
        {
            var path = Path.Combine(package.resolvedPath, "package.json");

            var lines = File.ReadAllLines(path);
            var openFlag = false;
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Contains("\"dependencies\""))
                {
                    openFlag = true;
                }

                if (openFlag)
                {
                    foreach (var item in needUpdate)
                    {
                        if (line.Contains(item.dName) && line.Contains(item.oldVersion))
                        {
                            int spaceCount = 0;
                            while (spaceCount < line.Length && line[spaceCount] == ' ')
                            {
                                spaceCount++;
                            }

                            var start = line.Substring(0, spaceCount);
                            var end = line.EndsWith(",") ? "," : "";
                            lines[i] = $"{start}\"{item.dName}\": \"{item.newVersion}\"{end}";
                            Debug.Log($"{package.name} Update Dependency {item.dName} : {item.oldVersion} -> {item.newVersion}");
                        }
                    }
                }
            }

            WriteAllLinesBetter(path, lines);

            ////与Unity默认行为一致，末尾不要空行
            //lines[lines.Length - 1] = lines[lines.Length - 1].TrimEnd();
            //if (string.IsNullOrEmpty(lines[lines.Length - 1]))
            //{
            //    //空行
            //    Array.Resize(ref lines, lines.Length - 1);
            //}

            //File.WriteAllLines(path, lines);
        }

        /// <summary>
        /// https://stackoverflow.com/questions/11689337/net-file-writealllines-leaves-empty-line-at-the-end-of-file
        /// </summary>
        /// <param name="path"></param>
        /// <param name="lines"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void WriteAllLinesBetter(string path, params string[] lines)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (lines == null)
                throw new ArgumentNullException("lines");

            using (var stream = File.OpenWrite(path))
            {
                stream.SetLength(0);
                using (var writer = new StreamWriter(stream))
                {
                    if (lines.Length > 0)
                    {
                        for (var i = 0; i < lines.Length - 1; i++)
                        {
                            writer.WriteLine(lines[i]);
                        }
                        writer.Write(lines[lines.Length - 1]);
                    }
                }
            }
        }

        public bool TryGetPackageInfo(string name, out PackageInfo info)
        {
            var infos = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
            foreach (var item in infos)
            {
                if (name.Trim().Equals(item.name, StringComparison.OrdinalIgnoreCase))
                {
                    info = item;
                    return true;

                }
            }
            info = null;
            return false;
        }


#endif
    }
}

