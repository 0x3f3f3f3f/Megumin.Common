using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;

namespace Megumin.IO
{
    public static class CopyUtility
    {
        /// <summary>
        /// 复制文件到指定目录
        /// </summary>
        /// <param name="sourceFileName"></param>
        /// <param name="destinationDir"></param>
        /// <param name="overwrite"></param>
        public static int CopyFileToDirectory(string sourceFileName, string destinationDir, bool overwrite = false)
        {
            if (File.Exists(sourceFileName))
            {
                string name = System.IO.Path.GetFileName(sourceFileName);
                string dest = System.IO.Path.Combine(destinationDir, name);
                System.IO.File.Copy(sourceFileName, dest, overwrite);//复制文件
                return 0;
            }
            return -1;
        }

        /// <summary>
        /// 复制文件夹及文件
        /// </summary>
        /// <param name="sourceDir">原文件路径</param>
        /// <param name="destinationDir">目标文件路径</param>
        /// <returns></returns>
        public static int CopyDirectory(string sourceDir,
                                        string destinationDir,
                                        bool deleteTargetFolderBeforeCopy = true,
                                        bool includeSourceDirSelf = false,
                                        bool overwrite = false,
                                        bool recursive = true,
                                        Func<string, bool> checkIgnore = null)
        {
            try
            {
                if (string.IsNullOrEmpty(sourceDir))
                {
                    return -1;
                }

                if (includeSourceDirSelf)
                {
                    ///包含源目录自身
                    DirectoryInfo info = new DirectoryInfo(sourceDir);
                    var f = info.Name;
                    destinationDir = Path.GetFullPath(Path.Combine(destinationDir, f));
                }

                if (deleteTargetFolderBeforeCopy && System.IO.Directory.Exists(destinationDir))
                {
                    System.IO.Directory.Delete(destinationDir, true);
                }

                //如果目标路径不存在,则创建目标路径
                if (!System.IO.Directory.Exists(destinationDir))
                {
                    System.IO.Directory.CreateDirectory(destinationDir);
                }

                //得到原文件根目录下的所有文件
                string[] files = System.IO.Directory.GetFiles(sourceDir);
                foreach (string file in files)
                {
                    if ((checkIgnore?.Invoke(file)) != true)
                    {
                        CopyFileToDirectory(file, destinationDir, overwrite);
                    }
                }

                if (recursive)
                {
                    //得到原文件根目录下的所有文件夹
                    string[] folders = System.IO.Directory.GetDirectories(sourceDir);
                    foreach (string folder in folders)
                    {
                        if ((checkIgnore?.Invoke(folder)) != true)
                        {
                            string name = System.IO.Path.GetFileName(folder);
                            string dest = System.IO.Path.Combine(destinationDir, name);
                            var dirName = System.IO.Path.GetDirectoryName(folder);
                            if (name == ".git")
                            {
                                continue;
                            }

                            //构建目标路径,递归复制文件
                            CopyDirectory(folder,
                                          dest,
                                          deleteTargetFolderBeforeCopy,
                                          false,
                                          overwrite,
                                          recursive,
                                          checkIgnore);
                        }
                    }
                }

                return 1;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return 0;
            }

        }
    }

    public abstract class CopyInfo
    {
        public abstract List<string> DestinationDirs { get; }

        public void OpenTarget()
        {
            foreach (var target in DestinationDirs)
            {
                var root = Path.Combine(PathUtility.ProjectPath, target);
                var targetFolder = Path.GetFullPath(root);
                Debug.Log($"Open {targetFolder}");
                System.Diagnostics.Process.Start(targetFolder);
            }
        }

        public void DeleteTarget()
        {
            foreach (var target in DestinationDirs)
            {
                var root = Path.Combine(PathUtility.ProjectPath, target);
                var targetFolder = Path.GetFullPath(root);
                Debug.Log($"Delete {targetFolder}");
                if (Directory.Exists(targetFolder))
                {
                    Directory.Delete(targetFolder, true);
                }
            }
        }
    }

    [Serializable]
    public class FileCopyInfo : CopyInfo
    {
        [Path(IsFolder = false, ForceDrag = true)]
        public List<string> File = new();
        public bool Overwrite = true;

        [Space]
        [Path]
        public List<string> Targets = new();
        public override List<string> DestinationDirs => Targets;

