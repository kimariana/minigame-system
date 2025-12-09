/*******************************************************************
* COPYRIGHT       : 2024
* PROJECT         : 24-25 Game Development and Production.
* FILE NAME       : CollectibleScrap.cs
* DESCRIPTION     : A script for the collectible scrap out in space.
*                    
* REVISION HISTORY:
* Date [YYYY/MM/DD] | Author | Comments
* ---------------------------------------------------------------------------
* 2025/02/07 | Ariana Kim | Created the script.
* 2025/03/07 | Ariana Kim | Collectible scrap movement.
* 2025/03/11 | Ariana Kim | Updated scrap movement, and scrap only collectible by hookshot or player ship but not both.
* 2025/04/29 | Ariana Kim | Changed scrap movement, added time to live.
* 2025/05/08 | Ariana Kim | Allow collecting scrap when returning hookshot or when static in space.
/******************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectibleScrap : MonoBehaviour
{
    [SerializeField] public string Name; // Name of collectible scrap
    public bool IsHighValue = false;
    public bool IsStaticMovement = false;

    private Rigidbody _rb;

    [SerializeField] public float Speed = 1f;
    private float _maxSpeed = 0.5f;
    private float _minSpeed = 0.05f;

    [SerializeField] private float _rotationSpeed = 1f;
    private float _maxRotationSpeed = 2.5f;

    private Vector3 _movementDir = new Vector3(0, 0, 0);

    private float _timeToLive = 120f;

    //Wwise event calls
    [SerializeField] private AK.Wwise.Event HitObstacleSFX;
    [SerializeField] private AK.Wwise.Event CollectFloatingScrapSFX;

    // Start is called before the first frame update
    void Start()
    {
        // Random movement of floating collectible scrap
        Speed = Random.Range(_minSpeed, _maxSpeed + 1);
        _rotationSpeed = Random.Range(-_maxRotationSpeed, _maxRotationSpeed + 1);
        float xDir = Random.Range(-1, 2);
        float zDir = Random.Range(-1, 2);
        while (xDir == 0 && zDir == 0) // Ensures floating collectible scrap will be moving
        {
            xDir = Random.Range(-1, 2);
            zDir = Random.Range(-1, 2);
        }
        _movementDir = new Vector3(xDir, 0, zDir);
        _rb = transform.GetComponent<Rigidbody>();
        _rb.velocity = _movementDir * Speed;
    }

    // Update is called once per frame
    void Update()
    {
        if(!IsStaticMovement) // Rotation of the scrap
        {
            this.transform.Rotate(new Vector3(0, 0, 1) * _rotationSpeed * Time.deltaTime);
        }
        else
        {
            _rb.isKinematic = true;
        }

        // Destroy scrap if a certain amount of time has passed
        _timeToLive -= Time.deltaTime;
        if(_timeToLive < 0)
        {
            Destroy(this.gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // When hook hits scrap, collect it
        if(MinigameManager.s_IsPlaying && !MinigameManager.s_IsCollecting
            && other.GetComponent<HookShot>() != null && IsStaticMovement)
        {

            Rigidbody rbOther = other.GetComponent<Rigidbody>();
            if (MinigameManager.s_IsShooting && !MinigameManager.s_IsReturning && Name == "Obstacle") // Scrap is an obstacle that returns the hook without being added to inventory
            {
                FollowCamera followCam;
                if(Camera.main.gameObject.TryGetComponent<FollowCamera>(out followCam))
                    followCam.CreateCameraShake(0.5f);
                if(HitObstacleSFX != null) //Plays rock collision sound
                    HitObstacleSFX.Post(gameObject);
                AkSoundEngine.PostEvent("Mini_HittingRock", gameObject);
                rbOther.velocity *= -1.5f; // Return hook
                AkSoundEngine.PostEvent("Mini_HookshotRealing", gameObject);
            }
            else if(Name != "Obstacle") // Scrap returns with the hook
            {
                transform.position = other.transform.position;
                transform.parent = other.transform;
                MinigameManager.s_IsCollecting = true;
                if(MinigameManager.s_IsShooting && !MinigameManager.s_IsReturning)
                {
                    rbOther.velocity *= -1.5f; // Return hook
                    AkSoundEngine.PostEvent("Mini_HookshotRealing", gameObject);
                }
            }
            MinigameManager.s_IsReturning = true;
        }
        
        // When ship runs over scrap, collect it
        if(!MinigameManager.s_IsPlaying && other.GetComponent<ShipMovement>() != null)
        {
            transform.parent = MinigameManager.s_Instance.gameObject.transform;
            MinigameManager.s_Instance.CollectScrap();
            if (CollectFloatingScrapSFX != null) //plays scrap collected sound
                CollectFloatingScrapSFX.Post(gameObject);
            AkSoundEngine.PostEvent("Mini_Ball_Correct", gameObject);
        }
    }
}
