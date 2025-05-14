namespace Verve.File
{
    
    using Unit;
    using System;
    using System.IO;
    using System.Text;
    using Serializable;
    using Newtonsoft.Json;
    using System.Collections.Generic;


    /// <summary>
    /// 文件单元
    /// </summary>
    [CustomUnit("File", dependencyUnits: typeof(SerializableUnit)), System.Serializable]
    public partial class FileUnit : UnitBase
    {
        private SerializableUnit m_Serializable;

        protected override void OnStartup(UnitRules parent, params object[] args)
        {
            base.OnStartup(parent, args);
            parent.onInitialized += (_) =>
            {
                parent.TryGetDependency<SerializableUnit>(out m_Serializable);
            };
        }

        public bool CreateDirectory(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return false;
            string fullPath = FileDefine.GetFileAbsolutePath(relativePath);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                return true;
            }
            return false;
        }
        
        public bool TryReadFile<T>(
            string relativePath,
            out T data,
            bool binary = false
        )
        {
            string fullPath = FileDefine.GetFileAbsolutePath(relativePath);
            if (!File.Exists(fullPath))
            {
                data = default;
                return false;
            }
            byte[] bytes = File.ReadAllBytes(fullPath);
            data = binary ? m_Serializable.Deserialize<T>(bytes) : JsonConvert.DeserializeObject<T>(Encoding.GetEncoding("UTF-8").GetString(bytes));
            return true;
        }

        public bool WriteFile<T>(
            string relativePath, 
            T data, 
            bool binary = false
        ) {
            if (string.IsNullOrEmpty(relativePath) || data == null)
                return false;

            string fullPath = FileDefine.GetFileAbsolutePath(relativePath);
            CreateDirectory(Path.GetDirectoryName(fullPath));
            byte[] bytes = binary ? 
                m_Serializable.Serialize(data) : 
                Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));

            try
            {
                bool fileExists = File.Exists(fullPath);

                if (fileExists)
                {
                    if (binary)
                    {
                        using (FileStream fs = new FileStream(fullPath, FileMode.Append, FileAccess.Write))
                        using (BinaryWriter writer = new BinaryWriter(fs))
                        {
                            writer.Write(bytes.Length);
                            writer.Write(bytes);
                        }
                    }
                    else
                    {
                        string jsonData = (fileExists ? "\n" : "") + JsonConvert.SerializeObject(data);
                        File.AppendAllText(fullPath, jsonData, Encoding.UTF8);
                    }
                }
                else
                {
                    string tempPath = Path.Combine(FileDefine.TempPath, Guid.NewGuid().ToString());
                    File.WriteAllBytes(tempPath, bytes);
                    File.Move(tempPath, fullPath);
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
            string fullPath = FileDefine.GetFileAbsolutePath(relativePath);
            if (!File.Exists(fullPath)) return false;
            File.Delete(fullPath);
            return true;
        }
        
        public bool DeleteDirectory(string relativePath, bool recursive = true)
        {
            if (string.IsNullOrEmpty(relativePath))
                return false;
            string fullPath = FileDefine.GetFileAbsolutePath(relativePath);
            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, recursive);
                return true;
            }
            return false;
        }
    }
}