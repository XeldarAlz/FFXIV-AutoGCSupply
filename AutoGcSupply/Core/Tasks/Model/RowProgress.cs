namespace AutoGcSupply.Core.Tasks.Model;

internal readonly record struct RowProgress(RowRunState State, RowNote Note, int Bought, int SealsLost)
{
    public static RowProgress Of(RowRunState state, RowNote note) => new(state, note, 0, 0);

    public RowProgress With(RowRunState state, RowNote note) => this with { State = state, Note = note };
}
