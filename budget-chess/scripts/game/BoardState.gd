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

const N_RANKS: int = 8
const N_FILES: int = 8

const NO_PIECE:          int = 0
const PIECE_TYPE_MASK:   int = 2 * PieceType.Pawn - 1
const PIECE_PLAYER_MASK: int = 2 * Player.Black   - 1 - PIECE_TYPE_MASK

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
  var orthogonals: Array = [
    [ 0,  1],
    [ 1,  0],
    [ 0, -1],
    [-1,  0]
  ]
  var diagonals: Array = [
    [ 1,  1],
    [ 1, -1],
    [-1, -1],
    [-1,  1]
  ]
  var orthodiagonals: Array = []
  orthodiagonals.append_array(orthogonals)
  orthodiagonals.append_array(diagonals)
  var hops: Array = [
    [ 1,  2],
    [ 2,  1],
    [ 2, -1],
    [ 1, -2],
    [-1,  2],
    [-2,  1],
    [-2, -1],
    [-1, -2]
  ]
  var moves: Array = []
  for square in get_player_squares(turn):
    var piece = get_square(square)
    match piece & PIECE_TYPE_MASK:
      PieceType.King:
        for dir in orthodiagonals:
          var target_square: SquarePos = square.move(dir[0], dir[1])
          if not is_valid_square(target_square):
            break
          var target_piece: int = get_square(target_square)
          if target_piece & turn == 0:
            moves.append(Ply.new(square, square.move(dir[0], dir[1])))
      PieceType.Queen:
        for dir in orthodiagonals:
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
        for dir in orthogonals:
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
        for dir in hops:
          var target_square: SquarePos = square.move(dir[0], dir[1])
          if not is_valid_square(target_square):
            continue
          var target_piece: int = get_square(target_square)
          if target_piece & turn == 0:
            moves.append(Ply.new(square, square.move(dir[0], dir[1])))
      PieceType.Bishop:
        for dir in diagonals:
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

      _:
        pass
  return moves

func print_board():
  for rank in N_RANKS:
    var s = ""
    for file in N_FILES:
      var piece = get_square(SquarePos.new(file, 7 - rank))
      s += ("[" + String(piece) + "]")
    print(s)
