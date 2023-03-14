using Overload;

namespace GameMod.Weapons
{
    public interface IWeapon
    {
        void Fire(Player player, float refire_multiplier);
    }
}