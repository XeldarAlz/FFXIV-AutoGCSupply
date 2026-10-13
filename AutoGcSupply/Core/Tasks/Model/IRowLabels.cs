namespace AutoGcSupply.Core.Tasks.Model;

internal interface IRowLabels
{
    string ClassName(byte classJobId);

    string ItemName(uint itemId);

    uint ItemIconId(uint itemId);
}
