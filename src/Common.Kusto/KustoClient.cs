// -----------------------------------------------------------------------
// <copyright file="KustoClient.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Kusto;

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Config;
using global::Kusto.Cloud.Platform.Data;
using global::Kusto.Data;
using global::Kusto.Data.Common;
using global::Kusto.Data.Ingestion;
using global::Kusto.Ingest;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenTelemetry.Trace;
using Settings;
using Shared;
using Status = OpenTelemetry.Trace.Status;

public class KustoClient : IKustoClient
{
    private readonly KustoSettings kustoSettings;
    private readonly ILogger<KustoClient> logger;
    private readonly Tracer tracer;
    private readonly AsyncLazy<ICslAdminProvider> adminClientFunc;
    private readonly AsyncLazy<IKustoIngestClient> ingestClientFunc;
    private readonly AsyncLazy<ICslQueryProvider> queryClientFunc;

    public KustoClient(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, KustoSettings? kustoSettings = null)
    {
        logger = loggerFactory.CreateLogger<KustoClient>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        this.kustoSettings = kustoSettings ?? configuration.GetConfiguredSettings<KustoSettings>();
        var metadata = configuration.GetConfiguredSettings<ApplicationMetadata>();
        var traceProvider = serviceProvider.GetRequiredService<TracerProvider>();
        tracer = traceProvider.GetTracer(metadata.ApplicationName + $".{nameof(KustoClient)}", metadata.BuildVersion);

        queryClientFunc = new AsyncLazy<ICslQueryProvider>(() =>
        {
            var clientFactory = new KustoAuthHelper(serviceProvider, kustoSettings);
            return clientFactory.QueryQueryClient;
        });
        adminClientFunc = new AsyncLazy<ICslAdminProvider>(() =>
        {
            var clientFactory = new KustoAuthHelper(serviceProvider, kustoSettings);
            return clientFactory.AdminClient;
        });
        ingestClientFunc = new AsyncLazy<IKustoIngestClient>(() =>
        {
            var clientFactory = new KustoAuthHelper(serviceProvider, kustoSettings);
            return clientFactory.IngestClient;
        });
    }

    public async Task<IEnumerable<T>> ExecuteQuery<T>(string query, TimeSpan timeout = default, CancellationToken cancellationToken = default)
    {
        using var _ = tracer.StartActiveSpan(nameof(ExecuteQuery));
        logger.ExecuteQueryStart(query);
        var stopWatch = Stopwatch.StartNew();
        var queryClient = await queryClientFunc.Value;
        var reader = await queryClient.ExecuteQueryAsync(
            kustoSettings.DbName,
            query,
            GetClientRequestProps(timeout),
            cancellationToken);
        var records = Read<T>(reader, cancellationToken).ToList();
        stopWatch.Stop();
        logger.ExecuteQueryStop(query, records.Count, stopWatch.ElapsedMilliseconds);
        return records;
    }

    public async Task<(int Total, T? LastRecord)> ExecuteQuery<T>(
        string query,
        Func<IList<T>, CancellationToken, Task> onBatchReceived,
        CancellationToken cancellationToken = default,
        int batchSize = 100)
    {
        using var _ = tracer.StartActiveSpan(nameof(ExecuteQuery));
        var queryClient = await queryClientFunc.Value;
        var reader = await queryClient.ExecuteQueryAsync(
            kustoSettings.DbName,
            query,
            GetClientRequestProps(),
            cancellationToken);
        return await Read(reader, onBatchReceived, cancellationToken, batchSize);
    }

    public async Task<(int Total, object? LastRecord)> ExecuteQuery(
        Type entityType,
        string query,
        Func<IList<object>, CancellationToken, Task> onBatchReceived,
        CancellationToken cancellationToken = default,
        int batchSize = 100)
    {
        using var _ = tracer.StartActiveSpan(nameof(ExecuteQuery));
        var queryClient = await queryClientFunc.Value;
        var reader = await queryClient.ExecuteQueryAsync(
            kustoSettings.DbName,
            query,
            GetClientRequestProps(),
            cancellationToken);
        return await Read(entityType, reader, onBatchReceived, cancellationToken, batchSize);
    }

