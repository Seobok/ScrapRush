using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ScrapRush.Core;
using ScrapRush.Player;
using ScrapRush.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ScrapRush.Editor
{
    // Run only in an isolated validation project: enters Play Mode and exits the batch editor.
    public static class MiningValidation
    {
        private const string Pending = "ScrapRush.MiningValidation.Pending";
        private static readonly List<string> Passed = new List<string>();
        private static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use an isolated batch-mode project for this validation.");
            SessionState.SetBool(Pending, true);
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/CorePlayTest.unity");
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void Resume()
        {
            if (SessionState.GetBool(Pending, false)) EditorApplication.update += Update;
        }

        private static void Update()
        {
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            var spawner = Object.FindFirstObjectByType<OreSpawner>();
            if (spawner == null) return;
            EditorApplication.update -= Update;
            SessionState.SetBool(Pending, false);
            try
            {
                Validate(spawner);
                ValidateTargetEffect();
                ValidateMiningHit(spawner);
                ValidateElectric();
                ValidateScrap(spawner);
                File.WriteAllLines(Path.Combine(Root, "mining-validation.txt"), Passed);
                Debug.Log("MINING VALIDATION PASSED: " + Passed.Count);
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                File.WriteAllText(Path.Combine(Root, "mining-validation.txt"), string.Join("\n", Passed) + "\nFAILED: " + error);
                Debug.LogException(error);
                EditorApplication.Exit(1);
            }
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
            Passed.Add("PASS: " + message);
        }

        private static void Validate(OreSpawner spawner)
        {
            var world = Object.FindFirstObjectByType<SectorWorld>();
            var miner = Object.FindFirstObjectByType<PlayerAutoMiner>();
            var prefab = AssetDatabase.LoadAssetAtPath<OreNode>("Assets/_Project/Prefabs/World/Ore.prefab");
            var settings = AssetDatabase.LoadAssetAtPath<OreSpawnSettings>("Assets/_Project/Data/OreSpawnSettings.asset");
            var mining = AssetDatabase.LoadAssetAtPath<MiningSettings>("Assets/_Project/Data/MiningSettings.asset");
            Check(world != null && miner != null && Object.FindFirstObjectByType<GameBootstrap>() != null,
                "CorePlayTest bootstrap initializes world, ore population and player miner");
            Check(spawner.Nodes.Count > 100 && spawner.transform.childCount == 9,
                "Nine sectors contain a populated set of ore clusters");
            Check(spawner.Nodes.Select(n => n.Definition.kind).Distinct().Count() == 4,
                "Default seed contains all four ore types");
            Check(settings.ores.Select(e => e.weight).SequenceEqual(new[] {60,27,10,3}) &&
                settings.ores.Select(e => e.definition.maxHealth).SequenceEqual(new[] {1,2,3,5}) &&
                settings.ores.Select(e => e.definition.baseValue).SequenceEqual(new[] {10,25,80,250}),
                "Ore HP, base values and spawn weights match the P0 hypotheses");
            Check(mining.range == 1.4f && mining.interval == 0.5f && mining.damage == 1,
                "Mining range, interval and damage match the P0 hypotheses");
            bool valid = true;
            for (int i = 0; i < spawner.Nodes.Count; i++)
            {
                var a = spawner.Nodes[i];
                var visual = a.GetComponentInChildren<SpriteRenderer>();
                valid &= visual.sprite != null && visual.sprite.rect.width > 1000 &&
                    Mathf.Abs(Mathf.Max(visual.bounds.size.x, visual.bounds.size.y) - a.Definition.visualSize) < 0.01f;
                for (int j = i + 1; j < spawner.Nodes.Count; j++)
                {
                    var b = spawner.Nodes[j];
                    valid &= Vector2.Distance(a.transform.position, b.transform.position) + 0.001f >=
                        a.Definition.collisionRadius + b.Definition.collisionRadius + settings.spacing;
                }
                Vector2 p = a.transform.position;
                Vector2Int sector = world.GetSector(p);
                float minX = world.Bounds.xMin + sector.x * world.SectorSize;
                float maxY = world.Bounds.yMax - sector.y * world.SectorSize;
                float margin = settings.sectorMargin + a.Definition.collisionRadius;
                valid &= p.x >= minX + margin && p.x <= minX + world.SectorSize - margin &&
                    p.y >= maxY - world.SectorSize + margin && p.y <= maxY - margin;
            }
            Check(valid, "Sprites use the full ore body at configured size; spawns obey spacing and sector margins");
            var repeat = new GameObject("Repeat").AddComponent<OreSpawner>();
            repeat.Initialize(world, prefab, settings);
            Check(repeat.Nodes.Count == spawner.Nodes.Count && repeat.Nodes.Select((n,i) =>
                    n.Definition == spawner.Nodes[i].Definition && n.transform.position == spawner.Nodes[i].transform.position).All(x => x),
                "The same seed reproduces identical ore types and positions");
            Object.DestroyImmediate(repeat.gameObject);

            int count = spawner.Nodes.Count;
            Check(spawner.FindNearest(new Vector2(1000,1000), mining.range) == null, "No target outside mining range");
            var tick = typeof(PlayerAutoMiner).GetMethod("Tick", BindingFlags.Instance | BindingFlags.NonPublic);
            var cores = spawner.Nodes.Where(n => n.Definition.kind == OreKind.Core).Take(2).ToArray();
            Check(cores.Length == 2, "Default population supports target-switch validation");
            miner.enabled = false;
            miner.Initialize(spawner);
            miner.transform.position = cores[0].transform.position;
            tick.Invoke(miner, new object[] {0f});
            Check(cores[0].Health == 4, "First target receives immediate damage");
            miner.transform.position = cores[1].transform.position;
            tick.Invoke(miner, new object[] {0.25f});
            Check(miner.Target == cores[1] && cores[1].Health == 5, "Movement switches to nearest target without an extra hit");
            tick.Invoke(miner, new object[] {0.25f});
            Check(cores[1].Health == 4 && cores[0].Health == 4, "Target switch retains cooldown and previous target HP");
            miner.transform.position = new Vector3(1000,1000,0);
            tick.Invoke(miner, new object[] {10f});
            miner.transform.position = cores[0].transform.position;
            tick.Invoke(miner, new object[] {0f});
            Check(cores[0].Health == 3 && miner.HitCount == 3, "Idle time preserves readiness without accumulating burst hits");

            int events = 0;
            OreBreakInfo last = default;
            spawner.OreBroken += info => { events++; last = info; };
            var broken = cores[0];
            Vector2 breakPosition = broken.transform.position;
            broken.ApplyDamage(3, MiningSource.Electric);
            bool secondHit = broken.ApplyDamage(100, MiningSource.Gravity);
            Check(!secondHit && events == 1 && spawner.Nodes.Count == count - 1 && !broken.IsAlive &&
                !broken.GetComponent<Collider2D>().enabled && last.Source == MiningSource.Electric &&
                last.Position == breakPosition && last.BaseValue == 250,
                "Break invalidates target once and carries position, value and killing source");

            foreach (var entry in settings.ores)
            {
                var node = Object.Instantiate(prefab, new Vector3(200,200,0), Quaternion.identity);
                node.Initialize(entry.definition);
                int breaks = 0;
                node.Broken += (n,info) => breaks++;
                for (int h=1; h <= entry.definition.maxHealth; h++)
                {
                    node.ApplyDamage(1, MiningSource.Basic);
                    if ((h < entry.definition.maxHealth && !node.IsAlive) ||
                        (h == entry.definition.maxHealth && (node.IsAlive || breaks != 1)))
                        throw new Exception("Incorrect hits-to-break for " + entry.definition.kind);
                }
            }
            Check(true, "Iron/Copper/Gold/Core break after exactly 1/2/3/5 basic hits");

            Check(!Physics2D.GetIgnoreLayerCollision(8,8) && Physics2D.GetIgnoreLayerCollision(0,8) &&
                prefab.GetComponent<Rigidbody2D>().bodyType == RigidbodyType2D.Dynamic &&
                prefab.GetComponent<Rigidbody2D>().gravityScale == 0 && !prefab.GetComponent<Collider2D>().isTrigger,
                "Ore physics collides with ore and passes through player layer");
            var aPhysics = Object.Instantiate(prefab, new Vector3(100,100,0), Quaternion.identity);
            var bPhysics = Object.Instantiate(prefab, new Vector3(100.1f,100,0), Quaternion.identity);
            aPhysics.Initialize(settings.ores[0].definition);
            bPhysics.Initialize(settings.ores[0].definition);
            var oldMode = Physics2D.simulationMode;
            Physics2D.simulationMode = SimulationMode2D.Script;
            Physics2D.SyncTransforms();
            for (int step=0;step<50;step++) Physics2D.Simulate(0.02f);
            Physics2D.simulationMode = oldMode;
            Check(Vector2.Distance(aPhysics.GetComponent<Rigidbody2D>().position, bPhysics.GetComponent<Rigidbody2D>().position) > 0.85f,
                "Physics simulation separates overlapping ores");
            Object.DestroyImmediate(aPhysics.gameObject);
            Object.DestroyImmediate(bPhysics.gameObject);
        }
        private static void ValidateMiningHit(OreSpawner spawner)
        {
            foreach (var old in Object.FindObjectsByType<MiningHitEffect>(FindObjectsSortMode.None))
                Object.DestroyImmediate(old.gameObject);
            var settings = AssetDatabase.LoadAssetAtPath<MiningSettings>("Assets/_Project/Data/MiningSettings.asset");
            Check(settings.hitFrames.Length == 6 && settings.hitFrames.Select((s, i) =>
                s != null && s.name == "VFX_MIN_002_MiningHit_v02_" + i && s.rect.width == 362 && s.rect.height == 362).All(x => x),
                "MiningHit references all six sheet frames in row order");
            var miner = Object.FindFirstObjectByType<PlayerAutoMiner>();
            miner.Initialize(spawner);
            var ore = spawner.Nodes.First(n => n.Definition.kind == OreKind.Iron);
            Vector3 position = ore.transform.position;
            miner.transform.position = position;
            var tick = typeof(PlayerAutoMiner).GetMethod("Tick", BindingFlags.Instance | BindingFlags.NonPublic);
            tick.Invoke(miner, new object[] {0f});
            var effects = Object.FindObjectsByType<MiningHitEffect>(FindObjectsSortMode.None).Where(e => e.name == "MiningHit").ToArray();
            Check(!ore.IsAlive && miner.HitCount == 1 && effects.Length == 1,
                "A lethal automatic hit creates exactly one surviving hit effect");
            var effect = effects[0];
            var breaks = Object.FindObjectsByType<MiningHitEffect>(FindObjectsSortMode.None).Where(e => e.name == "OreBreak").ToArray();
            Check(breaks.Length == 1 && breaks[0].transform.position == position && breaks[0].transform.parent == null,
                "Lethal hit creates one independent OreBreak at the destruction position");
            ore.ApplyDamage(100, MiningSource.Basic);
            Check(Object.FindObjectsByType<MiningHitEffect>(FindObjectsSortMode.None).Count(e => e.name == "OreBreak") == 1,
                "Already broken ore never emits a second OreBreak");
            var breakTick = typeof(MiningHitEffect).GetMethod("Tick", BindingFlags.Instance | BindingFlags.NonPublic);
            var breakRenderer = breaks[0].GetComponent<SpriteRenderer>();
            for (int i = 0; i < 8; i++)
            {
                if (i > 0) breakTick.Invoke(breaks[0], new object[] {1f / 24f + 0.00001f});
                Check(breakRenderer.sprite != null && breakRenderer.sprite.name == "VFX_ORE_001_OreBreak_v01_" + i,
                    "OreBreak plays frame " + (i + 1));
            }
            breakTick.Invoke(breaks[0], new object[] {1f / 24f});
            Check(!breakRenderer.enabled, "OreBreak finishes once and schedules cleanup");
            Object.DestroyImmediate(breaks[0].gameObject);
            var renderer = effect.GetComponent<SpriteRenderer>();
            miner.transform.position += Vector3.right * 100;
            tick.Invoke(miner, new object[] {0.1f});
            Check(effect.transform.position == position && effect.transform.parent == null &&
                Object.FindObjectsByType<MiningHitEffect>(FindObjectsSortMode.None).Count(e => e.name == "MiningHit") == 1,
                "MiningHit stays at impact position and no-target cooldown creates no extra effect");
            var effectTick = typeof(MiningHitEffect).GetMethod("Tick", BindingFlags.Instance | BindingFlags.NonPublic);
            for (int i = 0; i < settings.hitFrames.Length; i++)
            {
                if (i > 0) effectTick.Invoke(effect, new object[] {1f / 24f + 0.00001f});
                Check(renderer.sprite == settings.hitFrames[i], "MiningHit plays frame " + (i + 1));
            }
            Check(renderer.color.a < 1, "MiningHit fades its final glow");
            effectTick.Invoke(effect, new object[] {1f / 24f});
            Check(!renderer.enabled, "MiningHit stops after one playback and schedules cleanup");
            Object.DestroyImmediate(effect.gameObject);
        }

        private static void ValidateTargetEffect()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<OreNode>("Assets/_Project/Prefabs/World/Ore.prefab");
            var settings = AssetDatabase.LoadAssetAtPath<OreSpawnSettings>("Assets/_Project/Data/OreSpawnSettings.asset");
            var node = Object.Instantiate(prefab, new Vector3(200, 200, 0), Quaternion.identity);
            node.Initialize(settings.ores[3].definition);
            node.SetSelected(true);
            var effect = node.transform.Find("MiningTarget").GetComponent<SpriteRenderer>();
            var update = typeof(OreNode).GetMethod("UpdateTargetEffect", BindingFlags.Instance | BindingFlags.NonPublic);
            for (int i = 0; i < 8; i++)
            {
                if (i > 0) update.Invoke(node, new object[] {1f / 24f + 0.00001f});
                Check(effect.enabled && effect.sprite != null && effect.sprite.name.Contains("_0" + (i + 1) + "_v01") &&
                    effect.sprite.rect.width == 256 && effect.sprite.rect.height == 256,
                    "Target acquisition frame " + (i + 1) + " resolves as a full centered sprite");
            }
            update.Invoke(node, new object[] {1f / 24f});
            Check(effect.sprite != null && effect.sprite.name.Contains("Loop"), "Acquisition transitions to Loop sprite");
            Vector3 scale = effect.transform.localScale;
            update.Invoke(node, new object[] {0.45f});
            Check(effect.transform.localScale.x > scale.x && effect.color.a < 1,
                "Loop pulses in scale and opacity");
            node.SetSelected(false);
            Check(!effect.enabled, "Deselect hides the target effect immediately");
            node.SetSelected(true);
            Check(effect.enabled && effect.sprite.name.Contains("_01_v01"), "Reacquisition restarts Sequence");
            node.ApplyDamage(100, MiningSource.Basic);
            Check(!effect.enabled, "Ore break hides the target effect immediately");
        }

        private static void ValidateScrap(OreSpawner ores)
        {
            var scraps = Object.FindFirstObjectByType<ScrapSystem>();
            var miner = Object.FindFirstObjectByType<PlayerAutoMiner>();
            var settings = AssetDatabase.LoadAssetAtPath<ScrapSettings>("Assets/_Project/Data/ScrapSettings.asset");
            Check(scraps != null && settings.sprite != null && settings.absorbRange == 1.75f &&
                settings.absorbContactFrames.Length == 6 && settings.absorbContactFrames.Select((sprite, index) =>
                    sprite != null && sprite.name == "VFX_RES_003_AbsorbContactPop_v01_" + index &&
                    sprite.rect.width == 512 && sprite.rect.height == 512).All(x => x),
                "Scrap bootstrap, drop sprite and six contact-effect frames are wired");
            scraps.Initialize(ores, miner.transform, settings, 0.45f);
            var tick = typeof(ScrapSystem).GetMethod("Tick", BindingFlags.Instance | BindingFlags.NonPublic);
            Action<float> step = dt => tick.Invoke(scraps, new object[] {dt});
            int events = 0;
            OreBreakInfo last = default;
            scraps.Absorbed += info => { events++; last = info; };
            var node = ores.Nodes.First(n => n.Definition.kind == OreKind.Gold);
            Vector2 origin = node.transform.position;
            miner.transform.position = origin + Vector2.right * 20;
            node.ApplyDamage(100, new MiningDamageContext(MiningSource.Electric, 99, 0, 0.25f));
            node.ApplyDamage(100, MiningSource.Electric);
            Check(scraps.GeneratedCount == 1 && scraps.Drops.Count == 1 && scraps.Credits == 0 && events == 0 &&
                scraps.Drops[0].Origin.FinalCModifier == 0.25f && scraps.Drops[0].Origin.FinalValue == 100,
                "Ore break creates one modifier-carrying Scrap without paying C or duplicate generation");
            var pooledDrop = scraps.Drops[0];
            var trail = pooledDrop.GetComponent<TrailRenderer>();
            Check(trail != null && !trail.enabled && !trail.emitting && trail.sharedMaterial != null,
                "Scrap Trail exists but stays disabled before absorption");
            step(3600);
            Check(scraps.Drops.Count == 1 && !scraps.Drops[0].IsTracking && scraps.Credits == 0,
                "Out-of-range Scrap persists indefinitely without income");
            miner.transform.position = origin + Vector2.right * 1.75f;
            step(0.01f);
            Check(scraps.Drops[0].IsTracking && trail.enabled && trail.emitting && scraps.Credits == 0,
                "Range boundary starts tracking and Trail emission without early payment");
            miner.transform.position = origin + Vector2.right * 10;
            for (int i = 0; i < 200; i++) step(0.02f);
            Check(scraps.Drops.Count == 0 && scraps.Credits == 100 && events == 1 && last.Source == MiningSource.Electric &&
                last.RootEffectId == 99 && last.FinalValue == 100,
                "Tracking continues outside acquisition range and pays modified gold value with source context once");
            int pooledAfterAbsorb = scraps.PooledCount;
            Check(!pooledDrop.gameObject.activeSelf && pooledAfterAbsorb > 0,
                "Absorbed Scrap disables and returns to the pool");
            var contactEffects = Object.FindObjectsByType<ScrapAbsorbEffect>(FindObjectsSortMode.None);
            Check(contactEffects.Length == 1 && contactEffects[0].IsPlaying &&
                contactEffects[0].GetComponent<SpriteRenderer>().sprite == settings.absorbContactFrames[0],
                "Scrap arrival plays one contact effect at its first frame");
            var contactTick = typeof(ScrapAbsorbEffect).GetMethod("Tick", BindingFlags.Instance | BindingFlags.NonPublic);
            for (int i = 1; i < settings.absorbContactFrames.Length; i++)
            {
                contactTick.Invoke(contactEffects[0], new object[] {1f / settings.absorbContactFramesPerSecond + 0.00001f});
                Check(contactEffects[0].GetComponent<SpriteRenderer>().sprite == settings.absorbContactFrames[i],
                    "Scrap contact effect plays frame " + (i + 1));
            }
            contactTick.Invoke(contactEffects[0], new object[] {1f / settings.absorbContactFramesPerSecond});
            Check(!contactEffects[0].gameObject.activeSelf && !contactEffects[0].IsPlaying,
                "Scrap contact effect returns to its pool after one playback");
            step(1);
            Check(events == 1 && scraps.Credits == 100, "Collected Scrap never pays twice");
            step(30);
            Check(scraps.RecentCredits == 0 && scraps.Credits == 100, "Rolling income expires while session C persists");
            foreach (var kind in new[] {OreKind.Iron, OreKind.Copper, OreKind.Core})
            {
                var ore = ores.Nodes.First(n => n.Definition.kind == kind);
                miner.transform.position = ore.transform.position;
                ore.ApplyDamage(100, MiningSource.Basic);
                step(0);
                Check(scraps.Drops.Count == 1, "Zero delta pauses Scrap: " + kind);
                if (kind == OreKind.Iron)
                    Check(scraps.PooledCount == pooledAfterAbsorb - 1, "A pooled Scrap is reused for the next drop");
                step(1);
            }
            Check(scraps.Credits == 385 && scraps.AbsorbedCount == 4 && events == 4,
                "Three base values and one +25% electric value settle to exactly 385 C");
            var remaining = ores.Nodes.Take(100).ToArray();
            miner.transform.position = new Vector3(1000,1000,0);
            foreach (var ore in remaining) ore.ApplyDamage(100, MiningSource.Gravity);
            Check(scraps.Drops.Count == 100 && scraps.PeakActiveCount == 100, "Mass generation retains all 100 drops");
            scraps.ClearStage();
            step(1);
            Check(scraps.Drops.Count == 0 && scraps.ClearedCount == 100 && scraps.Credits == 385 && events == 4,
                "Stage cleanup discards remaining Scrap without currency or Absorb events");
        }

        private static void ValidateElectric()
        {
            var world = Object.FindFirstObjectByType<SectorWorld>();
            var prefab = AssetDatabase.LoadAssetAtPath<OreNode>("Assets/_Project/Prefabs/World/Ore.prefab");
            var spawnSettings = AssetDatabase.LoadAssetAtPath<OreSpawnSettings>("Assets/_Project/Data/OreSpawnSettings.asset");
            var electricSettings = AssetDatabase.LoadAssetAtPath<ElectricSettings>("Assets/_Project/Data/ElectricSettings.asset");
            Check(electricSettings != null && electricSettings.cooldown == 6f && electricSettings.targetRange == 6f &&
                electricSettings.damage == 1 && electricSettings.chainRange == 3f &&
                electricSettings.maxChainTargets == 4 && electricSettings.stormExtraDischarges == 4 &&
                electricSettings.stormInterval == 0.5f && electricSettings.enableElectricValueBonus &&
                electricSettings.electricValueBonus == 0.25f,
                "Electric settings match the P0 2/4/6 hypotheses");

            var root = new GameObject("Electric Validation");
            var player = new GameObject("Electric Player");
            player.transform.SetParent(root.transform, false);
            var spawner = new GameObject("Electric Ores").AddComponent<OreSpawner>();
            spawner.transform.SetParent(root.transform, false);
            spawner.Initialize(world, prefab, spawnSettings);
            foreach (var ore in spawner.Nodes) ore.transform.position = new Vector3(500, 500, 0);

            var core = spawner.Nodes.First(n => n.Definition.kind == OreKind.Core);
            var electric = player.AddComponent<ElectricSystem>();
            electric.Initialize(spawner, player.transform, electricSettings, 2);
            var electricTick = typeof(ElectricSystem).GetMethod("Tick", BindingFlags.Instance | BindingFlags.NonPublic);
            Action<float> stepElectric = dt => electricTick.Invoke(electric, new object[] {dt});
            stepElectric(6f);
            stepElectric(100f);
            Check(electric.IsReady && electric.StaticTriggerCount == 0 && core.Health == 5,
                "Electric 2 holds READY without a target and never banks missed discharges");
            core.transform.position = Vector3.right;
            stepElectric(0.01f);
            Check(!electric.IsReady && electric.StaticTriggerCount == 1 && electric.DischargeCount == 1 &&
                electric.HitCount == 1 && core.Health == 4 && electric.CooldownRemaining == electricSettings.cooldown,
                "Electric 2 fires once when a target enters range and restarts cooldown on the actual discharge");

            electric.SetTraitCount(0);
            core.transform.position = new Vector3(500, 500, 0);
            var chainNodes = spawner.Nodes.Where(n => n.Definition.kind == OreKind.Iron).Take(5).ToArray();
            Check(chainNodes.Length == 5, "Electric validation population contains five chain targets");
            for (int i = 0; i < chainNodes.Length; i++) chainNodes[i].transform.position = new Vector3(1f + i * 2.5f, 0, 0);
            electric.SetTraitCount(4);
            stepElectric(6f);
            Check(electric.StaticTriggerCount == 2 && electric.DischargeCount == 2 &&
                electric.HitCount == 5 && electric.ChainHitCount == 3 &&
                chainNodes.Take(4).All(n => !n.IsAlive) && chainNodes[4].IsAlive,
                "Electric 4 chains through the nearest unhit ores and stops at four total targets");

            electric.SetTraitCount(0);
            foreach (var ore in spawner.Nodes) if (ore != null && ore.IsAlive) ore.transform.position = new Vector3(500, 500, 0);
            var stormCore = spawner.Nodes.First(n => n.Definition.kind == OreKind.Core && n != core);
            stormCore.transform.position = Vector3.right;
            int dischargesBeforeStorm = electric.DischargeCount;
            int hitsBeforeStorm = electric.HitCount;
            electric.SetTraitCount(6);
            stepElectric(6f);
            stepElectric(0.49f);
            Check(electric.StormCount == 1 && electric.StormShotsRemaining == 4 && stormCore.Health == 4,
                "Electric 6 starts Storm with one immediate discharge and waits 0.5 seconds for extras");
            stepElectric(0.01f);
            stepElectric(1.5f);
            Check(electric.DischargeCount - dischargesBeforeStorm == 5 && electric.HitCount - hitsBeforeStorm == 5 &&
                electric.StormShotsRemaining == 0 && !stormCore.IsAlive,
                "Electric 6 performs exactly four scheduled extras and may retarget the same living ore between discharges");

            electric.SetTraitCount(0);
            Check(electric.ActiveTier == 0 && !electric.IsReady && electric.StormShotsRemaining == 0,
                "Disabling Electric clears charge and pending Storm work");
            foreach (var effect in Object.FindObjectsByType<ElectricArcEffect>(FindObjectsSortMode.None))
                Object.DestroyImmediate(effect.gameObject);
            Object.DestroyImmediate(root);
        }
    }
}
