#nullable enable
using System;
using Content.Server.Utility;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Server.GameObjects.Components.Destructible;
using Content.Shared.Utility;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedWindowComponent))]
    public class WindowComponent : SharedWindowComponent, IExamine, IInteractHand
    {

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case DamageChangedMessage msg:
                {
                    var current = msg.Damageable.TotalDamage;
                    UpdateVisuals(current);
                    break;
                }
            }
        }

        private void UpdateVisuals(int currentDamage)
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                if (Owner.TryGetComponent(out DestructibleComponent? destructible))
                {
                    var damageThreshold = destructible.LowestToHighestThresholds.FirstOrNull()?.Key;
                    if (damageThreshold == null) return;
                    appearance.SetData(WindowVisuals.Damage, (float) currentDamage / damageThreshold);
                }
            }
        }

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            var damage = Owner.GetComponentOrNull<IDamageableComponent>()?.TotalDamage;
            var damageThreshold = Owner.GetComponentOrNull<DestructibleComponent>()?.LowestToHighestThresholds.FirstOrNull()?.Key;
            if (damage == null || damageThreshold == null) return;
            var fraction = ((damage == 0 || damageThreshold == 0)
                ? 0f
                : (float) damage / damageThreshold);
            var level = Math.Min(ContentHelpers.RoundToLevels((double) fraction, 1, 7), 5);
            switch (level)
            {
                case 0:
                    message.AddText(Loc.GetString("It looks fully intact."));
                    break;
                case 1:
                    message.AddText(Loc.GetString("It has a few scratches."));
                    break;
                case 2:
                    message.AddText(Loc.GetString("It has a few small cracks."));
                    break;
                case 3:
                    message.AddText(Loc.GetString("It has several big cracks running along its surface."));
                    break;
                case 4:
                    message.AddText(Loc.GetString("It has deep cracks across multiple layers."));
                    break;
                case 5:
                    message.AddText(Loc.GetString("It is extremely badly cracked and on the verge of shattering."));
                    break;
            }
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            EntitySystem.Get<AudioSystem>()
                .PlayAtCoords("/Audio/Effects/glass_knock.ogg", eventArgs.Target.Transform.Coordinates, AudioHelpers.WithVariation(0.05f));
            eventArgs.Target.PopupMessageEveryone(Loc.GetString("*knock knock*"));
            return true;
        }
    }
}