    public async Task<IEnumerable<T>> ExecuteFunction<T>(
        string functionName,
        CancellationToken cancellationToken,
        params (string name, string value)[] parameters)
    {
        using var _ = tracer.StartActiveSpan(nameof(ExecuteFunction));
        var queryClient = await queryClientFunc.Value;
        var reader = await queryClient.ExecuteQueryAsync(
            kustoSettings.DbName,
            functionName,
            GetClientRequestProps(),
            cancellationToken);
        return Read<T>(reader, cancellationToken);
    }

    public async Task ExecuteFunction<T>(
        string functionName,
        (string name, string value)[] parameters,
        Func<IList<T>, CancellationToken, Task> onBatchReceived,
        CancellationToken cancellationToken = default,
        int batchSize = 100)
    {
        using var _ = tracer.StartActiveSpan(nameof(ExecuteFunction));
        var queryClient = await queryClientFunc.Value;
        var reader = await queryClient.ExecuteQueryAsync(
            kustoSettings.DbName,
            functionName,
            GetClientRequestProps(),
            cancellationToken);
        await Read(reader, onBatchReceived, cancellationToken, batchSize);
    }

    public async Task<int> BulkInsert<T>(
        string tableName,
        IList<T> items,
        IngestMode ingestMode,
        string idPropName,
        CancellationToken cancellationToken)
    {
        using var _ = tracer.StartActiveSpan(nameof(BulkInsert));
        var stopwatch = Stopwatch.StartNew();
        logger.BulkInsertStart(items.Count, tableName);
        await EnsureTable<T>(tableName);
        var columnMappings = typeof(T).GetKustoColumnMappings();
        var props = new KustoIngestionProperties(kustoSettings.DbName, tableName)
        {
            DropByTags = new List<string> { DateTime.Today.ToString("MM/dd/yyyy") },
            IngestByTags = new List<string> { Guid.Empty.ToString() },
            Format = DataSourceFormat.json,
            IngestionMapping = new IngestionMapping
            {
                IngestionMappingKind = IngestionMappingKind.Json,
                IngestionMappings = columnMappings
            }
        };

        long totalSize = 0;
        int itemChanged = 0;
        var ingestClient = await ingestClientFunc.Value;
        var adminClient = await adminClientFunc.Value;

        if (ingestMode == IngestMode.InsertNew)
        {
            var upserts = await CheckExistingRecords(tableName, items.ToList(), idPropName);
            await using var memoryStream = new MemoryStream();
            await using var writer = new StreamWriter(memoryStream);
            if (upserts.inserts.Count > 0)
            {
                itemChanged = upserts.inserts.Count;
                foreach (var item in upserts.inserts)
                {
                    await writer.WriteLineAsync(JsonConvert.SerializeObject(item));
                }

                await writer.FlushAsync();
                totalSize = memoryStream.Length;
                memoryStream.Seek(0, SeekOrigin.Begin);
                await ingestClient.IngestFromStreamAsync(memoryStream, props);
            }
        }
        else
        {
            if (ingestMode == IngestMode.Refresh)
            {
                await DropTable(tableName, cancellationToken);
                await EnsureTable<T>(tableName);
            }
            else if (ingestMode == IngestMode.AppendOnly)
            {
                var enableIngestionTimePolicyCommand = $".alter table {tableName} policy ingestiontime true";
                await adminClient.ExecuteControlCommandAsync(kustoSettings.DbName, enableIngestionTimePolicyCommand);
            }

            itemChanged = items.Count;
            await using var memoryStream = new MemoryStream();
            await using var writer = new StreamWriter(memoryStream);
            foreach (var item in items)
            {
                await writer.WriteLineAsync(JsonConvert.SerializeObject(item));
            }

            await writer.FlushAsync();
            totalSize = memoryStream.Length;
            memoryStream.Seek(0, SeekOrigin.Begin);
            await ingestClient.IngestFromStreamAsync(memoryStream, props);
        }

        stopwatch.Stop();
        logger.BulkInsertStop(itemChanged, tableName, stopwatch.ElapsedMilliseconds, totalSize);
        return itemChanged;
    }

