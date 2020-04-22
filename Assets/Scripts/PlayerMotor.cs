using System.Collections;
using System;
using UnityEngine;

public class PlayerMotor : MonoBehaviour
{
    Rigidbody2D playerRigidbody;
    //[ReadOnly]
    public bool isGrounded;
    public bool isFalling;
    public float speed = 1.2f;
    public float airSpeed = 1.2f;
    public float wallSlideSpeed = -0.5f;
    public float wallJumpSidesIntensity = 1.2f;
    public float wallJumpMovementFreezeDelay = 0.35f;
    public float wallJumpXVector = 2f;
    SpriteRenderer spriteRenderer;
    public LayerMask ground;
    bool wallLeft;
    bool noWallSlideLeft;
    bool wallRight;
    bool noWallSlideRight;
    bool onSlopeLeft;
    bool onSlopeRight;
    Animator anim;
    Animator p1aniamtor;
    public float wallCheckDistance = 0.12f;
    public float groundCheckDistance = 0.5f;
    public float jumpForce = 6f;
    public bool canDoubleJump;
    public float dashSpeed = 500f;
    public float dashDistance = 0.045f;
    [HideInInspector]
    public bool canDash = true;
    public Action onDash;
    public bool wallSliding = false;
    public bool wallJumped = false;
    [SerializeField]
    GameObject landParticles;
    [SerializeField]
    GameObject walkParticles;
    [SerializeField]
    GameObject jumpParticlesPrefab;
    bool wasGrounded = true; //used to determine when we play the landing particles
    float fallDistance;

    private bool hasJumped;
    private bool hasPressedJumpButton;
    //[HideInInspector]
    public int airJumpsLeft;
    private float initialGravityScale;
    bool startDash;
    bool startDamageWall;
    Vector2 oldPos;
    private float initialDashDistance;
    private float initialDashSpeed;
    bool blockRightMovement;
    bool blockLeftMovement;
    float lastTimeSpawnedWalkParticles;
    bool isBeingKnockbacked; //true when we are being knockbacked, used to stop moving manually when we are pushed
    bool isFastFalling;
    PlayerController playerController;

    void Start()
    {
        playerRigidbody = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        //inputManager = GameObject.Find("GameManager").GetComponent<InputManager>();
        Physics2D.IgnoreLayerCollision(this.gameObject.layer, 0);
        initialGravityScale = playerRigidbody.gravityScale;
        anim = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
        p1aniamtor = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Animator>();
        initialDashDistance = dashDistance;
        initialDashSpeed = dashSpeed;
        playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
        CheckWalls();
        DetectSlope();
        DetectGround();

        if (onSlopeLeft || onSlopeRight)
        {
            if (startDash == false && hasPressedJumpButton == false)
                playerRigidbody.simulated = false;
        }
        else
            playerRigidbody.simulated = true;

        #region Stuck In Wall
        if (CheckStuckWall())
        {
            print("PLAYER IS STUCK IN A WALL");
            if (!startDamageWall)
            {
                StartCoroutine(DoDamage());
                startDamageWall = true;
            }
        }
        else
        {
            StopCoroutine(DoDamage());
            startDamageWall = false;
        }
        #endregion

        #region Wall Slide
        if (isGrounded)
        {
            blockLeftMovement = false;
            blockRightMovement = false;
            if (wallSliding)
            {
                FinishWallSlide();
            }
            else if (wallJumped)
                wallJumped = false;
        }
        if (wallSliding)
            if (spriteRenderer.flipX == true && wallLeft == false || spriteRenderer.flipX == false && wallRight == false)
            {
                wallSliding = false;
                playerRigidbody.gravityScale = initialGravityScale;
                anim.SetBool("slide", false);
            }
        if (wallSliding) //Detach the player from the wall
        {
            if (spriteRenderer.flipX && playerController.movementAxis == 1)
                FinishWallSlide(true);
            else if (spriteRenderer.flipX == false && playerController.movementAxis == -1)
                FinishWallSlide(true);

            blockRightMovement = false;
            blockLeftMovement = false;
        }
        #endregion

        if (startDash)
        {
            oldPos.x = transform.position.x;
            transform.position = oldPos;
        }

        if (initialDashDistance != dashDistance)
        {
            float multiplicator = dashDistance / initialDashDistance;
            dashSpeed = initialDashSpeed * multiplicator;
        }
    }

