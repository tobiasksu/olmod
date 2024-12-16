using System;

namespace GameMod.CMTracker.Models
{
    public class LeaderboardEntry
    {
        public int Rank { get; set; }
        public int FavoriteWeaponId { get; set; }
        public float Time { get; set; }
        public int Kills { get; set; }
        public string PilotName { get; set; }
        public int Score { get; set; }
        public DateTime DateAdded { get; set; }
    }
}