namespace BudgetChess {

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

  static class Directions {
    public static readonly Direction[]
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
  }

}