    public async Task<T?> ExecuteScalar<T>(string query, string fieldName, CancellationToken cancel)
    {
        logger.ExecuteScalarStart(query);
        using var span = tracer.StartActiveSpan(nameof(ExecuteScalar));
        var watch = Stopwatch.StartNew();
        var queryClient = await queryClientFunc.Value;
        try
        {
            var reader = await queryClient.ExecuteQueryAsync(
                kustoSettings.DbName,
                query,
                GetClientRequestProps(),
                cancel);
            T? value = default;
            if (reader.Read())
            {
                value = reader.Value<T>(fieldName);
            }

            reader.Dispose();
            logger.ExecuteScalarStop(query, watch.ElapsedMilliseconds, value?.ToString() ?? "empty");
            return value;
        }
        catch (Exception ex)
        {
            logger.ExecuteScalarError(query, watch.ElapsedMilliseconds, ex.Message);
            span.SetStatus(Status.Error);
            return default;
        }
    }

    public async Task<IDataReader> ExecuteReader(string query)
    {
        logger.ExecuteReaderStart(query);
        var watch = Stopwatch.StartNew();
        using var _ = tracer.StartActiveSpan(nameof(ExecuteReader));
        var queryClient = await queryClientFunc.Value;
        var reader = await queryClient.ExecuteQueryAsync(
            kustoSettings.DbName,
            query,
            GetClientRequestProps());
        logger.ExecuteReaderStop(query, watch.ElapsedMilliseconds);
        return reader;
    }

    public async Task DropTable(string tableName, CancellationToken cancel)
    {
        logger.DropTableStart(tableName);
        using var _ = tracer.StartActiveSpan(nameof(DropTable));
        var watch = Stopwatch.StartNew();
        var adminClient = await adminClientFunc.Value;
        var showTableCmd = CslCommandGenerator.GenerateTableDropCommand(tableName, true);
        await adminClient.ExecuteControlCommandAsync(kustoSettings.DbName, showTableCmd);
        logger.DropTableStop(tableName, watch.ElapsedMilliseconds);
    }

    public async Task RefreshStagingTable(string targetTableName, string stagingTableName, string ingestionMapName, CancellationToken cancel)
    {
        logger.RefreshStagingTableStart(stagingTableName, targetTableName, ingestionMapName);
        var watch = Stopwatch.StartNew();
        using var _ = tracer.StartActiveSpan(nameof(RefreshStagingTable));
        var adminClient = await adminClientFunc.Value;
        var tableSchemaReader = await adminClient.ExecuteControlCommandAsync(kustoSettings.DbName, $".show table {targetTableName} cslschema");
        var tableSchema = tableSchemaReader.ToStringColumn("Schema").FirstOrDefault();
        await adminClient.ExecuteControlCommandAsync(kustoSettings.DbName, $".drop table {stagingTableName} ifexists");
        await adminClient.ExecuteControlCommandAsync(kustoSettings.DbName, $".create table {stagingTableName} ({tableSchema})");

        var ingestionMapReader = await adminClient.ExecuteControlCommandAsync(kustoSettings.DbName, $".show table {targetTableName} ingestion json mappings");
        var ingestionMapping = ingestionMapReader.ToStringColumn("Mapping").FirstOrDefault();
        await adminClient.ExecuteControlCommandAsync(
            kustoSettings.DbName,
            $".create table {stagingTableName} ingestion json mapping '{ingestionMapName}' '{ingestionMapping}'");
        logger.RefreshStagingTableStop(stagingTableName, targetTableName, ingestionMapName, watch.ElapsedMilliseconds);
    }

    public async Task SwapTable(string targetTable, string stagingTable)
    {
        using var _ = tracer.StartActiveSpan(nameof(SwapTable));
        logger.SwapTableStart(targetTable, stagingTable);
        var watch = Stopwatch.StartNew();
        var adminClient = await adminClientFunc.Value;
        await adminClient.ExecuteControlCommandAsync(kustoSettings.DbName, $".rename tables {stagingTable}={targetTable},{targetTable}={stagingTable}");
        logger.SwapTableStop(targetTable, stagingTable, watch.ElapsedMilliseconds);
    }