    private void LateUpdate()
    {
        if (isFalling)
        {
            fallDistance++;
            //runParticles.Stop();
        }
        else
        {
            if (isGrounded != wasGrounded && hasPressedJumpButton == false && fallDistance >= 15 && startDash == false)
            {
                SpawnLandParticles();
            }
            fallDistance = 0;
        }
        wasGrounded = isGrounded;
    }

    void CheckWalls()
    {
        RaycastHit2D hitLeft = Physics2D.Raycast(transform.position, Vector2.left, wallCheckDistance, ground);
        if (hitLeft.collider != null)
        {
            if (hitLeft.collider.isTrigger == false)
                wallLeft = true;
            else
                wallLeft = false;
            if (hitLeft.collider.gameObject.tag == "NoWallSlide")
            {
                noWallSlideLeft = true;
            }
        }
        else
        {
            wallLeft = false;
            noWallSlideLeft = false;
        }

        RaycastHit2D hitRight = Physics2D.Raycast(transform.position, Vector2.right, wallCheckDistance, ground);
        if (hitRight.collider != null)
        {
            if (hitRight.collider.isTrigger == false)
                wallRight = true;
            else
                wallRight = false;
            if (hitRight.collider.gameObject.tag == "NoWallSlide")
            {
                noWallSlideRight = true;
            }
        }
        else
        {
            wallRight = false;
            noWallSlideRight = false;
        }
    }

    public void Jump()
    {
        float _jumpForce = jumpForce;

        if (wallSliding)
        {
            WallJump();
            return;
        }
        if (startDash) //prevent jumping and dashing at the same time
            return;

        StartCoroutine(PressedJumpButton());
        playerRigidbody.simulated = true;

        if (isGrounded && canDoubleJump == false)
        {
            ApplyJumpForce();
            airJumpsLeft = 0;
        }
        if (canDoubleJump)
        {
            if (isGrounded)
            {
                ApplyJumpForce();
            }
            else if (airJumpsLeft > 0)
            {
                ApplyJumpForce();
                airJumpsLeft--;
                blockLeftMovement = false;
                blockRightMovement = false;
            }
        }
    }

    void ApplyJumpForce()
    {
        //CustomFunctions.PlaySound(CustomFunctions.instance.jumpSound);
        playerRigidbody.velocity = new Vector2(0f, 0f);

        playerRigidbody.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        //if (GetComponent<PlayerHealth>().currentHealth > 0)
        anim.Play("Jump");

        /*GameObject spawnedJumpParticles = Instantiate(jumpParticlesPrefab, this.transform);
        spawnedJumpParticles.transform.position = this.transform.position;
        Destroy(spawnedJumpParticles, 1f);*/
    }

    public void ShortHop()
    {
        if (playerRigidbody.velocity.y >= 3)
        {
            playerRigidbody.velocity = new Vector2(playerRigidbody.velocity.x, 0.95f);
        }
    }

    public void FastFall()
    {
        isFastFalling = true;
    }

    void WallJump()
    {
        wallSliding = false;
        playerRigidbody.gravityScale = initialGravityScale;

        bool jumpLeft = spriteRenderer.flipX;
        playerRigidbody.velocity = new Vector2(0f, 0f);
        Vector2 jumpVector;

        if (!jumpLeft) //we jumped right
        {
            blockLeftMovement = true;
            jumpVector = new Vector2(-wallJumpXVector, wallJumpSidesIntensity * jumpForce);
        }
        else //we jumped left
        {
            blockRightMovement = true;
            jumpVector = new Vector2(wallJumpXVector, wallJumpSidesIntensity * jumpForce);
        }

        wallJumped = true;
        playerRigidbody.AddForce(jumpVector, ForceMode2D.Impulse);
        anim.Play("Jump");
        spriteRenderer.flipX = !spriteRenderer.flipX;
        StartCoroutine(WallJumpDelay());
    }

    void FinishWallSlide(bool finishInTheAir = false)
    {
        wallSliding = false;
        playerRigidbody.gravityScale = initialGravityScale;
        anim.SetBool("slide", false);

        if (finishInTheAir)
        {
            anim.SetBool("run", false);
            //if (GetComponent<PlayerHealth>().currentHealth > 0)
            anim.Play("Jump");
        }
    }

