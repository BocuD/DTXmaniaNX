namespace DTXMania.UI.Inspector;

public class AddChildMenuAttribute : Attribute
{
    //make adding this attribute to a class constructor count as using it
    //so the ide doesn't complain about it not being used
    
    public AddChildMenuAttribute()
    {
        //this is a marker attribute
    }
}