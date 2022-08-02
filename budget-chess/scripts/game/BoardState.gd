extends Reference

class SquarePos:
  var file: int
  var rank: int

  func _init(file_: int, rank_: int):
    file = file_
    rank = rank_

  func move(files: int, ranks: int) -> SquarePos:
    return SquarePos.new(file + files, rank + ranks)

  func is_same(pos: SquarePos) -> bool:
    return file == pos.file and rank == pos.rank

class Ply:
  var source:      SquarePos
  var destination: SquarePos

  func _init(source_: SquarePos, destination_: SquarePos):
    source      = source_
    destination = destination_

enum PieceType {
  King   = 1,
  Queen  = 2,
  Rook   = 4,
  Knight = 8,
  Bishop = 16,
  Pawn   = 32
}

enum Player {
  White = 64,
  Black = 128
}

enum CastleType {
  Short = 1,
  Long  = 2
}

signal on_move_made(ply, next_player, capture_square)

const N_RANKS: int = 8
const N_FILES: int = 8

const NO_PIECE:          int = 0
const PIECE_TYPE_MASK:   int = 2 * PieceType.Pawn - 1
const PIECE_PLAYER_MASK: int = 2 * Player.Black   - 1 - PIECE_TYPE_MASK

const ORTHOGONALS: Array = [
  [ 0,  1],
  [ 1,  0],
  [ 0, -1],
  [-1,  0]
]
const DIAGONALS: Array = [
  [ 1,  1],
  [ 1, -1],
  [-1, -1],
  [-1,  1]
]
const ORTHODIAGONALS: Array = [
  [ 0,  1],
  [ 1,  0],
  [ 0, -1],
  [-1,  0],
  [ 1,  1],
  [ 1, -1],
  [-1, -1],
  [-1,  1]
]
const HOPS: Array = [
  [ 1,  2],
  [ 2,  1],
  [ 2, -1],
  [ 1, -2],
  [-1,  2],
  [-2,  1],
  [-2, -1],
  [-1, -2]
]

var board:             Array      = []
var turn:              int        = Player.White
var en_passant_square: SquarePos  = null
var castle_rights:     Dictionary = {
  Player.White: CastleType.Short | CastleType.Long,
  Player.Black: CastleType.Short | CastleType.Long
}

func _init():
  initialize_board()
  initialize_default_starting_position()

func clone():
  var c = get_script().new()
  c.board             = self.board.duplicate(true)
  c.turn              = self.turn
  c.en_passant_square = SquarePos.new(
    self.en_passant_square.file,
    self.en_passant_square.rank
  ) if self.en_passant_square else null
  c.castle_rights     = self.castle_rights.duplicate(true)
  return c

func initialize_board():
  for rank in N_RANKS:
    board.append([])
    for file in N_FILES:
      board[rank].append(NO_PIECE)

func initialize_test_position():
  turn = Player.Black
  for file in N_FILES:
    set_square(SquarePos.new(file, 1 + file % 3), Player.White | PieceType.Pawn)
  for file in N_FILES:
    set_square(SquarePos.new(file, 4), Player.Black | PieceType.Pawn)

func initialize_default_starting_position():
  set_square(SquarePos.new(0, 0), Player.White | PieceType.Rook)
  set_square(SquarePos.new(1, 0), Player.White | PieceType.Knight)
  set_square(SquarePos.new(2, 0), Player.White | PieceType.Bishop)
  set_square(SquarePos.new(3, 0), Player.White | PieceType.Queen)
  set_square(SquarePos.new(4, 0), Player.White | PieceType.King)
  set_square(SquarePos.new(5, 0), Player.White | PieceType.Bishop)
  set_square(SquarePos.new(6, 0), Player.White | PieceType.Knight)
  set_square(SquarePos.new(7, 0), Player.White | PieceType.Rook)
  for file in N_FILES:
    set_square(SquarePos.new(file, 1), Player.White | PieceType.Pawn)
  set_square(SquarePos.new(0, 7), Player.Black | PieceType.Rook)
  set_square(SquarePos.new(1, 7), Player.Black | PieceType.Knight)
  set_square(SquarePos.new(2, 7), Player.Black | PieceType.Bishop)
  set_square(SquarePos.new(3, 7), Player.Black | PieceType.Queen)
  set_square(SquarePos.new(4, 7), Player.Black | PieceType.King)
  set_square(SquarePos.new(5, 7), Player.Black | PieceType.Bishop)
  set_square(SquarePos.new(6, 7), Player.Black | PieceType.Knight)
  set_square(SquarePos.new(7, 7), Player.Black | PieceType.Rook)
  for file in N_FILES:
    set_square(SquarePos.new(file, 6), Player.Black | PieceType.Pawn)

