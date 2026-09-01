using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

public class Bootstrapper : MonoBehaviour
{
    [Header("Scene References")]
    // [SerializeField] private ObjectDataRegistry objectDataRegistry;
    [SerializeField] private SceneBlackboard sceneBlackboard;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameObject staticGeometry;
    [SerializeField] private GameObject dynamicGeometry;
    [SerializeField] private GameObject instances;

    [Space(10)]
    [SerializeField] private GameObject officeSpawnpoint;
    [SerializeField] private GameObject parkingLotSpawnpoint;
    [SerializeField] private List<GameObject> carSpawnpoints;
    [SerializeField] private Transform deerForestCenter;
    [SerializeField] private GameObject firstSight;
    [SerializeField] private Transform escapeDestionation;

    [Space(10)]
    [SerializeField] private GameObject player;

    [Space(10)]
    [SerializeField] private UIDocument dayTransitionDocument;

    [Space(10)]
    [SerializeField] private AudioClip sighClip;

    [Header("Prefab References")]
    [SerializeField] private InputService inputServicePrefab;
    [SerializeField] private AudioSource ambiencePlayer;
    [SerializeField] private Elk elkPrefab;

    private InputService _inputService;
    private SceneBlackboard _sceneBlackboard;
    private AudioSource _ambiencePlayer;
    private AudioSource _playerAudioSource;
    private PlayerMovement _playerMovement;
    private PlayerInteraction _playerInteraction;
    private PlayerDayTransition _playerDayTransition;
    private PlayerWarning _playerWarning;
    private PlayerDialog _playerDialog;
    private PlayerFlashlight _playerFlashlight;
    private PlayerShotgun _playerShotgun;
    private PlayerMop _playerMop;
    private PlayerObjective _playerObjective;
    private Generator _generator;
    private Computer _computer;
    private Phone _phone;
    private Mop _mop;
    private Elk _elk;

    private async UniTaskVoid Awake()
    {
        Debug.Log($"{GetType().Name} initializing systems.");
        InitializeSystems();

        Debug.Log($"{GetType().Name} initializing scene and behaviours.");
        InitializeSceneAndBehaviours();

        Debug.Log($"{GetType().Name} executing day one.");
        await ExecuteDayOne();

        Debug.Log($"{GetType().Name} executing day two.");
        await ExecuteDayTwo();

        Debug.Log($"{GetType().Name} executing day three.");
        await ExecuteDayThree();
    }

    private void InitializeSystems()
    {
        // Initialize services and data components

        // Disabled the ObjectDataRegistry since it is not usefull
        // objectDataRegistry = Instantiate(objectDataRegistry);
        // objectDataRegistry.Initialize();

        _sceneBlackboard = ScriptableObject.CreateInstance<SceneBlackboard>();
        _sceneBlackboard.ResetStates();

        _inputService = Instantiate(inputServicePrefab, transform);
        _inputService.Initialize();
        _inputService.EnablePlayerControls();

        _ambiencePlayer = Instantiate(ambiencePlayer, transform);
        _ambiencePlayer.volume = 0;
        _ambiencePlayer.Play();
        DOTween.To(() => _ambiencePlayer.volume, x => _ambiencePlayer.volume = x, 1, 0.3f);
    }

