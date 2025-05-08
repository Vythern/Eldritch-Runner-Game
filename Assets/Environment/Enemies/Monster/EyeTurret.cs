using System.Collections;
using UnityEngine;

public class EyeTurret : MonoBehaviour
{
    private GameObject playerReference = null;
    [SerializeField] private GameObject monsterProjectile = null;
    [SerializeField] private GameObject openVisual = null; //transition between open and "blink" state
    [SerializeField] private GameObject closedVisual = null; //enable and disable these based on when they were last hit / closed.  
    private Monster monsterReference = null;

    private float lastShot = 0f;
    private float lastScan = 0f;
    private float shotDelay = 1.5f;
    private float scanDelay = 0.5f;
    

    private LayerMask playerOnly = (1 << 6);

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.layer == 10)
        {
            //close the eye- disable openVisual, enable closedVisual
            //set a couroutine that re-enables this script's execution and flips the open and closed visuals back a random duration of time from now between 6 and 18 seconds
            openVisual.SetActive(false);
            closedVisual.SetActive(true);

            this.gameObject.layer = 5;
            this.enabled = false;

            StartCoroutine(openEye());
        }
    }

    private IEnumerator openEye()
    {
        float delay = Random.Range(12f, 37f);
        yield return new WaitForSeconds(delay);

        openVisual.SetActive(true);
        closedVisual.SetActive(false);

        this.enabled = true;
        this.gameObject.layer = 7;
    }

    private bool playerInRange()
    {
        if (playerReference == null && Time.time - lastScan >= scanDelay) //check for player every ~0.5f seconds
        {
            lastScan = Time.time;
            Collider2D[] collidersList = Physics2D.OverlapCircleAll((Vector2)this.transform.position, 40f, playerOnly);
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
            if (Vector2.Distance(playerReference.transform.position, this.gameObject.transform.position) <= 40f)
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

        currentProjectile.GetComponent<Projectile>().initializeProjectile(direction, this.gameObject);
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