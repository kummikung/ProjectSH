using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public PlayerData playerData;
    #region Componant
    [Header("REFERECNE PARAMETER")]
    public Rigidbody2D playerRigibody;
    public Transform checkground;
    public LayerMask groundLayer;
    public SpriteRenderer PlayerSprite;
    public PhysicsMaterial2D Smooth;
    #endregion;

    #region Status Componant

    private float speed { get; set; }
    private float jumpPower { get; set; }
    private bool isFacingRight { get; set; }
    private bool isJumping { get; set; }
    private bool isDashing { get; set; }
    private float horizontal { get; set; }

    private float Groundfar = 0.2f;
    private float Accelration { get; set; }
    private float Decceleration { get; set; }

    private float LastGroundedTime { get; set; }
    private float LastJumpTime { get; set; }
    private float JumpBufferTime { get; set; }
    private float JumpCoyoteTime { get; set; }
    private float velPower { get; set; }

    private bool JumpInputHold = true;

    private Vector2 move { get; set; }

    //Dash
    private int _dashesLeft{ get; set; }
    private bool _dashRefilling { get; set; }
    private Vector2 playerDir { get; set; }
    private Vector2 _lastDashDir { get; set; }
    private bool _isDashAttacking { get; set; }
    public float LastPressedDashTime { get; private set; }
    #endregion

    private void Awake()
    {
        Accelration = playerData.runAcceleration;
        Decceleration = playerData.runDecceleration;
        speed = playerData.runMaxSpeed;
        velPower = playerData.velPower;
        jumpPower = playerData.jumpHeight;
        JumpBufferTime = playerData.jumpInputBufferTime;
        JumpCoyoteTime = playerData.coyoteTime;
    }

    private void Start()
    {
        isFacingRight = true;
        playerDir = Vector2.zero;
    }

    private void Update()
    {
        CheckFilp();
    }

    private void FixedUpdate()
    {
        run();
        Onjump();
        OnJumpUp();
        //Friction();
        JumpGravity();
    }
    #region GENERAL METHOAD
    /*public void SetGravityScale(float scale)
    {
        playerRigibody.gravityScale = scale;
    }*/

    private void Sleep(float duration)
    {
        //Method used so we don't need to call StartCoroutine everywhere
        //nameof() notation means we don't need to input a string directly.
        //Removes chance of spelling mistakes and will improve error messages if any
        StartCoroutine(nameof(PerformSleep), duration);
    }

    private IEnumerator PerformSleep(float duration)
    {
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(duration); //Must be Realtime since timeScale with be 0 
        Time.timeScale = 1;
    }
    #endregion

    #region CHECK METHOAD

    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(checkground.position, Groundfar, groundLayer);
    }

    private bool CanDash()
    {
        if (!isDashing && _dashesLeft < playerData.dashAmount && LastGroundedTime > 0 && !_dashRefilling)
        {
            StartCoroutine(nameof(RefillDash), 1);
        }
        return _dashesLeft > 0;
    }

    private void Filp()
    {
        Vector3 localScale = transform.localScale;
        localScale.x *= horizontal;
        transform.localScale = localScale;
        isFacingRight = !isFacingRight;
    }

    private void CheckFilp()
    {
        if (!isFacingRight && horizontal > 0f)
        {
            horizontal = 1f;
            Filp();
            PlayerSprite.color = Color.yellow;

        }
        else if (isFacingRight && horizontal < 0f)
        {
            horizontal = -1f;
            Filp();
            PlayerSprite.color = Color.red;
        }
    }
    #endregion

    #region RUN,JUMP,DASH
    private void run()
    {
        float targetspeed = horizontal * speed;
        float speedDif = targetspeed - playerRigibody.velocity.x;
        float accelRate = (Mathf.Abs(targetspeed) > 0.01f) ? Accelration : Decceleration;
        float movement = Mathf.Pow(Mathf.Abs(speedDif) * accelRate, velPower) * Mathf.Sign(speedDif);
        playerRigibody.AddForce(movement * Vector2.right);
        //Debug.Log("Movement Active");
    }
    private void Friction()
    {
        if (LastGroundedTime > 0 && Mathf.Abs(horizontal) < 1f)
        {
            float amount = Mathf.Min(Mathf.Abs(playerRigibody.velocity.x), Mathf.Abs(Smooth.friction));
            amount *= Mathf.Sign(playerRigibody.velocity.x);
            playerRigibody.AddForce(Vector2.right * -amount, ForceMode2D.Impulse);
            //Debug.Log("Friction Active");
        }
    }

    private void Jumping()
    {
        if (LastGroundedTime > 0 && LastJumpTime > 0 && !isJumping)
            playerRigibody.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
        LastGroundedTime = 0;
        LastJumpTime = 0;
        if (playerRigibody.velocity.y <= 0)
        {
            jumpPower -= playerRigibody.velocity.y;
        }
        isJumping = true;
    }
    private void Onjump()
    {
        LastJumpTime = JumpBufferTime;
        LastGroundedTime = JumpCoyoteTime;
    }
    private void OnJumpUp()
    {
        if (playerRigibody.velocity.y > 0 && isJumping)
        {
            if (JumpInputHold)
            {
                playerRigibody.AddForce(Vector2.down * playerRigibody.velocity.y * (1 - playerData.jumpCutGravityMult), ForceMode2D.Impulse);
               // Debug.Log("isOnJumpUp Active");
                IsHoldingInputTime(0.1f);
                if (!JumpInputHold)
                {
                    IsHoldingInputTime(0.5f);
                }
            }
            return;
        }

    }
    IEnumerator IsHoldingInputTime(float time)
    {
        yield return new WaitForSeconds(time);
        JumpInputHold = !JumpInputHold;
    }
    private void JumpGravity()
    {
        if (isJumping)
        {
            playerRigibody.gravityScale = 4f; //playerData.gravityScale * playerData.fallGravityMult; 
        }
        else if (!isJumping)
        {
            playerRigibody.gravityScale = 6f;//playerData.gravityScale
        }
    }
    private void Dashing()
    {
        if (CanDash())
        {

            //If not direction pressed, dash forward
            if (playerDir != Vector2.zero)
                _lastDashDir = playerDir;
            else
                _lastDashDir = isFacingRight ? Vector2.right : Vector2.left;

            isDashing = true;
            isJumping = false;
            StartCoroutine(nameof(StartDash), _lastDashDir);
        }
    }

    private IEnumerator StartDash(Vector2 dir) 
     {
         LastGroundedTime = 0;
         LastPressedDashTime = 0;

         float startTime = Time.time;

         _dashesLeft--;
         _isDashAttacking = true;

         playerRigibody.gravityScale = 4f;

         //We keep the player's velocity at the dash speed during the "attack" phase (in celeste the first 0.15s)
         while (Time.time - startTime <= playerData.dashAttackTime)
         {
             playerRigibody.velocity = dir.normalized * playerData.dashSpeed;
             //Pauses the loop until the next frame, creating something of a Update loop. 
             //This is a cleaner implementation opposed to multiple timers and this coroutine approach is actually what is used in Celeste :D
             yield return null;
         }

         startTime = Time.time;

         _isDashAttacking = false;

         //Begins the "end" of our dash where we return some control to the player but still limit run acceleration (see Update() and Run())
         playerRigibody.gravityScale = 6f;
         playerRigibody.velocity = playerData.dashEndSpeed * dir.normalized;

         while (Time.time - startTime <= playerData.dashEndTime)
         {
             yield return null;
         }
        //Dash over
        isDashing = false;
    }

     //Short period before the player is able to dash again
     private IEnumerator RefillDash(int amount)
     {
         //SHoet cooldown, so we can't constantly dash along the ground, again this is the implementation in Celeste, feel free to change it up
         _dashRefilling = true;
        _dashesLeft = Mathf.Min(playerData.dashAmount, _dashesLeft + 1);
        yield return new WaitForSeconds(playerData.dashRefillTime);
        _dashRefilling = false;
     }
     #endregion


    #region MOVEMENT PHASE

    public void Move(InputAction.CallbackContext context)
    {
        horizontal = context.ReadValue<Vector2>().x;
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed && !isJumping && IsGrounded())
        {
            Jumping();
        }
        if (context.canceled && isJumping)
        {
            playerRigibody.velocity = new Vector2(playerRigibody.velocity.x, playerRigibody.velocity.y * playerRigibody.mass);
            isJumping = false;
        }
    }

    public void Dash(InputAction.CallbackContext context)
    {
        if(context.performed && CanDash())
        {
            Dashing();
            Debug.Log("Dash is Active");
        }

    }

    #endregion
}