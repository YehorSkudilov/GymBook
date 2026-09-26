using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace GymBook.Models;

/// <summary>
/// Checks a record and everything nested in it against the same attributes the API validates, so the app
/// can hold back a record the server would reject instead of failing the whole sync.
/// </summary>
public static class ModelValidator
{
    public static bool IsValid(object model) => IsValid(model, depth: 0);

    static bool IsValid(object model, int depth)
    {
        if (depth > 8 || !Validator.TryValidateObject(model, new ValidationContext(model), null, validateAllProperties: true))
            return false;

        foreach (var property in model.GetType().GetProperties())
        {
            if (property.GetIndexParameters().Length > 0 || property.PropertyType == typeof(string) || property.PropertyType.IsValueType)
                continue;
            var value = property.GetValue(model);
            var children = value is IEnumerable list ? list.Cast<object?>() : [value];
            foreach (var child in children)
                if (child != null && IsModel(child) && !IsValid(child, depth + 1))
                    return false;
        }
        return true;
    }

    static bool IsModel(object value) => value.GetType().Namespace == typeof(ModelValidator).Namespace;
}