func set_square(pos: SquarePos, piece: int):
  board[pos.rank][pos.file] = piece

func get_square(pos: SquarePos) -> int:
  return board[pos.rank][pos.file]

func get_squares_with_piece(piece_type: int, player: int) -> Array:
  var squares = []
  for rank in N_RANKS:
    for file in N_FILES:
      var pos   = SquarePos.new(file, rank)
      var piece = get_square(pos)
      if piece & piece_type != 0 and piece & player != 0:
        squares.append(pos)
  return squares

func is_valid_square(pos: SquarePos) -> bool:
  return pos.file >= 0 and pos.file < N_FILES \
     and pos.rank >= 0 and pos.rank < N_RANKS

func get_player_squares(player: int) -> Array:
  var squares = []
  for rank in N_RANKS:
    for file in N_FILES:
      var piece = board[rank][file]
      if piece & player:
        squares.append(SquarePos.new(file, rank))
  return squares

func get_occupied_squares() -> Array:
  var squares = get_player_squares(Player.White)
  squares.append_array(get_player_squares(Player.Black))
  return squares

func other_player(player: int) -> int:
  match player:
    Player.White:
      return Player.Black
    Player.Black:
      return Player.White
    _:
      return NO_PIECE

func get_turn() -> int:
  return turn

func can_castle(player: int) -> int:
  return castle_rights[player]

func get_legal_moves() -> Array:
  var moves: Array = []
  for square in get_player_squares(turn):
    var piece = get_square(square)
    match piece & PIECE_TYPE_MASK:
      PieceType.King:
        for dir in ORTHODIAGONALS:
          var target_square: SquarePos = square.move(dir[0], dir[1])
          if not is_valid_square(target_square):
            continue
          var target_piece: int = get_square(target_square)
          if target_piece & turn == 0:
            moves.append(Ply.new(square, target_square))
      PieceType.Queen:
        for dir in ORTHODIAGONALS:
          for i in range(1, N_FILES):
            var target_square: SquarePos = square.move(i * dir[0], i * dir[1])
            if not is_valid_square(target_square):
              break
            var target_piece: int = get_square(target_square)
            if target_piece != NO_PIECE:
              if target_piece & other_player(turn):
                moves.append(Ply.new(square, target_square))
              break
            moves.append(Ply.new(square, target_square))
      PieceType.Rook:
        for dir in ORTHOGONALS:
          for i in range(1, N_FILES):
            var target_square: SquarePos = square.move(i * dir[0], i * dir[1])
            if not is_valid_square(target_square):
              break
            var target_piece: int = get_square(target_square)
            if target_piece != NO_PIECE:
              if target_piece & other_player(turn):
                moves.append(Ply.new(square, target_square))
              break
            moves.append(Ply.new(square, target_square))
      PieceType.Knight:
        for dir in HOPS:
          var target_square: SquarePos = square.move(dir[0], dir[1])
          if not is_valid_square(target_square):
            continue
          var target_piece: int = get_square(target_square)
          if target_piece & turn == 0:
            moves.append(Ply.new(square, square.move(dir[0], dir[1])))
      PieceType.Bishop:
        for dir in DIAGONALS:
          for i in range(1, N_FILES):
            var target_square: SquarePos = square.move(i * dir[0], i * dir[1])
            if not is_valid_square(target_square):
              break
            var target_piece: int = get_square(target_square)
            if target_piece != NO_PIECE:
              if target_piece & other_player(turn):
                moves.append(Ply.new(square, target_square))
              break
            moves.append(Ply.new(square, target_square))
      PieceType.Pawn:
        var dir:       int
        var home_rank: int
        match turn:
          Player.White:
            dir       = 1
            home_rank = 1
          Player.Black:
            dir       = -1
            home_rank = N_RANKS - 2
        var target_square: SquarePos
        var target_piece:  int
        var move_range:    int = 2 if square.rank == home_rank else 1
        
        for i in range(1, move_range + 1, 1):
          target_square = square.move(0, i * dir)
          if not is_valid_square(target_square):
            break
          target_piece = get_square(target_square)
          if target_piece == NO_PIECE:
            moves.append(Ply.new(square, target_square))
          else:
            break

        for i in [-1, 1]:
          target_square = square.move(i, dir)
          if not is_valid_square(target_square):
            continue
          target_piece = get_square(target_square)
          if target_piece & other_player(turn) != 0 or \
              en_passant_square != null and \
              en_passant_square.is_same(target_square):
            moves.append(Ply.new(square, target_square))

  for i in range(len(moves) - 1, -1, -1):
    var ply = moves[i]
    if is_prevented_by_check(ply):
      moves.remove(i)
  return moves

