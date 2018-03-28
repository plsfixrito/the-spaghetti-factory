using System;
using System.Linq;
using BattleRight.Core;
using BattleRight.Core.Enumeration;
using BattleRight.Core.GameObjects;
using BattleRight.SDK;
using BattleRight.SDK.Enumeration;
using BattleRight.SDK.UI;
using BattleRight.SDK.UI.Values;
using Pearl.Utils;
using Stunlock.Console;
using UnityEngine;
using Vector2 = BattleRight.Core.Math.Vector2;

namespace Pearl
{
    internal class Program
    {
        private static float drawDebug;

        private static SkillBase eSkill;
        private static Player player;
        private static bool enabled;
        private static bool useQ;

        public static Projectile GetEnemyProjectiles(float worldDistance)
        {
            return EntitiesManager.ActiveProjectiles?.FirstOrDefault(p =>
                p?.TeamId != EntitiesManager.LocalPlayer?.TeamId && p.Distance(LocalPlayer.Instance) <= worldDistance);
        }

       //TODO Maybe try to merge Heal Combo and Combo together
        private static void HealCombo()
        {
            var allyTarget = TargetSelectorHelper.GetAlly(TargetingMode.LowestHealth, 11f);

            if (allyTarget == null)
                return;

            var RmbSkill = new SkillBase(AbilitySlot.Ability2, SkillType.Circle, 9.2f, 5.6f, 1.15f, 300f);
            var predictRMB = player.GetPrediction(allyTarget, RmbSkill.Speed, RmbSkill.Range, RmbSkill.SpellCollisionRadius, RmbSkill.SkillType);

            if (RmbSkill.Data.Charges > 0 && !allyTarget.IsImmaterial && allyTarget.Health != allyTarget.MaxRecoveryHealth &&
                predictRMB.HitChancePercent >= 75f)
            {
                LocalPlayer.UpdateCursorPosition(predictRMB.MoveMousePosition);
                LocalPlayer.CastAbility(RmbSkill.Slot);
            }
        }
        private static void Combo()
        {
            var enemyTarget = TargetSelectorHelper.GetTarget(TargetingMode.NearLocalPlayer, 9f);

            var enemyProjectiles = GetEnemyProjectiles(10f);

            if (enemyTarget == null)
                return;

            var enemyDistance = player.Distance(enemyTarget.WorldPosition);

            var intersectingWithProjectile = Helper.IsProjectileColliding(player, enemyProjectiles);
            Vector2 intersectPoint = Geometry.GetClosestPointOnLineSegment(enemyProjectiles.StartPosition,
                enemyProjectiles.CalculatedEndPosition, player.WorldPosition, out enemyDistance);
            if (useQ && LocalPlayer.GetAbilityHudData(AbilitySlot.Ability4).CooldownTime < 0.1f && intersectingWithProjectile)
            {
                //if (player.Distance(enemyProjectiles.WorldPosition) <= player.MapCollisionRadius*2.4) //
                    LocalPlayer.CastAbility(AbilitySlot.Ability4);
            }

            var hasBuff = Helper.HasBuff(enemyTarget, "RecastBuff");
            eSkill = new SkillBase(AbilitySlot.Ability5, SkillType.Circle, 10f, 4.4f, 2f);
            var lmbSkill = new SkillBase(AbilitySlot.Ability1, SkillType.Line, 8f, 8f, 0.6f);
            var predictLMB = player.GetPrediction(enemyTarget, lmbSkill.Speed, lmbSkill.Range, lmbSkill.SpellCollisionRadius, lmbSkill.SkillType);


            if (!enemyTarget.IsDead && !enemyTarget.HasConsumeBuff && !enemyTarget.IsCountering &&
                !enemyTarget.IsImmaterial && !Helper.HasBuff(enemyTarget, "GustBuff") &&
                !Helper.HasBuff(enemyTarget, "BulwarkBuff") && !Helper.HasBuff(enemyTarget, "Incapacitate") &&
                !Helper.HasBuff(enemyTarget, "PetrifyStone") && !Helper.IsColliding(player, enemyTarget))
            {
               if (LocalPlayer.GetAbilityHudData(eSkill.Slot).CooldownTime < 0.1f) // TODO incorporate RecastBuff with E
               {
                   LocalPlayer.UpdateCursorPosition(intersectPoint, true); // TODO needs more work
                   LocalPlayer.CastAbility(eSkill.Slot);
               }

                if (predictLMB.HitChancePercent >= 35f)
                {
                    LocalPlayer.UpdateCursorPosition(predictLMB.MoveMousePosition);
                    LocalPlayer.CastAbility(AbilitySlot.Ability1);
                }
            }
        }
        
        private static void Game_OnUpdate()
        {
            if (!Game.IsInGame) return;
            if (!enabled) return;
            player = EntitiesManager.LocalPlayer;

            if (Input.GetKeyDown(KeyCode.X) && EntitiesManager.ActiveGameObjects != null) //EntitiesManager.ActiveObjects
            {
                foreach (var o in EntitiesManager.ActiveGameObjects)
                    StunConsole.Write("o.ObjectName"); //
                StunConsole.Write("\n");
            }

            if (Input.GetMouseButton(4))
                Combo();
            if (Input.GetKey(KeyCode.LeftAlt))
                HealCombo();

        }
        //TODO Fix the match states -.-
        private static void Main()
        {
            var menu = MainMenu.AddMenu("Pearl", "Pearl the Inquisitor");

            var menuDrawDebug = menu.Add(new MenuSlider("drawDebug", "Draw Debug", 1f, 25f));
            var useQMenuValue = menu.Add(new MenuCheckBox("useQskill", "Use Q On Projectiles", false));

            Game.Instance.OnUpdate += delegate
            {
                drawDebug = menuDrawDebug.CurrentValue;
                useQ = useQMenuValue.CurrentValue;
                Game_OnUpdate();
            };
            Game.Instance.OnMatchEnd += delegate
            {
                 enabled = false; 
            };
            Game.Instance.OnMatchStart += delegate
            {
                if (EntitiesManager.LocalPlayer?.CharName != Champion.Pearl.ToString())
                    return;

                player = EntitiesManager.LocalPlayer;
                enabled = true;
            };

            Game.Instance.OnDraw += delegate
            {
                if (!Game.IsInGame) return;
                if (!enabled) return;

                if (player != null)
                {
                    Drawing.DrawCircle(player.WorldPosition, drawDebug, Color.yellow);
                    Drawing.DrawCircle(player.WorldPosition, 9.5f, Color.grey);
                    Drawing.DrawString(player.WorldPosition, "IsCasting: " + player.IsCasting, Color.gray);

                    var enemyProjectile = GetEnemyProjectiles(11);
                    if (enemyProjectile != null)
                    {
                        var intersect = Helper.IsProjectileColliding(player, enemyProjectile);

                        if (intersect)
                        {
                            var intersectPoint = Geometry.GetClosestPointOnLineSegment(enemyProjectile.StartPosition,
                                enemyProjectile.CalculatedEndPosition, player.WorldPosition);
                            var direction = player.WorldPosition - intersectPoint;
                            Drawing.DrawLine(enemyProjectile.WorldPosition, enemyProjectile.CalculatedEndPosition,
                                Color.white);
                            Drawing.DrawLine(player.WorldPosition, intersectPoint, Color.green);
                        }
                        //TODO DrawRect projectile path?
                    }
                }
            };
        }
    }
}