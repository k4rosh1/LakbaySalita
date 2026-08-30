
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;
using UnityEngine.UI;

[System.Serializable]
public class Character
{
   public string characterName;
   public Sprite characterSprite;
   public SpriteLibraryAsset characterAsset;
   public RuntimeAnimatorController characterAnimator;
}
