/*******************************************************************
* COPYRIGHT       : 2024
* PROJECT         : 24-25 Game Development and Production.
* FILE NAME       : MinigameManager.cs
* DESCRIPTION     : A script to manage the fishing minigame that occurs through the scrap nodes.
*                    
* REVISION HISTORY:
* Date [YYYY/MM/DD] | Author | Comments
* ---------------------------------------------------------------------------
* 2025/01/28 | Ariana Kim | Created the script.
* 2025/01/29 | Ariana Kim | Freeze position and rotation of the player ship depending on status of the fishing game.
* 2025/02/03 | Ariana Kim | Spawn objects around player ship with temporary assets for rings and scrap.
* 2025/02/06 | Ariana Kim | Rotation of ring and scraps, Display hook charges.
* 2025/02/07 | Ariana Kim | Collect scrap and spawn debris.
* 2025/02/11 | Ariana Kim | Added Minigame overlay.
* 2025/03/04 | Ariana Kim | Implemented barrier that destroys enemies during the minigame.
* 2025/03/07 | Ariana Kim | Prevent movement for collectible scraps spawned for the minigame.
* 2025/04/15 | Ariana Kim | Implemented hookshot timer to scrap minigame.
* 2025/04/16 | Ariana Kim | Added particle effects to timer.
* 2025/04/22 | Ariana Kim | Adjusted particle effects to timer.
* 2025/04/25 | Ariana Kim | Updated so that no links are required for MinigameManager to be added to any scene.
/******************************************************************/

using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Xml;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
//using static UnityEditor.Progress;

public class MinigameManager : MonoBehaviour
{
    [SerializeField] public float Timer = 6f; // Amount of time player has to shoot hook before losing one hookshot charge/ammo
    [SerializeField] public float RotateInRSpeed = 5f; // Speed of rotation for inner ring
    [SerializeField] public float RotateOutRSpeed = 7f; // Speed of rotation for outer ring
    [SerializeField] public float RotateInRMaxSpeed = 60; // Max speed of rotation for inner ring
    [SerializeField] public float RotateOutRMaxSpeed = 60; // Max speed of rotation for outer ring
    [SerializeField] public float Direction = 1f; // Direction of ring rotation, 1: CW and -1: CCW
    [SerializeField] public int ScrapInRChance = 3; // Chance out of 10 for scrap to spawn in inner ring
    [SerializeField] public int ScrapOutRChance = 5; // Chance out of 10 for scrap to spawn in outer ring

    //Wwise audio fields
    [SerializeField] private AK.Wwise.Event PlayMusic; //Starts Music
    [SerializeField] private AK.Wwise.Event StopMusic; //Stops Music
    [SerializeField] private AK.Wwise.Event HookshotFired;//Hook initial firing sound
    [SerializeField] private AK.Wwise.Event PortalOpen; //Player interacts with portal
    [SerializeField] private AK.Wwise.Event PortalClose; //Minigame Completes and portal explodes
    private string Minigame = "Shots_Remaining"; //The state group for the music (wwise stuff)



    private static MinigameManager s_instance; // Singleton for MinigameManager
    public static MinigameManager s_Instance { get { return s_instance; } }
    public static GameObject s_InteractText { get { return s_Instance._interactText; } set { s_Instance._interactText = value; } }

    private bool _isPlaying = false; // Fishing minigame is playing
    public static bool s_IsPlaying { get { return s_Instance._isPlaying; } set { s_Instance._isPlaying = value; } }
    private bool _isShooting = false; // Hook is shooting
    public static bool s_IsShooting { get { return s_Instance._isShooting; } set { s_Instance._isShooting = value; } }
    private bool _isReturning = false; // Hook is returning (if true, hook is considered to still be shooting)
    public static bool s_IsReturning { get { return s_Instance._isReturning; } set { s_Instance._isReturning = value; } }
    private bool _isCollecting = false; // Hook is collecting scrap (if true, hook is considered to still be shooting and returning)
    public static bool s_IsCollecting { get { return s_Instance._isCollecting; } set { s_Instance._isCollecting = value; } }

