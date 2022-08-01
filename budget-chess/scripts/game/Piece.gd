extends Node2D

const BoardState = preload("res://scripts/game/BoardState.gd")

export var square_size: float setget set_square_size
export var piece:       int   setget set_piece

const PIECE_TEXTURES: Dictionary = {
  BoardState.Player.White | BoardState.PieceType.King:
    preload("res://assets/sprites/chessmen/white-king.png"),
  BoardState.Player.White | BoardState.PieceType.Queen:
    preload("res://assets/sprites/chessmen/white-queen.png"),
  BoardState.Player.White | BoardState.PieceType.Rook:
    preload("res://assets/sprites/chessmen/white-rook.png"),
  BoardState.Player.White | BoardState.PieceType.Knight:
    preload("res://assets/sprites/chessmen/white-knight.png"),
  BoardState.Player.White | BoardState.PieceType.Bishop:
    preload("res://assets/sprites/chessmen/white-bishop.png"),
  BoardState.Player.White | BoardState.PieceType.Pawn:
    preload("res://assets/sprites/chessmen/white-pawn.png"),
  BoardState.Player.Black | BoardState.PieceType.King:
    preload("res://assets/sprites/chessmen/black-king.png"),
  BoardState.Player.Black | BoardState.PieceType.Queen:
    preload("res://assets/sprites/chessmen/black-queen.png"),
  BoardState.Player.Black | BoardState.PieceType.Rook:
    preload("res://assets/sprites/chessmen/black-rook.png"),
  BoardState.Player.Black | BoardState.PieceType.Knight:
    preload("res://assets/sprites/chessmen/black-knight.png"),
  BoardState.Player.Black | BoardState.PieceType.Bishop:
    preload("res://assets/sprites/chessmen/black-bishop.png"),
  BoardState.Player.Black | BoardState.PieceType.Pawn:
    preload("res://assets/sprites/chessmen/black-pawn.png")
}

func _ready():
  pass

func set_square_size(value: float):
  $TextureRect.rect_size = Vector2(value, value)

func set_piece(value: int):
  $TextureRect.texture = PIECE_TEXTURES[value]
