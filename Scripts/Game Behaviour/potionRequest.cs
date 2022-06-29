[System.Serializable]

public class potionRequest
{
    public string potionName; //What name should be shown at the top of the screen?
    public string[] castList; //What shapes need to be cast to make it?

    //Constructor for class
    public potionRequest(string name, string[] casts)
    {
        potionName = name;
        castList = casts;
    }
}
