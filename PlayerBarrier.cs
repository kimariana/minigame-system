/*******************************************************************
* COPYRIGHT       : 2024
* PROJECT         : 24-25 Game Development and Production.
* FILE NAME       : Barrier.cs
* DESCRIPTION     : A script to control the barrier for the fishing minigame.
*                    
* REVISION HISTORY:
* Date [YYYY/MM/DD] | Author | Comments
* ---------------------------------------------------------------------------
* 2025/02/27 | Ariana Kim | Created the script.
* 2025/03/04 | Ariana Kim | Preventing barrier from interfering with player hookshot.
* 2025/03/06 | Ariana Kim | Added barrier effect and proper destruction of intruders and projectiles.
* 2025/03/07 | Ariana Kim | Adjusted the radius and barrier effect range.
* 2025/03/10 | Ariana Kim | Modified enemies to explode when in barrier.
/******************************************************************/

using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerBarrier : MonoBehaviour
{
    private GameObject _player; // Player ship object
    private SphereCollider _barrierCollider; // Barrier collider object
    [SerializeField] private Transform _barrierEffect; // Barrier wave effect
    [SerializeField] public float Range = 12f;
    [SerializeField] public float Speed = 10f;
    private bool _isBarrierInitialized = false; // True when the barrier has been initialized
    private bool _isBarrierComplete = false; // True when the barrier has extended to the maximum range

    // Start is called before the first frame update
    void Start()
    {
        _player = GameObject.FindObjectOfType<ShipMovement>().gameObject;
        _barrierCollider = GetComponent<SphereCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        if(MinigameManager.s_IsPlaying)
        {
            if(!_isBarrierInitialized)
            {
                transform.position = new Vector3(_player.transform.position.x, 0f, _player.transform.position.z); ; // Sets the barrier's position to the player ship
                _isBarrierInitialized = true;
            }
            if(_barrierCollider.radius < Range) // Expand barrier until exceeding designated range
            {
                _barrierCollider.radius += Time.deltaTime * Speed;
                _barrierEffect.localScale = new Vector3(_barrierCollider.radius * 2.1f, _barrierCollider.radius * 2.1f, 0);
            }
            else if(_barrierCollider.radius >= Range)
            {
                if(!_isBarrierComplete)
                {
                    _isBarrierComplete = true;
                    MinigameManager.s_IsBarrierComplete = true;
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.gameObject != _player && other.GetComponent<Health>() != null) // Objects with health that are not the player ship
        {
            // Explode enemies
            //if(other.CompareTag("Enemy"))
            Health enemyHealth = other.GetComponent<Health>();
            float maxHealth = enemyHealth.GetMaxHealth();
            enemyHealth.ChangeHealth(-maxHealth, "Player");
            Destroy(other.gameObject); // Erase intruders
        }
        else if(other.GetComponent<ProjectileScript>() != null) // Projectiles that can harm the player
        {
            Destroy(other.gameObject); // Erase projectiles
        }
        else if(other.GetComponent<CollectibleScrap>() != null) // Collectible scrap not spawned from the minigame
        {
            CollectibleScrap scrap = other.GetComponent<CollectibleScrap>();
            if(!scrap.IsStaticMovement)
            {
                Destroy(other.gameObject); // Erase intruding scrap
            }
        }
        else if(other.GetComponent<WeaponPowerup>()) // Powerups that spawned from destroyed enemies
        {
            Destroy(other.gameObject); // Erase powerups
        }
    }

    // Resets the barrier once the minigame ends
    public void ResetBarrier()
    {
        _isBarrierInitialized = false;
        _isBarrierComplete = false;
        MinigameManager.s_IsBarrierComplete = false;
        _barrierCollider.radius = 1f;
        _barrierEffect.localScale = new Vector3(2f, 2f, 0f);
    }
}
