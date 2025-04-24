using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BTCPayServer.Plugins.Lqwd.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BTCPayServer.Plugins.Lqwd;

public class LqwdPluginDbContext : DbContext
{
    private readonly bool _designTime;

    public LqwdPluginDbContext(DbContextOptions<LqwdPluginDbContext> options, bool designTime = false)
        : base(options)
    {
        _designTime = designTime;
    }

    public DbSet<PluginData> PluginRecords { get; set; }
    public DbSet<OrderData> Orders { get; set; }

    public DbSet<PluginSettings> Settings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("BTCPayServer.Plugins.Lqwd");
    }
}
