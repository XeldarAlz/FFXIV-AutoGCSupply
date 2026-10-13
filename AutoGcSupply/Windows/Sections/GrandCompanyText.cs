using AutoGcSupply.Core.Game.Player;
using AutoGcSupply.Core.Localization;
using Dalamud.Game.Player;
using ECommons.DalamudServices;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;

namespace AutoGcSupply.Windows.Sections;

internal static class GrandCompanyText
{
    private static byte nameCompany = byte.MaxValue;
    private static string name = string.Empty;
    private static byte rankCompany = byte.MaxValue;
    private static byte rankValue = byte.MaxValue;
    private static Sex rankSex;
    private static LanguageInfo? rankLanguage;
    private static string rankName = string.Empty;
    private static uint rankIcon;

    public static string CompanyName(byte grandCompany)
    {
        if (grandCompany == nameCompany)
        {
            return name;
        }

        nameCompany = grandCompany;
        name = Svc.Data.GetExcelSheet<GrandCompany>().GetRowOrDefault(grandCompany)?.Name.ExtractText() ?? string.Empty;
        return name;
    }

    public static string RankName(byte grandCompany, byte rank)
    {
        Refresh(grandCompany, rank);
        return rankName;
    }

    public static uint RankIcon(byte grandCompany, byte rank)
    {
        Refresh(grandCompany, rank);
        return rankIcon;
    }

    private static void Refresh(byte grandCompany, byte rank)
    {
        var sex = Svc.PlayerState.Sex;
        if (grandCompany == rankCompany && rank == rankValue && sex == rankSex && ReferenceEquals(rankLanguage, Loc.Current))
        {
            return;
        }

        rankCompany = grandCompany;
        rankValue = rank;
        rankSex = sex;
        rankLanguage = Loc.Current;
        rankIcon = ReadRankIcon(grandCompany, rank);
        var title = ReadRankTitle(grandCompany, rank, sex == Sex.Female);
        rankName = title.Length > 0 ? title : Loc.T(L.Requests.Rank, rank);
    }

    private static uint ReadRankIcon(byte grandCompany, byte rank)
    {
        if (Svc.Data.GetExcelSheet<GrandCompanyRank>().GetRowOrDefault(rank) is not { } row)
        {
            return 0;
        }

        var icon = grandCompany switch
        {
            GrandCompanyId.Maelstrom => row.IconMaelstrom,
            GrandCompanyId.TwinAdder => row.IconSerpents,
            GrandCompanyId.ImmortalFlames => row.IconFlames,
            _ => 0,
        };
        return icon > 0 ? (uint)icon : 0;
    }

    private static string ReadRankTitle(byte grandCompany, byte rank, bool female) => grandCompany switch
    {
        GrandCompanyId.Maelstrom when female => Title<GCRankLimsaFemaleText>(rank, static row => (row.NameRank, row.Singular)),
        GrandCompanyId.Maelstrom => Title<GCRankLimsaMaleText>(rank, static row => (row.NameRank, row.Singular)),
        GrandCompanyId.TwinAdder when female => Title<GCRankGridaniaFemaleText>(rank, static row => (row.NameRank, row.Singular)),
        GrandCompanyId.TwinAdder => Title<GCRankGridaniaMaleText>(rank, static row => (row.NameRank, row.Singular)),
        GrandCompanyId.ImmortalFlames when female => Title<GCRankUldahFemaleText>(rank, static row => (row.NameRank, row.Singular)),
        GrandCompanyId.ImmortalFlames => Title<GCRankUldahMaleText>(rank, static row => (row.NameRank, row.Singular)),
        _ => string.Empty,
    };

    private static string Title<T>(byte rank, Func<T, (ReadOnlySeString Name, ReadOnlySeString Singular)> read) where T : struct, IExcelRow<T>
    {
        if (Svc.Data.GetExcelSheet<T>().GetRowOrDefault(rank) is not { } row)
        {
            return string.Empty;
        }

        var (rankTitle, singular) = read(row);
        var text = rankTitle.ExtractText();
        return text.Length > 0 ? text : singular.ExtractText();
    }
}
