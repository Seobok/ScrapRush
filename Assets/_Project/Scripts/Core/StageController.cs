using System;
using ScrapRush.Player;
using ScrapRush.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScrapRush.Core
{
    public enum StageState
    {
        Idle,
        Playing,
        Success,
        Failure
    }

    public sealed class StageController : MonoBehaviour
    {
        private StageSettings settings;
        private OreSpawner ores;
        private ScrapSystem scraps;
        private PlayerMotor player;
        private PlayerAutoMiner miner;
        private MagnetSystem magnet;
        private ElectricSystem electric;

        public StageState State { get; private set; } = StageState.Idle;
        public int StageNumber { get; private set; }
        public int Capacity => settings == null ? 0 : Mathf.Min(settings.maximumCapacity,
            settings.capacity + (StageNumber - 1) * settings.capacityIncreasePerStage);
        public int Quota => settings == null ? 0 : settings.quota +
            (StageNumber - 1) * settings.quotaIncreasePerStage;
        public float Duration => settings == null ? 0f : settings.duration;
        public float RemainingTime { get; private set; }
        public long CurrentCredits => scraps == null ? 0 : scraps.Credits;
        public float QuotaProgress => Quota <= 0 ? 0f : Mathf.Clamp01((float)CurrentCredits / Quota);
        public event Action<StageState> StateChanged;

        public void Initialize(StageSettings configuration, OreSpawner oreSpawner, ScrapSystem scrapSystem,
            PlayerMotor playerMotor, MagnetSystem magnetSystem, ElectricSystem electricSystem)
        {
            if (configuration == null || configuration.duration <= 0f || configuration.quota < 1 ||
                configuration.quotaIncreasePerStage < 0 || configuration.capacity < 1 ||
                configuration.capacity > configuration.maximumCapacity ||
                configuration.capacityIncreasePerStage < 0 || oreSpawner == null ||
                scrapSystem == null || playerMotor == null || magnetSystem == null || electricSystem == null)
                throw new ArgumentException("StageController requires valid settings and gameplay systems.");

            settings = configuration;
            ores = oreSpawner;
            scraps = scrapSystem;
            player = playerMotor;
            miner = player.GetComponent<PlayerAutoMiner>();
            magnet = magnetSystem;
            electric = electricSystem;
            if (miner == null) throw new ArgumentException("The stage player requires PlayerAutoMiner.");
            StartRun();
        }

        private void Update()
        {
            if (State != StageState.Playing)
            {
                if (Application.isFocused && Keyboard.current != null)
                {
                    if (Keyboard.current.rKey.wasPressedThisFrame) StartRun();
                    else if (State == StageState.Success &&
                        (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame))
                        StartNextStage();
                }
                return;
            }
            if (!Application.isFocused || Time.timeScale == 0f) return;
            Tick(Time.deltaTime);
        }

        public void StartRun()
        {
            if (settings == null) return;
            StageNumber = 1;
            BeginStage(true);
        }

        public void StartNextStage()
        {
            if (State != StageState.Success) return;
            StageNumber++;
            BeginStage(false);
        }

        private void BeginStage(bool resetRun)
        {
            SetGameplayEnabled(false);
            if (resetRun) scraps.ResetRun();
            else scraps.ResetStage();
            ores.ResetStage();
            player.transform.position = Vector3.zero;
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null) body.linearVelocity = Vector2.zero;
            miner.ResetStageState();
            electric.ResetStageState();
            RemainingTime = settings.duration;
            SetState(StageState.Playing);
            SetGameplayEnabled(true);
        }

        internal void Tick(float deltaTime)
        {
            if (State != StageState.Playing || deltaTime <= 0f) return;
            RemainingTime = Mathf.Max(0f, RemainingTime - deltaTime);
            if (RemainingTime <= 0f) FinishStage();
        }

        private void FinishStage()
        {
            StageState result = CurrentCredits >= Quota ? StageState.Success : StageState.Failure;
            scraps.ClearStage();
            miner.ResetStageState();
            electric.ResetStageState();
            SetGameplayEnabled(false);
            SetState(result);
        }

        private void SetGameplayEnabled(bool value)
        {
            if (player != null) player.enabled = value;
            if (miner != null) miner.enabled = value;
            if (scraps != null) scraps.enabled = value;
            if (magnet != null) magnet.enabled = value;
            if (electric != null) electric.enabled = value;
        }

        private void SetState(StageState value)
        {
            State = value;
            StateChanged?.Invoke(value);
        }
    }
}
