using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Abilities.Psionics;

[RegisterComponent, NetworkedComponent]
public sealed partial class PrecognitionPowerComponent : Component
{
    [DataField]
    public float RandomResultChance = 0.2f;

    [DataField]
    public SoundSpecifier VisionSound = new SoundPathSpecifier("/Audio/DeltaV/Effects/clang2.ogg");

    [DataField]
    public EntityUid? SoundStream;

    /// <summary>
    /// The length of the sound effect
    /// </summary>
    [DataField]
    public TimeSpan UseDelay = TimeSpan.FromSeconds(8.35);

    /// <summary>
    /// A short delay to give the action if the doafter fails.
    /// </summary>
    [DataField]
    public TimeSpan RetryDelay = TimeSpan.FromSeconds(15);

    [DataField]
    public EntProtoId PrecognitionActionId = "ActionPrecognition";

    [DataField]
    public EntityUid? PrecognitionActionEntity;
}
