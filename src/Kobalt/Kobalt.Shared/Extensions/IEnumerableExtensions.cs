namespace Kobalt.Shared.Extensions;

/// <summary>
/// An extension class to hold extensions for IEnumerbale and other iterable types.
/// </summary>
public static class IEnumerableExtensions
{

    /// <summary>
    /// Calculates the Standard Deviation (stdev) of a set of values.
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    public static double StdDev(this IEnumerable<double> values)
    {
        double ret = 0;
        double[] enumerable = values as double[] ?? values.ToArray();
        int count = enumerable.Length;
        if (count > 1)
        {
            double avg = enumerable.Average();
            double sum = 0;

            foreach (var d in enumerable)
                sum += Math.Pow(d - avg, 2);

            ret = Math.Sqrt(sum / count);
        }

        return ret;
    }
}
