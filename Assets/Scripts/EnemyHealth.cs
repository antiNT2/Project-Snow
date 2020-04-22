using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : DamagableEntity
{
    [SerializeField]
    ParticleSystem deathParticlesPrefab;
    [SerializeField]
    AudioClip hurtSound;

    public override void DoDamage(float damageAmount)
    {
        base.DoDamage(damageAmount);
        CustomFunctions.HitPause(true);
        CustomFunctions.PlaySound(hurtSound);
    }

    public override void Die()
    {
        GameObject particles = Instantiate(deathParticlesPrefab).gameObject;
        particles.transform.position = this.transform.position;
        Destroy(particles, 2.5f);
        base.Die();
    }
}