    private void InitializeSceneAndBehaviours()
    {
        // Initialize behaviours
        _playerAudioSource = player.GetComponent<AudioSource>();

        _playerMovement = player.GetComponent<PlayerMovement>();
        _playerMovement.Initialize(_inputService, _sceneBlackboard, mainCamera.transform);

        _playerInteraction = player.GetComponent<PlayerInteraction>();
        _playerInteraction.Initialize(_inputService, _sceneBlackboard, mainCamera.transform);

        _playerDayTransition = player.GetComponent<PlayerDayTransition>();
        _playerDayTransition.Initialize(_sceneBlackboard);

        _playerDialog = player.GetComponent<PlayerDialog>();
        _playerDialog.Initialize();

        _playerFlashlight = player.GetComponent<PlayerFlashlight>();
        _playerFlashlight.Initialize(_inputService, _sceneBlackboard, _playerDialog);

        _playerShotgun = player.GetComponent<PlayerShotgun>();
        _playerShotgun.Initialize(_inputService, _sceneBlackboard, _playerDialog, mainCamera);

        _playerMop = player.GetComponent<PlayerMop>();
        _playerMop.Initialize(_sceneBlackboard);

        _playerObjective = player.GetComponent<PlayerObjective>();
        _playerObjective.Initialize(_sceneBlackboard);

        _playerWarning = player.GetComponent<PlayerWarning>();
        _playerWarning.Initialize(_sceneBlackboard);

        PauseMenu pauseMenu = FindObjectsByType<PauseMenu>().FirstOrDefault();
        pauseMenu.Initialize(_inputService);

        _elk = Instantiate(elkPrefab, transform);
        _elk.Initialize(_sceneBlackboard, player.transform, escapeDestionation, player);

        // Initialize components which are in the scene

        // Initialize deer in the scene
        _sceneBlackboard.Set(SceneBlackboardKeys.Scene.DeerCount, 0);
        foreach (Deer deer in FindObjectsByType<Deer>())
        {
            deer.Initialize(_sceneBlackboard, _playerDialog);
            _sceneBlackboard.Set(SceneBlackboardKeys.Scene.DeerCount, _sceneBlackboard.Get<int>(SceneBlackboardKeys.Scene.DeerCount) + 1);
        }

        // Initialize wolves in the scene
        _sceneBlackboard.Set(SceneBlackboardKeys.Scene.WolfCount, 0);
        foreach (Wolf wolf in FindObjectsByType<Wolf>())
        {
            wolf.Initialize(_sceneBlackboard, _playerDialog);
            _sceneBlackboard.Set(SceneBlackboardKeys.Scene.WolfCount, _sceneBlackboard.Get<int>(SceneBlackboardKeys.Scene.WolfCount) + 1);
        }

        // Initialize doors in the scene
        foreach (Door door in FindObjectsByType<Door>())
            door.Initialize(_sceneBlackboard);

        // Initialize light switches in the scene
        foreach (LightSwitch lightSwitch in FindObjectsByType<LightSwitch>())
            lightSwitch.Initialize(_sceneBlackboard);

        // Initialize moppable objects in the scene
        foreach (Moppable moppable in FindObjectsByType<Moppable>())
            moppable.Initialize(_sceneBlackboard, _playerDialog);

        // Initialize zones
        foreach (TriggerZone zone in FindObjectsByType<TriggerZone>())
            zone.Initialize(_sceneBlackboard);

        // Initialize barriers
        foreach (Barrier barrier in FindObjectsByType<Barrier>())
            barrier.Initialize(_sceneBlackboard);

        // Initialize waypoints
        foreach (Waypoint waypoint in FindObjectsByType<Waypoint>())
            waypoint.Initialize(_sceneBlackboard);

        // Initialize the Generator
        _generator = FindObjectsByType<Generator>().FirstOrDefault();
        _generator.Initialize(_sceneBlackboard);
        _generator.DisableGenerator();

        // Initialize the Computer
        _computer = FindObjectsByType<Computer>().FirstOrDefault();
        _computer.Initialize(_sceneBlackboard);

        // Initialize the Phone
        _phone = FindObjectsByType<Phone>().FirstOrDefault();
        _phone.Initialize(_sceneBlackboard);

        // Initialize the Mop
        _mop = FindObjectsByType<Mop>().FirstOrDefault();
        _mop.Initialize(_sceneBlackboard, _playerDialog);

        DOTween.SetTweensCapacity(500, 50);
    }

