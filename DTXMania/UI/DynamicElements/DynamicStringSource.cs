namespace DTXMania.UI.DynamicElements;

public class DynamicStringSource
{
    private Func<string> getString;
    
    public DynamicStringSource(Func<string> getString) 
    {
        this.getString = getString;
    }
    
    public string GetString()
    {
        return getString();
    }
}