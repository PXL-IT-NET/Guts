using System;
using System.Reflection;
using Quartz;

namespace Guts.Api.ScheduledJobs;

public interface IRecurringJob : IJob
{
    static abstract string JobName { get; }
    static abstract string Description { get; }
    static abstract string DefaultCronExpression { get; }
    static abstract MisfirePolicy MisfirePolicy { get; }
    static abstract int MaxRetries { get; }
    static abstract TimeSpan RetryDelay { get; }
}

public sealed record RecurringJobDescriptor(
    Type JobType,
    string JobName,
    string Description,
    string DefaultCronExpression,
    MisfirePolicy MisfirePolicy,
    int MaxRetries,
    TimeSpan RetryDelay)
{
    public static RecurringJobDescriptor Create<TJob>() where TJob : IRecurringJob
    {
        return new RecurringJobDescriptor(
            typeof(TJob),
            TJob.JobName,
            TJob.Description,
            TJob.DefaultCronExpression,
            TJob.MisfirePolicy,
            TJob.MaxRetries,
            TJob.RetryDelay);
    }

    public static RecurringJobDescriptor FromType(Type jobType)
    {
        ArgumentNullException.ThrowIfNull(jobType);

        if (!typeof(IRecurringJob).IsAssignableFrom(jobType) || jobType.IsInterface || jobType.IsAbstract)
        {
            throw new ArgumentException($"Type '{jobType.FullName}' must be a concrete implementation of {nameof(IRecurringJob)}.", nameof(jobType));
        }

        MethodInfo method = typeof(RecurringJobDescriptor)
            .GetMethod(nameof(Create), BindingFlags.Public | BindingFlags.Static)
            .MakeGenericMethod(jobType);

        return (RecurringJobDescriptor)method.Invoke(null, null);
    }
}
