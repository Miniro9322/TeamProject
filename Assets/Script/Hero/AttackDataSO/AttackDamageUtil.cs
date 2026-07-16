using System.Collections.Generic;
using System.Diagnostics;

public static class AttackDamageUtil
{
    public static void ApplyInstantDamage(AttackDataSO data, AttackContext ctx)
    {
        int damage = (int)(ctx.sc[StatType.ATK] * data.attackPer);

        if (data.attackType == AttackType.Splash)
        {
            foreach (IDamageAble enemy in ctx.getEnemiesInRange(ctx.self.position, data.splashRange, data.splashSquare))
                enemy.TakeDamage(damage);
            SplashHighlighter.Instance?.Flash(ctx.self.position, data.splashRange, data.splashSquare);
        }
        else if (data.attackType == AttackType.Multiple)
        {
            List<IDamageAble> enemies = ctx.getEnemiesInRange(ctx.self.position, data.range, data.square);
            int count = data.targetCount < enemies.Count ? data.targetCount : enemies.Count;
            for (int i = 0; i < count; i++)
                enemies[i].TakeDamage(damage);
        }
        else
        {
            IDamageAble target = ctx.target.GetComponent<IDamageAble>();
            if (target != null)
                target.TakeDamage(damage);
        }
    }
}
