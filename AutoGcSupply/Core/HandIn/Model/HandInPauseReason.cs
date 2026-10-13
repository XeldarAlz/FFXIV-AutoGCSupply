namespace AutoGcSupply.Core.HandIn.Model;

internal enum HandInPauseReason : byte
{
    None,
    NotInGrandCompany,
    TravelFailed,
    OfficerNotFound,
    OfficerUnreachable,
    MenuTextUnavailable,
    MenuOptionNotFound,
    ListDidNotOpen,
    TabDidNotSwitch,
    UnexpectedPrompt,
    UnexpectedAddon,
    HighQualityPromptForNormalRow,
    RewardWindowDidNotOpen,
    DeliverButtonDisabled,
    DeliveryNotConfirmed,
    InCombatOrDuty,
    Fault,
}
