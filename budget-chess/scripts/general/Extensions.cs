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

}
