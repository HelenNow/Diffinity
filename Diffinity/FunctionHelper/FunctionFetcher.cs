using Dapper;
using Microsoft.Data.SqlClient;

namespace Diffinity.FunctionHelper;

public static class FunctionFetcher
{
    private const string GetFunctionsNamesQuery = @"
        SELECT s.name AS SchemaName, o.name AS FunctionName
        FROM sys.objects o
        JOIN sys.schemas s ON o.schema_id = s.schema_id
        WHERE o.type IN ('FN', 'IF', 'TF')  -- Scalar, Inline Table-Valued, Table-Valued
        ORDER BY s.name, o.name;
    ";

    private const string GetFunctionBodyQuery = @"
        SELECT sm.definition
        FROM sys.objects o
        JOIN sys.schemas s ON o.schema_id = s.schema_id
        JOIN sys.sql_modules sm ON o.object_id = sm.object_id
        WHERE o.name = @functionName
          AND s.name = @schemaName
          AND o.type IN ('FN', 'IF', 'TF');
    ";

    /// <summary>
    /// Retrieves the names of all functions from the source database.
    /// </summary>
    public static List<(string schema, string name)> GetFunctionNames(string sourceConnectionString)
    {
        using var sourceConnection = new SqlConnection(sourceConnectionString);
        var list = sourceConnection.Query<(string schema, string name)>(GetFunctionsNamesQuery).AsList();
        return list;
    }

    /// <summary>
    /// Returns the body of a function from both source and destination databases.
    /// </summary>
    public static (string sourceBody, string destinationBody) GetFunctionBody(
        string sourceConnectionString,
        string destinationConnectionString,
        string schema,
        string functionName)
    {
        using SqlConnection sourceConnection = new SqlConnection(sourceConnectionString);
        using SqlConnection destinationConnection = new SqlConnection(destinationConnectionString);

        string sourceBody = DbObjectHandler.ReplaceCreateWithCreateOrAlter(
            sourceConnection.QueryFirst<string>(GetFunctionBodyQuery,
                new { functionName = functionName, schemaName = schema }));

        string destinationBody = DbObjectHandler.ReplaceCreateWithCreateOrAlter(
            destinationConnection.QueryFirstOrDefault<string>(GetFunctionBodyQuery,
                new { functionName = functionName, schemaName = schema }) ?? "");

        sourceBody = DbObjectHandler.BracketObjectNameOnly(sourceBody);
        destinationBody = DbObjectHandler.BracketObjectNameOnly(destinationBody);

        return (sourceBody, destinationBody);
    }
}