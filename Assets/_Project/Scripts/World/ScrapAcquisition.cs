using UnityEngine;

namespace ScrapRush.World
{
    public enum ScrapAcquireCause
    {
        Natural,
        MagneticCluster,
        GravityField
    }

    public readonly struct ScrapAcquisitionInfo
    {
        public readonly ScrapDrop Drop;
        public readonly Vector2 Position;
        public readonly ScrapAcquireCause Cause;
        public readonly int RootEffectId;

        public ScrapAcquisitionInfo(ScrapDrop drop, ScrapAcquireCause cause, int rootEffectId)
            : this(drop, drop.transform.position, cause, rootEffectId) { }

        public ScrapAcquisitionInfo(ScrapDrop drop, Vector2 position, ScrapAcquireCause cause, int rootEffectId)
        {
            Drop = drop;
            Position = position;
            Cause = cause;
            RootEffectId = rootEffectId;
        }
    }

    public readonly struct ScrapAbsorbInfo
    {
        public readonly OreBreakInfo Origin;
        public readonly ScrapAcquireCause AcquireCause;
        public readonly int AcquireRootEffectId;

        public ScrapAbsorbInfo(OreBreakInfo origin, ScrapAcquireCause acquireCause, int acquireRootEffectId)
        {
            Origin = origin;
            AcquireCause = acquireCause;
            AcquireRootEffectId = acquireRootEffectId;
        }
    }
}
