using System;
using System.Collections.Generic;

namespace BudgetChess {

  [Flags]
  enum CastleType {
    Short = 1,
    Long  = 2
  }

  [Flags]
  enum MoveResult {
    Illegal    = 0,
    Legal      = 1,
    Capture    = 2,
    Check      = 4,
    Checkmate  = 8,
    Stalemate  = 16,
    EnPassant  = 32,
    Promotion  = 64,
    Castle     = 128
  }

  class BoardState {

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
      Ply        ply,
      MoveResult result,
      Player     next_player,
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

    public IEnumerable<Ply> GetLegalMoves() {
      foreach (var legal in legal_moves)
        yield return legal;
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

    private enum CaptureAbility {
      Mandatory,
      Optional,
      Impossible
    }

    private void AddReachableSquares(
      SquarePos      square,
      Direction[]    dirs,
      int            range,
      CaptureAbility capture_ability = CaptureAbility.Optional
    ) {
      foreach (var dir in dirs) {
        for (int i = 1; i <= range; i++) {
          var target_square = square + i * dir;
          if (!target_square.IsValid)
            break;
          var target_piece = GetSquare(target_square);
          if (target_piece.HasValue) {
            if (capture_ability != CaptureAbility.Impossible
                && target_piece.Value.Player != turn) {
              legal_moves.Add(new Ply(square, target_square));
            }
            break;
          }
          if (capture_ability != CaptureAbility.Mandatory)
            legal_moves.Add(new Ply(square, target_square));
        }
      }
    }

    private void UpdateLegalMoves() {
      legal_moves.Clear();

      foreach (var square in GetSquaresOccupiedByPlayer(turn)) {
        var piece = GetSquare(square).Value;
        switch (piece.PieceType) {
          case PieceType.King:
            AddReachableSquares(square, Directions.Orthodiagonals, 1);
            break;
          case PieceType.Queen:
            AddReachableSquares(square, Directions.Orthodiagonals, 7);
            break;
          case PieceType.Rook:
            AddReachableSquares(square, Directions.Orthogonals, 7);
            break;
          case PieceType.Knight:
            AddReachableSquares(square, Directions.KnightHops, 1);
            break;
          case PieceType.Bishop:
            AddReachableSquares(square, Directions.Diagonals, 7);
            break;
          case PieceType.Pawn:
            Direction pawn_direction = GetPawnDirection(turn);
            Direction[] dirs = { pawn_direction };
            bool is_at_home_rank = GetPawnHomeRank(turn) == square.Rank;
            AddReachableSquares(square, dirs, is_at_home_rank ? 2 : 1,
              CaptureAbility.Impossible);
            dirs = new Direction[] {
              (-1, pawn_direction.DY),
              ( 1, pawn_direction.DY)
            };
            AddReachableSquares(square, dirs, 1, CaptureAbility.Mandatory);
            if (en_passant_square.HasValue) {
              var eps = en_passant_square.Value;
              if (square + dirs[0] == eps || square + dirs[1] == eps)
                legal_moves.Add(new Ply(square, eps));
            }
            break;
        }
      }
    }

    private Direction GetPawnDirection(Player player)
      => (0, player == Player.White ? 1 : -1);
    
    private int GetPawnHomeRank(Player player)
      => player == Player.White ? 1 : 6;

    private MoveResult ForceMove(Ply ply, out SquarePos? capture_square) {
      MoveResult result = MoveResult.Legal;

      var piece    = GetSquare(ply.Source).Value;
      var captured = GetSquare(ply.Destination);
      capture_square = null;
      if (captured.HasValue) {
        capture_square = ply.Destination;
        result |= MoveResult.Capture;
      }
      SetSquare(ply.Source,      null);
      SetSquare(ply.Destination, piece);

      bool is_pawn = piece.PieceType == PieceType.Pawn;

      // En passant capture
      if (is_pawn
          && en_passant_square.HasValue
          && en_passant_square.Value == ply.Destination) {
        var eps = en_passant_square.Value;
        capture_square = (eps.File, eps.Rank - GetPawnDirection(turn).DY);
        SetSquare(capture_square.Value, null);
        result |= MoveResult.Capture | MoveResult.EnPassant;
      }
      
      // En passant availability
      en_passant_square = null;
      if (is_pawn) {
        int home_rank = GetPawnHomeRank(turn);
        int pawn_dir  = GetPawnDirection(turn).DY;
        int ep_rank   = home_rank + 2 * pawn_dir;
        if (ply.Source.Rank == home_rank && ply.Destination.Rank == ep_rank)
          en_passant_square = (ply.Source.File, home_rank + 1 * pawn_dir);
      }

      turn = turn.Other();

      UpdateLegalMoves();

      return result;
    }

    public MoveResult MakeMove(Ply ply) {
      foreach (var legal in legal_moves)
        if (ply == legal)
          goto LegalMove;
      return MoveResult.Illegal;

      LegalMove:;

      var result = ForceMove(ply, out var capture_square);

      OnMoveMade?.Invoke(ply, result, turn, capture_square);

      return result;
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
