using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Sharpify.Core.Data.Interceptors;

public sealed class SqlitePragmaConnectionInterceptor : DbConnectionInterceptor
{
    private static void ExecutePragmas(DbConnection connection)
    {
        if (!connection.GetType().Name.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode = WAL; PRAGMA foreign_keys = ON;";
        command.ExecuteNonQuery();
    }

    private static async Task ExecutePragmasAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (!connection.GetType().Name.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode = WAL; PRAGMA foreign_keys = ON;";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        ExecutePragmas(connection);
        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await ExecutePragmasAsync(connection, cancellationToken);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }
}
