using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Keepsake;
using Keepsake.Framework.Networking.Logic;
using Keepsake.Gold;
using Keepsake.HyperSpace.GameplayFeatures.Campaigns;
using Keepsake.HyperSpace.GameplayFeatures.Pickupables.RocketLauncher;
using Keepsake.Modal;
using Keepsake.Pickupables.GenericWeapon;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using Utilities.Enums;

namespace JSPlayerModPlugin;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    internal static new ManualLogSource Log;
    public static ConfigEntry<KeyCode> configMenuToggle;


    public JMod jMod = new();
    public static ConfigEntry<bool> configNoPlayerDamage;
    public static ConfigEntry<bool> configNoPlayerShipHealthDamage;
    public static ConfigEntry<bool> configNoPlayerShipShieldDamage;
    public static ConfigEntry<bool> configNoCraftCost;
    public static ConfigEntry<bool> configNoShipAmmoCost;
    public static ConfigEntry<bool> configNoPlayerReload;
    public static ConfigEntry<bool> configInstantBoost;
    public static ConfigEntry<bool> configInfiniteJump;

    public static ConfigEntry<float> configMateriaMultiplier;
    public static ConfigEntry<float> configPlayerDamageMultiplier;
    public static ConfigEntry<float> configPlayerShipDamageMultiplier;
    public static ConfigEntry<float> configPlayerSpeedMultiplier;
    public static ConfigEntry<float> configPlayerXPMultiplier;
    public static ConfigEntry<float> configBoostTimeMult;
    public static ConfigEntry<float> configIngotMultiplier;
    public static ConfigEntry<float> configCreditsMultiplier;



    public override void Load()
    {
        // Plugin startup logic
        Log = base.Log;

        Log.LogWarning($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        configNoPlayerDamage = Config.Bind("Toggles",
                                    "DisablePlayerDamage",
                                    false,
                                    "Disable damage to player");
        configNoPlayerShipHealthDamage = Config.Bind("Toggles",
                                    "DisablePlayerShipDamage",
                                    false,
                                    "Disable damage to player ship's health");
        configNoPlayerShipShieldDamage = Config.Bind("Toggles",
                                    "DisablePlayerShipDamage",
                                    false,
                                    "Disable damage to player ship's shields");
        configNoShipAmmoCost = Config.Bind("Toggles",
                                    "EnableNoShipAmmoCost",
                                    false,
                                    "No reload and ammo cost (spaceship)");
        configNoPlayerReload = Config.Bind("Toggles",
                                    "EnableNoPlayerReload",
                                    false,
                                    "No reload and ammo cost (on foot)");
        configNoCraftCost = Config.Bind("Toggles",
                                    "EnableFreeCraft",
                                    false,
                                    "No assembler craft cost");
        configInstantBoost = Config.Bind("Toggles",
                                    "EnableInstantBoost",
                                    false,
                                    "Enable instant ship boost");
        configInfiniteJump = Config.Bind("Toggles",
                                    "EnableInfiniteJump",
                                    false,
                                    "Enable infinite double jump");


        configPlayerDamageMultiplier = Config.Bind("MultFloat",
                                    "PlayerDamageMultiplier",
                                    1f,
                                    "Player damage multiplier (on foot)");
        configPlayerShipDamageMultiplier = Config.Bind("MultFloat",
                                    "PlayerShipDamageMultiplier",
                                    1f,
                                    "Player damage multiplier (spaceship)");
        configPlayerSpeedMultiplier = Config.Bind("MultFloat",
                                    "PlayerSpeedMultiplier",
                                    1f,
                                    "Player speed multiplier (on foot)");
        configBoostTimeMult = Config.Bind("MultFloat",
                                    "BoostTimeMult",
                                    1f,
                                    "Ship boost time multiplier");
        configMateriaMultiplier = Config.Bind("MultFloat",
                                    "MateriaMultiplier",
                                    1f,
                                    "Materia disassemble gains multiplier");
        configCreditsMultiplier = Config.Bind("MultFloat",
                                    "CreditsMultiplier",
                                    1f,
                                    "Mission credits reward multiplier");
        configIngotMultiplier = Config.Bind("MultFloat",
                                    "IngotMultiplier",
                                    1f,
                                    "Mission ingots reward multiplier");
        configPlayerXPMultiplier = Config.Bind("MultFloat",
                                    "PlayerXPMultiplier",
                                    1f,
                                    "Mission player XP reward multiplier");




        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

        jMod.JModInit(this);
        configMenuToggle = JMod.configMenuToggle;
        IL2CPPBase.Initialize(this);
    }

    //configNoPlayerDamage

    [HarmonyPatch(typeof(HealthComponent_Base), nameof(HealthComponent_Base.DealDamage))]
    class HealthComponentPatch4
    {
        static bool Prefix(ref float damageToDeal, HealthComponent_Base __instance)
        {
            if (!configNoPlayerDamage.Value || __instance.m_ParentPlayer == null)
            {
                //Log.LogInfo("PLAYER DAMAGE ALLOWED 4 - Damage: " + damageToDeal.ToString("0.00"));
                return true;
            }
            // Skip original
            damageToDeal = 0;
            //Log.LogInfo("!!NO PLAYER DAMAGE ALLOWED 4!!");
            return true;
        }
    }

    //configNoPlayerShipHealthDamage

    // Playership_DamageController ApplyDamageToShipCore(int damage)
    [HarmonyPatch(typeof(Playership_DamageController), nameof(Playership_DamageController.ApplyDamageToShipCore))]
    [HarmonyPatch(typeof(Playership_DamageController), nameof(Playership_DamageController.DelayedDamage))]
    class Playership_DamageControllerPatch2
    {
        static bool Prefix(ref int damage, ref Playership_DamageController __instance)
        {
            if (!configNoPlayerShipHealthDamage.Value)
            {
                //Log.LogInfo("SHIP CORE DAMAGE ALLOWED - Damage value: " + damage);
                return true;
            }
            damage = 0;
            //Log.LogInfo("!!NO SHIP CORE DAMAGE ALLOWED!! - Damage value: " + damage);
            return true;
        }
    }

    [HarmonyPatch(typeof(shipref), nameof(shipref.Update))]
    class shipref_UpdatePatch1
    {
        static void Postfix(ref shipref __instance)
        {
            if (!configNoPlayerShipHealthDamage.Value)
            {
                //Log("SHIP CORE DELAYED DAMAGE ALLOWED - Damage value: " + damage);
                return;
            }
            if (__instance.m_SpaceShip_StateBlackboardData != null)
                if (__instance.m_SpaceShip_StateBlackboardData.m_ShipHealth.Value < __instance.m_SpaceShip_StateBlackboardData.m_ShipMaxHealth.Value)
                {
                    int health = __instance.m_SpaceShip_StateBlackboardData.m_ShipHealth.Value;
                    int maxHealth = __instance.m_SpaceShip_StateBlackboardData.m_ShipMaxHealth.Value;
                    //Log("!!NO SHIP HEALTH DAMAGE ALLOWED!! - Health value: " + health + " max health: " + maxHealth);
                    __instance.m_SpaceShip_StateBlackboardData.m_ShipHealth.Value = __instance.m_SpaceShip_StateBlackboardData.m_ShipMaxHealth.Value;
                }

        }
    }

    [HarmonyPatch(typeof(SpaceShip_BaseComponent), nameof(SpaceShip_BaseComponent.SetComponentHealthStatus))]
    class Playership_BaseComponentPatch1
    {
        static bool Prefix(ref SpaceShip_BaseComponent.ComponentHealthStatus newCritical)
        {
            if (!configNoPlayerShipHealthDamage.Value)
            {
                //Log.LogInfo("SHIP DAMAGE ALLOWED - New component status: " + newCritical.ToString());
                return true;
            }
            newCritical = SpaceShip_BaseComponent.ComponentHealthStatus.Healthy;
            //Log.LogInfo("!!NO SHIP DAMAGE ALLOWED!! - New component status: " + newCritical.ToString());
            return true;
        }
    }

    //configPlayerShipDamageMultiplier

    [HarmonyPatch(typeof(SpaceShip_Cannon_Base), nameof(SpaceShip_Cannon_Base.SafeStart))]
    class SpaceShip_Cannon_BasePatch1
    {

        public class ShipCannon
        {
            public float hullDamage;
            public float shieldDamage;
            public int stationId;
        }

        static Dictionary<int, ShipCannon> shipCannonsDict = new();
        static bool Prefix(ref SpaceShip_Cannon_Base __instance)
        {
            if (configPlayerShipDamageMultiplier.Value <= 1 || __instance.TracerProjectileData == null)
            {
                Log.LogInfo("SHIP SafeStart CANNON DAMAGE NORMAL");
                return true;
            }
            ShipCannon currentCannon = new()
            {
                hullDamage = __instance.TracerProjectileData.ShipHullDamage * configPlayerShipDamageMultiplier.Value, 
                shieldDamage = __instance.TracerProjectileData.ShipshieldDamage * configPlayerShipDamageMultiplier.Value,
                stationId = __instance.CombatStationID
            };
            if (!shipCannonsDict.ContainsKey(__instance.CombatStationID))
            {
                shipCannonsDict.Add(__instance.CombatStationID, currentCannon);
                Log.LogInfo("Added SHIP SafeStart CANNON Id " + __instance.CombatStationID);
                __instance.TracerProjectileData.m_ShipHullDamage = currentCannon.hullDamage;
                __instance.TracerProjectileData.m_ShipShieldDamage = currentCannon.shieldDamage;
            }
            else
                if (shipCannonsDict[__instance.CombatStationID].hullDamage != __instance.TracerProjectileData.m_ShipHullDamage || shipCannonsDict[__instance.CombatStationID].shieldDamage != __instance.TracerProjectileData.m_ShipShieldDamage)
                {
                    shipCannonsDict[__instance.CombatStationID].hullDamage = __instance.TracerProjectileData.m_ShipHullDamage * configPlayerShipDamageMultiplier.Value;
                    shipCannonsDict[__instance.CombatStationID].shieldDamage = __instance.TracerProjectileData.m_ShipShieldDamage * configPlayerShipDamageMultiplier.Value;
                    Log.LogInfo("SHIP SafeStart CANNON Id " + __instance.CombatStationID + " Damage changed!");
                    __instance.TracerProjectileData.m_ShipHullDamage = currentCannon.hullDamage;
                    __instance.TracerProjectileData.m_ShipShieldDamage = currentCannon.shieldDamage;
                }

            Log.LogInfo("SHIP SafeStart CANNON Id " + __instance.CombatStationID + " HULL DAMAGE: " + __instance.TracerProjectileData.ShipHullDamage + " SHIELD DAMAGE: " + __instance.TracerProjectileData.ShipshieldDamage);
            return true;
        }
    }

    //configNoPlayerShipShieldDamage

    // Keepsake.ShieldUnitsManager !!! DealDamageToShields(int finalDamage, DamageInfo damageInfo, out int coreDamage)

    [HarmonyPatch(typeof(ShieldUnitController), nameof(ShieldUnitController.OnPlateHealthChanged))]
    class ShieldUnitControllerPatch1
    {
        static void Postfix(ref int plateHealth, ref ShieldUnitController __instance)
        {
            if (!configNoPlayerShipShieldDamage.Value)
            {
                //Log.LogInfo("SHIP SHIELD DAMAGE ALLOWED");
                return;
            }
            if (plateHealth < __instance.MaxHealth)
            {
                plateHealth = __instance.MaxHealth;
                __instance.m_ShieldUnitBlackboardData.m_IsShieldUnitActivated.Value = true;
                __instance.m_ShieldUnitBlackboardData.m_ShieldUnitHealth.Value = __instance.MaxHealth;
                //Log.LogInfo("SHIP SHIELD STRENGTH: " + plateHealth);
            }
        }
    }

    //configNoShipAmmoCost


    [HarmonyPatch(typeof(PlayerShip_IndividualTurretController), nameof(PlayerShip_IndividualTurretController.ExpendAmmunition))]
    class PlayerShip_IndividualTurretControllerPatch1
    {

        static bool Prefix(ref float ammoUsagePerRound)
        {
            if (!configNoShipAmmoCost.Value)
            {
                //Log.LogInfo("SHIP AMMO COST NORMAL - Cost value: " + ammoUsagePerRound.ToString());
                return true;
            }
            ammoUsagePerRound = 0;
            //Log.LogInfo("SHIP AMMO COST ZERO - Cost value: " + ammoUsagePerRound.ToString());
            return true;
        }
    }

    //configNoCraftCost

    [HarmonyPatch(typeof(Assembler), nameof(Assembler.CraftBlueprint))]
    class Assembler_CraftBlueprintPatch1
    {
        static bool Prefix(ref UnlockableBlueprint blueprint)
        {
            if (!configNoCraftCost.Value)
            {
                //Log.LogInfo("CRAFT COST NORMAL - Cost value: " + blueprint.m_MateriaCraftCost.ToString());
                return true;
            }
            blueprint.m_MateriaCraftCost = 0;
            //Log.LogInfo("CRAFT COST ZERO - Cost value: " + blueprint.m_MateriaCraftCost.ToString());
            return true;
        }
    }

    //configMateriaMultiplier

    [HarmonyPatch(typeof(Disassembler), nameof(Disassembler.Disassemble))]
    class DisassemblePatch1
    {

        static bool Prefix(ref int materiaValue)
        {
            if (configMateriaMultiplier.Value <= 1)
            {
                //Log.LogInfo("MATERIA MULTIPLIER NORMAL - Materia disassemble value: " + materiaValue.ToString());
                return true;
            }
            materiaValue = (int)(materiaValue * configMateriaMultiplier.Value);
            //Log.LogInfo("MATERIA MULTIPLIER " + configMateriaMultiplier.Value.ToString() + " - Materia disassemble value: " + materiaValue.ToString());
            return true;
        }
    }

    //configNoPlayerReload

    // Keepsake.PlayerPickupableItemHandler
    //Handle_AmmoInMagChanged(int ammoInMag)
    [HarmonyPatch(typeof(PlayerPickupableItemHandler), nameof(PlayerPickupableItemHandler.Handle_AmmoInMagChanged))]
    class ItemHandlerPatch1
    {
        static bool Prefix(ref PlayerPickupableItemHandler __instance, ref int ammoInMag)
        {
            if (!configNoPlayerReload.Value)
            {
                //Log.LogInfo("PLAYER RELOAD NORMAL");
                return true;
            }
            int maxAmmoInMag = __instance.ItemHeldPersistentPickupable.PickupableBlackboardData.m_MagSize.Value;
            int currentAmmoInMag = __instance.ItemHeldPersistentPickupable.PickupableBlackboardData.m_AmmoInMag.Value;
            //Log.LogInfo("PLAYER NO RELOAD - Current bulletcount: " + __instance.CurrentAmmo + " max ammo " + __instance.MaxAmmo + " max ammo in mag: " + maxAmmoInMag  + " new ammo in mag: " + currentAmmoInMag);
            if (currentAmmoInMag < maxAmmoInMag)
                __instance.ItemHeldPersistentPickupable.PickupableBlackboardData.m_AmmoInMag.Value = maxAmmoInMag;
            //Log.LogInfo("PLAYER AMMO COST ZERO - Current bulletcount: " + __instance.m_CurrentBulletCount.ToString());
            return true;
        }
    }
    [HarmonyPatch(typeof(PickupableItem_Railgun), nameof(PickupableItem_Railgun.FireRailgunProjectile))]
    class RailgunPatch1
    {
        static void Postfix(ref PickupableItem_Railgun __instance)
        {
            if (!configNoPlayerReload.Value)
            {
                return;
            }
            //Log("RAILGUN NO RELOAD - Current resource: " + __instance.m_PickupableItemBlackboardData.m_ResourceAmount.Value);
            //Log("RAILGUN NO RELOAD - Max resource to carry: " + __instance.m_ItemData.m_MaxResourceAmountToCarry);
            __instance.m_PickupableItemBlackboardData.m_ResourceAmount.Value = __instance.m_ItemData.m_MaxResourceAmountToCarry;
        }
    }
    // Il2CppKeepsake.HyperSpace.GameplayFeatures.Pickupables.RocketLauncher.PickupableItem_RPG_FirstPerson

    [HarmonyPatch(typeof(PickupableItem_RPG_FirstPerson), nameof(PickupableItem_RPG_FirstPerson.FireReleased))]
    class RPGPatch1
    {
        static void Postfix(ref PickupableItem_RPG_FirstPerson __instance)
        {
            if (!configNoPlayerReload.Value)
            {
                return;
            }
            //Log("RPG NO RELOAD - Current resource: " + __instance.m_PickupableItemBlackboardData.m_ResourceAmount.Value);
            //Log("RPG NO RELOAD - Max resource to carry: " + __instance.m_ItemData.m_MaxResourceAmountToCarry);
            __instance.m_PickupableItemBlackboardData.m_ResourceAmount.Value = __instance.m_ItemData.m_MaxResourceAmountToCarry;
        }
    }


    // configPlayerDamageMultiplier

    // PickupableItemFirstPerson_Base !!
    [HarmonyPatch(typeof(PickupableItemFirstPerson_Base), nameof(PickupableItemFirstPerson_Base.ShootProjectile))]
    class FPGunPatch1
    {
        static bool Prefix(ref float interiorDamage, ref float shipDamage)
        {
            if (configPlayerDamageMultiplier.Value <= 1f)
            {
                //Log.LogInfo("PLAYER DAMAGE MULTIPLIER NORMAL");
                return true;
            }
            interiorDamage *= configPlayerDamageMultiplier.Value;
            shipDamage *= configPlayerDamageMultiplier.Value;
            //Log.LogInfo("PLAYER DAMAGE MULTIPLIER " + configPlayerDamageMultiplier.Value.ToString() + " - Interior damage: " + interiorDamage + " - Ship damage: " + shipDamage);
            return true;
        }
    }

    //PlayerMeleeHandler 
    [HarmonyPatch(typeof(PlayerMeleeHandler), nameof(PlayerMeleeHandler.GetDamage))]
    class MeleePatch1
    {
        static void Postfix(ref PlayerMeleeHandler __instance, ref float __result)
        {
            if (configPlayerDamageMultiplier.Value <= 1f)
            {
                //Log.LogInfo("PLAYER MELEE DAMAGE MULTIPLIER NORMAL - Multiplier value: " + __result.ToString());
                return;
            }
            __result *= configPlayerDamageMultiplier.Value;
            //Log.LogInfo("PLAYER MELEE DAMAGE MULTIPLIER " + configPlayerDamageMultiplier.Value.ToString() + " - Multiplier value: " + __result.ToString());
        }
    }

    // configInstantBoost

    // SpaceShip_EngineController
    // BoostRechargeTime
    [HarmonyPatch(typeof(SpaceShip_EngineController), nameof(SpaceShip_EngineController.BoostRechargeTime), MethodType.Getter)]
    class Player_ShipBoostPatch1
    {
        static void Postfix(ref float __result)
        {
            if (!configInstantBoost.Value)
            {
                return;
            }
            __result = 0;

        }
    }

    // configInfiniteJump

    [HarmonyPatch(typeof(DoubleJumpAbility), nameof(DoubleJumpAbility.ShouldActivate))]
    class DoubleJumpAbilityPatch1
    {
        static void Postfix(ref bool __result)
        {
            if (!configInfiniteJump.Value)
            {
                return;
            }
            if (Keepsake.HyperSpace.NewInputSystem.InputManager.IsActionPressed(Keepsake.HyperSpace.NewInputSystem.InputManager.InputKeys.Jump))
            {
                __result = true;
            }

        }
    }

    // configPlayerSpeedMultiplier

    // Il2Cpp.Player_MovementHandler
    [HarmonyPatch(typeof(Player_MovementHandler), nameof(Player_MovementHandler.AfterCharacterUpdate))]
    class LocalVelocityPatch1
    {
        static void Postfix(ref Player_MovementHandler __instance)
        {
            if (configPlayerSpeedMultiplier.Value <= 1 || !__instance.IsOnGround)
            {
                return;
            }
            Vector3 vectorMult = new Vector3(configPlayerSpeedMultiplier.Value, configPlayerSpeedMultiplier.Value, configPlayerSpeedMultiplier.Value);
            Vector3 localV = Vector3.Scale(__instance.LocalMovementVelocity, vectorMult);
            float maxSpeed = __instance.MaxMovementSpeed * configPlayerSpeedMultiplier.Value;
            localV = Vector3.ClampMagnitude(localV, maxSpeed);
            //Log("MovementHandler sprint local velocity multiplied: " + localV.magnitude + " sprint time " + __instance.SprintElapsedTime);
            __instance.LocalMovementVelocity = localV;
        }
    }

    //configBoostTimeMult

    // BoostRechargeTime
    [HarmonyPatch(typeof(SpaceShip_EngineController), nameof(SpaceShip_EngineController.BoostTime), MethodType.Getter)]
    class Player_ShipBoostPatch2
    {
        static void Postfix(ref float __result)
        {
            if (configBoostTimeMult.Value <= 1)
            {
                return;
            }
            __result *= configBoostTimeMult.Value;

        }
    }

    // Keepsake.PersistentPickupable

    // Keepsake.PlayerPickupableItemHandler PersistentPickupable GetPersistentObjectFromItemData(PickupableItem_Data itemData)
    /*[HarmonyPatch(typeof(PlayerPickupableItemHandler), nameof(PlayerPickupableItemHandler.GetPersistentObjectFromItemData))]
    class IngotValuePatch1
    {
        static bool Prefix(ref PickupableItem_Data itemData, ref PlayerPickupableItemHandler __instance)
        {
            if (configIngotMultiplier.Value <= 1 || itemData == null)
            {
                return true;
            }
            itemData.m_Ingots *= configIngotMultiplier.Value;
            Log.LogInfo("INGOT VALUE MULTIPLIER: " + configIngotMultiplier.Value.ToString() + " - Item value: " + itemData.m_Ingots.ToString());
            return true;
        }
    }*/

    // Keepsake.HyperSpace.GameplayFeatures.Campaigns.MissionData
    [HarmonyPatch(typeof(MissionData), nameof(MissionData.GetIngotsReward))]
    class IngotValuePatch2
    {
        static void Postfix(ref int __result)
        {
            if (configIngotMultiplier.Value <= 1)
            {
                return;
            }
            __result = (int)(__result * configIngotMultiplier.Value);
            //Log.LogInfo("INGOT VALUE MULTIPLIER 2 (mission): " + configIngotMultiplier.Value.ToString() + " - Ingots: " + __result);
        }
    }

    [HarmonyPatch(typeof(MissionData), nameof(MissionData.GetCreditsReward))]
    class CreditsRewardPatch1
    {
        static void Postfix(ref int __result)
        {
            if (configCreditsMultiplier.Value <= 1)
            {
                return;
            }
            __result = (int)(__result * configCreditsMultiplier.Value);
            //Log.LogInfo("CREDITS VALUE MULTIPLIER 2 (mission): " + configCreditsMultiplier.Value.ToString() + " - Credits: " + __result);
        }
    }

    //configPlayerXPMultiplier
    [HarmonyPatch(typeof(MissionData), nameof(MissionData.GetExperienceReward))]
    class XPRewardPatch1
    {
        static void Postfix(ref int __result)
        {
            if (configPlayerXPMultiplier.Value <= 1)
            {
                return;
            }
            __result = (int)(__result * configPlayerXPMultiplier.Value);
            //Log.LogInfo("XP VALUE MULTIPLIER 2 (mission): " + configPlayerXPMultiplier.Value.ToString() + " - Experience: " + __result);
        }
    }

}

public class IL2CPPBase : MonoBehaviour
{
    private static Plugin baseJPlugin;

    public static void Initialize(Plugin plugin)
    {
        IL2CPPChainloader.AddUnityComponent(typeof(IL2CPPBase));
        baseJPlugin = plugin;
    }

    private void Update()
    {
        if (Event.current != null)
            if ((Event.current.keyCode == (Plugin.configMenuToggle.Value)) && (Event.current.type == EventType.KeyDown))
            {
                Plugin.Log.LogInfo("GUI Toggled!");
                baseJPlugin.jMod.SwitchMenu();
            }
    }

    private void OnGUI()
    {
        baseJPlugin.jMod.ShowMenu();
    }
}

