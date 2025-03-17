namespace DTXMania;

[AttributeUsage(AttributeTargets.Assembly)]
public class BuildDateTimeAttribute : Attribute
{
    public DateTime Built { get; }
    public BuildDateTimeAttribute(string date)
    {
        this.Built = DateTime.Parse(date);
    }
}