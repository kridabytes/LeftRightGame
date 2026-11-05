using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Game/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Character Information")]
    public  List<CharaacterDetails> charaacterDetails;

    // public string CharacterName => characterName;
    // public Sprite CharacterImage => characterImage;
    // public Color CharacterColor => characterColor;
    // public string Description => description;
}

[Serializable]
public class CharaacterDetails
{
    public string characterName;
    public Sprite characterImage;
    public Color characterColor = Color.white;

    [TextArea(3, 5)]
    public string description;
    public string poseName;
}
