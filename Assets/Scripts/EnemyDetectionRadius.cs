using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDetectionRadius : MonoBehaviour
{
    List<DamagableEntity> closeEntities = new List<DamagableEntity>();
    PlayerAttack playerAttack;
    [SerializeField]
    bool isGlobalRadius;

    private void Start()
    {
        playerAttack = PlayerAttack.instance;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        //print(collision.gameObject.name);
        if (collision.gameObject.GetComponent<DamagableEntity>() != null)
        {
            closeEntities.Add(collision.gameObject.GetComponent<DamagableEntity>());
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.GetComponent<DamagableEntity>() != null)
        {
            closeEntities.Remove(collision.gameObject.GetComponent<DamagableEntity>());
        }
    }

    private void Update()
    {
        if (isGlobalRadius)
            playerAttack.closeEntities = closeEntities;
        else
            playerAttack.entitiesToDamage = closeEntities;
    }
}
