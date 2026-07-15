public static class AttackDamageUtil
{
    public static void ApplyInstantDamage(AttackDataSO data, AttackContext ctx)
    {
        if (data.attackType == AttackType.Multiple)
        {
            foreach (IDamageAble enemy in ctx.getEnemiesInRange(ctx.self.position, data.range, data.square))
                enemy.TakeDamage((int)(ctx.sc[StatType.ATK] * data.attackPer));
        }
        else
        {
            IDamageAble target = ctx.target.GetComponent<IDamageAble>();
            if (target != null)
                target.TakeDamage((int)(ctx.sc[StatType.ATK] * data.attackPer));
        }
    }
}
