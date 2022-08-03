using System;

namespace BudgetChess {

  [Flags]
  enum Player {
    White = 64,
    Black = 128
  }

  static class PlayerEnumExtensions {
    public static Player Other(this Player player)
      => player == Player.White ? Player.Black : Player.White;
  }

}
