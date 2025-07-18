namespace Verve.File
{
    using System;
    using System.Security.Cryptography;
    

    public static class FileExtension
    {
        public static string GetMD5(this System.IO.Stream self)
        {
            long originalPosition = self.CanSeek ? self.Position : 0;
    
            try
            {
                using var md5 = MD5.Create();
                if (self.CanSeek) self.Position = 0;
                byte[] hashBytes = md5.ComputeHash(self);
                return BitConverter.ToString(hashBytes).Replace("-", "");
            }
            finally
            {
                if (self.CanSeek) self.Position = originalPosition;
            }
        }
    }
}