namespace BudgetChess {

  struct Ply {
    public SquarePos Source;
    public SquarePos Destination;

    public Ply(SquarePos source, SquarePos destination)
      => (Source, Destination) = (source, destination);

    public override string ToString()
      => $"{Source} -> {Destination}";

    #region Equality and HashCode
    public static bool operator == (Ply a, Ply b)
      => a.Source == b.Source && a.Destination == b.Destination;
    public static bool operator != (Ply a, Ply b)
      => a.Source != b.Source || a.Destination != b.Destination;
    public override bool Equals(object obj)
      => obj is Ply other
      && this == other;
    public override int GetHashCode()
      => Source.GetHashCode() * 4483 + Destination.GetHashCode();
    #endregion
  }
}