    #region obstacle/ground detection
    void DetectSlope()
    {
        if (isGrounded)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance + 0.05f, ground);

            if (hit.collider != null && Mathf.Abs(hit.normal.x) > 0.1f && isGrounded)
            {
                if (hit.normal.x < 0)
                {
                    onSlopeRight = false;
                    onSlopeLeft = true;
                }
                else if (hit.normal.x > 0)
                {
                    onSlopeLeft = false;
                    onSlopeRight = true;
                }
            }
            else
            {
                onSlopeLeft = false;
                onSlopeRight = false;
            }
        }
        else
        {
            onSlopeLeft = false;
            onSlopeRight = false;
        }
    }

    void DetectGround()
    {
        bool checkGround = false; //true if we're grounded

        Vector2 origin1 = new Vector2(transform.position.x - 0.06f, transform.position.y);
        RaycastHit2D groundHit1 = Physics2D.Raycast(origin1, Vector2.down, groundCheckDistance, ground);

        Vector2 origin2 = new Vector2(transform.position.x, transform.position.y);
        RaycastHit2D groundHit2 = Physics2D.Raycast(origin2, Vector2.down, groundCheckDistance, ground);

        Vector2 origin3 = new Vector2(transform.position.x + 0.06f, transform.position.y);
        RaycastHit2D groundHit3 = Physics2D.Raycast(origin3, Vector2.down, groundCheckDistance, ground);

        if (groundHit1.collider != null)
        {
            if (groundHit1.collider.isTrigger == false)
                checkGround = true;
            else
                checkGround = false;
        }
        else if (groundHit2.collider != null)
        {
            if (groundHit2.collider.isTrigger == false)
                checkGround = true;
            else
                checkGround = false;
        }
        else if (groundHit3.collider != null)
        {
            if (groundHit3.collider.isTrigger == false)
                checkGround = true;
            else
                checkGround = false;
        }
        else
        {
            checkGround = false;
        }

        /*if (checkGround == false && Mathf.Abs(playerRigidbody.velocity.y) <= 0.05f)
            checkGround = true;*/

        isGrounded = checkGround;

        if (checkGround)
            isFalling = false;
        else
            isFalling = true;

        if (isGrounded && canDoubleJump)
            airJumpsLeft = 1;

        /*if (isGrounded)
            playerRigidbody.gravityScale = 2f;*/
    }

    bool IsOnSlope()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, ground);

        if (hit.collider != null && Mathf.Abs(hit.normal.x) > 0.1f && isGrounded)
        {
            return true;
        }

        return false;
    }

    bool CheckWallOrSlope(bool left)
    {
        Vector2 origin;

        RaycastHit2D hit;
        if (left)
        {
            origin = new Vector2(transform.position.x, transform.position.y - 0.16f);
            hit = Physics2D.Raycast(origin, Vector2.left, wallCheckDistance, ground);
        }
        else
        {
            origin = new Vector2(transform.position.x, transform.position.y - 0.16f);
            hit = Physics2D.Raycast(origin, Vector2.right, wallCheckDistance, ground);
        }

        if (hit.collider != null)
        {
            if (hit.collider.isTrigger == false)
                return true;
            else
                return false;
        }
        else
            return false;
    }

    bool CheckStuckWall()
    {
        //this will return true if the player is stuck in a wall

        if (wallLeft && wallRight && isGrounded)
        {
            RaycastHit2D hitUp = Physics2D.Raycast(transform.position, Vector2.up, 0.01f, ground);
            if (hitUp.collider != null)
            {
                if (hitUp.collider.isTrigger == false && hitUp.collider.GetComponent<PlatformEffector2D>() == null)
                    return true;
            }

        }

        return false;
    }
    #endregion

    IEnumerator PressedJumpButton()
    {
        hasPressedJumpButton = true;
        isFalling = false;
        yield return new WaitForSeconds(0.1f);
        hasPressedJumpButton = false;
        yield return new WaitForSeconds(0.15f);
        isFalling = true;
    }

    IEnumerator PerformDash()
    {
        float initialPosX = transform.position.x;
        canDash = false;
        startDash = true;


        playerRigidbody.gravityScale = 0f;
        playerRigidbody.velocity = Vector2.zero;
        isFalling = false;

        bool isGoingLeft;
        if (Input.GetKey(KeyCode.A))
        {
            isGoingLeft = true;
            spriteRenderer.flipX = true;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            isGoingLeft = false;
            spriteRenderer.flipX = false;
        }
        else if (spriteRenderer.flipX)
        {
            isGoingLeft = true;
        }
        else
        {
            isGoingLeft = false;
        }

        playerRigidbody.simulated = true;
        if (onDash != null)
            onDash();
        if (isGoingLeft && !CheckWallOrSlope(true))
            playerRigidbody.AddForce(Vector2.left * dashSpeed, ForceMode2D.Impulse);
        else if (!isGoingLeft && !CheckWallOrSlope(false))
            playerRigidbody.AddForce(Vector2.right * dashSpeed, ForceMode2D.Impulse);

        yield return new WaitForSeconds(dashDistance);
        startDash = false;
        playerRigidbody.velocity = Vector2.zero;
        playerRigidbody.gravityScale = initialGravityScale;
        float dashDistanceCalculate2 = Mathf.Abs(transform.position.x - initialPosX);
        //print(dashDistanceCalculate2);
        yield return new WaitForSeconds(0.2f);
        canDash = true;
    }

    IEnumerator WallJumpDelay()
    {
        yield return new WaitForSeconds(wallJumpMovementFreezeDelay);
        wallJumped = false;
    }

    IEnumerator DoDamage()
    {
        /*while (HealthSoul.instance.currentHealth[9] > 0 && CheckStuckWall())
        {
            HealthSoul.instance.Damage(10, this.transform, 0, isPlayer1, null, true, 0);
            yield return new WaitForSeconds(0.75f);
        }*/
        yield return null;
    }

    #region Knockback
    /// <summary>
    /// Pushes the player in a direction
    /// </summary>
    /// <param name="direction">Use Vector2.left/right/up</param>
    /// <param name="intensity">Between 0.05 and 0.15</param>
    public void ApplyKnockback(Vector2 direction, float intensity)
    {
        if (isBeingKnockbacked)
            return;

        StartCoroutine(Knockback(direction, intensity));
        blockLeftMovement = false;
        blockRightMovement = false;
    }

    IEnumerator Knockback(Vector2 direction, float intensity)
    {
        float timer = intensity;
        playerRigidbody.velocity = new Vector2(0f, 0f);
        isBeingKnockbacked = true;
        while (timer > 0)
        {
            MoveKnockback(direction);
            timer -= 0.01f;
            yield return new WaitForSeconds(0.01f);
        }
        yield return new WaitForSeconds(0.01f);
        isBeingKnockbacked = false;
    }

    public void MoveKnockback(Vector2 direction)
    {
        float finalSpeed;
        if (isGrounded)
        {
            finalSpeed = speed * 3f;
        }
        else
            finalSpeed = airSpeed * 3f;

        bool checkWall;
        float slopeDirection;
        if (direction.x > 0)
        {
            checkWall = wallRight;
            slopeDirection = 1f;
        }
        else
        {
            if (direction.x == 0)
                checkWall = false;
            else
                checkWall = wallLeft;

            slopeDirection = -1f;
        }

        if (hasPressedJumpButton == false && checkWall == false)
        {
            if (onSlopeLeft && direction.x != 0)
            {
                this.transform.Translate(new Vector2(direction.x, slopeDirection) * finalSpeed * Time.deltaTime);
            }
            else if (onSlopeRight && direction.x != 0)
            {
                this.transform.Translate(new Vector2(direction.x, -slopeDirection) * finalSpeed * Time.deltaTime);
            }
            else
            {
                this.transform.Translate(direction * finalSpeed * Time.deltaTime);
            }
        }
        else if (checkWall == false)
        {
            this.transform.Translate(direction * finalSpeed * Time.deltaTime);
        }
    }

    #endregion

    #region Particles
    void SpawnLandParticles()
    {
        /*GameObject particle = Instantiate(landParticles);
        particle.transform.position = transform.position;
        Destroy(particle.gameObject, 0.8f);*/
    }

    void SpawnWalkParticles(bool left)
    {
        /*if (Time.time > lastTimeSpawnedWalkParticles + 0.5f && isGrounded)
        {
            GameObject particle = Instantiate(walkParticles);
            particle.transform.position = transform.position;
            if (left)
                particle.GetComponent<SpriteRenderer>().flipX = true;
            Destroy(particle.gameObject, 2f);
            lastTimeSpawnedWalkParticles = Time.time;
        }*/
    }
    #endregion

    #region movement
    public void MoveRight()
    {
        if (wallSliding || wallJumped || blockRightMovement || isBeingKnockbacked || startDash)
            return;

        PlayWalkAnim();
        float finalSpeed;
        playerRigidbody.velocity = new Vector2(0, playerRigidbody.velocity.y);
        if (isGrounded)
        {
            finalSpeed = speed;
            SpawnWalkParticles(false);
            // runParticles.Play();
        }
        else
            finalSpeed = airSpeed;

        if (hasPressedJumpButton == false && wallRight == false)
        {
            if (onSlopeLeft)
            {
                this.transform.Translate(new Vector2(1f, 1f) * finalSpeed * Time.deltaTime);
            }
            else if (onSlopeRight)
            {
                this.transform.Translate(new Vector2(1f, -1f) * finalSpeed * Time.deltaTime);
            }
            else
            {
                this.transform.Translate(Vector2.right * finalSpeed * Time.deltaTime);
            }
        }
        else if (wallRight == false)
        {
            this.transform.Translate(Vector2.right * finalSpeed * Time.deltaTime);
        }
        spriteRenderer.flipX = false;
    }

    public void MoveLeft()
    {
        if (wallSliding || wallJumped || blockLeftMovement || isBeingKnockbacked || startDash)
            return;

        PlayWalkAnim();
        float finalSpeed;
        playerRigidbody.velocity = new Vector2(0, playerRigidbody.velocity.y);
        if (isGrounded)
        {
            finalSpeed = speed;
            SpawnWalkParticles(true);
            //runParticles.Play();
        }
        else
            finalSpeed = airSpeed;

        if (hasPressedJumpButton == false && wallLeft == false)
        {
            if (onSlopeRight)
            {
                this.transform.Translate(new Vector2(-1f, 1f) * finalSpeed * Time.deltaTime);
            }
            else if (onSlopeLeft)
            {
                this.transform.Translate(new Vector2(-1f, -1f) * finalSpeed * Time.deltaTime);
            }
            else
            {
                this.transform.Translate(Vector2.left * finalSpeed * Time.deltaTime);
            }
        }
        else if (wallLeft == false)
        {
            this.transform.Translate(Vector2.left * finalSpeed * Time.deltaTime);
        }
        spriteRenderer.flipX = true;
    }

    void PlayWalkAnim()
    {
        if (wallSliding == false && isGrounded)
            anim.SetBool("walk", true);
        else
            anim.SetBool("walk", false);
    }

    public void Dash()
    {
        if (canDash)
        {
            if (spriteRenderer.flipX == false && wallRight == false || spriteRenderer.flipX == true && wallLeft == false)
            {
                oldPos = transform.position;
                StartCoroutine(PerformDash());
            }
        }
    }
    #endregion

    void StartWallSlide()
    {
        if (playerRigidbody.velocity.y < 0.1f && isGrounded == false)
        {
            float wallSlideAxis = 0;
            if (spriteRenderer.flipX)
            {
                wallSlideAxis = -1f;
            }
            else
            {
                wallSlideAxis = 1f;
            }

            if ((wallLeft && !noWallSlideLeft) || (wallRight && !noWallSlideRight))
            {
                if (playerController.movementAxis == wallSlideAxis)
                    wallSliding = true;
            }

            if (wallSliding)
            {
                playerRigidbody.gravityScale = 0;
                isFalling = false;
                playerRigidbody.velocity = new Vector2(0, wallSlideSpeed);
            }
        }
    }

    private void FixedUpdate()
    {
        StartWallSlide();

        if (isFastFalling == false)
        {
            if (playerRigidbody.velocity.y < 0)
            {
                playerRigidbody.gravityScale = initialGravityScale * 1.3f;
            }
            else
            {
                playerRigidbody.gravityScale = initialGravityScale;
            }
        }
        else
        {
            if (isGrounded == false)
                playerRigidbody.gravityScale = initialGravityScale * 1.75f;
            else
                isFastFalling = false;
        }
    }
}
