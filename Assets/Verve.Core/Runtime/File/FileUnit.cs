namespace Verve.File
{
    using Unit;
    using System;
    using System.IO;
    using Serializable;
    using System.Collections.Generic;


    /// <summary>
    /// 文件单元
    /// </summary>
    [CustomUnit("File", dependencyUnits: typeof(SerializableUnit)), System.Serializable]
    public partial class FileUnit : UnitBase<IFileService>
    {
        private SerializableUnit m_Serializable;

        protected virtual IFileService FileService => GetService<DefaultFileService>();
        

        protected override void OnStartup(params object[] args)
        {
            base.OnStartup(args);
            AddService(new DefaultFileService());
        }

        protected override void OnPostStartup(UnitRules parent)
        {
            base.OnPostStartup(parent);
            parent.TryGetDependency<SerializableUnit>(out m_Serializable);
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
            where TSerializable : class, ISerializableService
        {
            string fullPath = GetFullFilePath(relativePath);
            if (!File.Exists(fullPath))
            {
                data = default;
                return false;
            }
            using (var fs = FileService.OpenRead(fullPath))
            {
                data = m_Serializable.DeserializeFromStream<TSerializable, TData>(fs);
            }
            return true;
        }

        public bool WriteFile<TSerializable, TData>(string relativePath, TData data, bool isOverwrite = true)
            where TSerializable : class, ISerializableService
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
                        m_Serializable.Serialize<TSerializable>(fs, data);
                    }
                }
                else
                {
                    string tempPath;
                    using (FileStream fs = CreateTemporaryFile(out tempPath))
                    {
                        m_Serializable.Serialize<TSerializable>(fs, data);
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

        public string GetFullFilePath(string relativePath) => Path.IsPathRooted(relativePath) ? relativePath : Path.Combine(PersistentDataPath, relativePath);
        public string GetTempFilePath() => Path.Combine(TemporaryPath, Guid.NewGuid().ToString());

        public string PersistentDataPath => FileService.PersistentDataPath;
        public string TemporaryPath => FileService.TemporaryPath;
    }
}