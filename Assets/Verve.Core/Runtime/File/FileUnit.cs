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
    public partial class FileUnit : UnitBase
    {
        private SerializableUnit m_Serializable;
        

        protected override void OnPostStartup(UnitRules parent)
        {
            base.OnPostStartup(parent);
            parent.TryGetDependency<SerializableUnit>(out m_Serializable);
        }

        public bool CreateDirectory(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return false;
            string fullPath = FileDefine.GetPersistentFilePath(relativePath);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                return true;
            }
            return false;
        }
        
        public bool TryReadFile<TSerializable, TData>(string relativePath, out TData data)
            where TSerializable : class, ISerializableConverter
        {
            string fullPath = FileDefine.GetPersistentFilePath(relativePath);
            if (!File.Exists(fullPath))
            {
                data = default;
                return false;
            }
            using (FileStream fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
            {
                data = m_Serializable.DeserializeFromStream<TSerializable, TData>(fs);
            }
            return true;
        }

        public bool WriteFile<TSerializable, TData>(string relativePath, TData data, bool isOverwrite = true)
            where TSerializable : class, ISerializableConverter
        {
            if (string.IsNullOrEmpty(relativePath) || data == null)
                return false;

            string fullPath = FileDefine.GetPersistentFilePath(relativePath);
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
                    string tempPath = FileDefine.TempFilePath;
                    if (!Directory.Exists(Path.GetDirectoryName(tempPath)))
                        Directory.CreateDirectory(Path.GetDirectoryName(tempPath));
                    using (FileStream fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
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
            string fullPath = FileDefine.GetPersistentFilePath(relativePath);
            if (!File.Exists(fullPath)) return false;
            File.Delete(fullPath);
            return true;
        }
        
        public bool DeleteDirectory(string relativePath, bool recursive = true)
        {
            if (string.IsNullOrEmpty(relativePath))
                return false;
            string fullPath = FileDefine.GetPersistentFilePath(relativePath);
            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, recursive);
                return true;
            }
            return false;
        }
    }
}