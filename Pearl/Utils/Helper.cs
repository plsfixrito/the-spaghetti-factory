using System.Linq;
using BattleRight.Core;
using BattleRight.Core.GameObjects;
using BattleRight.Core.Math;
using BattleRight.Core.Models;

namespace Pearl.Utils
{
    internal class Helper
    {
        public static bool IsValidTarget(Player player)
        {
            return player == null || (!player.IsDead && !player.HasConsumeBuff && !player.IsCountering && !player.IsImmaterial && !HasBuff(player, "GustBuff") && !HasBuff(player, "BulwarkBuff") && !HasBuff(player, "Incapacitate") && !HasBuff(player, "PetrifyStone"));
        }

        public static bool HasBuff(Player player, string buffName)
        {
            return player?.Buffs != null && player.Buffs.FirstOrDefault(b => b.ObjectName == buffName) != default(Buff);
        }

        public static bool IsProjectileColliding(Player player, Projectile enemyProjectile)
        {
            return enemyProjectile != null && Geometry.CircleVsThickLine(new Vector2(player.WorldPosition.X, player.WorldPosition.Y), player.MapCollisionRadius,
                       enemyProjectile.StartPosition, enemyProjectile.CalculatedEndPosition, enemyProjectile.SpellCollisionRadius, true);
        }

        public static bool IsColliding(Player localPlayer, Player target)
        {
            if (target != null && target.IsLocalPlayer)
                return false;

            if (localPlayer != null && target != null) // && !target.IsLocalPlayer)
            {
                var heading = target.WorldPosition - localPlayer.WorldPosition;
                var distance = heading.Length();
                var direction = heading / distance;

                return CollisionSolver.CheckThickLineCollision(localPlayer.WorldPosition,
                    target.WorldPosition + direction, target.MapCollisionRadius-0.3f).IsColliding;
            }
            return false;
        }

        public static Vector2 GetBestIncommingProjectileWallPosition(Projectile projectile) //thnx snowy
        {
            if (projectile == null)
            {
                return default(Vector2);
            }

            var difPos = LocalPlayer.Instance.WorldPosition - projectile.WorldPosition;
            return projectile.WorldPosition + (difPos / 2);
        }
    }
}