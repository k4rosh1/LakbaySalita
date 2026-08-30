using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.U2D.Animation;
using UnityEngine.UI;

public class SelectedCharacter : MonoBehaviour
{
    public CharacterDatabase characterDB;
    public SpriteRenderer artworkSprite;
    public SpriteLibrary spriteLibrary;
    public int selectedOption = 0;

    private Animator anim;

    void Start()
    {
        if (!PlayerPrefs.HasKey("selectedOption"))
        {
            selectedOption = 0;
        }

        else
        {
            Load();
        }
        UpdateCharacter(selectedOption);

        anim = GetComponent<Animator>();

        // Start walking immediately since the character auto-moves
        if (anim != null)
        {
            anim.SetBool("isWalking", true);
        }
    }

    public bool isStopped = false;

    void Update()
    {
        // Keep isWalking true every frame unless explicitly stopped
        if (anim != null)
        {
            anim.SetBool("isWalking", !isStopped);
        }
    }

    public void StopWalking()
    {
        isStopped = true;
    }

    public void ResumeWalking()
    {
        isStopped = false;
    }

    private void UpdateCharacter(int selectedOption)
    {
        if (characterDB == null) return;
        Character character = characterDB.GetCharacter(selectedOption);
        if (character == null) return;

        if (spriteLibrary != null) spriteLibrary.spriteLibraryAsset = character.characterAsset;

        if (artworkSprite != null)
        {
            artworkSprite.sprite = character.characterSprite;
            artworkSprite.sortingOrder = 10;
        }
        
        Animator anim = GetComponent<Animator>();
        if (anim != null && character.characterAnimator != null)
        {
            anim.runtimeAnimatorController = character.characterAnimator;
        }

        // Destroy the SpriteResolver so it cannot fight the Animator
        SpriteResolver resolver = GetComponent<SpriteResolver>();
        if (resolver != null)
        {
            Destroy(resolver);
        }

        // --- CRITICAL FIX FOR HOVERING CHARACTERS ---
        // Since different characters were drawn at different heights in their canvas,
        // we can adjust the BoxCollider2D offset. Pushing the collider UP makes the physics 
        // engine drop the Player DOWN, which perfectly lowers the sprite to the grass!
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            float defaultOffsetY = 1.75f;
            float pushDownAmount = 0f;

            if (character.characterName == "Arkero") {
                pushDownAmount = 0f; // Arkero is already close to the ground
            } else if (character.characterName == "Mandirigma") {
                pushDownAmount = 0.4f; // Push down into the grass
            } else if (character.characterName == "Salamangkero") {
                pushDownAmount = 0.4f; // Push down into the grass
            } else if (character.characterName == "Albularyo") {
                pushDownAmount = 0.3f; // Push down slightly less since it's a PNG
            }

            collider.offset = new Vector2(collider.offset.x, defaultOffsetY + pushDownAmount);
        }
    }

    public void PlayAttack()
    {
        if (anim != null) anim.SetTrigger("attack");
    }

    public void PlayHurt()
    {
        if (anim != null) anim.SetTrigger("takeDamage");
    }

    private void Load()
    {
        Debug.Log("thecharacterhasloaded");
        selectedOption = PlayerPrefs.GetInt("selectedOption");
    }

}
