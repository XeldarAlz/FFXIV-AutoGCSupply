namespace AutoGcSupply.Core.HandIn.Model;

internal enum HandInStep : byte
{
    Starting,
    Travelling,
    ApproachingOfficer,
    OpeningMenu,
    OpeningList,
    SelectingTab,
    PickingRow,
    Confirming,
    Delivering,
    Verifying,
    ClosingList,
    LeavingMenu,
    Finished,
}
