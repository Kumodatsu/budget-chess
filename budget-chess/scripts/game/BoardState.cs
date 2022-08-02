using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BudgetChess {

  static class MultidimensionalArrayExtensions {
    public static bool SequenceEquals<T>(this T[,] xs, T[,] ys) {
      if (xs.GetLength(0) != ys.GetLength(0)
          || xs.GetLength(1) != ys.GetLength(1)) {
        return false;
      }

      for (int y = 0; y < xs.GetLength(1); y++)
        for (int x = 0; x < xs.GetLength(0); x++)
          if (!xs[x, y].Equals(ys[x, y]))
            return false;

      return true;
    }
  }

  [Flags]
  enum PieceType {
    King   = 1,
    Queen  = 2,
    Rook   = 4,
    Knight = 8,
    Bishop = 16,
    Pawn   = 32
  }

  [Flags]
  enum Player {
    White = 64,
    Black = 128
  }

  static class PlayerEnumExtensions {
    public static Player Other(this Player player)
      => player == Player.White ? Player.Black : Player.White;
  }

  [Flags]
  enum CastleType {
    Short = 1,
    Long  = 2
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

  struct Direction {
    public int DX { get; set; }
    public int DY { get; set; }

    public Direction(int dx, int dy)
      => (DX, DY) = (dx, dy);

    public static implicit operator Direction ((int DX, int DY) tuple)
      => new Direction(tuple.DX, tuple.DY);

    public static Direction operator * (int s, Direction d)
      => (s * d.DX, s * d.DY);
  }

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
  };

  struct Ply {
    public SquarePos Source;
    public SquarePos Destination;

    public Ply(SquarePos source, SquarePos destination)
      => (Source, Destination) = (source, destination);

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
  };

  class BoardState {
    static readonly Direction[]
      Orthogonals = {
        ( 0,  1),
        ( 1,  0),
        ( 0, -1),
        (-1,  0)
      },
      Diagonals = {
        ( 1,  1),
        ( 1, -1),
        (-1, -1),
        (-1,  1)
      },
      Orthodiagonals = {
        ( 0,  1),
        ( 1,  0),
        ( 0, -1),
        (-1,  0),
        ( 1,  1),
        ( 1, -1),
        (-1, -1),
        (-1,  1)
      },
      KnightHops = {
        ( 1,  2),
        ( 2,  1),
        ( 2, -1),
        ( 1, -2),
        (-1,  2),
        (-2,  1),
        (-2, -1),
        (-1, -2)
      };

    private Piece?[,] board =
      new Piece?[SquarePos.FileCount, SquarePos.RankCount];
    private Player turn =
      Player.White;
    private SquarePos? en_passant_square =
      null;
    private Dictionary<Player, CastleType> castle_rights =
      new Dictionary<Player, CastleType> {
        { Player.White, CastleType.Short | CastleType.Long },
        { Player.Black, CastleType.Short | CastleType.Long }
      };

    private List<Ply> legal_moves = new List<Ply>();

    public BoardState() {
      InitializeBoard();
      InitializeDefaultStartingPosition();
      UpdateLegalMoves();
    }

    public delegate void OnMoveMadeHandler(
      Ply    ply,
      Player next_player,
      SquarePos? capture_square
    );
    public event OnMoveMadeHandler OnMoveMade;

    public void SetSquare(SquarePos pos, Piece? piece)
      => board[pos.File, pos.Rank] = piece;

    public Piece? GetSquare(SquarePos pos)
      => board[pos.File, pos.Rank];

    public Player Turn => turn;

    public CastleType GetCastleRights(Player player)
      => castle_rights[player];

    public IEnumerable<SquarePos> GetSquaresOccupiedByPlayer(Player player) {
      for (int file = 0; file < SquarePos.FileCount; file++)
        for (int rank = 0; rank < SquarePos.RankCount; rank++) {
          var piece = board[file, rank];
          if (piece.HasValue && piece.Value.Player == player)
            yield return (file, rank);
        }
    }

    public IEnumerable<SquarePos> GetSquaresOccupied() {
      foreach (var pos in GetSquaresOccupiedByPlayer(Player.White))
        yield return pos;
      foreach (var pos in GetSquaresOccupiedByPlayer(Player.Black))
        yield return pos;
    }

    private void InitializeBoard() {
      for (int file = 0; file < SquarePos.FileCount; file++)
        for (int rank = 0; rank < SquarePos.RankCount; rank++)
          board[file, rank] = null;
    }

    private void InitializeDefaultStartingPosition() {
      SetSquare((0, 0), new Piece(Player.White, PieceType.Rook));
      SetSquare((1, 0), new Piece(Player.White, PieceType.Knight));
      SetSquare((2, 0), new Piece(Player.White, PieceType.Bishop));
      SetSquare((3, 0), new Piece(Player.White, PieceType.Queen));
      SetSquare((4, 0), new Piece(Player.White, PieceType.King));
      SetSquare((5, 0), new Piece(Player.White, PieceType.Bishop));
      SetSquare((6, 0), new Piece(Player.White, PieceType.Knight));
      SetSquare((7, 0), new Piece(Player.White, PieceType.Rook));
      for (int i = 0; i < SquarePos.FileCount; i++)
        SetSquare((i, 1), new Piece(Player.White, PieceType.Pawn));
      SetSquare((0, 7), new Piece(Player.Black, PieceType.Rook));
      SetSquare((1, 7), new Piece(Player.Black, PieceType.Knight));
      SetSquare((2, 7), new Piece(Player.Black, PieceType.Bishop));
      SetSquare((3, 7), new Piece(Player.Black, PieceType.Queen));
      SetSquare((4, 7), new Piece(Player.Black, PieceType.King));
      SetSquare((5, 7), new Piece(Player.Black, PieceType.Bishop));
      SetSquare((6, 7), new Piece(Player.Black, PieceType.Knight));
      SetSquare((7, 7), new Piece(Player.Black, PieceType.Rook));
      for (int i = 0; i < SquarePos.FileCount; i++)
        SetSquare((i, 6), new Piece(Player.Black, PieceType.Pawn));
    }

    private void UpdateLegalMoves() {
      legal_moves.Clear();

      foreach (var square in GetSquaresOccupiedByPlayer(turn)) {
        Action<Direction[], int> add_reachable_squares =
          (Direction[] dirs, int range) => {
            foreach (var dir in dirs) {
              for (int i = 0; i < range; i++) {
                var target_square = square + i * dir;
                if (!target_square.IsValid)
                  break;
                var target_piece = GetSquare(target_square);
                if (target_piece.HasValue) {
                  if (target_piece.Value.Player != turn)
                    legal_moves.Append(new Ply(square, target_square));
                  break;
                }
                legal_moves.Append(new Ply(square, target_square));
              }
            }
          };

        var piece = GetSquare(square).Value;
        switch (piece.PieceType) {
          case PieceType.King:
            add_reachable_squares(Orthodiagonals, 1);
            break;
          case PieceType.Queen:
            add_reachable_squares(Orthodiagonals, 7);
            break;
          case PieceType.Rook:
            add_reachable_squares(Orthogonals, 7);
            break;
          case PieceType.Knight:
            add_reachable_squares(KnightHops, 1);
            break;
          case PieceType.Bishop:
            add_reachable_squares(Diagonals, 7);
            break;
          case PieceType.Pawn:
            add_reachable_squares(Orthodiagonals, 1); // TODO
            break;
        }
      }
    }

    private SquarePos? ForceMove(Ply ply) {
      var piece    = GetSquare(ply.Source);
      var captured = GetSquare(ply.Destination);
      SquarePos? capture_square = null;
      if (captured.HasValue)
        capture_square = ply.Destination;
      SetSquare(ply.Source,      null);
      SetSquare(ply.Destination, piece);
      turn = turn.Other();

      // TODO: En passant

      return capture_square;
    }

    public bool MakeMove(Ply ply) {
      foreach (var legal in legal_moves)
        if (ply == legal)
          goto LegalMove;
      return false;

      LegalMove:;

      var capture_square = ForceMove(ply);

      OnMoveMade?.Invoke(ply, turn, capture_square);

      return true;
    }

    #region Equality and Hash Code
    public static bool operator == (BoardState a, BoardState b)
      => a.board.SequenceEquals(b.board)
      && a.turn == b.turn
      && a.en_passant_square.Equals(b.en_passant_square);
    public static bool operator != (BoardState a, BoardState b)
      => !a.board.SequenceEquals(b.board)
      || a.turn != b.turn
      || !a.en_passant_square.Equals(b.en_passant_square);
    public override bool Equals(object obj)
      => obj is BoardState other
      && this == other;
    public override int GetHashCode()
      => 0;
    #endregion
  }
  
}