    private async UniTask ExecuteDayOne()
    {
        // Advance to the first day
        ResetBlackboardStates();
        _sceneBlackboard.Set(SceneBlackboardKeys.Scene.Day, 1);
        _sceneBlackboard.Set(SceneBlackboardKeys.Scene.DayDescription, "First day at work");

        await _playerDayTransition.ExecuteAsync(() =>
        {
            // Move the player to the spawnpoint
            MoveCarToRandomSpawnpoint();
            MovePlayerToSpawnpoint(parkingLotSpawnpoint);

            // Allow player to interact with interactables
            _sceneBlackboard.Set(SceneBlackboardKeys.Player.CanInteract, true);

            // Allow player to interact with the door
            // Give the Office key to the player since the door is locked
            _sceneBlackboard.Set($"office_{SceneBlackboardKeys.Door.Interactable}", true);
            _sceneBlackboard.Set($"office_{SceneBlackboardKeys.Door.HasKey}", true);

            // Hide wolves
            foreach (Wolf wolf in FindObjectsByType<Wolf>())
                wolf.enabled = false;
        });

        // Play self-dialog
        await _playerDialog.ExecuteDialogAsync("I should check-in.");

        // Allow player to use Flashlight
        _sceneBlackboard.Set(SceneBlackboardKeys.Player.Flashlight.CanEquip, true);

        // Introduce Flashlight mechanic
        await _playerDialog.ExecuteDialogAsync("[ Press F ] \t Toggle Flashlight", 2f, false);

        // [Main Objective] Check-In
        SetMainObjective("Check-In");

        // Enable Office waypoint
        _sceneBlackboard.Set(SceneBlackboardKeys.Scene.Waypoints.Office.IsActive, true);

        // Wait until player arrives to the Office
        await _sceneBlackboard.WaitUntilKeyMatches(SceneBlackboardKeys.Player.InOffice, true);

        // Disable Office waypoint
        _sceneBlackboard.Set(SceneBlackboardKeys.Scene.Waypoints.Office.IsActive, false);

        // Play self-dialogs
        await _playerDialog.ExecuteDialogAsync("Light switch doesn't seem to work...");
        await _playerDialog.ExecuteDialogAsync("I think I should turn on the Generator.", 3f);

        // [Sub Objective] Restore power
        SetSubObjective("Restore power.");

        // Enable Generator waypoint
        _sceneBlackboard.Set(SceneBlackboardKeys.Scene.Waypoints.Generator.IsActive, true);

        // Make the Generator interactable
        _sceneBlackboard.Set(SceneBlackboardKeys.Generator.Interactable, true);

        // Wait until player turns on the Generator
        await _sceneBlackboard.WaitUntilKeyMatches(SceneBlackboardKeys.Generator.IsRunning, true);

        // Disable Generator waypoint
        _sceneBlackboard.Set(SceneBlackboardKeys.Scene.Waypoints.Generator.IsActive, false);

        // Make the Generator non-interactable & the Office light switch interactable
        _sceneBlackboard.Set(SceneBlackboardKeys.Generator.Interactable, false);
        _sceneBlackboard.Set($"office_{SceneBlackboardKeys.LightSwitch.Interactable}", true);

        // Enable the Generator
        _generator.EnableGenerator();
        CompleteSubObjective();

        // Play self-dialogs
        await _playerDialog.ExecuteDialogAsync("It's definitely better...");
        await _playerDialog.ExecuteDialogAsync("Okay, I can check-in now.");

        // Wait until player gets back in the Office
        await _sceneBlackboard.WaitUntilKeyMatches(SceneBlackboardKeys.Player.InOffice, true);

        // [Sub Objective] Turn on the lights
        SetSubObjective("Turn on the lights.");

        // Play self-dialog if lights are off
        if (!_sceneBlackboard.Get<bool>($"office_{SceneBlackboardKeys.LightSwitch.Enabled}"))
            await _playerDialog.ExecuteDialogAsync("It's dark...");

        // Wait until lights are on
        await _sceneBlackboard.WaitUntilKeyMatches($"office_{SceneBlackboardKeys.LightSwitch.Enabled}", true);
        CompleteSubObjective();

        // Make the Computer interactable so player could check-in
        _sceneBlackboard.Set(SceneBlackboardKeys.Computer.Interactable, true);

        // Wait until player checks-in
        await _sceneBlackboard.WaitUntilKeyMatches(SceneBlackboardKeys.Computer.Interacted, true);

        // Reset the Computer
        _sceneBlackboard.Set(SceneBlackboardKeys.Computer.Interacted, false);
        _sceneBlackboard.Set(SceneBlackboardKeys.Computer.Interactable, false);

        // Play self-dialog
        await _playerDialog.ExecuteDialogAsync("Here comes my first shift...");
        CompleteMainObjective();

        // Ring the phone and make it interactable
        _sceneBlackboard.Set(SceneBlackboardKeys.Phone.Ringing, true);
        _sceneBlackboard.Set(SceneBlackboardKeys.Phone.Interactable, true);

        // Wait until player picks up the phone
        await _sceneBlackboard.WaitUntilKeyMatches(SceneBlackboardKeys.Phone.Interacted, true);

        // Reset the Phone
        _sceneBlackboard.Set(SceneBlackboardKeys.Phone.Interacted, false);
        _sceneBlackboard.Set(SceneBlackboardKeys.Phone.Interactable, false);

        // Play dialog
        _sceneBlackboard.Set(SceneBlackboardKeys.Phone.Speaking, Time.time);
        await _playerDialog.ExecuteDialogAsync("You're already late on your very first day...", 3f, false);

        // Play Dialog
        _sceneBlackboard.Set(SceneBlackboardKeys.Phone.Speaking, Time.time);
        await _playerDialog.ExecuteDialogAsync("...so stop loitering and go check on and feed the deer already!", 3f, false);

        // Play sighing sound
        _playerAudioSource.PlayOneShot(sighClip, 1);
        await UniTask.Delay(2500, cancellationToken: this.GetCancellationTokenOnDestroy());

        // Allow player to sprint & crouch
        _sceneBlackboard.Set(SceneBlackboardKeys.Player.CanSprint, true);
        _sceneBlackboard.Set(SceneBlackboardKeys.Player.CanCrouch, true);

        // [Main Objective] Check and feed the deer
        SetMainObjective("Check and feed the deer.");

        // Setup deer
        _sceneBlackboard.Set(SceneBlackboardKeys.Deer.CanFlee, true);
        _sceneBlackboard.Set(SceneBlackboardKeys.Deer.Interactable, true);
        _sceneBlackboard.Set(SceneBlackboardKeys.Deer.Fed, 0);

        // Allow access to the Square
        _sceneBlackboard.Set(SceneBlackboardKeys.Scene.Barriers.SquareEntrance.IsActive, false);

        // Play self-dialogs
        await _playerDialog.ExecuteDialogAsync("He could fire me. At my first day?");
        await _playerDialog.ExecuteDialogAsync("I would definitely not like that!");

        // Breathe delay
        await UniTask.Delay(2000, cancellationToken: this.GetCancellationTokenOnDestroy());

        // Introduce new movement mechanics
        await _playerDialog.ExecuteDialogAsync("[ Hold Left Shift ] \t Sprint\n[ Hold Left Control ] \t Crouch", 3f, false);

        // Wait until player feeds all the existing deer
        await _sceneBlackboard.WaitUntilKeyMatches(SceneBlackboardKeys.Deer.Fed, _sceneBlackboard.Get<int>(SceneBlackboardKeys.Scene.DeerCount));

        // Play self-dialog
        await _playerDialog.ExecuteDialogAsync("We are done with the deer.");
        CompleteMainObjective();

        // Delay to prevent objective removing next one
        await UniTask.Delay(3000, cancellationToken: this.GetCancellationTokenOnDestroy());

        // [Main Objective] Clean the graffiti.
        SetMainObjective("Clean the graffiti.");

        // Play self-dialogs
        await _playerDialog.ExecuteDialogAsync("I think I saw a graffiti at the entrance of the square,\nI should clean it.", 3f);
        await _playerDialog.ExecuteDialogAsync("There was a mop in the office I suppose.");

        // [Sub Objective] Get the mop from the Office
        SetSubObjective("Get the mop from the Office.");

        // Make the mop and the mess interactable
        _sceneBlackboard.Set(SceneBlackboardKeys.Player.Mop.Interactable, true);
        _sceneBlackboard.Set(SceneBlackboardKeys.Scene.Decals.Sprint.Interactable, true);

        // Wait until the player actually equips the mop
        await _sceneBlackboard.WaitUntilKeyMatches(SceneBlackboardKeys.Player.Mop.IsEquipped, true);
        CompleteSubObjective();

        // Enable Graffiti waypoint
        _sceneBlackboard.Set(SceneBlackboardKeys.Scene.Waypoints.Graffiti.IsActive, true);

        // Wait until the player cleans the mess
        await _sceneBlackboard.WaitUntilKeyMatches(SceneBlackboardKeys.Scene.Decals.Sprint.Removed, true);

        // Disable Graffiti waypoint
        _sceneBlackboard.Set(SceneBlackboardKeys.Scene.Waypoints.Graffiti.IsActive, false);

        CompleteMainObjective();

        await _playerDialog.ExecuteDialogAsync("It smells like blood though?");
        await _playerDialog.ExecuteDialogAsync("Whatever.");

        // [Main Objective] Return to the office
        SetMainObjective("Return to the office.");

        // [Sub Objective] Put the mop back
        SetSubObjective("Put the mop back.");

        // Play self-dialog
        await _playerDialog.ExecuteDialogAsync("I should return to the office.");

        // Wait until the player gets in the Office and puts the mop back
        await _sceneBlackboard.WaitUntilKeyMatches(SceneBlackboardKeys.Player.InOffice, true);
        await _sceneBlackboard.WaitUntilKeyMatches(SceneBlackboardKeys.Player.Mop.IsEquipped, false);
        CompleteSubObjective();
        CompleteMainObjective();

        // Breathe delay
        await UniTask.Delay(2000, cancellationToken: this.GetCancellationTokenOnDestroy());
    }

