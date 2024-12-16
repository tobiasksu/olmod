namespace GameMod.CMTracker.Models
{
    public class RobotStat
    {
        public int EnemyTypeId { get; set; }
        public bool IsSuper { get; set; }
        public float DamageReceived { get; set; }
        public float DamageDealt { get; set; }
        public int NumKilled { get; set; }
    }
}