using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class StringHelper
{
    public static string DeepToString<T>(IEnumerable<T> collection)
    {
        return DeepToString(collection, false);
    }
    public static string DeepToString<T>(IEnumerable<T> collection, bool omitBrackets)
    {
        var builder = new StringBuilder(omitBrackets ? string.Empty : "{");

        foreach (T obj in collection)
        {
            builder.Append(obj == null ? string.Empty : obj.ToString());
            builder.Append(", ");
        }
        if (builder.Length > 1)
            builder.Remove(builder.Length - 2, 2);
        if (!omitBrackets)
            builder.Append("}");

        return builder.ToString();
    }
}
