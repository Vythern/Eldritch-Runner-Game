using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private Rigidbody2D projectileBody;
    [SerializeField] private float projectileSpeed;
    private GameObject projectileOrigin = null;

    [SerializeField] private int projectileType = 0; //determines how the projectile should be treated on impact / etc.  
    [SerializeField] private float lifetime = 10f; //determines how long the projectile should stay in the scene.  
    private float spawnTime = 0f; //when the projectile was instantiated

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spawnTime = Time.time;
    }


    //Each projectile created sends a direction, a speed, and an origin.  The direction is where the projectile is facing, and the origin is used to find the "shooter" if it exists.  
    //Projectiles are instantiated, and then immediately initialized.  The prefab is responsible for tracking the rigidbody as well as the projectile's speed.  
    public void initializeProjectile(Vector2 direction, GameObject origin)
    {
        projectileOrigin = origin;
        projectileBody.AddForce(direction * projectileSpeed);
    }

    public void reflect()
    {
        if(projectileOrigin != null)
        {
            this.gameObject.layer = 10; //change layer to reflected projectile so that it can no longer hit the player.  
            projectileBody.linearVelocity = Vector2.zero;
            Vector2 direction = projectileOrigin.transform.position - projectileBody.transform.position;
            projectileBody.AddForce(direction * this.projectileSpeed * 4f);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        switch(collision.gameObject.layer)
        {
            case 0: //collided with ground
                activateHitEffect();
                break;
            case 6: //collided with player
                activateHitEffect(); 
                break;
            default:
                activateHitEffect();
                break;
        }
    }

    public void activateHitEffect() //upon colliding, activate this projectile's on hit effect.  
    {
        switch(this.projectileType)
        {
            default:
            //for now, we just destroy the projectile, but probably this should play a sound and a particle effect.  
            GameObject.Destroy(this.gameObject);
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(Time.time - spawnTime >= lifetime)
        {
            activateHitEffect(); //destroy projectile for now.  
        }
    }
}