    private bool _isBarrierComplete = false; // Whether player barrier for the minigame is fully in effect/expanded to maximum range
    public static bool s_IsBarrierComplete { get { return s_Instance._isBarrierComplete; } set { s_Instance._isBarrierComplete = value; } }
    private bool _isAdditionalScrap = false; // Whether additional scraps (other than the guaranteed ones in each ring) are present
    public static bool s_IsAdditionalScrap { get { return s_Instance._isAdditionalScrap; } set { s_Instance._isAdditionalScrap = value; } }
    private int _scrapCollected = 0; // Number of scrap collected (Up to three possible in one minigame session)
    public static int s_ScrapCollected { get { return s_Instance._scrapCollected; } set { s_Instance._scrapCollected = value; } }
    private int _count = 3; // Hook shot charges (lives) remaining
    public static int s_Count { get { return s_Instance._count; } set { s_Instance._count = value; } }

    public static List<GameObject> s_Scraps { get { return s_Instance._scraps; } set { s_Instance._scraps = value; } }

    private bool _isTimerOngoing = false; // Timer is counting down for a hookshot charge
    private bool _isInitialTimerExpand = false; // Hook has fired, visual timer started expanding back to maximum range to reset timer

    private GameObject _player; // Player ship object
    private Rigidbody _rbPlayer; // Rigidbody of the player ship
    private ShipMovement _shipScript; // Ship Movement script attached to the player ship
    private GameObject _spaceUI; // Space UI Canvas
    [SerializeField] private GameObject _interactText; // Interaction text when in vicinity of scrap node
    [SerializeField] private GameObject _hook; // Hook object on player ship
    [SerializeField] private GameObject _hookChargePrefab; // Prefab for hook charges UI
    [SerializeField] private GameObject _hookCharges; // Hook charges for lives
    [SerializeField] private GameObject _overlay; // Transparent overlay
    [SerializeField] private GameObject _barrier; // Barrier to destroy objects inside during the minigame
    [SerializeField] private GameObject _visualTimer; // Visual circle closing in on player ship representing the timer for each hookshot charge
    private float _timer; // Current amount of time remaining before losing one hookshot charge
    public float _minigameTimer; // Cumulative time minigame is active

    [SerializeField] private List<GameObject> _scraps = new List<GameObject>(); // Possible scraps
    [SerializeField] private List<GameObject> _glitchScraps = new List<GameObject>(); // Possible scraps with glitch animation (used in minigame)
    private List<GameObject> _currScraps = new List<GameObject>(); // Scraps spawned
    [SerializeField] private List<Scrap> _scrapObjs = new List<Scrap>(); // Possible scriptable objects for scraps
    [SerializeField] private List<GameObject> _obstacles = new List<GameObject>(); // Possible obstacles

    [SerializeField] private List<GameObject> _ringsInner = new List<GameObject>(); // Possible inner ring layouts
    [SerializeField] private List<GameObject> _ringsOuter = new List<GameObject>(); // Possible outer ring layouts
    private GameObject _ringInner; // Current inner ring
    private GameObject _ringOuter; // Current outer ring

    [SerializeField] private GameObject _ringExplosionPrefab; // Prefab for explosion particle effect once minigame ends
    [SerializeField] private GameObject _ringTimerPrefab; // Prefab for timer particle effect during the minigame
    private ParticleSystem _ringTimerPS; // Particle system for ring timer
    private ParticleSystem.ShapeModule _ringTimerShape; // Shape module of particle system for ring timer

