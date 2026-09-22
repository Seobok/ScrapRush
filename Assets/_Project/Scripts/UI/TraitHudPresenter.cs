using ScrapRush.Player;
using UnityEngine;

namespace ScrapRush.UI
{
    public sealed class TraitHudPresenter : MonoBehaviour
    {
        private TraitHudRail view;
        private MagnetSystem magnet;
        private ElectricSystem electric;

        public void Initialize(TraitHudRail traitView, MagnetSystem magnetSystem, ElectricSystem electricSystem)
        {
            Unsubscribe();
            view = traitView;
            magnet = magnetSystem;
            electric = electricSystem;

            if (view == null || magnet == null || electric == null)
            {
                Debug.LogError("TraitHudPresenter requires a view, MagnetSystem and ElectricSystem.", this);
                enabled = false;
                return;
            }

            magnet.TraitCountChanged += OnMagnetCountChanged;
            electric.TraitCountChanged += OnElectricCountChanged;
            RefreshAll();
        }

        private void RefreshAll()
        {
            view.SetTraitState(TraitId.Magnet, magnet.TraitCount);
            view.SetTraitState(TraitId.Electric, electric.TraitCount);
            view.SetTraitState(TraitId.Industry, 0);
            view.SetTraitState(TraitId.Recycle, 0);
            view.SetTraitState(TraitId.Explosion, 0);
            view.SetTraitState(TraitId.Luck, 0);
            view.SetTraitState(TraitId.Overload, 0);
        }

        private void OnMagnetCountChanged(int count)
        {
            view.SetTraitState(TraitId.Magnet, count, 0, true, GetMagnetThresholdName(count));
        }

        private void OnElectricCountChanged(int count)
        {
            view.SetTraitState(TraitId.Electric, count, 0, true, GetElectricThresholdName(count));
        }

        private void Unsubscribe()
        {
            if (magnet != null) magnet.TraitCountChanged -= OnMagnetCountChanged;
            if (electric != null) electric.TraitCountChanged -= OnElectricCountChanged;
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private static string GetMagnetThresholdName(int count)
        {
            return count switch
            {
                2 => "MAGNETIC PULL",
                4 => "CLUSTER LINK",
                6 => "GRAVITY ENGINE",
                8 => "SINGULARITY",
                _ => null
            };
        }

        private static string GetElectricThresholdName(int count)
        {
            return count switch
            {
                2 => "STATIC DISCHARGE",
                4 => "CHAIN LIGHTNING",
                6 => "STORM CIRCUIT",
                _ => null
            };
        }
    }
}
