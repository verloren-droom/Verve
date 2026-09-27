---
name: verve-config-table
description: Install Verve's optional configuration-table module and use generated typed extension methods without creating wrapper table services.
---

# Scope

ConfigTableModule reads StreamingAssets/Tables/ConfigTableManifest.json and .ctable files. Its runtime types are in the Verve namespace. The assembly remains Verve.Table.

## Install and wait

~~~csharp
using System;
using System.Threading.Tasks;
using Verve;

var manifest = new GameModuleManifest();
manifest.Add<ConfigTableModule>();
var modules = Game.CreateModules();
await modules.Modules.InstallFromManifestAsync(manifest);
var tables = modules.Modules.GetModule<IConfigTables>();
~~~

The module is optional. Keep the IConfigTables module reference; do not create a second ConfigTables wrapper.

## Generated accessors

The editor generates extension methods in the Verve namespace for each table. A table named Item typically exposes:

~~~csharp
var item = tables.GetItemTable(1001);
if (tables.TryGetItemTable(1001, out var found))
{
    var rows = tables.GetItemTableRows();
}
~~~

Generated methods use the supplied IConfigTables instance and reuse row/table caches. They do not allocate a new service object per query.

## Data layout and reload

~~~text
Assets/StreamingAssets/Tables/
  ConfigTableManifest.json
  Item.ctable
~~~

- The first column is id and must be int or string.
- Generated rows support the scalar and array types declared by the table importer.
- Call await tables.ReloadAsync() on the Unity main thread to reload all tables.
- Reload replaces table data and row caches; discard row objects obtained before the reload.

## Rules

- Await installation before querying; installation and reload propagate loading errors. Missing table names and invalid scalar values throw.
- Keep generated files under source control or regenerate them as part of the editor build step; do not hand-edit generated members.
- Use typed generated methods in gameplay code; use IConfigTables.Get(name) only for tooling or dynamic table names.
- Dispose the module container after its owning game scope ends.