    private async UniTask ExecuteDayTwo()
    {
        // Advence to the second day
        ResetBlackboardStates();
        _sceneBlackboard.Set(SceneBlackboardKeys.Scene.Day, 2);
        _sceneBlackboard.Set(SceneBlackboardKeys.Scene.DayDescription, "Welcome the wolves");

        await _playerDayTransition.ExecuteAsync(() =>
        {
            // Move player to the spawnpoint
            MovePlayerToSpawnpoint(officeSpawnpoint);
            MoveCarToRandomSpawnpoint();

            // Allow player to interact
            _sceneBlackboard.Set(SceneBlackboardKeys.Player.CanInteract, true);

            // Allow player to play with the light switch
            _sceneBlackboard.Set($"office_{SceneBlackboardKeys.LightSwitch.Enabled}", true);
            _sceneBlackboard.Set($"office_{SceneBlackboardKeys.LightSwitch.Interactable}", true);
            
            _sceneBlackboard.Set($"office_{SceneBlackboardKeys.Door.Interactable}", true);
            _sceneBlackboard.Set($"office_{SceneBlackboardKeys.Door.Locked}", false);

            _sceneBlackboard.Set(SceneBlackboardKeys.Scene.Barriers.SquareEntrance.IsActive, false);

            // Allow player to use flashlight
            _sceneBlackboard.Set(SceneBlackboardKeys.Player.Flashlight.CanEquip, true);
        });

        // Breathe delay
        await UniTask.Delay(2000, cancellationToken: this.GetCancellationTokenOnDestroy());

        // Play self-dialog
        await _playerDialog.ExecuteDialogAsync("It's my second shift.");
        await _playerDialog.ExecuteDialogAsync("I should get used to this.");

        // Phone dialog between the player and the Boss

        // Reset the phone
        _sceneBlackboard.Set(SceneBlackboardKeys.Phone.Interacted, false);

        // Enable and ring the phone
        _sceneBlackboard.Set(SceneBlackboardKeys.Phone.Ringing, true);
        _sceneBlackboard.Set(SceneBlackboardKeys.Phone.Interactable, true);

        // Wait until player picks up the phone
        await _sceneBlackboard.WaitUntilKeyMatches(SceneBlackboardKeys.Phone.Interacted, true);

        // Reset the phone
        _sceneBlackboard.Set(SceneBlackboardKeys.Phone.Interactable, false);
        _sceneBlackboard.Set(SceneBlackboardKeys.Phone.Interactable, false);

        // Play dialog
        _sceneBlackboard.Set(SceneBlackboardKeys.Phone.Speaking, Time.time);
        await _playerDialog.ExecuteDialogAsync("Hey! How is it going? Whatever..", 3f, false);

        // Play dialog
        _sceneBlackboard.Set(SceneBlackboardKeys.Phone.Speaking, Time.time);
        await _playerDialog.ExecuteDialogAsync("I don't really care...", 3f, false);

        // Play self-dialog
        await _playerDialog.ExecuteDialogAsync("Wow. He really said that.");
        await _playerDialog.ExecuteDialogAsync("...");

        // Play dialog
        _sceneBlackboard.Set(SceneBlackboardKeys.Phone.Speaking, Time.time);
        await _playerDialog.ExecuteDialogAsync("Listen to me.", 1f, false);

        // Play dialog
        _sceneBlackboard.Set(SceneBlackboardKeys.Phone.Speaking, Time.time);
        await _playerDialog.ExecuteDialogAsync("Our crew has brought some wolves.\nYou better take care of them.", 4f, false);

        // [Main Objective] Feed the animals
        _sceneBlackboard.Set(SceneBlackboardKeys.Scene.Objectives.MainObjective, "Feed the animals.");

        // [Sub Objective] Feed deer
        SetSubObjective("Feed deer.");

        // Enable the door so player could get out
        _sceneBlackboard.Set($"office_{SceneBlackboardKeys.Door.Interactable}", true);

        // Enable advanced controls
        _sceneBlackboard.Set(SceneBlackboardKeys.Player.CanCrouch, true);
        _sceneBlackboard.Set(SceneBlackboardKeys.Player.CanSprint, true);

        // Reset deer
        _sceneBlackboard.Set(SceneBlackboardKeys.Deer.CanFlee, true);
        _sceneBlackboard.Set(SceneBlackboardKeys.Deer.Interactable, true);
        _sceneBlackboard.Set(SceneBlackboardKeys.Deer.Fed, 0);

        // Wait for player to feed deer
        await _sceneBlackboard.WaitUntilKeyMatches(SceneBlackboardKeys.Deer.Fed, _sceneBlackboard.Get<int>(SceneBlackboardKeys.Scene.DeerCount));
        CompleteSubObjective();

        // Reset deer
        _sceneBlackboard.Set(SceneBlackboardKeys.Deer.CanFlee, false);
        _sceneBlackboard.Set(SceneBlackboardKeys.Deer.Interactable, false);
        _sceneBlackboard.Set(SceneBlackboardKeys.Deer.Fed, 0);

        // [Sub Objective] Feed the wolves
        await _playerDialog.ExecuteDialogAsync("Now I could look after the wolves.");
        SetSubObjective("Feed the wolves.");

        // Reset wolves
        _sceneBlackboard.Set(SceneBlackboardKeys.Wolf.CanFlee, true);
        _sceneBlackboard.Set(SceneBlackboardKeys.Wolf.Interactable, true);
        _sceneBlackboard.Set(SceneBlackboardKeys.Wolf.Fed, 0);

        // Enable wolves
        foreach (Wolf wolf in FindObjectsByType<Wolf>())
            wolf.enabled = true;

        // Wait for player to feed all the wolves
        await _sceneBlackboard.WaitUntilKeyMatches(SceneBlackboardKeys.Wolf.Fed, _sceneBlackboard.Get<int>(SceneBlackboardKeys.Scene.WolfCount));
        CompleteSubObjective();

        // [Sub Objective] Go to the Office
        await _playerDialog.ExecuteDialogAsync("I will call it a day now.");
        SetSubObjective("Go to the office.");

        _elk.transform.SetPositionAndRotation(firstSight.transform.position, firstSight.transform.rotation);

        _sceneBlackboard.Set(SceneBlackboardKeys.Elk.IsVisible, true);

        await UniTask.Delay(5000, cancellationToken: this.GetCancellationTokenOnDestroy());

        _sceneBlackboard.Set(SceneBlackboardKeys.Elk.IsVisible, false);
        await _playerDialog.ExecuteDialogAsync("W-what was that?");
        await _playerDialog.ExecuteDialogAsync("I better hurry.");

        // Wait for player to get in the Office
        await _sceneBlackboard.WaitUntilKeyMatches(SceneBlackboardKeys.Player.InOffice, true);

        // Complete the objectives
        CompleteSubObjective();
        CompleteMainObjective();

        await UniTask.Delay(1000, cancellationToken: this.GetCancellationTokenOnDestroy());
    }