    public async Task CopyRetentionPolicy(string fromTableName, string toTableName)
    {
        using var _ = tracer.StartActiveSpan(nameof(CopyRetentionPolicy));
        var showRetentionPolicyCommand = CslCommandGenerator.GenerateTableShowRetentionPolicyCommand(fromTableName);
        logger.CopyRetentionPolicyStart(fromTableName, toTableName);
        var watch = Stopwatch.StartNew();
        var adminClient = await adminClientFunc.Value;
        var reader = await adminClient.ExecuteControlCommandAsync(kustoSettings.DbName, showRetentionPolicyCommand);
        if (reader.Read())
        {
            var retentionPolicy = reader["Policy"] as string;
            if (!string.IsNullOrEmpty(retentionPolicy) && !retentionPolicy.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                var policy = JsonConvert.DeserializeObject<DataRetentionPolicy>(retentionPolicy);
                if (policy != null)
                {
                    var alterRetentionPolicyCommand =
                        CslCommandGenerator.GenerateTableAlterRetentionPolicyCommand(kustoSettings.DbName,
                            toTableName,
                            policy);
                    logger.AlterRetentionCommand(alterRetentionPolicyCommand);
                    await adminClient.ExecuteControlCommandAsync(kustoSettings.DbName,
                        alterRetentionPolicyCommand);
                }
            }
        }

        reader.Close();
        logger.CopyRetentionPolicyStop(fromTableName, toTableName, watch.ElapsedMilliseconds);
    }

    #region schema

    public async Task<IEnumerable<KustoTable>> ListTables()
    {
        using var _ = tracer.StartActiveSpan(nameof(ListTables));
        logger.ListTablesStart();
        var watch = Stopwatch.StartNew();
        var showTablesCmd = CslCommandGenerator.GenerateTablesShowCommand();
        var adminClient = await adminClientFunc.Value;
        var reader = await adminClient.ExecuteControlCommandAsync(kustoSettings.DbName, showTablesCmd);
        var tableNames = new List<string>();
        while (reader.Read())
        {
            tableNames.Add(reader.GetString(0));
        }

        reader.Close();

        var tables = new List<KustoTable>();
        foreach (var tableName in tableNames)
        {
            logger.ReadTableSchema(tableName);
            var table = new KustoTable { Name = tableName, Columns = new List<KustoColumn>(), RetentionPolicy = new KustoTableRetentionPolicy() { Enabled = false, } };
            var showTblCmd = $".show table {tableName} schema as json";
            reader = await adminClient.ExecuteControlCommandAsync(kustoSettings.DbName, showTblCmd);
            if (reader.Read())
            {
                var schemaJson = reader.GetString(1);
                var schema = JObject.Parse(schemaJson);
                var columns = new List<KustoColumn>();
                foreach (var column in schema.Value<JArray>("OrderedColumns")!)
                {
                    var columnName = column.Value<string>("Name");
                    var columnType = Type.GetType(column.Value<string>("Type")!);
                    var cslType = column.Value<string>("CslType");
                    columns.Add(new KustoColumn() { Name = columnName!, Type = columnType!, CslType = cslType! });
                }

                table.Columns = columns;
            }

            reader.Close();

            var showDetailCmd = $".show table {tableName} details";
            reader = await adminClient.ExecuteControlCommandAsync(kustoSettings.DbName, showDetailCmd);
            if (reader.Read())
            {
                table.Folder = reader.Value<string>("Folder")!;
                table.DocString = reader.Value<string>("DocString")!;
                var retentionPolicy = reader.Value<string>("RetentionPolicy");
                if (!string.IsNullOrEmpty(retentionPolicy))
                {
                    var jobj = JObject.Parse(retentionPolicy);
                    table.RetentionPolicy.Enabled = jobj.Value<string>("Recoverability") == "Enabled";
                    if (TimeSpan.TryParse(jobj.Value<string>("SoftDeletePeriod"), out var span))
                    {
                        table.RetentionPolicy.SoftDeletePeriod = span;
                    }
                }
            }

            reader.Close();

            tables.Add(table);
        }

        logger.ListTablesStop(tables.Count, watch.ElapsedMilliseconds);

        return tables;
    }

