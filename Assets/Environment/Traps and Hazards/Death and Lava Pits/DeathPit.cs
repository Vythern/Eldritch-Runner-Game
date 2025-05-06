using UnityEngine;

public class DeathPit : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {   
        if(collision.gameObject.layer == 6 || collision.gameObject.layer == 11)
        {
            print("Triggering death pit");
            collision.gameObject.GetComponent<Player>().triggerDeathPit();
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
