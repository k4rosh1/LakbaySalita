using UnityEngine;

public class GroundMovement : MonoBehaviour
{
    public GameObject groundPrefab;

    public float speed;

    private bool hasSpawnedGround = false;
    private void Update()
    {
        // If the player is currently stopped (e.g. spawned an enemy), stop moving the ground!
        SelectedCharacter player = FindObjectOfType<SelectedCharacter>();
        if (player != null && player.isStopped)
        {
            return;
        }

        if (Vector3.Distance(new Vector3(-0.819092035F, -3.38623667F, 0), transform.position) < 0.2f && !hasSpawnedGround)
        {
            Instantiate(groundPrefab, new Vector3(-0.819092035F + 20, -3.38623667F, 0), Quaternion.identity);   
            hasSpawnedGround = true;
        }
        else if (Vector3.Distance(new Vector3(-21f, -3.38623667f, 0), transform.position) < 0.2f)
        {
            Destroy(gameObject);
        }

        transform.Translate(Vector3.left * speed * Time.deltaTime);
    }
}
