using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PlayerAttack : MonoBehaviour
{
    public static PlayerAttack instance;
    Rigidbody2D playerRigidbody;
    SpriteRenderer spriteRenderer;
    [SerializeField]
    GameObject slashObject;
    [SerializeField]
    AudioClip attackDashSound;
    PlayerMotor playerMotor;
    public List<DamagableEntity> closeEntities = new List<DamagableEntity>();
    public List<DamagableEntity> entitiesToDamage = new List<DamagableEntity>();

    bool hasAttackedWithThisSlash;
    bool attackDelayNotPassed;
    Coroutine attackDelayCoroutine;

    Vector2 lastAttackForceApplied;

    public AttackState currentAttackState;
    public enum AttackState
    {
        None,
        AirDash,
        GroundDash,
        Bounce
    }

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerMotor = GetComponent<PlayerMotor>();
        playerRigidbody = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {     
        DoDamage();

        if (currentAttackState == AttackState.Bounce && playerMotor.isGrounded)
        {
            currentAttackState = AttackState.None;
        }
        if (currentAttackState == AttackState.GroundDash && Mathf.Abs(playerRigidbody.velocity.x) < 1f)
        {
            currentAttackState = AttackState.None;
        }
    }

    IEnumerator AttackDelay()
    {
        attackDelayNotPassed = true;
        yield return new WaitForSeconds(.1f);
        attackDelayNotPassed = false;
    }

    public void Attack()
    {
        if (attackDelayNotPassed == false && closeEntities.Count > 0)
        {
            if (playerMotor.isGrounded)
                currentAttackState = AttackState.GroundDash;
            else
                currentAttackState = AttackState.AirDash;

            hasAttackedWithThisSlash = false;
            playerRigidbody.velocity = Vector2.zero;
            playerRigidbody.AddForce(GetForceVectorTowardsClosestEntity() * 95f, ForceMode2D.Impulse);
            lastAttackForceApplied = GetForceVectorTowardsClosestEntity();
            CustomFunctions.PlaySound(attackDashSound);

            if (attackDelayCoroutine != null)
                StopCoroutine(attackDelayCoroutine);
            attackDelayCoroutine = StartCoroutine(AttackDelay());
        }
    }

    void DoDamage()
    {
        if ((currentAttackState == AttackState.GroundDash || currentAttackState == AttackState.AirDash) && hasAttackedWithThisSlash == false)
        {
            for (int i = 0; i < entitiesToDamage.Count; i++)
            {
                entitiesToDamage[i].DoDamage(1f);
                hasAttackedWithThisSlash = true;
            }

            if (hasAttackedWithThisSlash)
            {
                playerRigidbody.velocity = Vector2.zero;
                playerRigidbody.AddForce((GetForceVectorTowardsClosestEntity() + (Vector2.down * 1f)) * -25f, ForceMode2D.Impulse);
                currentAttackState = AttackState.Bounce;
                CustomFunctions.VibrateController();
                //CustomFunctions.ZoomCamera(0.2f);
                playerMotor.airJumpsLeft++;
            }
        }
    }

    DamagableEntity GetClosestEntity()
    {
        if (closeEntities.Count == 0)
            return null;

        List<float> distances = new List<float>();

        for (int i = 0; i < closeEntities.Count; i++)
        {
            distances.Add(Vector2.Distance(this.transform.position, closeEntities[i].transform.position));
        }

        return closeEntities[distances.IndexOf(distances.Min())];
    }

    Vector2 GetForceVectorTowardsClosestEntity()
    {
        if (GetClosestEntity() == null)
        {
            return Vector2.zero;
        }

        Transform closestEntity = GetClosestEntity().transform;

        Vector2 output = (closestEntity.transform.position - this.transform.position).normalized;

        return output;
    }

    void RotateSlash()
    {
        Vector3 vectorToTarget = transform.position - GetClosestEntity().transform.position;
        float angle = Mathf.Atan2(vectorToTarget.y, vectorToTarget.x) * Mathf.Rad2Deg;
        Quaternion q = Quaternion.AngleAxis(angle - 54f, Vector3.forward);
        slashObject.transform.rotation = Quaternion.Slerp(slashObject.transform.rotation, q, Time.deltaTime * 15f);
    }
}
