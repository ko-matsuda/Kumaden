using UnityEngine;

public static class GameFlags
{
    // RETRYの時だけ true にして再ロード → Main起動時にプロローグをスキップ
    public static bool SkipPrologueOnce = false;
}