    void Awake()
    {
        if(s_instance != null && s_instance != this) // Ensure MinigameManager is a singleton
        {
            Destroy(this.gameObject);
            return;
        }

        s_instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        _shipScript = GameObject.FindObjectOfType<ShipMovement>();
        _player = _shipScript.gameObject;
        _rbPlayer = _player.GetComponent<Rigidbody>();
        _spaceUI = GameObject.FindWithTag("UI");
        if(_hookCharges == null) // If no hook charges UI exists, add one to the SpaceUI Canvas
        {
            _hookCharges = Instantiate(_hookChargePrefab, new Vector3(0, 0, 0), Quaternion.identity);
            _hookCharges.transform.SetParent(_spaceUI.transform, false);
        }
        _timer = Timer;
        if(_interactText == null) // If no interaction text UI exists, link the one in the SpaceUI Canvas
        {
            foreach(Transform child in _spaceUI.transform)
            {
                if(child.gameObject.CompareTag("InteractUI"))
                {
                    _interactText = child.gameObject;
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(_isPlaying) // Fishing minigame is playing
        {
            // Player ship shoots the hook in the direction it is facing
            if(Input.GetKey(KeyCode.Space) && !_isShooting && _isBarrierComplete && _visualTimer != null && _isTimerOngoing && _count > 0) // Input for firing hook
            {
                if (HookshotFired != null)
                    HookshotFired.Post(gameObject);
                _isTimerOngoing = false;
                _isInitialTimerExpand = true;
                _isShooting = true;
                _hook.SetActive(true);
                _count--;
                UpdateCharges();
            }

            // Update timer for each hookshot charge
            if(!_isShooting && !_isReturning && _isBarrierComplete && _isTimerOngoing)
            {
                if(_visualTimer == null) // Create new visual timer particle effect
                {
                    _visualTimer = Instantiate(_ringTimerPrefab);
                    _visualTimer.transform.position = _player.transform.position + new Vector3(0,0,0);
                    _ringTimerPS = _visualTimer.transform.GetChild(0).GetComponent<ParticleSystem>();
                    _ringTimerShape = _ringTimerPS.shape;
                    _timer = Timer;
                }
                else // Decremement timer
                {
                    _timer -= Time.deltaTime;
                    float radius = _timer * (20f / Timer); // Radius of visual timer (amount of seconds remaining * radius of visual timer for each second)
                    if(_timer <= 0) // Failed to shoot hook within time limit
                    {
                        _timer = Timer; // Reset timer
                        // Shake camera effect
                        FollowCamera followCam;
                        if(Camera.main.gameObject.TryGetComponent<FollowCamera>(out followCam))
                            followCam.CreateCameraShake(1f);
                        // Decrement hookshot charges
                        _count--;
                        UpdateCharges();
                        Destroy(_visualTimer); // Destroy timer particle effect

                        if(_count == 0) // No lives
                        {
                            EndMinigame();
                        }
                    }
                    else if(_isTimerOngoing)
                    {
                        // Reduce size of visual timer particle effect
                        _ringTimerShape.radius = radius;
                    }
                }
            }
            else if(!_isTimerOngoing && _ringTimerShape.radius < 20f) // Hook has fired, expand visual timer to maximum range to reset timer
            {
                ParticleSystem.MainModule mainModule = _ringTimerPS.main;
                mainModule.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 0.7f); // Reduce MinMaxCurve for start lifetime
                if(_isInitialTimerExpand) // Clear existing particles before expanding visual timer
                {
                    // Get a list of all alive particles
                    ParticleSystem.Particle[] particles = new ParticleSystem.Particle[_ringTimerPS.particleCount];
                    _ringTimerPS.GetParticles(particles);
                    // Iterate through each particle and kill existing particles
                    for(int i = 0; i < particles.Length; i++)
                    {
                        particles[i].remainingLifetime = 0;
                    }
                    // Apply the changes to the particle system
                    _ringTimerPS.SetParticles(particles, particles.Length);
                    _isInitialTimerExpand = false;
                }

                _timer = Timer; // Reset timer
                _ringTimerShape.radius += 10f * Time.deltaTime; // Expands the visual timer particle effect
                if(_ringTimerShape.radius >= 20f)
                {
                    mainModule.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2f);
                    _isTimerOngoing = true; // Start timer
                }
            }
            else if(_ringTimerShape.radius >= 20f)
            {
                _isTimerOngoing = true; // Start timer
            }

            // Rotation of the rings
            _ringInner.transform.Rotate(new Vector3(0, 0, 1) * RotateInRSpeed * Time.deltaTime);
            _ringOuter.transform.Rotate(new Vector3(0, 0, 1) * RotateOutRSpeed * Time.deltaTime);

            // Add to cumulative minigame timer
            _minigameTimer += Time.deltaTime;
            if(_minigameTimer > Timer * 3.5f) // End minigame if it has exceeded the intended time (a bug has occurred)
            {
                Debug.Log("Error: Minigame Terminated");
                EndMinigame();
            }
        }
    }

    public void StartMinigame() // Play fishing minigame
    {
        _player.transform.position = new Vector3(_player.transform.position.x, 10f, _player.transform.position.z); // Move player ship above scene level
        _overlay.transform.position = new Vector3(_player.transform.position.x, 5f, _player.transform.position.z);
        _overlay.SetActive(true); // Display the overlay
        _barrier.SetActive(true); // Display the barrier
        if (PortalOpen != null)
            PortalOpen.Post(gameObject);
        _isTimerOngoing = true; // Start timer
        _shipScript.ParticleSystem.gameObject.SetActive(false); // Disable booster particles for ship
        foreach(TurretTurnScript turret in _shipScript.turrets) // Disable turrets on ship
        {
            turret.enabled = false;
        }
        _hookCharges.SetActive(true); // Display hook charges
        UpdateCharges(); // Reset number of hook charges
        _scrapCollected = 0; // Reset number of scrap collected
        float randSpeed = Random.Range(5, RotateInRMaxSpeed); // Randomize rotation speed for inner ring
        RotateInRSpeed = randSpeed;
        randSpeed = Random.Range(5, RotateOutRMaxSpeed); // Randomize rotation speed for outer ring
        RotateOutRSpeed = randSpeed;
        _minigameTimer = 0f; // Reset cumulative minigame timer
        _isPlaying = true; // Indicate minigame has started
        _rbPlayer.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; // Freeze player ship

        // Spawn random inner and outer rings
        Quaternion rotation = Quaternion.AngleAxis(90, Vector3.right);
        int index = Random.Range(0, _ringsInner.Count);
        _ringInner = Instantiate(_ringsInner[index], _player.transform.position, rotation);
        index = Random.Range(0, _ringsOuter.Count);
        _ringOuter = Instantiate(_ringsOuter[index], _player.transform.position, rotation);

        // Spawn random scrap for inner ring
        int numChild = _ringInner.transform.childCount;
        int randomScrap = Random.Range(0, _glitchScraps.Count);
        GameObject guaranteedScrap = Instantiate(_glitchScraps[randomScrap], _ringInner.transform.GetChild(0).position, rotation); // Spawn one guaranteed scrap in inner ring
        CollectibleScrap guaranteedScrapCollectible = guaranteedScrap.GetComponent<CollectibleScrap>();
        guaranteedScrapCollectible.IsStaticMovement = true; // Disable collectible scrap movement
        guaranteedScrap.transform.parent = _ringInner.transform.GetChild(0);
        _currScraps.Add(guaranteedScrap);
        for (int emptySlot = 1; emptySlot < numChild; emptySlot++)
        {
            int num = Random.Range(0, 10);
            if(num < ScrapInRChance) // Spawn collectible scrap
            {
                _isAdditionalScrap = true;
                randomScrap = Random.Range(0, _glitchScraps.Count);
                GameObject scrap = Instantiate(_glitchScraps[randomScrap], _ringInner.transform.GetChild(emptySlot).position, rotation);
                CollectibleScrap scrapCollectible = scrap.GetComponent<CollectibleScrap>();
                scrapCollectible.IsStaticMovement = true; // Disable collectible scrap movement
                scrap.transform.parent = _ringInner.transform.GetChild(emptySlot);
                _currScraps.Add(scrap);
            }
            else // Spawn obstacle
            {
                int randObstacle = Random.Range(0, _obstacles.Count);
                GameObject obstacle = Instantiate(_obstacles[randObstacle], _ringInner.transform.GetChild(emptySlot).position, rotation);
                CollectibleScrap scrapCollectible = obstacle.GetComponent<CollectibleScrap>();
                scrapCollectible.IsStaticMovement = true; // Disable collectible scrap movement
                obstacle.transform.parent = _ringInner.transform.GetChild(emptySlot);
                _currScraps.Add(obstacle);
            }
        }

        // Spawn random scrap for outer ring
        numChild = _ringOuter.transform.childCount;
        randomScrap = Random.Range(0, _glitchScraps.Count);
        guaranteedScrap = Instantiate(_glitchScraps[randomScrap], _ringOuter.transform.GetChild(0).position, rotation); // Spawn one guaranteed scrap in outer ring
        guaranteedScrapCollectible = guaranteedScrap.GetComponent<CollectibleScrap>();
        guaranteedScrapCollectible.IsStaticMovement = true; // Disable collectible scrap movement
        guaranteedScrap.transform.parent = _ringOuter.transform.GetChild(0);
        _currScraps.Add(guaranteedScrap);
        for (int emptySlot = 1; emptySlot < numChild; emptySlot++)
        {
            int num = Random.Range(0, 10);
            if(num < ScrapOutRChance) // Spawn collectible scrap
            {
                _isAdditionalScrap = true;
                randomScrap = Random.Range(0, _glitchScraps.Count);
                GameObject scrap = Instantiate(_glitchScraps[randomScrap], _ringOuter.transform.GetChild(emptySlot).position, rotation);
                CollectibleScrap scrapCollectible = scrap.GetComponent<CollectibleScrap>();
                scrapCollectible.IsStaticMovement = true; // Disable collectible scrap movement
                scrap.transform.parent = _ringOuter.transform.GetChild(emptySlot);
                _currScraps.Add(scrap);
            }
            else // Spawn obstacle
            {
                int randObstacle = Random.Range(0, _obstacles.Count);
                GameObject obstacle = Instantiate(_obstacles[randObstacle], _ringOuter.transform.GetChild(emptySlot).position, rotation);
                CollectibleScrap scrapCollectible = obstacle.GetComponent<CollectibleScrap>();
                scrapCollectible.IsStaticMovement = true; // Disable collectible scrap movement
                obstacle.transform.parent = _ringOuter.transform.GetChild(emptySlot);
                _currScraps.Add(obstacle);
            }
        }
    }

    public void EndMinigame()
    {
        GameObject ringExplosion = Instantiate(_ringExplosionPrefab); // Play explosion particle effect
        ringExplosion.transform.position = _player.transform.position + new Vector3(0,0,0);
        Destroy(ringExplosion, 8);
        if(_visualTimer != null)
        {
            Destroy(_visualTimer); // Destroy timer particle effect
            _isTimerOngoing = false;
        }

        _player.transform.position = new Vector3(_player.transform.position.x, 0f, _player.transform.position.z); // Move player ship back to scene level
        _overlay.SetActive(false); // Hide the overlay

        _shipScript.ParticleSystem.gameObject.SetActive(true); // Enable booster particles for ship
        foreach(TurretTurnScript turret in _shipScript.turrets) // Enable turrets on ship
        {
            turret.enabled = true;
        }

        _hookCharges.SetActive(false); // Hide hook charges
        _isPlaying = false;
        _isShooting = false;
        _isReturning = false;
        _isCollecting = false;
        _isAdditionalScrap = false;
        _count = 3; // Reset hookshot count
        _barrier.GetComponent<PlayerBarrier>().ResetBarrier(); // Reset barrier
        _barrier.SetActive(false); // Remove the barrier

        // Allow player ship to move
        _rbPlayer.constraints = RigidbodyConstraints.None;
        _rbPlayer.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        //Portal exploding sound
        if(PortalClose != null)
            PortalClose.Post(gameObject);

        // Delete all rings and scraps
        Destroy(_ringInner);
        Destroy(_ringOuter);
        foreach(GameObject scrap in _currScraps)
        {
            Destroy(scrap);
        }
    }

    // Display number of hook charges remaining
    private void UpdateCharges()
    {
        switch(_count)
        {
            case 3:
                // Player has three lives
                for (int i = 0; i < 3; i++)
                {
                    GameObject hookCharge = _hookCharges.transform.GetChild(i).gameObject;
                    hookCharge.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f); // Resets color for all charges
                }
                AkSoundEngine.SetState(Minigame, "Shots_Remaining_3"); //Reset music state group to all charges
                Debug.Log("weawy now");
                if (PlayMusic != null)
                { //Begins minigame music
                    PlayMusic.Post(gameObject);
                    Debug.Log("why no worky");
                }

                break;
            case 2:
                // Player has two lives
                GameObject thirdCharge = _hookCharges.transform.GetChild(2).gameObject;
                thirdCharge.GetComponent<Image>().color = new Color(.3f, .3f, .3f, 1f); // Set third charge to be dark
                AkSoundEngine.SetState(Minigame, "Shots_Remaining_2"); //Adjust music state group to 2 charges 
                break;
            case 1:
                // Player has one life
                GameObject secondCharge = _hookCharges.transform.GetChild(1).gameObject;
                secondCharge.GetComponent<Image>().color = new Color(.3f, .3f, .3f, 1f); // Set second charge to be dark
                AkSoundEngine.SetState(Minigame, "Shots_Remaining_1"); //Adjust music state group to 1 charge 
                break;
            case 0:
                // Player has no lives
                GameObject firstCharge = _hookCharges.transform.GetChild(0).gameObject;
                firstCharge.GetComponent<Image>().color = new Color(.3f, .3f, .3f, 1f); // Set first charge to be dark
                if (StopMusic != null) //Stop minigame music
                    StopMusic.Post(gameObject);
                AkSoundEngine.SetState(Minigame, "Shots_Remaining_3");
                break;
            default:
                return;
        }
    }