    private async UniTask ExecuteDayThree()
    {
        // Advance to the third day
        ResetBlackboardStates();
        _sceneBlackboard.Set(SceneBlackboardKeys.Scene.Day, 3);
        _sceneBlackboard.Set(SceneBlackboardKeys.Scene.DayDescription, "Things get strange?");

        await _playerDayTransition.ExecuteAsync(() =>
        {
            // Move player to the spawnpoint
            MovePlayerToSpawnpoint(officeSpawnpoint);
            MoveCarToRandomSpawnpoint();

            // Allow player to interact
            _sceneBlackboard.Set(SceneBlackboardKeys.Player.CanInteract, true);

            // Allow player to play with the light switch
            _sceneBlackboard.Set($"office_{SceneBlackboardKeys.LightSwitch.Enabled}", true);
            _sceneBlackboard.Set($"office_{SceneBlackboardKeys.LightSwitch.Interactable}", true);

            _sceneBlackboard.Set($"office_{SceneBlackboardKeys.Door.Interactable}", true);
            _sceneBlackboard.Set($"office_{SceneBlackboardKeys.Door.Locked}", false);

            _sceneBlackboard.Set(SceneBlackboardKeys.Scene.Barriers.SquareEntrance.IsActive, false);

            // Allow player to use flashlight
            _sceneBlackboard.Set(SceneBlackboardKeys.Player.Flashlight.CanEquip, true);

            // Disable second deer
            foreach (Deer deer in FindObjectsByType<Deer>())
            {
                if (deer.name == "Deer2")
                    deer.gameObject.SetActive(false);
            }
        });

        // Play self-dialog
        await _playerDialog.ExecuteDialogAsync("Maybe I am getting paranoiac...");

        // Ring the phone and make it interactable
        _sceneBlackboard.Set(SceneBlackboardKeys.Phone.Ringing, true);
        _sceneBlackboard.Set(SceneBlackboardKeys.Phone.Interactable, true);

        // Wait until player picks up the phone
        await _sceneBlackboard.WaitUntilKeyMatches(SceneBlackboardKeys.Phone.Interacted, true);

        // Reset the Phone
        _sceneBlackboard.Set(SceneBlackboardKeys.Phone.Interacted, false);
        _sceneBlackboard.Set(SceneBlackboardKeys.Phone.Interactable, false);

        // Play dialog
        _sceneBlackboard.Set(SceneBlackboardKeys.Phone.Speaking, Time.time);
        await _playerDialog.ExecuteDialogAsync("We have given you a shotgun after you mentioned that thing from yesterday.", 4f, false);

        // Play dialog
        _sceneBlackboard.Set(SceneBlackboardKeys.Phone.Speaking, Time.time);
        await _playerDialog.ExecuteDialogAsync("Take care of yourself and don't neglect animals!", 2f, false);

        // Play self-dialogs
        await _playerDialog.ExecuteDialogAsync("...");
        await _playerDialog.ExecuteDialogAsync("I'm curious if he really cares about me.");

        // [Main Objective]
        SetMainObjective("Feed the animals and check surroundings.");

        _sceneBlackboard.Set(SceneBlackboardKeys.Player.CanSprint, true);
        _sceneBlackboard.Set(SceneBlackboardKeys.Player.CanCrouch, true);
        _sceneBlackboard.Set(SceneBlackboardKeys.Player.Flashlight.CanEquip, true);

        // [Sub Objective]
        SetSubObjective("Feed wolves.");

        // Reset wolves
        _sceneBlackboard.Set(SceneBlackboardKeys.Wolf.CanFlee, true);
        _sceneBlackboard.Set(SceneBlackboardKeys.Wolf.Interactable, true);
        _sceneBlackboard.Set(SceneBlackboardKeys.Wolf.Fed, 0);

        await _sceneBlackboard.WaitUntilKeyMatches(SceneBlackboardKeys.Wolf.Fed, 3);
        CompleteSubObjective();

        _sceneBlackboard.Set(SceneBlackboardKeys.Wolf.Interactable, true);
        _sceneBlackboard.Set(SceneBlackboardKeys.Wolf.Fed, 0);

        // [Sub Objective]
        SetSubObjective("Feed deer.");

        await _playerDialog.ExecuteDialogAsync("Perfect, now it's time to feed the deer.");

        // Reset deer
        _sceneBlackboard.Set(SceneBlackboardKeys.Deer.CanFlee, true);
        _sceneBlackboard.Set(SceneBlackboardKeys.Deer.Interactable, true);
        _sceneBlackboard.Set(SceneBlackboardKeys.Deer.Fed, 0);

        await _sceneBlackboard.WaitUntilKeyMatches(SceneBlackboardKeys.Deer.Fed, 1);
        CompleteSubObjective();

        await _playerDialog.ExecuteDialogAsync("Huh, where is the second one?");
        _sceneBlackboard.Set(SceneBlackboardKeys.Scene.Warning.IsVisible, true);
        _sceneBlackboard.Set(SceneBlackboardKeys.Player.Shotgun.CanEquip, true);
        _sceneBlackboard.Set(SceneBlackboardKeys.Player.Flashlight.IsEquipped, false);

        if (!TryGetRandomPointOnNavMesh(deerForestCenter.position, 5f, out Vector3 randomPosition))
        {
            foreach (Deer deer in FindObjectsByType<Deer>())
            {
                if (deer.name == "Deer1")
                    randomPosition = deer.transform.position;
            }
        }

        _elk.transform.position = randomPosition;
        _elk.transform.DOLookAt(player.transform.position, 2f);
        _elk.GetComponent<NavMeshAgent>().ResetPath();

        _sceneBlackboard.Set(SceneBlackboardKeys.Elk.IsVisible, true);
        _sceneBlackboard.Set(SceneBlackboardKeys.Elk.Screaming, true);

        await UniTask.Delay(1500);

        _sceneBlackboard.Set(SceneBlackboardKeys.Elk.Chasing, true);

        await _sceneBlackboard.WaitUntilKeyMatches(SceneBlackboardKeys.Elk.Chasing, false);

        if (_sceneBlackboard.Get<bool>("player_caught"))
        {
            GameOverController over = FindObjectsByType<GameOverController>().FirstOrDefault();
            over.ShowGameOver();

            await UniTask.Delay(3000);

            return;
        }

        _sceneBlackboard.Set(SceneBlackboardKeys.Scene.Warning.IsVisible, false);

        await _playerDialog.ExecuteDialogAsync("Oh my fucking god.");
        await _playerDialog.ExecuteDialogAsync("I can't believe that I've survived that.");
        await _playerDialog.ExecuteDialogAsync("I am leaving this job.", 4f);

        Application.Quit();
    }

