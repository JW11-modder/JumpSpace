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

    // configNoPlayerDamage

    [HarmonyPatch(typeof(HealthComponent_Base), nameof(HealthComponent_Base.DealDamage))]
    class HealthComponentPatch4
    {
        static bool Prefix(ref float damageToDeal, HealthComponent_Base __instance)
        {
            if (!configNoPlayerDamage.Value || __instance.m_ParentPlayer == null)
                return true;
            damageToDeal = 0;
            return true;
        }
    }

    // configNoPlayerShipHealthDamage

    [HarmonyPatch(typeof(Playership_DamageController), nameof(Playership_DamageController.ApplyDamageToShipCore))]
    class Playership_DamageControllerPatch2
    {
        static bool Prefix(ref int damage, ref Playership_DamageController __instance)
        {
            if (!configNoPlayerShipHealthDamage.Value)
                return true;
            damage = 0;
            return true;
        }
    }

    [HarmonyPatch(typeof(Playership_DamageController), nameof(Playership_DamageController.DelayedDamage))]
    class Playership_DamageControllerPatch3
    {
        static bool Prefix(ref int damage, ref Playership_DamageController __instance)
        {
            if (!configNoPlayerShipHealthDamage.Value)
                return true;
            damage = 0;
            return true;
        }
    }

    [HarmonyPatch(typeof(shipref), nameof(shipref.Update))]
    class shipref_UpdatePatch1
    {
        static void Postfix(ref shipref __instance)
        {
            if (!configNoPlayerShipHealthDamage.Value)
                return;
            if (__instance.m_SpaceShip_StateBlackboardData != null)
                if (__instance.m_SpaceShip_StateBlackboardData.m_ShipHealth.Value < __instance.m_SpaceShip_StateBlackboardData.m_ShipMaxHealth.Value)
                {
                    int health = __instance.m_SpaceShip_StateBlackboardData.m_ShipHealth.Value;
                    int maxHealth = __instance.m_SpaceShip_StateBlackboardData.m_ShipMaxHealth.Value;
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
                return true;
            newCritical = SpaceShip_BaseComponent.ComponentHealthStatus.Healthy;
            return true;
        }
    }

    // configPlayerShipDamageMultiplier

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
            if (configPlayerShipDamageMultiplier.Value <= 1f || __instance.TracerProjectileData == null)
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

    // configNoPlayerShipShieldDamage

    [HarmonyPatch(typeof(ShieldUnitController), nameof(ShieldUnitController.OnPlateHealthChanged))]
    class ShieldUnitControllerPatch1
    {
        static void Postfix(ref int plateHealth, ref ShieldUnitController __instance)
        {
            if (!configNoPlayerShipShieldDamage.Value)
                return;
            if (plateHealth < __instance.MaxHealth)
            {
                plateHealth = __instance.MaxHealth;
                __instance.m_ShieldUnitBlackboardData.m_IsShieldUnitActivated.Value = true;
                __instance.m_ShieldUnitBlackboardData.m_ShieldUnitHealth.Value = __instance.MaxHealth;
            }
        }
    }

    // configNoShipAmmoCost


    [HarmonyPatch(typeof(PlayerShip_IndividualTurretController), nameof(PlayerShip_IndividualTurretController.ExpendAmmunition))]
    class PlayerShip_IndividualTurretControllerPatch1
    {

        static bool Prefix(ref float ammoUsagePerRound)
        {
            if (!configNoShipAmmoCost.Value)
                return true;
            ammoUsagePerRound = 0;
            return true;
        }
    }

    // configNoCraftCost

    [HarmonyPatch(typeof(Assembler), nameof(Assembler.CraftBlueprint))]
    class Assembler_CraftBlueprintPatch1
    {
        static bool Prefix(ref UnlockableBlueprint blueprint)
        {
            if (!configNoCraftCost.Value)
                return true;
            blueprint.m_MateriaCraftCost = 0;
            return true;
        }
    }

    // configMateriaMultiplier

    [HarmonyPatch(typeof(Disassembler), nameof(Disassembler.Disassemble))]
    class DisassemblePatch1
    {

        static bool Prefix(ref int materiaValue)
        {
            if (configMateriaMultiplier.Value <= 1)
                return true;
            materiaValue = Mathf.RoundToInt(materiaValue * configMateriaMultiplier.Value);
            return true;
        }
    }

    // configNoPlayerReload

    [HarmonyPatch(typeof(PlayerPickupableItemHandler), nameof(PlayerPickupableItemHandler.Handle_AmmoInMagChanged))]
    class ItemHandlerPatch1
    {
        static bool Prefix(ref PlayerPickupableItemHandler __instance, ref int ammoInMag)
        {
            if (!configNoPlayerReload.Value)
                return true;
            int maxAmmoInMag = __instance.ItemHeldPersistentPickupable.PickupableBlackboardData.m_MagSize.Value;
            int currentAmmoInMag = __instance.ItemHeldPersistentPickupable.PickupableBlackboardData.m_AmmoInMag.Value;
            if (currentAmmoInMag < maxAmmoInMag)
                __instance.ItemHeldPersistentPickupable.PickupableBlackboardData.m_AmmoInMag.Value = maxAmmoInMag;
            return true;
        }
    }
    [HarmonyPatch(typeof(PickupableItem_Railgun), nameof(PickupableItem_Railgun.FireRailgunProjectile))]
    class RailgunPatch1
    {
        static void Postfix(ref PickupableItem_Railgun __instance)
        {
            if (!configNoPlayerReload.Value)
                return;
            __instance.m_PickupableItemBlackboardData.m_ResourceAmount.Value = __instance.m_ItemData.m_MaxResourceAmountToCarry;
        }
    }
    [HarmonyPatch(typeof(PickupableItem_RPG_FirstPerson), nameof(PickupableItem_RPG_FirstPerson.FireReleased))]
    class RPGPatch1
    {
        static void Postfix(ref PickupableItem_RPG_FirstPerson __instance)
        {
            if (!configNoPlayerReload.Value)
                return;
            __instance.m_PickupableItemBlackboardData.m_ResourceAmount.Value = __instance.m_ItemData.m_MaxResourceAmountToCarry;
        }
    }

    // configPlayerDamageMultiplier

    [HarmonyPatch(typeof(PickupableItemFirstPerson_Base), nameof(PickupableItemFirstPerson_Base.ShootProjectile))]
    class FPGunPatch1
    {
        static bool Prefix(ref float interiorDamage, ref float shipDamage)
        {
            if (configPlayerDamageMultiplier.Value <= 1f)
                return true;
            interiorDamage *= configPlayerDamageMultiplier.Value;
            shipDamage *= configPlayerDamageMultiplier.Value;
            return true;
        }
    }
    [HarmonyPatch(typeof(PlayerMeleeHandler), nameof(PlayerMeleeHandler.GetDamage))]
    class MeleePatch1
    {
        static void Postfix(ref PlayerMeleeHandler __instance, ref float __result)
        {
            if (configPlayerDamageMultiplier.Value <= 1f)
                return;
            __result *= configPlayerDamageMultiplier.Value;
        }
    }

    // configInstantBoost

    [HarmonyPatch(typeof(SpaceShip_EngineController), nameof(SpaceShip_EngineController.BoostRechargeTime), MethodType.Getter)]
    class Player_ShipBoostPatch1
    {
        static void Postfix(ref float __result)
        {
            if (!configInstantBoost.Value)
                return;
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
                return;
            if (Keepsake.HyperSpace.NewInputSystem.InputManager.IsActionPressed(Keepsake.HyperSpace.NewInputSystem.InputManager.InputKeys.Jump))
                __result = true;
        }
    }

    // configPlayerSpeedMultiplier

    private static float maxSlideSpeed = 16f;

    [HarmonyPatch(typeof(Player_MovementHandler), nameof(Player_MovementHandler.Awake))]
    class LocalVelocityPatch1
    {
        static void Postfix(ref Player_MovementHandler __instance)
        {
            maxSlideSpeed = (float)__instance?.m_PlayerController?.SlideSettings?.m_MaxSlideSpeed;
        }
    }

    [HarmonyPatch(typeof(Player_MovementHandler), nameof(Player_MovementHandler.AfterCharacterUpdate))]
    class LocalVelocityPatch2
    {
        static void Postfix(ref Player_MovementHandler __instance)
        {
            if (configPlayerSpeedMultiplier.Value <= 1f || !__instance.IsOnGround)
                return;
            Vector3 vectorMult = new Vector3(configPlayerSpeedMultiplier.Value, configPlayerSpeedMultiplier.Value, configPlayerSpeedMultiplier.Value);
            Vector3 localV = Vector3.Scale(__instance.LocalMovementVelocity, vectorMult);
            float maxSpeed = __instance.MaxMovementSpeed * configPlayerSpeedMultiplier.Value;
            if (__instance.m_PlayerBlackboardData.m_IsSliding.Value)
                __instance.m_PlayerController.SlideSettings.m_MaxSlideSpeed = maxSlideSpeed * configPlayerSpeedMultiplier.Value;
            localV = Vector3.ClampMagnitude(localV, maxSpeed);
            if (!__instance.m_PlayerBlackboardData.m_IsSliding.Value)
                __instance.LocalMovementVelocity = localV;
        }
    }

    // configBoostTimeMult

    [HarmonyPatch(typeof(SpaceShip_EngineController), nameof(SpaceShip_EngineController.BoostTime), MethodType.Getter)]
    class Player_ShipBoostPatch2
    {
        static void Postfix(ref float __result)
        {
            if (configBoostTimeMult.Value <= 1)
                return;
            __result *= configBoostTimeMult.Value;
        }
    }

    // configIngotMultiplier

    [HarmonyPatch(typeof(MissionData), nameof(MissionData.GetIngotsReward))]
    class IngotValuePatch2
    {
        static void Postfix(ref int __result)
        {
            if (configIngotMultiplier.Value <= 1)
                return;
            __result = Mathf.RoundToInt(__result * configIngotMultiplier.Value);
        }
    }

    // configCreditsMultiplier

    [HarmonyPatch(typeof(MissionData), nameof(MissionData.GetCreditsReward))]
    class CreditsRewardPatch1
    {
        static void Postfix(ref int __result)
        {
            if (configCreditsMultiplier.Value <= 1)
                return;
            __result = Mathf.RoundToInt(__result * configCreditsMultiplier.Value);
        }
    }

    // configPlayerXPMultiplier

    [HarmonyPatch(typeof(MissionData), nameof(MissionData.GetExperienceReward))]
    class XPRewardPatch1
    {
        static void Postfix(ref int __result)
        {
            if (configPlayerXPMultiplier.Value <= 1)
                return;
            __result = Mathf.RoundToInt(__result * configPlayerXPMultiplier.Value);
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

