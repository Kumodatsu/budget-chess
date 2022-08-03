using System;
using System.Collections.Generic;
using System.Linq;

namespace BudgetChess {

  [Flags]
  enum CastleType {
    None  = 0,
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
      UpdatePsuedoLegalMoves();
    }

    private BoardState(BoardState src) {
      for (int rank = 0; rank < SquarePos.RankCount; rank++)
        for (int file = 0; file < SquarePos.FileCount; file++)
          this.board[file, rank] = src.board[file, rank];
      this.turn = src.turn;
      this.en_passant_square = src.en_passant_square;
      this.castle_rights[Player.White] = src.castle_rights[Player.White];
      this.castle_rights[Player.Black] = src.castle_rights[Player.Black];
      this.legal_moves = new List<Ply>(src.legal_moves);
    }

    public delegate void OnMoveMadeHandler(
      Ply        ply,
      MoveResult result,
      Player     next_player,
      SquarePos? capture_square,
      Ply?       castle_rook_movement,
      Piece?     promoted_piece
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

    private void UpdatePsuedoLegalMoves() {
      legal_moves.Clear();

      foreach (var square in GetSquaresOccupiedByPlayer(turn)) {
        var piece = GetSquare(square).Value;
        switch (piece.PieceType) {
          case PieceType.King:
            AddReachableSquares(square, Directions.Orthodiagonals, 1);

            // Castling
            var castle_rights = GetCastleRights(turn);
            if (castle_rights == CastleType.None || IsInCheck(turn))
              break;
            Func<SquarePos, bool> is_available = p
                => !GetSquare(p).HasValue
                && !IsPreventedByCheck(new Ply(square, p));
            if (castle_rights.HasFlag(CastleType.Short)) {
              SquarePos[] passage_squares = {
                square + (1, 0),
                square + (2, 0),
              };
              if (passage_squares.All(is_available))
                legal_moves.Add(new Ply(square, passage_squares[1]));
            }
            if (castle_rights.HasFlag(CastleType.Long)) {
              SquarePos[] passage_squares = {
                square + (-1, 0),
                square + (-2, 0),
              };
              if (passage_squares.All(is_available))
                legal_moves.Add(new Ply(square, passage_squares[1]));
            }

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

            // Moving
            Direction[] dirs = { pawn_direction };
            bool is_at_home_rank = GetPawnHomeRank(turn) == square.Rank;
            AddReachableSquares(square, dirs, is_at_home_rank ? 2 : 1,
              CaptureAbility.Impossible);

            // Capturing
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

    public void UpdateLegalMoves() {
      legal_moves.Filter(ply => !IsPreventedByCheck(ply));
    }

    private bool IsPreventedByCheck(Ply ply) {
      BoardState hypothetical = new BoardState(this);
      hypothetical.ForceMove(ply, null, out var _, out var _, out var _);
      return hypothetical.IsInCheck(turn);
    }

    private bool TestLineOfSight(
      SquarePos   target_square,
      Player      player,
      Direction[] directions,
      int         range,
      PieceType   piece_type
    ) {
      foreach (var dir in directions) {
        for (int i = 1; i <= range; i++) {
          var square = target_square + i * dir;
          if (!square.IsValid)
            break;
          var maybe_piece = GetSquare(square);
          if (maybe_piece.HasValue) {
            var piece = maybe_piece.Value;
            var is_check =
              piece.Player == player.Other()
              && (piece.PieceType & piece_type) != 0;
            if (is_check)
              return true;
            break;
          }
        }
      }
      return false;
    }

    private bool IsInCheck(Player player) {
      var king_square = GetKingSquare(player);
      return TestLineOfSight(king_square, player,
        Directions.Orthogonals, 7, PieceType.Queen | PieceType.Rook)
        || TestLineOfSight(king_square, player,
        Directions.Diagonals, 7, PieceType.Queen | PieceType.Bishop)
        || TestLineOfSight(king_square, player,
        Directions.Orthodiagonals, 1, PieceType.King)
        || TestLineOfSight(king_square, player,
        Directions.KnightHops, 1, PieceType.Knight)
        || TestLineOfSight(king_square, player,
        Directions.Diagonals, 1, PieceType.Pawn)
        ;
    }

    private Direction GetPawnDirection(Player player)
      => (0, player == Player.White ? 1 : -1);
    
    private int GetPawnHomeRank(Player player)
      => player == Player.White ? 1 : 6;

    private SquarePos GetKingSquare(Player player) {
      for (int rank = 0; rank < SquarePos.RankCount; rank++)
        for (int file = 0; file < SquarePos.FileCount; file++) {
          var pos   = new SquarePos(file, rank);
          var piece = GetSquare(pos);
          if (piece.HasValue
              && piece.Value.Player == player
              && piece.Value.PieceType == PieceType.King) {
            return pos;
          }
        }
      throw new InvalidOperationException(
        "The given player has no king on the board."
      );
    }

    private MoveResult ForceMove(
      Ply            ply,
      PieceType?     promotion_type,
      out SquarePos? capture_square,
      out Ply?       castling_rook_movement,
      out Piece?     promoted_piece
    ) {
      var result             = MoveResult.Legal;
      capture_square         = null;
      castling_rook_movement = null;
      promoted_piece         = null;

      var piece    = GetSquare(ply.Source).Value;
      var captured = GetSquare(ply.Destination);
      if (captured.HasValue) {
        capture_square = ply.Destination;
        result |= MoveResult.Capture;
      }
      SetSquare(ply.Source,      null);
      SetSquare(ply.Destination, piece);

      bool is_king = piece.PieceType == PieceType.King;
      bool is_pawn = piece.PieceType == PieceType.Pawn;

      // Castling
      if (is_king) {
        var movement  = ply.Destination.File - ply.Source.File;
        var is_castle = Math.Abs(movement) == 2;
        if (is_castle) {
          if (movement == 2) {
            var rook_square = (7, ply.Source.Rank);
            var rook        = GetSquare(rook_square).Value;
            castling_rook_movement = new Ply(
              rook_square,
              ply.Source + (1, 0)
            );
            SetSquare(castling_rook_movement.Value.Destination, rook);
            SetSquare(castling_rook_movement.Value.Source,      null);
          } else if (movement == -2) {
            var rook_square = (0, ply.Source.Rank);
            var rook        = GetSquare(rook_square).Value;
            castling_rook_movement = new Ply(
              rook_square,
              ply.Source + (-1, 0)
            );
            SetSquare(castling_rook_movement.Value.Destination, rook);
            SetSquare(castling_rook_movement.Value.Source,      null);
          }
          castle_rights[turn] = CastleType.None;
          result |= MoveResult.Castle;
        }
      }

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

      // Promotion
      var is_last_rank = ply.Destination.Rank == 0 || ply.Destination.Rank == 7;
      if (is_pawn && is_last_rank) {
        promoted_piece = new Piece(turn,
          promotion_type.HasValue ? promotion_type.Value : PieceType.Pawn
        );
        SetSquare(ply.Destination, promoted_piece);
        result |= MoveResult.Promotion;
      }

      turn = turn.Other();
      
      return result;
    }

    public MoveResult MakeMove(Ply ply, PieceType? promotion_piece = null) {
      foreach (var legal in legal_moves)
        if (ply == legal)
          goto LegalMove;
      return MoveResult.Illegal;

      LegalMove:;

      var result = ForceMove(
        ply,
        promotion_piece,
        out var capture_square,
        out var castle_rook_movement,
        out var promoted_piece
      );
      UpdatePsuedoLegalMoves();
      UpdateLegalMoves();

      bool is_check     = IsInCheck(turn);
      bool has_no_moves = legal_moves.Count == 0;
      if (is_check) {
        result |= MoveResult.Check;
        if (has_no_moves)
          result |= MoveResult.Checkmate;
      } else if (has_no_moves) {
        result |= MoveResult.Stalemate;
      }

      OnMoveMade?.Invoke(
        ply,
        result,
        turn,
        capture_square,
        castle_rook_movement,
        promoted_piece
      );
      
      return result;
    }

    public bool IsPromotion(Ply ply) {
      var piece = GetSquare(ply.Source);
      if (!piece.HasValue || piece.Value.PieceType != PieceType.Pawn)
        return false;
      var is_last_rank = ply.Destination.Rank == 0 || ply.Destination.Rank == 7;
      if (!is_last_rank)
        return false;
      var dest = GetSquare(ply.Destination);
      if (ply.Source.File == ply.Destination.File)
        return !dest.HasValue;
      return dest.HasValue;
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
