using Content.Shared._NF.Interaction.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._NF.Interaction.Systems;

/// <summary>
/// Handles interactions with items that swap with HandPlaceholder items.
/// </summary>
public sealed partial class HandPlaceholderSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public readonly EntProtoId<HandPlaceholderComponent> Placeholder = "HandPlaceholder";

    public override void Initialize()
    {
        SubscribeLocalEvent<HandPlaceholderRemoveableComponent, EntGotRemovedFromContainerMessage>(OnEntityRemovedFromContainer);

        SubscribeLocalEvent<HandPlaceholderComponent, AfterInteractEvent>(AfterInteract);
        SubscribeLocalEvent<HandPlaceholderComponent, BeforeRangedInteractEvent>(BeforeRangedInteract);
    }

    /// <summary>
    /// Spawns a new placeholder and ties it to an item.
    /// When dropped the item will replace itself with the placeholder in its container.
    /// </summary>
    public EntityUid GivePlaceholder(EntityUid item, EntProtoId id, EntityWhitelist whitelist)
    {
        var coords = new EntityCoordinates(item, 0f, 0f);
        var placeholder = Spawn(Placeholder, coords);
        var comp = Comp<HandPlaceholderComponent>(placeholder);
        comp.Prototype = id;
        comp.Whitelist = whitelist;
        Dirty(placeholder, comp);

        var name = _proto.Index(id).Name;
        _metadata.SetEntityName(placeholder, name);
        SetPlaceholder(item, placeholder);
        return placeholder;
    }

    /// <summary>
    /// Sets the placeholder entity for an item.
    /// </summary>
    public void SetPlaceholder(EntityUid item, EntityUid placeholder)
    {
        var comp = EnsureComp<HandPlaceholderRemoveableComponent>(item);
        comp.Placeholder = placeholder;
        Dirty(item, comp);
    }

    public void SetEnabled(EntityUid item, bool enabled)
    {
        if (!TryComp<HandPlaceholderRemoveableComponent>(item, out var comp))
            return;

        comp.Enabled = enabled;
        Dirty(item, comp);
    }

    private void OnEntityRemovedFromContainer(Entity<HandPlaceholderRemoveableComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        // trying to insert when deleted is an error, and only handle when it is being actually dropped
        var owner = args.Container.Owner;
        if (!ent.Comp.Enabled || TerminatingOrDeleted(owner))
            return;

        var placeholder = ent.Comp.Placeholder;

        var succeeded = _container.Insert(placeholder, args.Container, force: true);
        DebugTools.Assert(succeeded, $"Failed to insert placeholder {ToPrettyString(placeholder)} of {ToPrettyString(ent)} into container of {ToPrettyString(owner)}");
        RemComp<HandPlaceholderRemoveableComponent>(ent);

        // prevent spam dropping giving you an empty hand
        EnsureComp<UnremoveableComponent>(placeholder);
    }

    private void BeforeRangedInteract(Entity<HandPlaceholderComponent> ent, ref BeforeRangedInteractEvent args)
    {
        if (args.Handled || args.Target is not {} target)
            return;

        args.Handled = true;
        TryToPickUpTarget(ent, target, args.User);
    }

    private void AfterInteract(Entity<HandPlaceholderComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not {} target)
            return;

        args.Handled = true;
        TryToPickUpTarget(ent, target, args.User);
    }

    private void TryToPickUpTarget(Entity<HandPlaceholderComponent> ent, EntityUid target, EntityUid user)
    {
        // require items regardless of the whitelist
        if (!HasComp<ItemComponent>(target) || _whitelist.IsWhitelistFail(ent.Comp.Whitelist, target))
            return;

        if (!TryComp<HandsComponent>(user, out var hands))
            return;

        // Can't get the hand we're holding this with? Something's wrong, abort.  No empty hands.
        if (!_hands.IsHolding(user, ent, out var hand, hands))
            return;

        SetPlaceholder(target, ent);
        SetEnabled(target, true);

        RemComp<UnremoveableComponent>(ent);
        _hands.DoDrop(user, hand, handsComp: hands);
        _transform.SetParent(ent, target);

        _hands.DoPickup(user, hand, target, hands); // Force pickup - empty hands are not okay
        _interaction.DoContactInteraction(user, target); // allow for forensics and other systems to work (why does hands system not do this???)
    }
}
