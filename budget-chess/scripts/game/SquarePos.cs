namespace BudgetChess {

  struct SquarePos {
    public const int
      FileCount = 8,
      RankCount = 8;
    
    public int File { get; set; }
    public int Rank { get; set; }

    public SquarePos(int file, int rank)
      => (File, Rank) = (file, rank);

    public static implicit operator SquarePos ((int File, int Rank) tuple)
      => new SquarePos(tuple.File, tuple.Rank);

    public bool IsValid
      => 0 <= File && File < FileCount
      && 0 <= Rank && Rank < RankCount;

    public static SquarePos operator + (SquarePos pos, Direction d)
      => (pos.File + d.DX, pos.Rank + d.DY);

    public string Name => $"{(char) ('a' + File)}{Rank + 1}";

    public override string ToString() => Name;

    #region Equality and HashCode
    public static bool operator == (SquarePos a, SquarePos b)
      => a.File == b.File && a.Rank == b.Rank;
    public static bool operator != (SquarePos a, SquarePos b)
      => a.File != b.File || a.Rank != b.Rank;
    public override bool Equals(object obj)
      => obj is SquarePos other
      && this == other;
    public override int GetHashCode()
      => File + FileCount * Rank;
    #endregion
  }
  
}