    public async Task<IEnumerable<KustoFunction>> ListFunctions()
    {
        using var _ = tracer.StartActiveSpan(nameof(ListFunctions));
        logger.ListFunctionsStart();
        var watch = Stopwatch.StartNew();
        var showFunctionsCmd = CslCommandGenerator.GenerateFunctionsShowCommand();
        var adminClient = await adminClientFunc.Value;
        var reader = await adminClient.ExecuteControlCommandAsync(kustoSettings.DbName, showFunctionsCmd);
        var functionNames = new List<string>();
        while (reader.Read())
        {
            functionNames.Add(reader.GetString(0));
        }

        reader.Close();

        var output = new List<KustoFunction>();
        foreach (var funcName in functionNames)
        {
            logger.ReadFunctionSchema(funcName);
            var showFunctionCmd = CslCommandGenerator.GenerateFunctionShowCommand(funcName);
            reader = await adminClient.ExecuteControlCommandAsync(kustoSettings.DbName, showFunctionCmd);
            if (reader.Read())
            {
                var function = new KustoFunction
                {
                    Name = reader.GetString(0),
                    Parameters = reader[1] == DBNull.Value
                        ? null
                        : reader.GetString(1),
                    Body = reader[2] == DBNull.Value
                        ? null
                        : reader.GetString(2),
                    Folder = reader[3] == DBNull.Value
                        ? null
                        : reader.GetString(3),
                    DocString = reader[4] == DBNull.Value
                        ? null
                        : reader.GetString(4)
                };
                output.Add(function);
            }

            reader.Close();
        }

        logger.ListFunctionsStop(output.Count, watch.ElapsedMilliseconds);

        return output;
    }

    public async Task<long> GetTableRecordCount(string tableName, CancellationToken cancel)
    {
        var kustoQuery = $"{tableName} | summarize Count=count()";
        var recordCount = await ExecuteScalar<long>(kustoQuery, "Count", cancel);
        logger.GetRecordCountStop(recordCount, tableName);
        return recordCount;
    }

    public async Task<(DateTime ingestionTime, long count)> GetLastIngestionTimeAndCount(string tableName, CancellationToken cancel)
    {
        var kustoQuery = $"{tableName} | extend IngestionTime=ingestion_time() | summarize LastWriteTime=max(IngestionTime)";
        var lastIngestionTime = await ExecuteScalar<DateTime>(kustoQuery, "LastWriteTime", cancel);
        var queryTemplate = @"
let lastWrite = {0} | extend {1}=ingestion_time() | summarize LastWriteTime=max({1});
{0} | extend {1}=ingestion_time() | join lastWrite on $left.{1}==$right.LastWriteTime | summarize Count=count()";
        kustoQuery = string.Format(queryTemplate, tableName, "IngestionTime");
        var count = await ExecuteScalar<long>(kustoQuery, "Count", cancel);
        logger.GetLastIngestionTimeAndCount(lastIngestionTime, count);
        return (lastIngestionTime, count);
    }

    #endregion

    public async Task EnsureTable<T>(string tableName)
    {
        using var span = tracer.StartActiveSpan(nameof(EnsureTable));
        var tableExists = false;
        var adminClient = await adminClientFunc.Value;

        try
        {
            var showTableCmd = CslCommandGenerator.GenerateShowTableAdminsCommand(tableName);
            var tableCheck = await adminClient.ExecuteControlCommandAsync(kustoSettings.DbName, showTableCmd);
            if (tableCheck.RecordsAffected >= 0 && tableCheck.FieldCount > 0)
            {
                tableExists = true;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex.Message);
            logger.TableNotExists(tableName);
            span.SetStatus(Status.Error);
        }

        if (!tableExists)
        {
            var cols = typeof(T).GetColumns();
            var createTableCmd = CslCommandGenerator.GenerateTableCreateCommand(
                tableName, cols.Select(c => new Tuple<string, Type>(c.columnName, c.columnType)).ToList());
            var columnMappings = typeof(T).GetKustoColumnMappings();
            logger.CreateTable(tableName, columnMappings.Count);
            await adminClient.ExecuteControlCommandAsync(kustoSettings.DbName, createTableCmd);
        }
    }