    public bool TryGetRandomPointOnNavMesh(Vector3 center, float range, out Vector3 result)
    {
        for (int i = 0; i < 30; i++)
        {
            // Get a random point on a 2D circle instead of a 3D sphere
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * range;

            // Apply it to the X and Z axes, keeping the original Y center
            Vector3 randomPoint = center + new Vector3(randomCircle.x, 0, randomCircle.y);

            // Increase the search distance (e.g., to 5.0f) to account for slopes/hills
            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 20.0f, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }

        result = Vector3.zero;
        return false;
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
        player.transform.rotation = spawnpoint.transform.rotation;

        cinemachineInputAxisController.enabled = true;
        _inputService.EnablePlayerControls();
    }

    private void MoveCarToRandomSpawnpoint()
    {
        int randomIndex = UnityEngine.Random.Range(0, carSpawnpoints.Count);
        GameObject randomSpawnpoint = carSpawnpoints[randomIndex];

        GameObject car = GameObject.FindGameObjectWithTag("Car");

        if (car == null)
            throw new NullReferenceException("Car doesn't exist!");

        car.transform.SetPositionAndRotation(randomSpawnpoint.transform.position, randomSpawnpoint.transform.rotation);
    }

    private bool TryGetActiveVirtualCamera(out CinemachineCamera cinemachineCamera)
    {
        cinemachineCamera = null;

        if (!mainCamera.TryGetComponent<CinemachineBrain>(out CinemachineBrain cinemachineBrain))
        {
            Debug.LogError($"{GetType().Name} encountered an error: CinemachineBrain component was null for {mainCamera.GetType().Name}");
            return false;
        }

        if (cinemachineBrain.ActiveVirtualCamera == null)
            return false;

        cinemachineCamera = (CinemachineCamera)cinemachineBrain.ActiveVirtualCamera;
        return true;
    }

