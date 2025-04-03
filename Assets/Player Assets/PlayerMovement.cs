using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Rigidbody2D playerBody; //Reference, add, and manipulate velocity / movement.  
    [SerializeField] private Camera playerCamera; //Manipulate the camera based on mouse movement with this.  
    [SerializeField] private Collider2D groundCollider; //Used to verify whether the player is in the air / on the ground.  
    //Groundedness is checked via a trigger collider on the player game object.  




    private Vector2 movementDirection = Vector2.zero; //Track what direction the player is trying to move.  Send to fixed update later.  
    private Vector2 movementDirectionAngular = Vector2.zero; //same- for turning the player instead.  
    private bool grounded = false;

    //Friction
    [SerializeField] private float frictionOffset = 49f; //do not set above 50 or else player will accelerate indefenitely on the current time step.  frictionOffset determines how fast the player slows down.  Lower values = faster slow down.  
    //Default value is set to 49.  Could do with tweaking, the player's horizontal velocity caps at about 15.75 with this number.  Fast enough, but the player still benefits from jumping and maintaining air time.  
    [SerializeField] private float airFrictionOffset = 49.75f; //same as normal friction, represents air friction and thus is much weaker.  dash is much stronger in air, so potentially needs slowed in air.  
    //At peak speeds of 19, this is faster, but you get less control over movement.  Faster than the ground speed, but less control.  Requires more planning and precision.  



    KeyCode StrafeLeft = KeyCode.A;
    KeyCode StrafeRight = KeyCode.D;
    KeyCode MoveUp = KeyCode.W;
    KeyCode MoveDown = KeyCode.S;
    KeyCode Jump = KeyCode.Space;
    KeyCode Crouch = KeyCode.X;


    private void OnTriggerStay2D(Collider2D collision)
    {
        if(collision != null)
        {
            if (collision.IsTouchingLayers(LayerMask.GetMask("Default")))
            {
                grounded = true;
            }
            else
            {
                //what other conditions might go here?  Eg jumping on enemies?  
            }
        }
        else
        {
            grounded = false;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        grounded = false;
    }

    private void handleInput()
    {
        movementDirection = Vector2.zero;
        movementDirectionAngular = Vector2.zero;
        if (grounded)
        {
            if (Input.GetKey(StrafeLeft))
            {
                movementDirection.x -= 1f;
            }
            if (Input.GetKey(StrafeRight))
            {
                movementDirection.x += 1f;
            }
            if (Input.GetKey(MoveUp))
            {
                //ascend ladders?  Alternatively, another jump option?  
            }
            if (Input.GetKey(MoveDown))
            {
                //descend ladder, maybe crouch or slide?  
            }
            if (Input.GetKeyDown(Jump))
            {
                playerBody.AddForce(transform.up * 250f);
            }
            if (Input.GetKey(Crouch))
            {
                //disable jump
            }
        }
        else
        {
            if (Input.GetKey(StrafeLeft))
            {
                movementDirection.x -= 0.33f;
            }
            if (Input.GetKey(StrafeRight))
            {
                movementDirection.x += 0.33f;
            }
        }
    }

    private void applyPhysics() //apply friction runs in fixedUpdate, so it uses fixedDeltaTime.  
    {
        //Slow the player's horizontal speed.  
        Vector2 tempVelocity = playerBody.linearVelocity;

        if (grounded) { tempVelocity *= Time.fixedDeltaTime * frictionOffset; }
        else { tempVelocity *= Time.fixedDeltaTime * airFrictionOffset; }

        playerBody.linearVelocityX = tempVelocity.x;
    }



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //print(grounded);
        //print(playerBody.linearVelocity);
        handleInput();
    }


    void FixedUpdate()
    {
        applyPhysics();
        print(playerBody.linearVelocity.x);
        playerBody.AddForce(movementDirection.x * 1000 * transform.right * Time.fixedDeltaTime);
        playerBody.AddForce(movementDirection.y * 1000 * transform.up * Time.fixedDeltaTime);
    }
}