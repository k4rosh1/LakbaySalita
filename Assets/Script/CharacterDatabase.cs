using UnityEngine;


[CreateAssetMenu]
public class CharacterDatabase : ScriptableObject
{ 
  public Character[] character;

    public int CharacterCount
    {
        get
        {
            return character.Length;
        }
    }

    public Character GetCharacter(int index)
    {
        Debug.Log(index);
        return character[index]; 
    }
}
