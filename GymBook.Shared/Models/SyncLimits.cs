using System.ComponentModel.DataAnnotations;
using System.Collections;

namespace GymBook.Models;

/// <summary>Upper bounds on what a client may upload, so one request can't exhaust server memory or storage.</summary>
public static class SyncLimits
{
    public const int IdLength = 64;
    public const int NameLength = 200;
    public const int TextLength = 4000;

    /// <summary>Max records of each kind in one sync request or response page.</summary>
    public const int BatchSize = 500;
}

/// <summary>Caps the number of items in a collection. Unlike MaxLength, EF ignores it, so it doesn't turn into a column size.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class MaxItemsAttribute(int max) : ValidationAttribute($"The field {{0}} can have at most {max} items.")
{
    public int Max { get; } = max;

    public override bool IsValid(object? value) => value is not ICollection c || c.Count <= Max;
}