    private void SetMainObjective(string objective) => _sceneBlackboard.Set(SceneBlackboardKeys.Scene.Objectives.MainObjective, objective);

    private void SetSubObjective(string objective)
    {
        int currentCount = _sceneBlackboard.Get<int>(SceneBlackboardKeys.Scene.Objectives.ObjectiveCount);
        int nextSubObjectiveIndex = currentCount;

        _sceneBlackboard.Set(SceneBlackboardKeys.Scene.Objectives.ObjectiveCount, currentCount + 1);

        string objectiveString = $"{nextSubObjectiveIndex}{objective}";
        _sceneBlackboard.Set(SceneBlackboardKeys.Scene.Objectives.SubObjective, objectiveString);
    }

    private void CompleteSubObjective()
    {
        string currentSubObjective = _sceneBlackboard.Get<string>(SceneBlackboardKeys.Scene.Objectives.SubObjective);
        _sceneBlackboard.Set($"{SceneBlackboardKeys.Scene.Objectives.SubObjective}{SceneBlackboardKeys.CompletedSuffix}", currentSubObjective);
    }

    private void CompleteMainObjective()
    {
        string currentMainObjective = _sceneBlackboard.Get<string>(SceneBlackboardKeys.Scene.Objectives.MainObjective);
        _sceneBlackboard.Set($"{SceneBlackboardKeys.Scene.Objectives.MainObjective}{SceneBlackboardKeys.CompletedSuffix}", currentMainObjective);
    }

