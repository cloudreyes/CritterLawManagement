using Marten;
using Marten.Events;
using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Weasel.Core;
using ApexLegal.Api.Features.Dashboard;
using ApexLegal.Api.Features.MatterManagement;
using Wolverine.Marten;

namespace ApexLegal.Api.Infrastructure;

public static class MartenConfiguration
{
    public static IHostApplicationBuilder AddMartenWithEventSourcing(this IHostApplicationBuilder builder)
    {
        builder.Services.AddMarten((StoreOptions options) =>
        {
            // The connection string will be provided by Aspire service discovery via Npgsql
            var connectionString = builder.Configuration.GetConnectionString("apexlegaldb");
            options.Connection(connectionString!);

            // Event Sourcing configuration
            options.Events.StreamIdentity = JasperFx.Events.StreamIdentity.AsGuid;

            // Serialize enums as strings for readability in PostgreSQL
            options.UseSystemTextJsonForSerialization(EnumStorage.AsString);

            // Register projections
            options.Projections.Add<MatterDetailsProjection>(ProjectionLifecycle.Inline);
            options.Projections.Add<DashboardStatisticsProjection>(ProjectionLifecycle.Async);
        })
        .IntegrateWithWolverine()
        .UseLightweightSessions()
        .AddAsyncDaemon(DaemonMode.Solo);

        return builder;
    }
}
