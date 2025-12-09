/*******************************************************************
* COPYRIGHT       : 2024
* PROJECT         : 24-25 Game Development and Production.
* FILE NAME       : HookShot.cs
* DESCRIPTION     : A script to control the hookshot of the player ship.
*                    
* REVISION HISTORY:
* Date [YYYY/MM/DD] | Author | Comments
* ---------------------------------------------------------------------------
* 2025/01/27 | Ariana Kim | Created the script.
* 2025/01/29 | Ariana Kim | Added hook shooting and returning.
* 2025/01/30 | Ariana Kim | Implemented hookshot and asset.
* 2025/02/07 | Ariana Kim | Collect scrap items.
* 2025/05/06 | Ariana Kim | Adjusting conditions for state of hookshot.
/******************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(LineRenderer))]
public class HookShot : MonoBehaviour
{
    [SerializeField] public float Speed = 9f;
    [Tooltip("The distance the hook travels before retracting")]
    [SerializeField] public float Range = 10f;

    //Wwise audio fields
    [SerializeField] private AK.Wwise.Event HookshotReturnSFX;//Hook traveling back to the player ship sound
    [SerializeField] private AK.Wwise.Event HookshotReadySFX;//Hookshot returned to player
    [SerializeField] private AK.Wwise.Event HookshotMissSFX;//Hook shot whiffed all objects
    [SerializeField] private AK.Wwise.Event HookshotSuccessSFX;//Scrap Collected sound
    [SerializeField] private AK.Wwise.Event HookshotFailSFX;//No scrap collected sound


    private GameObject _player; // Player ship object
    private Rigidbody _rbPlayer; // Rigidbody of the player ship
    private Rigidbody _rb; // Rigidbody of the hook
    private LineRenderer _lineRenderer; // Rope of the hook

    private Vector3 _initialPos; // Starting position of the hook
    [SerializeField] private Transform _firePoint; // Firing point of the hook

    void OnEnable()
    {
        _player = GameObject.FindObjectOfType<ShipMovement>().gameObject;
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.SetPosition(0, _player.transform.position);
        _lineRenderer.SetPosition(1, _player.transform.position);
        _rbPlayer = _player.GetComponent<Rigidbody>();
        _rb = GetComponent<Rigidbody>();
        if(_firePoint == null)
        {
            _firePoint = _player.transform;
        }

        // Just started shooting the hook
        _rbPlayer.freezeRotation = true;
        _initialPos = new Vector3(_firePoint.position.x, _player.transform.position.y, _firePoint.position.z); // Starting position when the hook is shot
        _lineRenderer.SetPosition(0, _initialPos);
        transform.position = _initialPos;
        transform.rotation = _player.transform.rotation;
        _rb.velocity = _rbPlayer.velocity + transform.forward * Speed;
    }

    // Update is called once per frame
    void Update()
    {
        // Update rope of the hook while hook is shooting out
        _lineRenderer.SetPosition(1, transform.position);

        // Return hook if exceeds the designated range
        if(!MinigameManager.s_IsReturning && Vector3.Distance(_initialPos, transform.position) > Range)
        {
            MinigameManager.s_IsReturning = true;
            _rb.velocity *= -1.5f;
            if(HookshotReturnSFX != null)
                HookshotReturnSFX.Post(gameObject);
            if(HookshotMissSFX != null)
                HookshotMissSFX.Post(gameObject);
        }
        else if(Vector3.Distance(_initialPos, transform.position) > Range + 10f)
        {
            // When hook is returning but exceeds range, deactivate hook
            MinigameManager.s_IsShooting = false;
            MinigameManager.s_IsReturning = false;
            if(MinigameManager.s_IsCollecting)
            {
                MinigameManager.s_Instance.CollectScrap();
                MinigameManager.s_IsCollecting = false;
            }

            // Allow player ship to rotate
            _rbPlayer.constraints = RigidbodyConstraints.None;
            _rbPlayer.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            if(MinigameManager.s_Count == 0) // No lives
            {
                MinigameManager.s_Instance.EndMinigame();
            }
            else if(MinigameManager.s_Count == 1 && !MinigameManager.s_IsAdditionalScrap && MinigameManager.s_ScrapCollected == 2) // Have one life left but collected all the remaining scraps
            {
                MinigameManager.s_Instance.EndMinigame();
            }
            this.gameObject.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // When hook returns to player ship, deactivate hook
        if(MinigameManager.s_IsReturning && other.GetComponent<ShipMovement>() != null)
        {
            MinigameManager.s_IsShooting = false;
            MinigameManager.s_IsReturning = false;
            //if(HookshotReadySFX != null)
            //    HookshotReadySFX.Post(gameObject);

            AkSoundEngine.StopPlayingID(1294883058);
            AkSoundEngine.PostEvent("Mini_HookshotReady", gameObject);
            AkSoundEngine.PostEvent("Mini_HookShotRealing_Stop", gameObject);


            if(MinigameManager.s_IsCollecting)
            {
                MinigameManager.s_Instance.CollectScrap();
                MinigameManager.s_IsCollecting = false;
                if (HookshotSuccessSFX != null) //Scrap Collected
                    HookshotSuccessSFX.Post(gameObject);
            }
            else //Hook was returning empty
            {
                if (HookshotFailSFX != null) //Failure.jpg
                    HookshotFailSFX.Post(gameObject);
            }
            // Allow player ship to rotate
            _rbPlayer.constraints = RigidbodyConstraints.None;
            _rbPlayer.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            if(MinigameManager.s_Count == 0) // No lives
            {
                MinigameManager.s_Instance.EndMinigame();
            }
            else if(MinigameManager.s_Count == 1 && !MinigameManager.s_IsAdditionalScrap && MinigameManager.s_ScrapCollected == 2) // Have one life left but collected all the remaining scraps
            {
                MinigameManager.s_Instance.EndMinigame();
            }
            this.gameObject.SetActive(false);
        }
    }
}