    private async Task<(List<T> updates, List<T> inserts)> CheckExistingRecords<T>(string tableName, List<T> items, string idPropName)
    {
        var updates = new List<T>();
        var inserts = new List<T>();

        var idProp = typeof(T).GetProperties()
            .FirstOrDefault(p =>
            {
                var jsonProp = p.GetCustomAttribute<JsonPropertyAttribute>();
                var propName = jsonProp?.PropertyName ?? p.Name;
                return propName.Equals(idPropName, StringComparison.OrdinalIgnoreCase);
            });
        if (idProp == null)
        {
            return (updates, items);
        }

        var existingIds = new HashSet<string>();

        string? lastId = null;
        const int ThrottleSize = 500000;
        var queryClient = await queryClientFunc.Value;
        while (true)
        {
            var batchRead = 0;
            var idQuery = $"{tableName} \n| order by {idPropName} asc \n| project {idPropName} \n| take {ThrottleSize}";
            if (lastId != null)
            {
                idQuery = $"{tableName} \n| where strcmp({idPropName},'{lastId}')>0 \n| order by {idPropName} asc \n| project {idPropName} \n| take {ThrottleSize}";
            }

            logger.IdQuery(idQuery);
            var reader = await queryClient.ExecuteQueryAsync(
                kustoSettings.DbName,
                idQuery,
                GetClientRequestProps());
            while (reader.Read())
            {
                lastId = reader.GetString(0);
                existingIds.Add(lastId);
                batchRead++;
            }

            reader.Close();

            if (batchRead < ThrottleSize)
            {
                break;
            }
        }

        foreach (var item in items)
        {
            var id = idProp.GetValue(item)?.ToString();
            var found = !string.IsNullOrEmpty(id) && existingIds.Contains(id);
            if (found)
                updates.Add(item);
            else
                inserts.Add(item);
        }

        return (updates, inserts);
    }

    private ClientRequestProperties GetClientRequestProps(TimeSpan timeout = default)
    {
        var requestProps = new ClientRequestProperties { ClientRequestId = Guid.NewGuid().ToString() };
        if (timeout != default)
        {
            requestProps.SetOption(ClientRequestProperties.OptionServerTimeout, timeout);
        }

        return requestProps;
    }

    #region obj mapping

    private IEnumerable<T> Read<T>(IDataReader reader, CancellationToken cancellationToken)
    {
        var watch = Stopwatch.StartNew();
        var propMappings = BuildFieldMapping<T>(reader);
        var output = new List<T>();
        var total = 0;

        while (reader.Read() && !cancellationToken.IsCancellationRequested)
        {
            var instance = Create<T>(reader, propMappings!);
            output.Add(instance);
            total++;
            if (total % 100 == 0)
            {
                logger.ReadingRecords(total);
            }
        }

        reader.Dispose();

        logger.ReadReaderStop(output.Count, watch.ElapsedMilliseconds);

        return output;
    }

