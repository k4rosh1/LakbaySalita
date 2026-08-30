using UnityEngine;

public class Player : MonoBehaviour
{
    public float speed = 5f;
    public float acceleration = 1.2f;
    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        speed += acceleration * Time.deltaTime;
        transform.Translate(new Vector2(1f,0f) * speed * Time.deltaTime);

        if (anim != null)
        {
            anim.SetBool("isWalking", speed > 0);
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        //if (other.gameObject.tag == "Ground")
        {
            //FindObjectOfType<GroundSpawner>().SpawnGround();
        }
    }
}
