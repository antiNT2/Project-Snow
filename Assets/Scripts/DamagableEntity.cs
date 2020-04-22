using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class DamagableEntity : MonoBehaviour
{
    public float currentHealth;
    public float maxHealth;

    public virtual void DoDamage(float damageAmount)
    {
        currentHealth -= damageAmount;

        if (currentHealth <= 0)
            Die();
    }

    public virtual void Die()
    {
        this.gameObject.SetActive(false);
    }
}
