using ScrapRush.Debugging;
using ScrapRush.Player;
using ScrapRush.World;
using UnityEngine;

namespace ScrapRush.Core
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private CorePlaySettings settings = null;
        [SerializeField] private PlayerMotor playerPrefab = null;
        [SerializeField] private SectorDefinition sectorPrefab = null;
        [SerializeField] private OreNode orePrefab = null;
        [SerializeField] private OreSpawnSettings oreSpawnSettings = null;
        [SerializeField] private StageSettings stageSettings = null;

        [SerializeField] private ScrapSettings scrapSettings = null;
        [SerializeField] private MagnetSettings magnetSettings = null;
        [SerializeField] private ElectricSettings electricSettings = null;

        private void Awake()
        {
            if (settings == null || playerPrefab == null || sectorPrefab == null || orePrefab == null ||
                oreSpawnSettings == null || stageSettings == null || scrapSettings == null ||
                magnetSettings == null || electricSettings == null)
            {
                Debug.LogError("Core, Stage, Ore, Scrap, Magnet and Electric settings are required.", this);
                enabled = false;
                return;
            }

            var world = CreateChild("WorldRoot").AddComponent<SectorWorld>();
            world.Initialize(sectorPrefab);
            var ores = CreateChild("OreRoot").AddComponent<OreSpawner>();
            ores.Initialize(world, orePrefab, oreSpawnSettings);
            var player = Instantiate(playerPrefab, transform);
            player.name = "Player";
            player.transform.position = Vector3.zero;
            player.Initialize(world, settings.moveSpeed, settings.playerRadius);
            player.GetComponent<PlayerAutoMiner>().Initialize(ores);

            var scraps = CreateChild("ScrapRoot").AddComponent<ScrapSystem>();
            scraps.Initialize(ores, player.transform, scrapSettings, settings.playerRadius);
            var magnet = player.gameObject.AddComponent<MagnetSystem>();
            magnet.Initialize(scraps, ores, player.transform, magnetSettings, settings.startingMagnetCount);
            var electric = player.gameObject.AddComponent<ElectricSystem>();
            electric.Initialize(ores, player.transform, electricSettings, settings.startingElectricCount);

            Camera camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = CreateChild("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }
            var follow = camera.GetComponent<CameraFollow>();
            if (follow == null) follow = camera.gameObject.AddComponent<CameraFollow>();
            follow.Initialize(player.transform, world.Bounds, settings.cameraSize, settings.cameraSmoothTime);
            var stage = CreateChild("StageController").AddComponent<StageController>();
            stage.Initialize(stageSettings, ores, scraps, player, magnet, electric);
            GameObject hudObject = GameObject.Find("HUD_TopPressureBar");
            if (hudObject == null)
            {
                Debug.LogError("HUD_TopPressureBar must be present in the gameplay scene.", this);
            }
            else
            {
                var hud = hudObject.GetComponent<StageHud>();
                if (hud == null) hud = hudObject.AddComponent<StageHud>();
                hud.Initialize(stage);
            }
            CreateChild("DebugHUD").AddComponent<CorePlayDebugHud>().Initialize(world, player.transform, ores,
                scraps, magnet, electric);
        }

        private GameObject CreateChild(string objectName)
        {
            var child = new GameObject(objectName);
            child.transform.SetParent(transform, false);
            return child;
        }

    }
}
