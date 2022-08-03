using System;

namespace BudgetChess {

  [Flags]
  enum PieceType {
    King   = 1,
    Queen  = 2,
    Rook   = 4,
    Knight = 8,
    Bishop = 16,
    Pawn   = 32
  }

  struct Piece {
    public Player    Player    { get; set; }
    public PieceType PieceType { get; set; }

    public Piece(Player player, PieceType piece_type)
      => (Player, PieceType) = (player, piece_type);

    #region Equality and HashCode
    public static bool operator == (Piece a, Piece b)
      => a.PieceType == b.PieceType && a.Player == b.Player;
    public static bool operator != (Piece a, Piece b)
      => a.PieceType != b.PieceType || a.Player != b.Player;
    public override bool Equals(object obj)
      => obj is Piece other
      && this == other;
    public override int GetHashCode()
      => (int) PieceType + (int) Player;
    #endregion
  }

}
