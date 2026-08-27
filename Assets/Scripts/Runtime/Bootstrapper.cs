using DG.Tweening;
using System.Linq;
using System.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UIElements;

public class Bootstrapper : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private ObjectDataRegistry objectDataRegistry;
    [SerializeField] private SceneBlackboard sceneBlackboard;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameObject staticGeometry;
    [SerializeField] private GameObject dynamicGeometry;
    [SerializeField] private GameObject instances;
    [Space(10)]
    [SerializeField] private GameObject officeSpawnpoint;
    [SerializeField] private GameObject parkingLotSpawnpoint;

    [Space(10)]
    [SerializeField] private GameObject player;

    [Space(10)]
    [SerializeField] private UIDocument dayTransitionDocument;

    [Space(10)]
    [SerializeField] private AudioClip sighClip;

    [Header("Prefab References")]
    [SerializeField] private InputService inputServicePrefab;

    private InputService _inputService;

    private async void Awake()
    {
        _inputService = Instantiate(inputServicePrefab, transform);
        _inputService.Initialize();

        objectDataRegistry = Instantiate(objectDataRegistry);
        objectDataRegistry.Initialize();

        sceneBlackboard = Instantiate(sceneBlackboard);
        sceneBlackboard.ResetStates();

        AudioSource playerAudioSource = player.GetComponent<AudioSource>();

        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
        playerMovement.Initialize(_inputService, mainCamera.transform);

        PlayerInteraction playerInteraction = player.GetComponent<PlayerInteraction>();
        playerInteraction.Initialize(_inputService, mainCamera.transform);

        PlayerFlashlight playerFlashlight = player.GetComponent<PlayerFlashlight>();
        playerFlashlight.Initialize(_inputService);

        PlayerNoise playerNoise = player.GetComponent<PlayerNoise>();

        PlayerDayTransition playerDayTransition = player.GetComponent<PlayerDayTransition>();
        playerDayTransition.Initialize(sceneBlackboard);

        PlayerDialog playerDialog = player.GetComponent<PlayerDialog>();
        playerDialog.Initialize(sceneBlackboard);

        foreach (Deer deer in FindObjectsByType<Deer>())
            deer.Initialize(sceneBlackboard, playerNoise, playerDialog);

        foreach (Door door in FindObjectsByType<Door>())
            door.Initialize(sceneBlackboard);

        foreach (LightSwitch lightSwitch in FindObjectsByType<LightSwitch>())
            lightSwitch.Initialize(sceneBlackboard);

        Generator generator = FindObjectsByType<Generator>().FirstOrDefault();
        generator.Initialize(sceneBlackboard);

        Computer computer = FindObjectsByType<Computer>().FirstOrDefault();
        computer.Initialize(sceneBlackboard);

        Phone phone = FindObjectsByType<Phone>().FirstOrDefault();
        phone.Initialize(sceneBlackboard);

        foreach (TriggerZone zone in FindObjectsByType<TriggerZone>())
            zone.Initialize(sceneBlackboard);

        generator.DisableGenerator();

        DOTween.SetTweensCapacity(500, 50);

        sceneBlackboard.Set("day", 1);
        sceneBlackboard.Set("objective", "First day at the work");

        sceneBlackboard.Set("office_door_interactable", true);
        sceneBlackboard.Set("office_door_hasKey", true);

        await playerDayTransition.ExecuteAsync(() => MovePlayerToSpawnpoint(parkingLotSpawnpoint));

        _inputService.EnablePlayerControls();

        await playerDialog.SetDialogAsync("I should check in.", 3);
        sceneBlackboard.Set("main_objective", "Check-in");

        await sceneBlackboard.WaitUntilKeyMatches("player_in_office", true);

        await playerDialog.SetDialogAsync("Light switch doesn't seem to work...");
        await playerDialog.SetDialogAsync("I think I should turn on the Generator.");
        sceneBlackboard.Set("generator_interactable", true);
        sceneBlackboard.Set("main_objective", "Restore power.");

        await sceneBlackboard.WaitUntilKeyMatches("generator_running", true);
        sceneBlackboard.Set("generator_interactable", false);
        sceneBlackboard.Set("office_lightswitch_interactable", true);
        generator.EnableGenerator();

        await playerDialog.SetDialogAsync("It's definitely better now...");

        await playerDialog.SetDialogAsync("Okay, now I should check in.\nBoss is going to be mad at me,");
        await playerDialog.SetDialogAsync("I can't stand him.");

        await sceneBlackboard.WaitUntilKeyMatches("player_in_office", true);
        
        if (!sceneBlackboard.Get<bool>("office_lightswitch_enabled"))
            await playerDialog.SetDialogAsync("It's dark...");

        await sceneBlackboard.WaitUntilKeyMatches("office_lightswitch_enabled", true);

        sceneBlackboard.Set("computer_interactable", true);
        sceneBlackboard.Set("computer_interactionprompt", "Check-In");
        await sceneBlackboard.WaitUntilKeyMatches("computer_interacted", true);

        sceneBlackboard.Set("computer_interactable", false);

        await playerDialog.SetDialogAsync("Okay, it looks fine now.");
        sceneBlackboard.Set("phone_incoming", true);
        sceneBlackboard.Set("phone_interactable", true);
        sceneBlackboard.Set("phone_interactionprompt", "Respond");

        await sceneBlackboard.WaitUntilKeyMatches("phone_interacted", true);

        sceneBlackboard.Set("phone_interactable", false);

        sceneBlackboard.Set("phone_interactable", false);
        sceneBlackboard.Set("phone_interactionprompt", "...");

        sceneBlackboard.Set("phone_speak", Time.time);
        await playerDialog.SetDialogAsync("You're already late on your very first day...");
        sceneBlackboard.Set("phone_speak", Time.time);
        await playerDialog.SetDialogAsync("...so stop loitering and go check on and feed the deer already!");

        playerAudioSource.PlayOneShot(sighClip);
        await Task.Delay(2000);

        sceneBlackboard.Set("main_objective", "Check and feed the deer.");
        sceneBlackboard.Set("deer1_shouldFlee", true);
        sceneBlackboard.Set("deer1_interactable", true);

        await playerDialog.SetDialogAsync("I should better do as he says.\nOtherwise he would fire me.");
        await playerDialog.SetDialogAsync("At my first day? I would definitely not like that.");

        sceneBlackboard.Set("deers_fed", 0);
        await sceneBlackboard.WaitUntilKeyMatches("deers_fed", 1);

        await playerDialog.SetDialogAsync("I am done with the deers.");
        await playerDialog.SetDialogAsync("I should return to the office.");

        sceneBlackboard.Set("main_objective", "Return to the office.");
        await sceneBlackboard.WaitUntilKeyMatches("player_in_office", true);

        await playerDialog.SetDialogAsync("It's going to be a long night", 1);

        sceneBlackboard.Set("day", 2);
        sceneBlackboard.Set("objective", "DEMO END");

        await playerDayTransition.ExecuteAsync(() => MovePlayerToSpawnpoint(officeSpawnpoint));

        Application.Quit();
    }

    private void MovePlayerToSpawnpoint(GameObject spawnpoint)
    {
        _inputService.DisablePlayerControls();
        player.transform.position = spawnpoint.transform.position;

        if (!TryGetActiveVirtualCamera(out CinemachineCamera activeCamera))
            return;

        if (!activeCamera.TryGetComponent<CinemachineInputAxisController>(out CinemachineInputAxisController cinemachineInputAxisController))
            return;

        cinemachineInputAxisController.enabled = false;
        player.transform.rotation = parkingLotSpawnpoint.transform.rotation;

        cinemachineInputAxisController.enabled = true;
        _inputService.EnablePlayerControls();
    }

    private bool TryGetActiveVirtualCamera(out CinemachineCamera cinemachineCamera)
    {
        cinemachineCamera = null;

        if (!mainCamera.TryGetComponent<CinemachineBrain>(out CinemachineBrain cinemachineBrain))
        {
            Debug.LogError($"{GetType().Name} encountered an error: CinemachineBrain component was null for MainCamera");
            return false;
        }

        if (cinemachineBrain.ActiveVirtualCamera == null)
            return false;

        cinemachineCamera = (CinemachineCamera)cinemachineBrain.ActiveVirtualCamera;
        return true;
    }
}
