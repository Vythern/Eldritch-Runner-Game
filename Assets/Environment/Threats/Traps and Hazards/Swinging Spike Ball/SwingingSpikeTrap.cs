using UnityEngine;

public class SwingingSpikeTrap : MonoBehaviour
{
    [SerializeField] GameObject pivot = null;

    public float swingAngle = 45f;
    public float swingSpeed = 2f;

    
    //Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    //Update is called once per frame
    void Update()
    {
        float angle = Mathf.Sin(Time.time * swingSpeed) * swingAngle;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}