using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI;
using DTXMania.UI.Drawable;
using DTXMania.UI.Inspector;

namespace DTXMania;

public class SongSearchMenu : UIGroup, IUIInputHandler
{
    private UIImGuiTextInput textInput;
    private UIText statusText;
    
    public SongSearchMenu() : base("SongSearchMenu")
    {
        //add child elements
        var header = AddChild(new UIText("Search", 28));
        header.name = "Header";
        
        textInput = AddChild(new UIImGuiTextInput());
        textInput.name = "SearchInput";
        textInput.fontSize = 25;
        textInput.position.Y = 30;

        var description = AddChild(new UIText("Search for songs by name or artist.\n" +
                                              "You can search for Japanese text using Romaji input.\n" +
                                              "Search scope is the current song list view.", 18));
        description.name = "Description";
        description.position.Y = 60;
        
        statusText = AddChild(new UIText("", 18));
        statusText.name = "StatusText";
        statusText.position.Y = 250;
        
        size = new Vector2(500, 300);
        
        //add background
        var bg = AddChild(new UIImage(BaseTexture.CreateSolidColor(Color4.White)));
        bg.name = "Background";
        bg.color = new Color4(0f, 0f, 0f, 0.90f);
        bg.size = UISize.Inherited;
        bg.renderOrder = -100;
    }

    [AddChildMenu]
    public SongSearchMenu Create()
    {
        return new SongSearchMenu();
    }

    public string FocusName => "SongSearch";

    //the text field itself takes the keyboard through ImGui; holding focus is what stops the song list
    //scrolling to whatever is being typed
    public void HandleInput()
    {
    }

    /// <summary>The Search key, polled by the stage whatever holds focus, since it is what opens this.</summary>
    public void PollOpenGesture()
    {
        if (!CDTXMania.Pad.bPressed(EKeyConfigPart.SYSTEM, EKeyConfigPad.Search))
        {
            return;
        }

        isVisible = true;
        UIFocus.Push(this);
        textInput.ActivateTextInput(null, OnSearch, OnCancel);
    }

    private void OnSearch(string searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            CDTXMania.StageManager.stageSongSelectionNew.UpdateSearch(searchQuery);
            OnCancel();
            return;
        }
        
        int results = CDTXMania.StageManager.stageSongSelectionNew.UpdateSearch(searchQuery);

        if (results == 0)
        {
            statusText.SetText($"No results for search query '{searchQuery}'");
            textInput.ActivateTextInput(searchQuery, OnSearch, OnCancel);
        }
        else if (results > 0)
        {
            statusText.SetText($"Found {results} result{(results > 1 ? "s" : "")} for search query '{searchQuery}'");
            isVisible = false;
            UIFocus.Pop(this);
        }
        else
        {
            statusText.SetText($"An error occurred while searching for '{searchQuery}'");
            textInput.ActivateTextInput(searchQuery, OnSearch, OnCancel);
        }
    }
    
    private void OnCancel()
    {
        isVisible = false;
        statusText.SetText("");
        textInput.DeactivateTextInput(true);
        UIFocus.Pop(this);
    }
}