namespace Verve
{
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    ///   <para>CSV 读写工具。</para>
    /// </summary>
    [GameTool("CSV 读写工具", typeof(CsvTool))]
    public interface ICsv : IGameTool
    {
        /// <summary>
        ///   <para>解析 CSV 文本。</para>
        /// </summary>
        /// <param name="text">CSV 文本。</param>
        /// <returns>解析后的行和字段；返回类型不暴露具体集合实现。</returns>
        IReadOnlyList<string[]> Parse(string text);

        /// <summary>
        ///   <para>向构建器追加一行 CSV。</para>
        /// </summary>
        /// <param name="builder">目标构建器。</param>
        /// <param name="values">字段值。</param>
        void AppendRow(StringBuilder builder, IReadOnlyList<string> values);
    }
}