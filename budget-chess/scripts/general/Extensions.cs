using System;
using System.Collections.Generic;
using System.Text;

namespace BudgetChess {

  public static class MultidimensionalArrayExtensions {
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

  public static class ListExtensions {
    public static void Filter<T>(this List<T> xs, Predicate<T> p) {
      for (int i = xs.Count - 1; i >= 0; i--)
        if (!p(xs[i]))
          xs.RemoveAt(i);
    }

    public static void Print<T>(this List<T> xs) {
      var sb = new StringBuilder();
      sb.AppendLine("==== List ====");
      foreach (T x in xs)
        sb.AppendLine(x.ToString());
      sb.AppendLine("==============");
      Godot.GD.Print(sb.ToString());
    }
  }

}
