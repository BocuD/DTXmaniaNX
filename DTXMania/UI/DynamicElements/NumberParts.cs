namespace DTXMania.UI.DynamicElements;

public static class NumberParts
{
    public static string Whole(double value) => Math.Truncate(value).ToString("0");

    public static string Fraction(double value, int digits = 2)
    {
        double scale = Math.Pow(10.0, digits);

        double parts = (value - Math.Truncate(value)) * scale + 1e-6;

        return "." + ((int)parts).ToString(new string('0', digits));
    }
}
