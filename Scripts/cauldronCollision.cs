using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cauldronCollision : MonoBehaviour
{
    private gameHandler game;
    private ParticleSystem splashEffect;

    // Start is called before the first frame update
    void Start()
    {
        game = GameObject.Find("GameCanvas").GetComponent<gameHandler>();
        splashEffect = Resources.Load<ParticleSystem>("Prefabs/Particle System/Splash");
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.collider && collision.gameObject.CompareTag("Ingredient"))
        {
            Instantiate(splashEffect).gameObject.transform.position = collision.gameObject.transform.position;

            game.addToCast(collision.gameObject.name);

            Destroy(collision.gameObject);
        }
    }
}
