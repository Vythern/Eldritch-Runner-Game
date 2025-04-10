using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Rigidbody2D playerBody; //Reference, add, and manipulate velocity / movement.  
    [SerializeField] private Camera playerCamera; //Manipulate the camera based on mouse movement with this.  
    [SerializeField] private Collider2D groundCollider; //Used to verify whether the player is in the air / on the ground.  
    //Groundedness is checked via a trigger collider on the player game object.  
    //TODO:  The ground collider may or may not actually be getting used.  I believe it is set as a trigger and therefore separate, but the serialized field might do nothing in this case?  


    private float dashCooldown = 1f; //The player can dash once per second, this cooldown can be reset by interacting with the game environment in some ways, eg parrying attacks or "pogoing".  
    private float lastDash = 0f;
    private bool dashReady = true;
    [SerializeField] private float dashForce = 20f; //dash force should be roughly equivalent to the player's maximum horizontal movement speed.  
    //If it's too low, then the player loses speed by dashing.  If it's really high, it will quickly be dropped by friction.  Aim high with this number.  

    private Vector2 cursorCoordinates; //Used to determine where the player's attacks and dash will go to.  Also could be used for parry / block angle.  
    [SerializeField] private GameObject cursorHelper; //Visual demonstration for where the cursor is.  


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
    KeyCode Dash = KeyCode.LeftShift;


    private void OnTriggerStay2D(Collider2D collision)
    {
        if(collision != null)
        {
            if (collision.IsTouchingLayers(LayerMask.GetMask("Default")))
            {
                grounded = true;
                dashReady = true; //player can only dash again after touching the ground.  
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
                //descend ladder, maybe crouch or slide?  Fast fall?  
            }
            if (Input.GetKeyDown(Jump))
            {
                playerBody.AddForce(transform.up * 250f);
            }
            if (Input.GetKey(Crouch))
            {
                //disable jump
            }
            if (Input.GetKeyDown(Dash))
            {
                if (Input.GetKeyDown(Dash))
                {
                    if (Time.time - dashCooldown > lastDash)
                    {
                        lastDash = Time.time;

                        Vector2 dashDirection = (cursorHelper.transform.position - transform.position).normalized;
                        playerBody.linearVelocity = Vector2.zero;
                        playerBody.AddForce(dashDirection * dashForce, ForceMode2D.Impulse);
                        //get mouse cursor position, add velocity in direction of mouse cursor.  
                    }
                }
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
            if (Input.GetKeyDown(Dash))
            {
                if (dashReady)
                {
                    dashReady = false;
                    lastDash = Time.time;

                    Vector2 dashDirection = (cursorHelper.transform.position - transform.position).normalized;
                    playerBody.linearVelocity = Vector2.zero;
                    playerBody.AddForce(dashDirection * dashForce, ForceMode2D.Impulse);
                    //get mouse cursor position, add velocity in direction of mouse cursor.  
                }
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
        cursorCoordinates = Camera.main.ScreenToWorldPoint((Vector2)Input.mousePosition);
        cursorHelper.transform.position = new Vector3(cursorCoordinates.x, cursorCoordinates.y, 0f);
        //print("Coordinates:  " + cursorCoordinates);
        //print("Helper location:  " + cursorHelper.transform.position);

        
        
        //print(grounded);
        //print(playerBody.linearVelocity);
        handleInput();
    }

    private void OnDrawGizmos() //visual debugging
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, cursorHelper.transform.position);
    }


    void FixedUpdate()
    {
        applyPhysics();
        //print(playerBody.linearVelocity.x);
        playerBody.AddForce(movementDirection.x * 1000 * transform.right * Time.fixedDeltaTime);
        playerBody.AddForce(movementDirection.y * 1000 * transform.up * Time.fixedDeltaTime);
    }
}