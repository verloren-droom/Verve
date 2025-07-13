namespace Verve.File
{
    using System;
    using Platform;
    using System.IO;
    using Serializable;
    using System.Collections.Generic;

    
    /// <summary>
    /// 文件功能
    /// </summary>
    [Serializable]
    public partial class FileFeature : GameFeature
    {
        protected IFileSubmodule m_FileSubmodule;
        protected SerializableFeature m_Serializable;
        protected PlatformFeature m_Platform;


        protected override void OnLoad(IReadOnlyFeatureDependencies dependencies)
        {
            base.OnLoad(dependencies);
            
            m_Serializable = dependencies.Get<SerializableFeature>();
            m_Platform = dependencies.Get<PlatformFeature>();
            m_FileSubmodule = new GenericFileSubmodule();
            m_FileSubmodule.OnModuleLoaded(dependencies);
        }
        
        protected override void OnUnload()
        {
            base.OnUnload();
            
            m_FileSubmodule?.OnModuleUnloaded();
            m_FileSubmodule = null;
            m_Serializable = null;
        }

        public bool CreateDirectory(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return false;
            string fullPath = GetFullFilePath(relativePath);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                return true;
            }
            return false;
        }
        
        public bool TryReadFile<TSerializable, TData>(string relativePath, out TData data)
            where TSerializable : class, ISerializableSubmodule
        {
            string fullPath = GetFullFilePath(relativePath);
            if (!File.Exists(fullPath))
            {
                data = default;
                return false;
            }
            using (var fs = m_FileSubmodule?.OpenRead(fullPath))
            {
                data = m_Serializable.GetSubmodule<TSerializable>().DeserializeFromStream<TData>(fs);
            }
            return true;
        }

        public bool WriteFile<TSerializable, TData>(string relativePath, TData data, bool isOverwrite = true)
            where TSerializable : class, ISerializableSubmodule
        {
            if (string.IsNullOrEmpty(relativePath) || data == null)
                return false;

            string fullPath = GetFullFilePath(relativePath);
            CreateDirectory(Path.GetDirectoryName(fullPath));

            try
            {
                bool fileExists = File.Exists(fullPath);

                if (fileExists && !isOverwrite)
                {
                    using (FileStream fs = new FileStream(fullPath, FileMode.Append, FileAccess.Write))
                    {
                        m_Serializable.GetSubmodule<TSerializable>().Serialize(fs, data);
                    }
                }
                else
                {
                    string tempPath;
                    using (FileStream fs = CreateTemporaryFile(out tempPath))
                    {
                        m_Serializable?.GetSubmodule<TSerializable>().Serialize(fs, data);
                    }

                    if (fileExists)
                    {
                        File.Replace(tempPath, fullPath, null);
                    }
                    else
                    {
                        File.Move(tempPath, fullPath);
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool DeleteFile(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return false;
            string fullPath = Path.IsPathRooted(relativePath) ? relativePath : GetFullFilePath(relativePath);
            if (!File.Exists(fullPath)) return false;
            File.Delete(fullPath);
            return true;
        }
        
        public bool DeleteDirectory(string relativePath, bool recursive = true)
        {
            if (string.IsNullOrEmpty(relativePath))
                return false;
            string fullPath = GetFullFilePath(relativePath);
            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, recursive);
                return true;
            }
            return false;
        }
        
        public FileStream CreateTemporaryFile(out string tempPath)
        {
            tempPath = GetTempFilePath();
            if (!Directory.Exists(Path.GetDirectoryName(tempPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(tempPath));
            return File.Create(tempPath);
        }

        public IObservable<FileSystemEventArgs> Watch(string path)
        {
            return m_FileSubmodule.Watch(path);
        }

        public string GetFullFilePath(string relativePath) => Path.IsPathRooted(relativePath) ? relativePath : Path.Combine(m_Platform.GetPersistentDataPath(), relativePath);
        public string GetTempFilePath() => Path.Combine(m_Platform.GetTemporaryCachePath(), Guid.NewGuid().ToString());
    }
}