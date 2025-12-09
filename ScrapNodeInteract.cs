/*******************************************************************
* COPYRIGHT       : 2024
* PROJECT         : 24-25 Game Development and Production.
* FILE NAME       : ScrapNodeInteract.cs
* DESCRIPTION     : A script attached to the scrap nodes to listen to the player interacting with it.
*                    
* REVISION HISTORY:
* Date [YYYY/MM/DD] | Author | Comments
* ---------------------------------------------------------------------------
* 2025/01/27 | Ariana Kim | Created the script.
* 2025/03/11 | Ariana Kim | Updated scrap node interaction with fishing minigame.
* 2025/03/12 | Ariana Kim | Spawn collectible scrap near scrap node
* 2025/05/08 | Ariana Kim | Delay start of minigame for the animation.
/******************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrapNodeInteract : MonoBehaviour
{
    private bool _inTrigger; // Player ship is in trigger of scrap node
    private GameObject _player; // Player ship object
    private Rigidbody _rbPlayer; // Player ship rigidbody
    private Animator _animator; // Animator for the scrap node
    private Quaternion _rotation = Quaternion.AngleAxis(90, Vector3.right); // Rotation for instantiating scrap
    [SerializeField] private float _spawnRadius = 5f;
    private float _spawnTimer = 5.0f;
    public GameObject AnimationGO;
    public GameObject SpaceUI; 
    [SerializeField] AK.Wwise.Event Ambience_Stop;

    // Start is called before the first frame update
    void Start()
    {
        _player = GameObject.FindObjectOfType<ShipMovement>().gameObject;
        _rbPlayer = _player.GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();

        SpaceUI = GameObject.FindWithTag("UI");
    }


   
    // Update is called once per frame
    void Update()
    {
        
        // Check if interacting with scrap node
        if (Input.GetKeyDown(KeyCode.E) && _inTrigger)
        {
            GameObject minigameAnimation = Instantiate(AnimationGO, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
            minigameAnimation.transform.SetParent(SpaceUI.transform, false);

            Destroy(minigameAnimation, 2f);

            if (Ambience_Stop != null)
                Ambience_Stop.Post(gameObject);

            StartCoroutine(TransitionToMinigame()); // Play minigame after animation has finished
        }

        // Modify the animation of scrap nodes during the minigame
        if (MinigameManager.s_IsPlaying)
        {
            _animator.SetBool("Pause", true); // Pause the animation of other scrap nodes
        }
        else
        {
            _animator.SetBool("Pause", false); // Restart the animation
        }

        // Spawn floating collectible scrap
        _spawnTimer -= Time.deltaTime;
        if(_spawnTimer <= 0 && !MinigameManager.s_IsPlaying)
        {
            int randomScrap = Random.Range(0, MinigameManager.s_Scraps.Count); // Randomly select a scrap
            Vector3 randomOffset = new Vector3(Random.Range(-_spawnRadius, _spawnRadius + 1), 0, Random.Range(-_spawnRadius, _spawnRadius + 1)); // Random offset for scrap position
            GameObject scrap = Instantiate(MinigameManager.s_Scraps[randomScrap], transform.position + randomOffset, _rotation); // Spawn scrap in random location near scrap node

            _spawnTimer = Random.Range(50f, 70f); // Randomly spawn scrap after 50 to 70 seconds
        }
    }

    IEnumerator TransitionToMinigame()
    {
        _player.transform.position = new Vector3(_player.transform.position.x, 10f, _player.transform.position.z); // Move player ship above scene level
        _rbPlayer.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; // Freeze player ship
        MinigameManager.s_InteractText.SetActive(false); // Hide interaction text
        yield return new WaitForSeconds(1.5f); // Wait until animation is done
        MinigameManager.s_Instance.StartMinigame(); // Play fishing minigame
        Destroy(this.gameObject); // Destroy scrap node
    }

    // When near scrap node, display text informing player to interact by pressing E
    void OnTriggerEnter(Collider _player)
    {
        if(_player.GetComponent<ShipMovement>() != null && !MinigameManager.s_IsPlaying)
        {
            _inTrigger = true;
            MinigameManager.s_InteractText.SetActive(true);
        }
    }

    // When away from scrap node, hide the interaction text
    void OnTriggerExit(Collider _player)
    {
        if(_player.GetComponent<ShipMovement>() != null && !MinigameManager.s_IsPlaying)
        {
            _inTrigger = false;
            MinigameManager.s_InteractText?.SetActive(false);
        }
    }
}
