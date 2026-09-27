namespace Verve.Tests.Table
{
    using Verve;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [Category("Table")]
    internal class ConfigTableTests
    {
        [Test]
        public async Task Uninstall_ReportsReloadFailureCompletedByCancellationAndClearsOwnership()
        {
            using var module = new ConfigTableModule();
            using var cancellation = new CancellationTokenSource();
            var completion = new TaskCompletionSource<bool>();
            var cancellationFailure = new InvalidOperationException("cancel failure");
            var loadFailure = new InvalidOperationException("reload failure");
            using var registration = cancellation.Token.Register(() =>
            {
                completion.SetException(loadFailure);
                throw cancellationFailure;
            });
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            Game.ReflectionUtility.SetFieldValue(module, "m_Lifetime", cancellation);
            Game.ReflectionUtility.SetFieldValue(module, "m_ReloadTask", completion.Task);
            var operation = (ValueTask)typeof(ConfigTableModule).GetMethod("OnUninstall", flags)
                .Invoke(module, new object[] { null, CancellationToken.None });
            Exception failure = null;
            try { await operation; }
            catch (Exception error) { failure = error; }

            Assert.That(failure, Is.TypeOf<AggregateException>());
            CollectionAssert.AreEquivalent(new[] { cancellationFailure, loadFailure },
                ((AggregateException)failure).Flatten().InnerExceptions);
            Assert.That(Game.ReflectionUtility.GetFieldValue<CancellationTokenSource>(module, "m_Lifetime"), Is.Null);
        }

#if UNITY_EDITOR
        [Test]
        public async Task ModuleInstallAndReload_PropagateDataFailures()
        {
            var directory = Path.Combine(UnityEngine.Application.streamingAssetsPath, ConfigTableModule.TablesFolder);
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, ConfigTableModule.ManifestFileName);
            var original = File.Exists(path) ? File.ReadAllBytes(path) : null;
            var modules = new GameModules();
            try
            {
                File.WriteAllText(path, "{\"tables\":[],\"sources\":[]}");
                await modules.InstallAsync<ConfigTableModule>();
                var tables = modules.GetModule<IConfigTables>();
                Assert.Throws<KeyNotFoundException>(() => tables.Get("Missing"));
                File.WriteAllText(path, "{}");
                try { await tables.ReloadAsync(); Assert.Fail("Expected invalid manifest failure."); }
                catch (InvalidOperationException) { }
                Assert.Throws<InvalidOperationException>(() => tables.Get("Missing"));
                await modules.DisposeAsync();
                modules = new GameModules();
                try { await modules.InstallAsync<ConfigTableModule>(); Assert.Fail("Expected failed installation."); }
                catch (InvalidOperationException) { }
                Assert.That(modules.TryGetModule<IConfigTables>(out _), Is.False);
            }
            finally
            {
                await modules.DisposeAsync();
                if (original == null) File.Delete(path);
                else File.WriteAllBytes(path, original);
            }
        }
#endif

        [Test]
        public void EmptyScalarAndMissingField_DoNotBecomeDefaults()
        {
            var asset = CreateAsset();
            asset.rows[0].values[1] = "";
            Assert.Throws<InvalidDataException>(() => ConfigTableSchema.ValidateAsset(asset));
            var row = new ConfigTable(CreateAsset()).Get(42);
            Assert.Throws<KeyNotFoundException>(() => row.GetInt("missing"));
            Assert.That(ConfigTableSchema.IsValidValue("string", ""), Is.True);
            Assert.That(ConfigTableSchema.IsValidValue("int[]", ""), Is.True);
        }

        [Test]
        public void GeneratedTableAccess_CachesRowsAndInvalidatesAfterReload()
        {
            var service = new TestTableService(new ConfigTable(CreateAsset()));
            var view = new TestTableCache(service, "Units");

            var first = view.Get(42);
            Assert.That(first, Is.SameAs(view.Rows[0]));
            Assert.That(view.Get(42), Is.SameAs(first));

            var replacement = CreateAsset();
            replacement.rows[0].values[1] = "8";
            service.SetTable(new ConfigTable(replacement));

            var reloaded = view.Get(42);
            Assert.That(reloaded, Is.Not.SameAs(first));
            Assert.That(reloaded.Level, Is.EqualTo(8));
            Assert.That(view.Rows[0], Is.SameAs(reloaded));
        }

        [Test]
        public void ConfigTableGeneratedCache_ReusesRowsAndLists()
        {
            var table = new ConfigTable(CreateAsset());
            var service = new TestTableService(table);
            var rawRow = table.Get(42);

            var first = table.GetOrCreateRow(service, rawRow, CreateTestRow);
            Assert.That(table.GetOrCreateRow(service, rawRow, CreateTestRow), Is.SameAs(first));

            var rows = table.GetOrCreateRows(service, CreateTestRow);
            Assert.That(table.GetOrCreateRows(service, CreateTestRow), Is.SameAs(rows));
            Assert.That(rows[0], Is.SameAs(first));
        }

        [Test]
        public void AssetJsonRoundTrip_PreservesSchemaAndTypedRowAccess()
        {
            var asset = CreateAsset();
            var json = UnityEngine.JsonUtility.ToJson(asset);
            var decoded = UnityEngine.JsonUtility.FromJson<ConfigTableAsset>(json);

            ConfigTableSchema.ValidateAsset(decoded);
            var table = new ConfigTable(decoded);

            Assert.That(table.Name, Is.EqualTo("Units"));
            Assert.That(table.TryGet(42, out var row), Is.True);
            Assert.That(table.Get(42), Is.SameAs(row));
            Assert.That(table.Get("42"), Is.SameAs(row));
            Assert.That(table.TryGet("+42", out _), Is.False);
            Assert.That(row.GetInt("level"), Is.EqualTo(5));
            Assert.That(row.GetBool("enabled"), Is.True);
            CollectionAssert.AreEqual(new[] { "red", "blue" }, row.GetStringArray("tags"));
            CollectionAssert.AreEqual(new[] { 2147483648L, 9223372036854775807L }, row.GetLongArray("largeValues"));
            Assert.That(ConfigTableSchema.GetGetterName("long[]"), Is.EqualTo("GetLongArray"));
            var pairs = row.GetTupleArray<int, string>("pairs");
            Assert.That(pairs.Length, Is.EqualTo(2));
            Assert.That(pairs[0].Item1, Is.EqualTo(1));
            Assert.That(pairs[0].Item2, Is.EqualTo("north"));
            Assert.That(table.TryGet("42", out _), Is.True);
        }

        [Test]
        public void StringIdLookupUsesStringIndex()
        {
            var asset = new ConfigTableAsset
            {
                tableName = "Names",
                fields = new[] { "id", "value" },
                types = new[] { "string", "string" },
                comments = new[] { "", "" },
                references = new[] { "", "" },
                rows = new[]
                {
                    new ConfigTableRecord { id = "hero", values = new[] { "hero", "Hero" } }
                }
            };

            var table = new ConfigTable(asset);
            Assert.That(table.TryGet("hero", out var row), Is.True);
            Assert.That(table.Get("hero"), Is.SameAs(row));
            Assert.That(table.Get(0), Is.Null);
        }

        [Test]
        public void Validation_RejectsInvalidRecordValuesAndUnsafeFileNames()
        {
            var asset = CreateAsset();
            asset.rows[0].values[1] = "not-an-int";

            Assert.Throws<InvalidDataException>(() => ConfigTableSchema.ValidateAsset(asset));
            Assert.That(ConfigTableSchema.IsValidTableFileName("Units"), Is.True);
            Assert.That(ConfigTableSchema.IsValidTableFileName("../Units"), Is.False);
            Assert.That(ConfigTableSchema.IsValidTableFileName("Units/Debug"), Is.False);
        }

        [Test]
        public void MergePartitions_ReconstructsSourceRowsAndRejectsMissingColumns()
        {
            var source = CreateSchema("Stats", new[] { "id", "score", "name" }, new[] { "int", "float", "string" });
            var scores = CreatePartition(
                "Stats.Score",
                new[] { "id", "score" },
                new[] { "int", "float" },
                new[] { "1", "2.5" });
            var names = CreatePartition(
                "Stats.Name",
                new[] { "id", "name" },
                new[] { "int", "string" },
                new[] { "1", "Hero" });

            var merged = ConfigTableSchema.MergePartitions(source, new[]
            {
                new ConfigTableMergePartition("Stats.Score", scores, new[] { 0, 1 }),
                new ConfigTableMergePartition("Stats.Name", names, new[] { 0, 2 })
            });

            Assert.That(merged.rows.Length, Is.EqualTo(1));
            CollectionAssert.AreEqual(new[] { "1", "2.5", "Hero" }, merged.rows[0].values);
            Assert.Throws<InvalidDataException>(() => ConfigTableSchema.MergePartitions(source, new[]
            {
                new ConfigTableMergePartition("Stats.Score", scores, new[] { 0, 1 })
            }));
        }

        private static ConfigTableAsset CreateAsset()
        {
            return new ConfigTableAsset
            {
                tableName = "Units",
                fields = new[] { "id", "level", "enabled", "tags", "largeValues", "pairs" },
                types = new[] { "int", "int", "bool", "string[]", "long[]", "(int#string)[]" },
                comments = new[] { "", "", "", "", "", "" },
                references = new[] { "", "", "", "", "", "" },
                rows = new[]
                {
                    new ConfigTableRecord
                    {
                        id = "42",
                        values = new[] { "42", "5", "1", "red|blue", "2147483648|9223372036854775807", "1#north|2#south" }
                    }
                }
            };
        }

        private sealed class TestTableCache
        {
            private readonly IConfigTables m_Service;
            private readonly string m_TableName;
            private ConfigTable m_Source;
            private IReadOnlyList<TestRow> m_Rows;
            private Dictionary<ConfigTableRow, TestRow> m_RowCache;

            public TestTableCache(IConfigTables service, string tableName)
            {
                m_Service = service;
                m_TableName = tableName;
            }

            public TestRow Get(int id)
            {
                var table = GetSource();
                return table == null ? null : Wrap(table.Get(id));
            }

            public IReadOnlyList<TestRow> Rows
            {
                get
                {
                    var table = GetSource();
                    if (table == null)
                    {
                        return Array.Empty<TestRow>();
                    }
                    if (m_Rows != null)
                    {
                        return m_Rows;
                    }
                    var rawRows = table.Rows;
                    var rows = new TestRow[rawRows.Count];
                    for (int i = 0; i < rawRows.Count; i++)
                    {
                        rows[i] = Wrap(rawRows[i]);
                    }
                    m_Rows = Array.AsReadOnly(rows);
                    return m_Rows;
                }
            }

            private ConfigTable GetSource()
            {
                var table = m_Service.Get(m_TableName);
                if (!object.ReferenceEquals(m_Source, table))
                {
                    m_Source = table;
                    m_Rows = null;
                    m_RowCache = table == null ? null : new Dictionary<ConfigTableRow, TestRow>(table.Count);
                }
                return table;
            }

            private TestRow Wrap(ConfigTableRow rawRow)
            {
                if (rawRow == null)
                {
                    return null;
                }
                if (m_RowCache == null)
                {
                    m_RowCache = new Dictionary<ConfigTableRow, TestRow>();
                }
                if (m_RowCache.TryGetValue(rawRow, out var row))
                {
                    return row;
                }
                row = new TestRow(rawRow);
                m_RowCache.Add(rawRow, row);
                return row;
            }
        }

        private sealed class TestRow
        {
            private readonly ConfigTableRow m_Row;

            public TestRow(ConfigTableRow row)
            {
                m_Row = row;
            }

            public int Level => m_Row.GetInt(1);
        }

        private static TestRow CreateTestRow(IConfigTables service, ConfigTableRow row)
        {
            return new TestRow(row);
        }

        private sealed class TestTableService : IConfigTables
        {
            private ConfigTable m_Table;

            public TestTableService(ConfigTable table)
            {
                m_Table = table;
            }


            public ConfigTable Get(string tableName)
            {
                return string.Equals(tableName, m_Table.Name, StringComparison.OrdinalIgnoreCase) ? m_Table : null;
            }

            public Task ReloadAsync(System.Threading.CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public void SetTable(ConfigTable table)
            {
                m_Table = table;
            }
        }

        private static ConfigTableAsset CreateSchema(string tableName, string[] fields, string[] types)
        {
            return new ConfigTableAsset
            {
                tableName = tableName,
                fields = fields,
                types = types,
                comments = Array.ConvertAll(fields, _ => string.Empty),
                references = Array.ConvertAll(fields, _ => string.Empty)
            };
        }

        private static ConfigTableAsset CreatePartition(
            string tableName,
            string[] fields,
            string[] types,
            string[] values)
        {
            return new ConfigTableAsset
            {
                tableName = tableName,
                fields = fields,
                types = types,
                comments = Array.ConvertAll(fields, _ => string.Empty),
                references = Array.ConvertAll(fields, _ => string.Empty),
                rows = new[]
                {
                    new ConfigTableRecord { id = values[0], values = values }
                }
            };
        }
    }
}
