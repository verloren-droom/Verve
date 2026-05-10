# 多语言（I18N）

## 概述

运行时的多语言查询工具。

## 快速开始

### 1. 实现查找接口

```csharp
using Verve;
using System.Collections.Generic;

public sealed class MyI18NLookup : II18NLookup
{
    private readonly Dictionary<string, Dictionary<string, string>> tables =
        new(StringComparer.OrdinalIgnoreCase);

    public bool TryGet(string language, string key, out string value)
    {
        value = null;
        if (string.IsNullOrEmpty(language) || string.IsNullOrEmpty(key)) return false;
        if (!tables.TryGetValue(language, out var table)) return false;
        return table.TryGetValue(key, out value);
    }

    public IReadOnlyDictionary<string, string> GetTable(string language)
    {
        if (string.IsNullOrEmpty(language)) return null;
        tables.TryGetValue(language, out var table);
        return table;
    }
}
```

### 2. 绑定查找并设置语言

```csharp
using Verve;
using UnityEngine;

public class Bootstrap : MonoBehaviour
{
    private void Awake()
    {
        Game.I18NLookup = new MyI18NLookup();
        Game.SystemLanguageToLanguage = lang => lang.ToString();
        Game.PickLanguage = () => null;
        Game.LanguageAliasMap = v => v;
        Game.Language = "zh-CN";
    }
}
```

## 核心 API

- `Game.Language`：读写当前语言
- `Game.I18NLookup`：设置热插拔查找实现
- `Game.Tr(string)` / `Game.TryTr(string, out string)`：读取多语言文本
- `Game.AddI18NTable(...)` / `AddOrUpdateI18N(...)` / `ClearI18NTables()`：本地表操作
- `Game.SystemLanguageToLanguage` / `Game.CultureToLanguage`：系统语言映射
- `Game.PickLanguage` / `Game.LanguageAliasMap`：自定义选择与别名映射
