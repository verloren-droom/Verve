namespace Verve
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Security.Cryptography;

    /// <summary>
    ///   <para>游戏入口；工具部分。</para>
    /// </summary>
    public static partial class Game
    {
        /// <summary>
        ///   <para>哈希计算工具。</para>
        /// </summary>
        public static class HashUtility
        {
            /// <summary>
            ///   <para>读取缓冲区大小；文件大小不影响内存占用。</para>
            /// </summary>
            private const int BufferSize = 81920;

            /// <summary>
            ///   <para>计算稳定的 FNV-1a 64 位哈希；用于缓存和格式指纹，不用于安全校验。</para>
            /// </summary>
            /// <param name="input">借用的字节序列。</param>
            /// <param name="seed">初始值；分段计算时传入上一段的结果。</param>
            public static ulong ComputeFnv1a64(ReadOnlySpan<byte> input, ulong seed = 14695981039346656037UL)
            {
                unchecked
                {
                    foreach (var value in input) seed = (seed ^ value) * 1099511628211UL;
                    return seed;
                }
            }

            /// <summary>
            ///   <para>计算文件摘要；返回小写十六进制字符串。</para>
            /// </summary>
            /// <param name="path">本地文件路径。</param>
            /// <param name="algorithm">摘要算法，如 <see cref="HashAlgorithmName.SHA256"/> 或 <see cref="HashAlgorithmName.MD5"/>。</param>
            public static string ComputeFileHash(string path, HashAlgorithmName algorithm)
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan);
                return ComputeHash(stream, algorithm);
            }

            /// <summary>
            ///   <para>异步计算文件摘要；返回小写十六进制字符串。</para>
            /// </summary>
            /// <param name="path">本地文件路径。</param>
            /// <param name="algorithm">摘要算法。</param>
            /// <param name="cancellationToken">取消令牌。</param>
            public static async Task<string> ComputeFileHashAsync(string path, HashAlgorithmName algorithm, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
                return await ComputeHashAsync(stream, algorithm, cancellationToken).ConfigureAwait(false);
            }

            /// <summary>
            ///   <para>计算流摘要；读取当前位置到末尾，返回小写十六进制字符串。</para>
            /// </summary>
            /// <param name="input">借用的可读流；不关闭或重置位置。</param>
            /// <param name="algorithm">摘要算法。</param>
            public static string ComputeHash(Stream input, HashAlgorithmName algorithm)
            {
                if (input == null) throw new ArgumentNullException(nameof(input));
                using var hash = IncrementalHash.CreateHash(algorithm);
                var buffer = new byte[BufferSize];
                int count;
                while ((count = input.Read(buffer, 0, buffer.Length)) != 0)
                    hash.AppendData(buffer, 0, count);
                return ToHex(hash.GetHashAndReset());
            }

            /// <summary>
            ///   <para>异步计算流摘要；读取当前位置到末尾，返回小写十六进制字符串。</para>
            /// </summary>
            /// <param name="input">借用的可读流；不关闭或重置位置。</param>
            /// <param name="algorithm">摘要算法。</param>
            /// <param name="cancellationToken">取消令牌。</param>
            public static async Task<string> ComputeHashAsync(Stream input, HashAlgorithmName algorithm, CancellationToken cancellationToken = default)
            {
                if (input == null) throw new ArgumentNullException(nameof(input));
                cancellationToken.ThrowIfCancellationRequested();
                using var hash = IncrementalHash.CreateHash(algorithm);
                var buffer = new byte[BufferSize];
                int count;
                while ((count = await input.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    hash.AppendData(buffer, 0, count);
                }
                cancellationToken.ThrowIfCancellationRequested();
                return ToHex(hash.GetHashAndReset());
            }

            /// <summary>
            ///   <para>转换为十六进制。</para>
            /// </summary>
            /// <param name="hash">摘要字节。</param>
            private static string ToHex(byte[] hash) => BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}