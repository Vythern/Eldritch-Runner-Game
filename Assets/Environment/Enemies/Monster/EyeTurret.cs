using UnityEngine;

public class EyeTurret : MonoBehaviour
{
    private GameObject playerReference = null;
    [SerializeField] private GameObject monsterProjectile = null;
    private Monster monsterReference = null;

    private float lastShot = 0f;
    private float lastScan = 0f;
    private float shotDelay = 2f;
    private float scanDelay = 0.5f;

    private LayerMask playerOnly = (1 << 6);

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.layer == 10)
        {
            //kill turret.  
            //close the eye
            //re-enable a random amount of time later divided by the difficulty.  
        }
    }

    private bool playerInRange()
    {
        if (playerReference == null && Time.time - lastScan >= scanDelay) //check for player every ~0.5f seconds
        {
            lastScan = Time.time;
            Collider2D[] collidersList = Physics2D.OverlapCircleAll((Vector2)this.transform.position, 100f, playerOnly);
            for (int i = 0; i < collidersList.Length; i++)
            {
                if (collidersList[i].CompareTag("Player"))
                {
                    playerReference = collidersList[i].gameObject;
                    return true;
                }
            }
        }
        else if(playerReference != null && Time.time - lastScan >= scanDelay)
        {
            lastScan = Time.time;
            if (Vector2.Distance(playerReference.transform.position, this.gameObject.transform.position) <= 100f)
            {
                return true;
            }
        }
        return false;
    }

    private void createProjectile()
    {
        lastShot = Time.time; //Update shot timer.  

        GameObject currentProjectile = Instantiate(monsterProjectile, this.transform.position, Quaternion.identity); //Create the projectile at the location of the turret.  
        
        Vector2 direction = playerReference.transform.position - this.transform.position;

        currentProjectile.GetComponent<Projectile>().initializeProjectile(direction, monsterReference.gameObject);
    }

    private void Start()
    {
        monsterReference = GetComponentInParent<Monster>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time - lastShot >= shotDelay && playerInRange())
        {
            createProjectile();
        }
    }
}