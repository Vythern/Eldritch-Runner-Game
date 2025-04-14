using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private Rigidbody2D projectileBody;
    [SerializeField] private float projectileSpeed;
    private GameObject projectileOrigin = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
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
            projectileBody.AddForce(direction * this.projectileSpeed * 2f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
