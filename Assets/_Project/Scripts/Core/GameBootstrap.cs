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

        private void Awake()
        {
            if (settings == null || playerPrefab == null || sectorPrefab == null || orePrefab == null || oreSpawnSettings == null)
            {
                Debug.LogError("Core settings, Player/Sector/Ore prefabs and OreSpawnSettings are required.", this);
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
            CreateChild("DebugHUD").AddComponent<CorePlayDebugHud>().Initialize(world, player.transform, ores);
        }

        private GameObject CreateChild(string objectName)
        {
            var child = new GameObject(objectName);
            child.transform.SetParent(transform, false);
            return child;
        }

    }
}
