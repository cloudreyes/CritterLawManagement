using Wolverine;
using Wolverine.Marten;
using Microsoft.Extensions.Hosting;

namespace ApexLegal.Api.Infrastructure;

public static class WolverineConfiguration
{
    public static IHostApplicationBuilder AddWolverineMessaging(this IHostApplicationBuilder builder)
    {
        builder.UseWolverine(options =>
        {
            // Integrate with Marten for transactional inbox/outbox
            // This is usually done via .IntegrateWithWolverine() on Marten registration,
            // which I already added in MartenConfiguration.cs

            // Local queue for task processing
            options.LocalQueue("tasks")
                .MaximumParallelMessages(5);

            // Default retry policy
            options.Policies.AutoApplyTransactions();
        });

        return builder;
    }
}