func is_prevented_by_check(ply: Ply) -> bool:
  var c = self.clone()
  if ply:
    c.force_move(ply)
  var player      = c.other_player(c.turn)
  var king_square = c.get_squares_with_piece(PieceType.King, player)[0]
  for dir in ORTHOGONALS:
    for i in range(1, N_FILES):
      var square: SquarePos = king_square.move(i * dir[0], i * dir[1])
      if not c.is_valid_square(square):
        break
      var piece: int = c.get_square(square)
      if piece != NO_PIECE:
        if piece & c.other_player(player) != 0 \
            and (piece & (PieceType.Rook | PieceType.Queen) != 0 \
            or (i == 1 and piece & PieceType.King != 0)):
          return true
        break
  for dir in DIAGONALS:
    for i in range(1, N_FILES):
      var square: SquarePos = king_square.move(i * dir[0], i * dir[1])
      if not c.is_valid_square(square):
        break
      var piece: int = c.get_square(square)
      if piece != NO_PIECE:
        if piece & c.other_player(player) != 0 \
            and (piece & (PieceType.Bishop | PieceType.Queen) != 0 \
            or (i == 1 and piece & PieceType.King != 0)):
          return true
        break
  for dir in HOPS:
    var square: SquarePos = king_square.move(dir[0], dir[1])
    if not c.is_valid_square(square):
      continue
    var piece: int = c.get_square(square)
    if piece != NO_PIECE:
      if piece & c.other_player(player) != 0 \
          and piece & PieceType.Knight != 0:
        return true

  var attacker = c.other_player(player)
  var pawn_dir = 1 if attacker == Player.White else -1

  for i in [-1, 1]:
    var square: SquarePos = king_square.move(i, -pawn_dir)
    if not c.is_valid_square(square):
      continue
    var piece: int = c.get_square(square)
    if piece != NO_PIECE and piece & attacker != 0 \
        and piece & PieceType.Pawn != 0:
      return true

  return false

func force_move(ply: Ply) -> SquarePos:
  var piece          = get_square(ply.source)
  var captured       = get_square(ply.destination)
  var capture_square = null
  if captured != NO_PIECE:
    capture_square = ply.destination
  set_square(ply.source,      NO_PIECE)
  set_square(ply.destination, piece)

  turn = other_player(turn)

  # En Passant
  if en_passant_square and piece & PieceType.Pawn \
      and en_passant_square.is_same(ply.destination):
    capture_square = SquarePos.new(en_passant_square.file, 0)
    if en_passant_square.rank == 2:
      capture_square.rank = 3
    else:
      capture_square.rank = 4
    set_square(capture_square, NO_PIECE)

  en_passant_square = null
  if piece & PieceType.Pawn:
    var en_passant_rank: int
    var capture_rank:    int
    match piece & PIECE_PLAYER_MASK:
      Player.White:
        en_passant_rank = 3
        capture_rank    = 2
      Player.Black:
        en_passant_rank = 4
        capture_rank    = 5
    if ply.destination.rank == en_passant_rank:
      en_passant_square = SquarePos.new(ply.destination.file, capture_rank)

  return capture_square

func make_move(ply: Ply) -> bool:
  var legal_moves = get_legal_moves()
  var legal       = false
  for move in legal_moves:
    if ply.source.is_same(move.source) \
        and ply.destination.is_same(move.destination):
      legal = true
      break
  if not legal:
    return false
  
  var capture_square = force_move(ply)

  emit_signal("on_move_made", ply, turn, capture_square)

  return true

func print_board():
  for rank in N_RANKS:
    var s = ""
    for file in N_FILES:
      var piece = get_square(SquarePos.new(file, 7 - rank))
      s += ("[" + String(piece) + "]")
    print(s)
