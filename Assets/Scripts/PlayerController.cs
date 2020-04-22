using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    PlayerInput playerInput;
    PlayerMotor playerMotor;
    Rigidbody2D playerRigidbody;
    Animator anim;

    EnemyHealth[] enemiesDebug;

    public float movementAxis = 0; // 1 for right / -1 for left / 0 for nothing
    float lastMovementAxis;

    private void Start()
    {
        playerMotor = GetComponent<PlayerMotor>();
        playerRigidbody = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        DebugEnemies();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
            DebugEnemies();
        if (Input.GetKeyDown(KeyCode.Mouse2))
            Debug.Break();

        if (CanMove())
        {
            if (HasFullControl())
                MoveAccordingToAxis(movementAxis);
            else if (playerMotor.isGrounded == false && movementAxis == 0)
                MoveAccordingToAxis(lastMovementAxis);


        }
        if (movementAxis != 1 && movementAxis != -1)
        {
            anim.SetBool("walk", false);
        }

        anim.SetBool("slide", playerMotor.wallSliding);

    }

    private void LateUpdate()
    {
        if (HasFullControl())
            lastMovementAxis = movementAxis;
    }

    void MoveAccordingToAxis(float axis)
    {
        if (axis > 0)
        {
            playerMotor.MoveRight();
        }
        else if (axis < 0)
        {
            playerMotor.MoveLeft();
        }
    }

    bool HasFullControl()
    {
        if (playerMotor.isGrounded || movementAxis != 0 || playerMotor.wallSliding || playerMotor.wallJumped)
            return true;

        return false;
    }

    #region PlayerInputMethods
    void OnMove(InputValue value)
    {
        //print("Isse");
        movementAxis = value.Get<Vector2>().x;
        if (value.Get<Vector2>().y < 0)
            playerMotor.FastFall();
    }

    void OnJumpButton()
    {
        if (CanJump())
        {
            playerMotor.Jump();
        }
    }
    #endregion

    public bool CanMove()
    {
        if (Mathf.Abs(playerRigidbody.velocity.x) > 12f)
            return false;

        return true;
    }

    public bool CanJump()
    {
        if ((Mathf.Abs(playerRigidbody.velocity.x) > 12f && playerMotor.isGrounded == false))
            return false;

        return true;
    }

    void DebugReload()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void DebugEnemies()
    {
        if (enemiesDebug == null)
            enemiesDebug = FindObjectsOfType<EnemyHealth>();

        for (int i = 0; i < enemiesDebug.Length; i++)
        {
            enemiesDebug[i].gameObject.SetActive(true);
            enemiesDebug[i].transform.position = new Vector2(Random.Range(-24f, 24f), Random.Range(-9.5f, 22f));
        }
    }
}