    private void ResetBlackboardStates()
    {
        _sceneBlackboard.Set(SceneBlackboardKeys.Player.CanCrouch, false);
        _sceneBlackboard.Set(SceneBlackboardKeys.Player.CanJump, false);
        _sceneBlackboard.Set(SceneBlackboardKeys.Player.CanSprint, false);
        _sceneBlackboard.Set(SceneBlackboardKeys.Player.CanInteract, false);
        _sceneBlackboard.Set(SceneBlackboardKeys.Player.Flashlight.CanEquip, false);
        _sceneBlackboard.Set(SceneBlackboardKeys.Player.Flashlight.IsEquipped, false);
        _sceneBlackboard.Set(SceneBlackboardKeys.Player.Mop.Interactable, false);
        _sceneBlackboard.Set(SceneBlackboardKeys.Player.Mop.IsEquipped, false);
        _sceneBlackboard.Set(SceneBlackboardKeys.Scene.Day, 0);
        _sceneBlackboard.Set(SceneBlackboardKeys.Scene.DayDescription, string.Empty);
        _sceneBlackboard.Set(SceneBlackboardKeys.Generator.Interactable, false);
        _sceneBlackboard.Set(SceneBlackboardKeys.Computer.Interactable, false);
        _sceneBlackboard.Set(SceneBlackboardKeys.Computer.Interacted, false);
        _sceneBlackboard.Set(SceneBlackboardKeys.Phone.Interactable, false);
        _sceneBlackboard.Set(SceneBlackboardKeys.Phone.Interacted, false);
        _sceneBlackboard.Set(SceneBlackboardKeys.LightSwitch.Interactable, false);
        _sceneBlackboard.Set(SceneBlackboardKeys.LightSwitch.Enabled, false);
        _sceneBlackboard.Set(SceneBlackboardKeys.Door.Opened, false);
        _sceneBlackboard.Set(SceneBlackboardKeys.Scene.Waypoints.Office.IsActive, false);
        _sceneBlackboard.Set(SceneBlackboardKeys.Scene.Waypoints.Graffiti.IsActive, false);
        _sceneBlackboard.Set(SceneBlackboardKeys.Scene.Waypoints.Generator.IsActive, false);
    }
}