        public void Copy()
        {
            foreach (var item in DestinationDirs)
            {
                string destinationDir = item.GetFullPathWithProject();
                foreach (var filePath in File)
                {
                    var result = CopyUtility.CopyFileToDirectory(filePath, destinationDir, Overwrite);
                    if (result >= 0)
                    {
                        Debug.Log($"Copy File {filePath}  To  {destinationDir}");
                    }
                }
            }
        }
    }

    [Serializable]
    public class DirectoryCopyInfo : CopyInfo
    {
        [Path]
        public string Source;

        public bool DeleteTargetFolderBeforeCopy = true;
        public bool IncludeSourceDirSelf = true;
        public bool Overwrite = true;
        public bool Recursive = true;

        [Space]
        [Path]
        public List<string> Targets = new();
        public List<string> IgnoreExtension = new();

        public void Copy()
        {
            foreach (string target in DestinationDirs)
            {
                if (!string.IsNullOrEmpty(Source))
                {
                    string sourceDir = Source.GetFullPathWithProject();
                    string destinationDir = target.GetFullPathWithProject();

                    CopyUtility.CopyDirectory(sourceDir,
                                              destinationDir,
                                              DeleteTargetFolderBeforeCopy,
                                              IncludeSourceDirSelf,
                                              Overwrite,
                                              Recursive,
                                              CheckIgnore);

                    Debug.Log($"Copy Directory {sourceDir}  To  {destinationDir}");
                }
            }
        }

        private bool CheckIgnore(string path)
        {
            var ex = Path.GetExtension(path);
            return IgnoreExtension.Contains(ex);
        }

        public override List<string> DestinationDirs => Targets;
    }

    [Serializable]
    public class UPMCopyInfo : CopyInfo
    {
        [Space]
        public List<string> packageName = new();

        public bool DeleteTargetFolderBeforeCopy = true;

        [Space]
        [Path]
        public List<string> Targets = new();
        public override List<string> DestinationDirs => Targets;

        public void CopyOne(string packageName, string targetFolder)
        {
            string sourceDir = null;
            string destinationDir = targetFolder.GetFullPathWithProject();

#if UNITY_EDITOR

            var infos = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
            foreach (var info in infos)
            {
                if (packageName.Trim().Equals(info.name, StringComparison.OrdinalIgnoreCase))
                {
                    sourceDir = info.resolvedPath;
                    destinationDir = Path.GetFullPath(Path.Combine(destinationDir, info.name));
                }
            }

#endif

            CopyUtility.CopyDirectory(sourceDir,
                                      destinationDir,
                                      DeleteTargetFolderBeforeCopy);

            Debug.Log($"Copy UPM {sourceDir}  To  {destinationDir}");
        }

        public void Copy()
        {
            foreach (var target in Targets)
            {
                foreach (var item in packageName)
                {
                    CopyOne(item, target);
                }
            }
        }
    }


    public class FastCopy : ScriptableObject
    {
        public List<FileCopyInfo> FileCopy = new();
        [FormerlySerializedAs("ops")]
        public List<DirectoryCopyInfo> DirectoryCopy = new();
        public List<UPMCopyInfo> UPMCopy = new();

        [ContextMenu("Copy")]
        public void Copy()
        {
            foreach (var fileCopy in FileCopy)
            {
                fileCopy.Copy();
            }

            foreach (DirectoryCopyInfo op in DirectoryCopy)
            {
                op.Copy();
            }

            foreach (var item in UPMCopy)
            {
                item.Copy();
            }
        }

        [ContextMenu("OpenTarget")]
        public void OpenTarget()
        {
            foreach (var fileCopy in FileCopy)
            {
                fileCopy.OpenTarget();
            }

            foreach (DirectoryCopyInfo op in DirectoryCopy)
            {
                op.OpenTarget();
            }

            foreach (var item in UPMCopy)
            {
                item.OpenTarget();
            }
        }

        //[ContextMenu("DeleteTarget")]
        //public void DeleteTarget()
        //{
        //    foreach (DirectoryCopyInfo op in ops)
        //    {
        //        op.DeleteTarget();
        //    }

        //    foreach (var item in UPMCopy)
        //    {
        //        item.DeleteTarget();
        //    }
        //}
    }
}

