using System;
using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using BTCPayServer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace BTCPayServer.Plugins.Lqwd.Services;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LqwdPluginDbContext>
{
    public LqwdPluginDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<LqwdPluginDbContext>();

        // FIXME: Somehow the DateTimeOffset column types get messed up when not using Postgres
        // https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers?tabs=dotnet-core-cli
        builder.UseNpgsql("User ID=user;Password=123456;Host=127.0.0.1;Port=5433;Database=designtimebtcpay");

        return new LqwdPluginDbContext(builder.Options, true);
    }
}

public class LqwdPluginDbContextFactory : BaseDbContextFactory<LqwdPluginDbContext>
{
    public LqwdPluginDbContextFactory(IOptions<DatabaseOptions> options) : base(options, "BTCPayServer.Plugins.Lqwd")
    {
    }

    public override LqwdPluginDbContext CreateContext(Action<NpgsqlDbContextOptionsBuilder> npgsqlOptionsAction = null)
    {
        var builder = new DbContextOptionsBuilder<LqwdPluginDbContext>();
        ConfigureBuilder(builder, npgsqlOptionsAction);
        return new LqwdPluginDbContext(builder.Options);
    }
}
