﻿using Robust.Shared.Containers;
using Content.Shared.Implants;
using Content.Shared.CombatMode.Pacification;

namespace Content.Shared.DeltaV.Implants.Pacifism;

public sealed class PacifismImplantSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PacifismImplantComponent, ImplantImplantedEvent>(OnImplantImplantedEvent);
        SubscribeLocalEvent<PacifismImplantComponent, EntGotRemovedFromContainerMessage>(OnRemove);
    }

    private void OnImplantImplantedEvent(Entity<PacifismImplantComponent> ent, ref ImplantImplantedEvent args)
    {
        if (args.Implanted == null)
            return;

        EnsureComp<PacifiedComponent>(args.Implanted.Value, out var pacifiedComponent);
        pacifiedComponent.DisallowAllCombat = ent.Comp.DisallowAllCombat;
        pacifiedComponent.DisallowDisarm = ent.Comp.DisallowDisarm;

        Dirty(args.Implanted.Value, pacifiedComponent);
    }

    // TODO: Update this function to actually remove pacification when removal of the implant is supported.
    private void OnRemove(Entity<PacifismImplantComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {

    }
}