    // Add scrap collected to inventory
    public void CollectScrap()
    {
        _scrapCollected++; // Increment number of scrap collected
        Debug.Log("Start Collecting");
        GameObject scrap = null;
        if(_isPlaying)
        {
            scrap = _hook.transform.GetChild(1).gameObject; // Collected scrap game object, which became a child of the hook
        }
        else
        {
            scrap = transform.GetChild(3).gameObject; // Collected scrap game object, which became a child of the minigame manager
        }
        if(scrap != null)
        {
            CollectibleScrap scrapCollect = scrap.GetComponent<CollectibleScrap>();
            foreach(Scrap scrapObj in _scrapObjs)
            {
                if(scrapObj.Name == scrapCollect.Name) // Find scriptable object corresponding to collected scrap
                {
                    if(InventoryManager.s_Instance == null) // No inventory manager in scene
                    {
                        Debug.Log("Not Collected: No Inventory Manager");
                        break;
                    }
                    Debug.Log("Collected Scrap: " + scrapCollect.Name);
                    InventoryManager.s_Instance.ItemAddedMinigame.Invoke(scrapCollect, scrapObj);
                    InventoryManager.s_Instance.AddToPlayerInventory(scrapObj);
                }
            }
            Destroy(scrap);
        }
        else
        {
            Debug.Log("Not Collected");
        }
    }
}