    private async Task<(int Total, T? LastRecord)> Read<T>(
        IDataReader reader,
        Func<IList<T>, CancellationToken, Task> onBatchReceived,
        CancellationToken cancellationToken,
        int batchSize)
    {
        var propMappings = BuildFieldMapping<T>(reader);

        var output = new List<T>();
        var batchCount = 0;
        var total = 0;
        var watch = Stopwatch.StartNew();
        T? lastRecord = default;
        while (reader.Read() && !cancellationToken.IsCancellationRequested)
        {
            var instance = Create<T>(reader, propMappings!);
            output.Add(instance);
            if (output.Count >= batchSize)
            {
                batchCount++;
                total += output.Count;
                await onBatchReceived(output, cancellationToken);
                logger.OnBatchReceived(batchCount, total);
                lastRecord = output[^1];
                output = new List<T>();
            }
        }

        reader.Dispose();

        if (output.Count > 0 && !cancellationToken.IsCancellationRequested)
        {
            batchCount++;
            total += output.Count;
            logger.OnBatchReceived(batchCount, total);
            await onBatchReceived(output, cancellationToken);
            lastRecord = output[^1];
            output.Clear();
        }

        if (cancellationToken.IsCancellationRequested)
        {
            logger.QueryCancelled();
        }

        logger.ReadRecordsStop(output.Count, watch.ElapsedMilliseconds);

        return (total, lastRecord);
    }

    private async Task<(int Total, object? LastRecord)> Read(
        Type entityType,
        IDataReader reader,
        Func<IList<object>, CancellationToken, Task> onBatchReceived,
        CancellationToken cancellationToken,
        int batchSize)
    {
        var propMappings = BuildFieldMapping(entityType, reader);

        var output = new List<object>();
        int batchCount = 0;
        int total = 0;
        var watch = Stopwatch.StartNew();
        object? lastRecord = null;
        while (reader.Read() && !cancellationToken.IsCancellationRequested)
        {
            var instance = Create(entityType, reader, propMappings);
            output.Add(instance);
            if (output.Count >= batchSize)
            {
                batchCount++;
                total += output.Count;
                await onBatchReceived(output, cancellationToken);
                logger.OnBatchReceived(batchCount, total);
                lastRecord = output[^1];
                output = new List<object>();
            }
        }

        reader.Dispose();

        if (output.Count > 0 && !cancellationToken.IsCancellationRequested)
        {
            batchCount++;
            total += output.Count;
            logger.OnBatchReceived(batchCount, total);
            await onBatchReceived(output, cancellationToken);
            lastRecord = output[^1];
            output.Clear();
        }

        if (cancellationToken.IsCancellationRequested)
        {
            logger.QueryCancelled();
        }

        logger.ReadRecordsStop(output.Count, watch.ElapsedMilliseconds);

        return (total, lastRecord);
    }

    private Dictionary<int, (PropertyInfo prop, Func<object, object>? converter)> BuildFieldMapping<T>(
        IDataReader reader)
    {
        return BuildFieldMapping(typeof(T), reader);
    }

    private Dictionary<int, (PropertyInfo prop, Func<object, object>? converter)> BuildFieldMapping(
        Type type,
        IDataReader reader)
    {
        var constructor = type.GetConstructors().SingleOrDefault(c => !c.GetParameters().Any());
        if (constructor == null)
        {
            throw new InvalidOperationException($"type {type.Name} doesn't have parameterless constructor");
        }

        // handle json property mappings
        var props = type.GetProperties().Where(p => p.CanWrite).ToList();
        var propNameMappings = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in props)
        {
            var jsonProp = prop.GetCustomAttribute<JsonPropertyAttribute>();
            if (jsonProp != null && !string.IsNullOrWhiteSpace(jsonProp.PropertyName))
            {
                propNameMappings.Add(jsonProp.PropertyName, prop);
            }
            else
            {
                propNameMappings.Add(prop.Name, prop);
            }
        }

        var propMappings = new Dictionary<int, (PropertyInfo prop, Func<object, object>? converter)>();
        var fieldTable = reader.GetSchemaTable();
        if (fieldTable == null)
            throw new InvalidOperationException("Query doesn't return schema info");

        for (var i = 0; i < fieldTable.Rows.Count; i++)
        {
            var fieldName = (string)fieldTable.Rows[i]["ColumnName"];
            var property = type.GetProperty(fieldName);
            if (property == null)
                propNameMappings.TryGetValue(fieldName, out property);
            var dataType = (Type)fieldTable.Rows[i]["DataType"];
            if (property != null)
            {
                Func<object, object>? converter = null;
                if (!property.PropertyType.IsAssignableFrom(dataType))
                {
                    converter = CreateConverter(dataType, property.PropertyType)!;
                }

                propMappings.Add(i, (property, converter));
            }
            else
            {
                logger.LogWarning($"Missing mapping for field: {fieldName}");
            }
        }

