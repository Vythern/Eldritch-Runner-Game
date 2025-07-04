using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    //BASICS
    [Header("Basics")]
    [SerializeField] private Rigidbody2D playerBody; //Reference, add, and manipulate velocity / movement.  
    [SerializeField] private Camera playerCamera; //Manipulate the camera based on mouse movement with this.  
    [SerializeField] private Collider2D groundCollider; //Used to verify whether the player is in the air / on the ground.  Checked via trigger collider on player game object.  
    private LayerMask defaultAndTraps = (1 << 0) | (1 << 8);
    private LayerMask defaultOnly = (1 << 0);
    private LayerMask projectileOnly = (1 << 9);

    //MOVEMENT / FRICTION
    [Header("Player Movement")]
    private Vector2 movementDirection = Vector2.zero; //Track what direction the player is trying to move.  Send to fixed update later.  
    private Vector2 movementDirectionAngular = Vector2.zero; //same- for turning the player instead.  
    private bool grounded = false;
    [SerializeField] private float frictionOffset = 49f; //do not set above 50 or else player will accelerate indefenitely on the current time step.  frictionOffset determines how fast the player slows down.  Lower values = faster slow down.  
    //Default value is set to 49.  Could do with tweaking, the player's horizontal velocity caps at about 15.75 with this number.  Fast enough, but the player still benefits from jumping and maintaining air time.  
    [SerializeField] private float airFrictionOffset = 49.75f; //same as normal friction, represents air friction and thus is much weaker.  dash is much stronger in air, so potentially needs slowed in air.  
    //At peak speeds of 19, this is faster, but you get less control over movement.  Faster than the ground speed, but less control.  Requires more planning and precision.  

    //DAMAGE / HEALTH
    private int playerHealth = 2; //the number of hits that the player can take before death.  
    private float intangibleDuration = 2f; //Duration that the player is immune to taking another hit when damaged.  
    private float lastDamageInstance = 0f;

    //ATTACKING
    private float lastAttack = -1f;
    private float attackCooldown = 0.6f; //attack has a 1 second cooldown.  
    private bool isAttacking = false;
    private List<GameObject> hitObjects = new List<GameObject>();
    private float attackDuration = 0.2f; //Duration that the player's sword is active.  

    //PARRYING
    private float lastParry = -4f;
    private float parryCooldown = 4f; //parry delay on whiff is large, but if the parry is successful, then the "lastParry" will be reset, allowing an immediate followup.  
    private bool isParrying = false;
    private float parryDuration = 0.3f; //Duration that the player is immune to damage and capable of reflecting projectiles / attacks.  
    private bool successfulParry = false; //Determines what sound effect to play when parrying.  


    //DASHES / TELEPORT
    private float teleportDashCooldown = 4f; //the player also has a teleport dash that provides immunity frames.  
    private float lastTeleportDash = 0f;
    private bool dashReady = true;
    private bool teleportReady = true;
    private float dashImmunityDuration = 1.2f;
    private Color originalColor;
    [SerializeField] private float dashForce = 20f; //dash force should be roughly equivalent to the player's maximum horizontal movement speed.  
                                                    //If it's too low, then the player loses speed by dashing.  If it's really high, it will quickly be dropped by friction.  Aim high with this number.  

    //UI / VISUALS
    [Header("UI / Visuals")]
    [SerializeField] private GameObject cursorHelper; //Visual demonstration for where the cursor is.  
    private Vector2 cursorCoordinates; //Used to determine where the player's attacks and dash will go to.  Also could be used for parry / block angle.  

    //AUDIO / SOUND
    [Header("Player SFX")]
    [SerializeField] private AudioSource audioOrigin;
    [SerializeField] private AudioClip[] playerJumpSound;
    [SerializeField] private AudioClip[] playerLandSound;
    [SerializeField] private AudioClip[] playerFootstepSound;
    [SerializeField] private AudioClip[] playerFootstepGrassSound;
    [SerializeField] private AudioClip[] playerHurtSound;
    [SerializeField] private AudioClip[] playerDashSound;
    [SerializeField] private AudioClip[] playerTeleportSound;
    [SerializeField] private AudioClip[] playerAttackSound;
    [SerializeField] private AudioClip[] playerParrySound;
    [SerializeField] private AudioClip[] playerSuccessfulParrySound;
    [SerializeField] private AudioClip[] playerFailedParrySound;
    private float stepCooldown = 0.5f; //play relevant footstep sound every n seconds



    //CONTROLS
    KeyCode StrafeLeft = KeyCode.A;
    KeyCode StrafeRight = KeyCode.D;
    KeyCode MoveUp = KeyCode.W;
    KeyCode MoveDown = KeyCode.S;
    KeyCode Jump = KeyCode.Space;
    KeyCode Crouch = KeyCode.X;
    KeyCode Dash = KeyCode.LeftShift;
    KeyCode Attack = KeyCode.Mouse0;
    KeyCode Parry = KeyCode.Mouse1;


    private void setPlayerGroundedness()
    {
        //create a box with the same radius as player downwards a short distance one half the player's height downwards
        //if it collides with the default layer, then set grounded to true.  
        //if it does not collide with the default layer, set grounded to false
        Vector2 boxSize = new Vector2(0.9f, 0.05f);
        Vector2 boxCenter = this.gameObject.transform.position - new Vector3(0f, 1.025f, 0f);

        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, defaultOnly);
        if (hits.Length >= 1) //if at least one default layer collider was found beneath the player
        {
            if (!grounded) //player just landed
            {
                PlaySound(playerLandSound);
            }
            grounded = true;
            dashReady = true;
        }
        else { grounded = false; }
    }

    private void handleProjectileCollision()
    {
        if (Time.time - lastDamageInstance >= intangibleDuration) //Do not register damage if the player is intangible.  
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(this.transform.position, 5f, projectileOnly);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].gameObject.GetComponent<Projectile>().activateHitEffect();
            }
            //start intangibility coroutine.  
            StartCoroutine("activateInvulnerability");
            lastDamageInstance = Time.time;
            playerHealth--;
            if (playerHealth >= 1) //if player has health remaining, then give them temporary intangibility
            {
                PlaySound(playerHurtSound);
                //set player to only collide with default layer until intangible duration is over.  
            }
            else
            {
                PlaySound(playerHurtSound); //TODO:  Player death sound
                playerBody.linearVelocityX = 0;
                this.enabled = false;
                //kill player.  
            }
        }
    }

    private void handleMonsterCollision(Collision2D col)
    {
        if (Time.time - lastDamageInstance >= intangibleDuration) //Do not register damage if the player is intangible.  
        {
            //start intangibility coroutine.  
            StartCoroutine("activateInvulnerability");
            lastDamageInstance = Time.time;
            playerHealth--;
            if (playerHealth >= 1) //if player has health remaining, then give them temporary intangibility
            {
                PlaySound(playerHurtSound);
                //knockback player in opposite direction of monster.  
                Vector2 direction = Vector2.right;
                playerBody.linearVelocity = Vector2.zero;
                playerBody.AddForce(direction * 10f, ForceMode2D.Impulse);
            }
            else
            {
                PlaySound(playerHurtSound); //TODO:  Player death sound
                Vector2 direction = Vector2.right;
                playerBody.linearVelocity = Vector2.zero;
                playerBody.AddForce(direction * 10f, ForceMode2D.Impulse);
                playerBody.gravityScale = 0.33f;
                this.enabled = false;
                //kill player.  
            }
        }
    }

    private void handleTrapCollision(Collision2D col)
    {
        if (Time.time - lastDamageInstance >= intangibleDuration) //Do not register damage if the player is intangible.  
        {
            //start intangibility coroutine.  
            StartCoroutine("activateInvulnerability");
            lastDamageInstance = Time.time;
            playerHealth--;
            if (playerHealth >= 1) //if player has health remaining, then give them temporary intangibility
            {
                PlaySound(playerHurtSound);
                //knockback player in opposite direction of trap.  
                Vector2 direction = (this.transform.position - col.transform.position).normalized;
                playerBody.linearVelocity = Vector2.zero;
                playerBody.AddForce(direction * 5f, ForceMode2D.Impulse);
            }
            else
            {
                PlaySound(playerHurtSound); //TODO:  Player death sound
                Vector2 direction = (this.transform.position - col.transform.position).normalized;
                playerBody.linearVelocity = Vector2.zero;
                playerBody.AddForce(direction * 5f, ForceMode2D.Impulse);
                playerBody.gravityScale = 0.33f;
                this.enabled = false;
                //kill player.  
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.layer == 9) //if the player hit a projectile
        {
            handleProjectileCollision();
        }
        else if (collision.gameObject.layer == 8) //player collided with spike pit trap, or saw trap (moving or stationary).  
        {
            handleTrapCollision(collision);
        }
        else if (collision.gameObject.layer == 12) //player collided with the eldritch horror
        {
            handleMonsterCollision(collision);
        }

    }

    private void PlaySound(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        audioOrigin.PlayOneShot(clip);
    }

    private IEnumerator activateInvulnerability()
    {
        //immediately set visibility to 66%
        //make player's layer "intangible" for the duration
        //make player's visibility alternate from 33% to 66% every 0.2 seconds for the duration of intangibleDuration.  

        this.gameObject.layer = 11;

        lastDamageInstance = Time.time;

        SpriteRenderer renderer = GetComponent<SpriteRenderer>(); //sprite renderer must not be in children.  

        float elapsedTime = 0f;
        float flickerSpeed = 0.2f;
        bool visible = true;

        while (elapsedTime < intangibleDuration)
        {
            elapsedTime += flickerSpeed;

            float alpha = visible ? 0.33f : 0.66f;
            Color newColor = renderer.color;
            newColor.a = alpha;
            renderer.color = newColor;

            visible = !visible;
            yield return new WaitForSeconds(flickerSpeed);
        }

        //restore transparency.  
        Color finalColor = renderer.color;
        finalColor.a = 1f;
        renderer.color = finalColor;
    }

    private IEnumerator activateDashImmunity()
    {
        //make player's layer "intangible" for the duration
        //make player generate particle effects.  
        //play teleport dash sound effect
        //Make player orange (for now).  

        this.gameObject.layer = 11;

        SpriteRenderer renderer = GetComponent<SpriteRenderer>(); //sprite renderer must not be in children.  
        renderer.color = Color.black;

        yield return new WaitForSeconds(dashImmunityDuration);
    }

    private IEnumerator activateParryRoutine()
    {
        lastParry = Time.time;

        isParrying = true;

        float elapsedTime = 0f;
        float parryTick = 0.03f; //how long each scan for projectiles lasts.  
        //by default, scan 10 times per parry.  

        while (elapsedTime < parryDuration)
        {
            handleParry();
            elapsedTime += parryTick;

            yield return new WaitForSeconds(parryTick);
        }

        if(!successfulParry) { PlaySound(playerFailedParrySound); } //player did not parry anything

        successfulParry = false; //reset for next time.  
        isParrying = false;        
    }

    private bool isPlayerIntangible()
    {
        //if the player has an active invulnerability, then we can use this to check whether or not the coroutines should reset it.  
        if (Time.time - lastDamageInstance <= intangibleDuration) 
        {
            //player's transparency blink should be active
            return true;
        }
        if (Time.time - lastTeleportDash <= dashImmunityDuration)
        {
            //player's dash visual should be active.  
            return true;
        }
        else
        {
            this.gameObject.GetComponent<SpriteRenderer>().color = originalColor;
            //return player's dash visual to off state.  (For now this means return to original renderer colour)
        }
        return false;
    }

    private IEnumerator activateAttackRoutine()
    {
        lastAttack = Time.time;

        isAttacking = true;

        int attackIterator = 0;
        float attackTick = attackDuration / 10f; //how long each scan for enemies and obstacles lasts.  
        //by default, scan 10 times per attack.  

        while (attackIterator < 10) //check for targets 10 times over the course of the attack.  
        {
            handleAttack();
            attackIterator += 1;

            yield return new WaitForSeconds(attackTick);
        }

        hitObjects.Clear();
        isAttacking = false;
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
                PlaySound(playerJumpSound);
            }
            if (Input.GetKey(Crouch))
            {
                //disable jump
            }
            if (Input.GetKeyDown(Dash))
            {
                if (Input.GetKeyDown(Dash))
                {
                    if (dashReady)
                    {
                        dash();
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
                    dash();
                }
            }
        }
        if (Input.GetKeyDown(Attack) && attackReady())
        {
            PlaySound(playerAttackSound);
            StartCoroutine("activateAttackRoutine");
        }
        if (Input.GetKeyDown(Parry) && parryReady())
        {
            PlaySound(playerParrySound);
            StartCoroutine("activateParryRoutine");
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

    private void resetCooldowns()
    {
        lastParry = Time.time - parryCooldown; //reset parry

        lastAttack = Time.time - attackCooldown; //reset attack

        //lastTeleportDash = Time.time - teleportDashCooldown; //reset the player's normal dash and their teleport dash.  
        dashReady = true;
        teleportReady = true;

    }

    private void handleAttack()
    {
        //attack will run every 0.02 seconds (10 times)
        //attack will generate a hitbox in the direction of the player's cursor
        //attack will destroy enemies and delete projectiles in the hurtbox.  
        //this helps the player if they whiff their parry, but they have to be precise with the attack.  
        //origin should be transform.pos + cursor position's normalized direction * 1.  
        //direction should be the cursor's position * 1.  

        //cursorHelper.transform.position - transform.position

        Vector2 direction = cursorHelper.transform.position - transform.position;
        Vector2 origin = new Vector2(this.transform.position.x, this.transform.position.y);
        origin += direction.normalized * 1.25f;
        float angle = 90f + Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;


        Collider2D[] colliders = Physics2D.OverlapBoxAll(origin, new Vector2(2, 1.5f), angle);

        //RaycastHit2D[] hits = Physics2D.BoxCastAll(origin, new Vector2(2f, 1f), 0f, direction.normalized);

        for (int i = 0; i < colliders.Length; i++)
        {
            if( !(hitObjects.Contains(colliders[i].gameObject)) ) //only interact with objects that have been hit by the currently active attack.  
            {
                if (colliders[i].CompareTag("Projectile"))
                {
                    Projectile currentProjectile = colliders[i].gameObject.GetComponent<Projectile>();
                    currentProjectile.activateHitEffect();
                }
                if (colliders[i].CompareTag("Trap") || colliders[i].CompareTag("Untagged"))
                {
                    playerBody.AddForce(-direction.normalized * 8.5f, ForceMode2D.Impulse);
                }
            }
            hitObjects.Add(colliders[i].gameObject); //stop object / enemy / projectile / etc from being hit again
        }
    }

    private void handleParry()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(this.transform.position, 2f, projectileOnly); //make the parry directional?  
        for(int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].CompareTag("Projectile"))
            {
                if(!successfulParry) { PlaySound(playerSuccessfulParrySound); } //play parry sound exactly once.  
                
                Projectile currentProjectile = colliders[i].gameObject.GetComponent<Projectile>();
                currentProjectile.reflect();
                resetCooldowns();


                successfulParry = true;

                //TODO:  if projectile was sent by enemy, reflect.  if else, then send in a random direction.  
                //play successful parry sound
                //reset parry cooldown.  reset jumps and dash.  reset attack cooldown.  
            }
        }
        //scan in circle around player.  
        //get list of projectiles in scan range
        //delete projectiles in range
        //play parry noise
    }

    public void triggerDeathPit()
    {
        if(playerHealth == 1) //separate logic when dying to death pit
        {
            print("Lava pit death");
            this.gameObject.layer = 5;
            playerBody.linearVelocityX = 0;
            playerBody.gravityScale = 0.33f;
            this.enabled = false;
        }
    }

    private bool parryReady()
    {
        if(Time.time - lastParry >= parryCooldown) { return true; }
        else { return false; }
    }

    private bool attackReady()
    {
        if (Time.time - lastAttack >= attackCooldown) { return true; }
        else { return false; }
    }

    private void dash()
    {
        dashReady = false;

        Vector2 dashDirection = (cursorHelper.transform.position - transform.position).normalized;

        playerBody.linearVelocity = Vector2.zero;
        
        Vector2 finalForce = dashForce * new Vector2(dashDirection.x, dashDirection.y * 0.5f);

        if (teleportReady)
        {
            lastTeleportDash = Time.time;
            teleportReady = false;
            dashReady = true; //allow player to immediately dash again after a teleport dash.  

            StartCoroutine("activateDashImmunity");
            //teleport towards cursor up to a maximum distance of 5f.  
            //If there is a trap or wall in the way, teleport to the closest point on bounds.  

            Vector3 teleportTransform = Vector3.zero;
            RaycastHit2D hit = Physics2D.Raycast(this.gameObject.transform.position, dashDirection, 5f, defaultAndTraps);
            if(hit.collider != null) //object within 5f units
            {
                //We multiply the number by 0.5 so that the player does not get stuck in walls.  All though the hit.point is correct, the player's body is not infinitely small.  
                teleportTransform = 0.5f * dashDirection * Mathf.Clamp(Vector2.Distance(hit.point, this.gameObject.transform.position), 0f, 5f);
            }
            else //Nothing in the way
            {
                teleportTransform = dashDirection * Mathf.Clamp(Vector2.Distance(cursorCoordinates, this.transform.position), 0f, 5f);
            }
            PlaySound(playerTeleportSound);
            this.gameObject.transform.position += teleportTransform;
        }

        PlaySound(playerDashSound);
        playerBody.AddForce(finalForce, ForceMode2D.Impulse);
        //get mouse cursor position, add velocity in direction of mouse cursor.  
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        originalColor = GetComponent<SpriteRenderer>().color;
    }

    // Update is called once per frame
    void Update()
    {
        cursorCoordinates = Camera.main.ScreenToWorldPoint((Vector2)Input.mousePosition);
        cursorHelper.transform.position = new Vector3(cursorCoordinates.x, cursorCoordinates.y, 0f);
        
        if(Time.time - lastTeleportDash >= teleportDashCooldown) { teleportReady = true; }
        if (!isPlayerIntangible()) { this.gameObject.layer = 6; }



        setPlayerGroundedness();
        handleInput();
        //print("Coordinates:  " + cursorCoordinates);
        //print("Helper location:  " + cursorHelper.transform.position);
        //print(grounded);
        //print(playerBody.linearVelocity);
    }
    
    private void OnDrawGizmos() //visual debugging
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, cursorHelper.transform.position);


        if (isParrying)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 2f);
        }

        if(isAttacking)
        {
            Gizmos.color = Color.red;

            Vector2 direction = cursorHelper.transform.position - transform.position;
            Vector2 origin = (Vector2)transform.position + direction.normalized * 1.25f;
            float angle = 90f + Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            Matrix4x4 rotationMatrix = Matrix4x4.TRS(origin, Quaternion.Euler(0, 0, angle), Vector3.one);
            Gizmos.matrix = rotationMatrix;

            Gizmos.DrawWireCube(Vector3.zero, new Vector2(2, 1.5f));

            Gizmos.matrix = Matrix4x4.identity;
        }

        Vector2 boxSize = new Vector2(0.9f, 0.05f);
        Vector2 boxCenter = this.gameObject.transform.position - new Vector3(0f, 1.025f, 0f);

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(boxCenter, boxSize);

    }


    void FixedUpdate()
    {
        if(Input.GetKeyDown(KeyCode.F))
        {
            playerHealth = 1000;
        }



        applyPhysics();
        //print(playerBody.linearVelocity.x);
        playerBody.AddForce(movementDirection.x * 1000 * transform.right * Time.fixedDeltaTime);
        if(Mathf.Abs(playerBody.linearVelocityX) > 0 && grounded) //if the player is moving on the ground:  
        {
            //TODO:  If player is not trying to move (grounded velocity > 0 but not because of player input), then play looping slide sound?  
            stepCooldown -= Time.fixedDeltaTime; //Then decrement step cooldown this frame.  
            if(stepCooldown <= 0)
            {
                PlaySound(playerFootstepSound); //play step noise
                stepCooldown = 0.5f; //reset step cooldown
            }
        }
        playerBody.AddForce(movementDirection.y * 1000 * transform.up * Time.fixedDeltaTime);
    }
}