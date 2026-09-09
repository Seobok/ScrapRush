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
        private static void ValidateScrap(OreSpawner ores)
        {
            var scraps = Object.FindFirstObjectByType<ScrapSystem>();
            var miner = Object.FindFirstObjectByType<PlayerAutoMiner>();
            var settings = AssetDatabase.LoadAssetAtPath<ScrapSettings>("Assets/_Project/Data/ScrapSettings.asset");
            Check(scraps != null && settings.sprite != null && settings.absorbRange == 1.75f,
                "Scrap bootstrap and imported sprite are wired with range 1.75");
            scraps.Initialize(ores, miner.transform, settings, 0.45f);
            var tick = typeof(ScrapSystem).GetMethod("Tick", BindingFlags.Instance | BindingFlags.NonPublic);
            Action<float> step = dt => tick.Invoke(scraps, new object[] {dt});
            int events = 0;
            OreBreakInfo last = default;
            scraps.Absorbed += info => { events++; last = info; };
            var node = ores.Nodes.First(n => n.Definition.kind == OreKind.Gold);
            Vector2 origin = node.transform.position;
            miner.transform.position = origin + Vector2.right * 20;
            node.ApplyDamage(100, MiningSource.Electric);
            node.ApplyDamage(100, MiningSource.Electric);
            Check(scraps.GeneratedCount == 1 && scraps.Drops.Count == 1 && scraps.Credits == 0 && events == 0,
                "Ore break creates one Scrap without paying C or duplicate generation");
            step(3600);
            Check(scraps.Drops.Count == 1 && !scraps.Drops[0].IsTracking && scraps.Credits == 0,
                "Out-of-range Scrap persists indefinitely without income");
            miner.transform.position = origin + Vector2.right * 1.75f;
            step(0.01f);
            Check(scraps.Drops[0].IsTracking && scraps.Credits == 0, "Range boundary starts tracking without early payment");
            miner.transform.position = origin + Vector2.right * 10;
            for (int i = 0; i < 200; i++) step(0.02f);
            Check(scraps.Drops.Count == 0 && scraps.Credits == 80 && events == 1 && last.Source == MiningSource.Electric,
                "Tracking continues outside acquisition range and pays exact gold value with one source-preserving event");
            step(1);
            Check(events == 1 && scraps.Credits == 80, "Collected Scrap never pays twice");
            step(30);
            Check(scraps.RecentCredits == 0 && scraps.Credits == 80, "Rolling income expires while session C persists");
            foreach (var kind in new[] {OreKind.Iron, OreKind.Copper, OreKind.Core})
            {
                var ore = ores.Nodes.First(n => n.Definition.kind == kind);
                miner.transform.position = ore.transform.position;
                ore.ApplyDamage(100, MiningSource.Basic);
                step(0);
                Check(scraps.Drops.Count == 1, "Zero delta pauses Scrap: " + kind);
                step(1);
            }
            Check(scraps.Credits == 365 && scraps.AbsorbedCount == 4 && events == 4,
                "All four ore values settle to exactly 365 C and four Absorb events");
            var remaining = ores.Nodes.Take(100).ToArray();
            miner.transform.position = new Vector3(1000,1000,0);
            foreach (var ore in remaining) ore.ApplyDamage(100, MiningSource.Gravity);
            Check(scraps.Drops.Count == 100 && scraps.PeakActiveCount == 100, "Mass generation retains all 100 drops");
            scraps.ClearStage();
            step(1);
            Check(scraps.Drops.Count == 0 && scraps.ClearedCount == 100 && scraps.Credits == 365 && events == 4,
                "Stage cleanup discards remaining Scrap without currency or Absorb events");
        }
    }
}