        return propMappings;
    }

    private T Create<T>(
        IDataReader reader,
        Dictionary<int, (PropertyInfo prop, Func<object, object> converter)> propMappings)
    {
        return (T)Create(typeof(T), reader, propMappings!);
    }

    private object Create(
        Type type,
        IDataReader reader,
        Dictionary<int, (PropertyInfo prop, Func<object, object>? converter)> propMappings)
    {
        var instance = Activator.CreateInstance(type);
        foreach (var idx in propMappings.Keys)
        {
            var value = reader.GetValue(idx);
            if (value == DBNull.Value)
            {
                continue;
            }

            var prop = propMappings[idx].prop;
            if (prop.PropertyType != value.GetType())
            {
                var converter = propMappings[idx].converter;
                if (converter != null)
                {
                    value = converter(value);
                    if (value != DBNull.Value)
                    {
                        prop.SetValue(instance, value);
                    }
                }
                else
                {
                    if (value is JArray array && prop.PropertyType == typeof(string[]))
                    {
                        value = array.Select(a => a.Value<string>()).ToArray();
                        prop.SetValue(instance, value);
                    }
                    else
                    {
                        try
                        {
                            var underlyingType = Nullable.GetUnderlyingType(prop.PropertyType);
                            value = Convert.ChangeType(
                                value.ToString(),
                                underlyingType ?? prop.PropertyType);
                            prop.SetValue(instance, value);
                        }
                        catch
                        {
                            logger.LogWarning($"Failed to convert type for column: {prop.Name}, value: {value}");
                        }
                    }
                }
            }
            else
            {
                prop.SetValue(instance, value);
            }
        }

        return instance!;
    }

    private Func<object, object?>? CreateConverter(Type srcType, Type tgtType)
    {
        if (tgtType.IsEnum && srcType == typeof(string))
        {
            object? Converter(object s)
            {
                if (Enum.TryParse(tgtType, (string)s, true, out var value))
                {
                    return value;
                }

                return null;
            }

            return Converter;
        }

        if (tgtType == typeof(bool) && (srcType == typeof(sbyte) || srcType == typeof(int) || srcType == typeof(long)))
        {
            object Converter(object s) => Convert.ChangeType(s, tgtType);

            return Converter;
        }

        if (tgtType == typeof(bool?) &&
            (srcType == typeof(sbyte) ||
             srcType == typeof(int) ||
             srcType == typeof(long)))
        {
            object? Converter(object? s) => s != null
                ? Convert.ChangeType(s, typeof(bool))
                : default(bool?);

            return Converter;
        }

        if (tgtType == typeof(string[]) && srcType == typeof(string))
        {
            object Converter(object s)
            {
                var stringValue = s.ToString()?.Trim().Trim('[', ']');
                var items = stringValue?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(a => a.Trim().Trim(new[] { '"' }).Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToArray();
                return items!;
            }

            return Converter;
        }

        if (tgtType == typeof(string) && srcType == typeof(string[]))
        {
            object Converter(object s) => s is string[] stringArray && stringArray.Length > 0
                    ? string.Join(",", stringArray)
                    : "";

            return Converter;
        }

        return null;
    }

    #endregion

    private void ReleaseUnmanagedResources()
    {
        try
        {
            if (this.adminClientFunc.IsValueCreated)
            {
                this.adminClientFunc.Value.Dispose();
            }
            if (this.queryClientFunc.IsValueCreated)
            {
                this.queryClientFunc.Value.Dispose();
            }
            if (this.ingestClientFunc.IsValueCreated)
            {
                this.ingestClientFunc.Value.Dispose();
            }
        }
        catch (ObjectDisposedException)
        {
            // ignore
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error releasing unmanaged resources");
        }
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~KustoClient()
    {
        ReleaseUnmanagedResources();
    }
}