using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Keepsake;
using Keepsake.Common.State;
using Keepsake.Framework.Networking.Logic;
using Keepsake.GameplayFeatures.Assembler;
using Keepsake.GameplayFeatures.JumpMap;
using Keepsake.GeneratedItems;
using Keepsake.Gold;
using Keepsake.HyperSpace.GameplayFeatures.AI.BuddyBot;
using Keepsake.HyperSpace.GameplayFeatures.Campaigns;
using Keepsake.HyperSpace.GameplayFeatures.Pickupables.RocketLauncher;
using Keepsake.MetaProgression;
using Keepsake.Modal;
using Keepsake.Pickupables.GenericWeapon;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using UniRx;
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
    public static ConfigEntry<bool> configNoUpgradeCost;
    public static ConfigEntry<bool> configMaxRarity;
    public static ConfigEntry<bool> configNoShipAmmoCost;
    public static ConfigEntry<bool> configNoPlayerReload;
    public static ConfigEntry<bool> configInstantBoost;
    public static ConfigEntry<bool> configInfiniteJump;
    public static ConfigEntry<bool> configFreeRoam;
    public static ConfigEntry<bool> configBuddyUpgrade;

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
                                    "DisablePlayerShipHealthDamage",
                                    false,
                                    "Disable damage to player ship's health");
        configNoPlayerShipShieldDamage = Config.Bind("Toggles",
                                    "DisablePlayerShipShieldsDamage",
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
        configNoUpgradeCost = Config.Bind("Toggles",
                                    "EnableFreeUpgrades",
                                    false,
                                    "No blueprint upgrade cost");
        /*configMaxRarity = Config.Bind("Toggles",
                                    "EnableMaxRarity",
                                    false,
                                    "Get items of maximum rarity");*/
        configInstantBoost = Config.Bind("Toggles",
                                    "EnableInstantBoost",
                                    false,
                                    "Enable instant ship boost");
        configInfiniteJump = Config.Bind("Toggles",
                                    "EnableInfiniteJump",
                                    false,
                                    "Enable infinite double jump");
        configFreeRoam = Config.Bind("Toggles",
                                    "EnableFreeRoam",
                                    false,
                                    "Enable ship jump to any sector");
        configBuddyUpgrade = Config.Bind("Toggles",
                                    "EnableBuddyUpgrade",
                                    false,
                                    "Apply on foot damage multiplier to Buddy bot");


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
                                    new ConfigDescription("Player speed multiplier (on foot)", new AcceptableValueRange<float>(0, 5f)));
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
                                    "Pickup credits multiplier");
        configIngotMultiplier = Config.Bind("MultFloat",
                                    "IngotMultiplier",
                                    1f,
                                    "Mission currency reward (credits and ingots) multiplier");
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
    class HealthComponentPatch1
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

    [HarmonyPatch(typeof(Playership_DamageController), nameof(Playership_DamageController.ApplyDamageToShipCore), new System.Type[] { typeof(float) })]
    class Playership_DamageControllerPatch1
    {
        static bool Prefix(ref float damage, ref Playership_DamageController __instance)
        {
            if (!configNoPlayerShipHealthDamage.Value)
                return true;
            damage = 0;
            return true;
        }
    }

    [HarmonyPatch(typeof(Playership_DamageController), nameof(Playership_DamageController.DelayedDamage))]
    class Playership_DelayedDamagePatch1
    {
        static bool Prefix(ref int damage, ref Playership_DamageController __instance)
        {
            if (!configNoPlayerShipHealthDamage.Value)
            {
                return true;
            }
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
            {
                return;
            }
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

    //configNoPlayerShipShieldDamage

    [HarmonyPatch(typeof(ShieldUnitsManager), nameof(ShieldUnitsManager.DealDamageToShields))]
    class ShieldUnitsManagerPatch1
    {
        static bool Prefix()
        {
            if (!configNoPlayerShipShieldDamage.Value)
                return true;
            return false;
        }
    }

    // configNoCraftCost

    [HarmonyPatch(typeof(Assembler), nameof(Assembler.RefreshInventory))]
    class Assembler_RefreshInventoryPatch1
    {
        static void Postfix(ref Assembler __instance)
        {
            if (!configNoCraftCost.Value)
                return;
            ReactiveCollection<Craftable> craftables = __instance?.m_BlackboardData?.m_AvailableItems;
            if (craftables != null)
            {
                for (int i = 0; i < craftables.Count; i++)
                {
                    craftables[i].m_MateriaCraftCost = 0;
                }
                __instance?.m_BlackboardData?.m_AvailableItems = craftables;
            }
            Il2CppSystem.Collections.Generic.List<Craftable> extraCraftables = __instance?.m_ExtraLocalCraftables;
            if (extraCraftables != null)
            {
                for (int i = 0; i < extraCraftables.Count; i++)
                {
                    extraCraftables[i].m_MateriaCraftCost = 0;
                }
                __instance?.m_ExtraLocalCraftables = extraCraftables;
            }
        }
    }
    [HarmonyPatch(typeof(UnlockableCraftableEntry), nameof(UnlockableCraftableEntry.Awake))]
    class UnlockableCraftableEntryPatch1
    {
        static void Postfix(ref UnlockableCraftableEntry __instance)
        {
            if (!configNoCraftCost.Value)
                return;
            __instance?.m_CraftCost = 0;
        }
    }

    [HarmonyPatch(typeof(ShipMateriaController), nameof(ShipMateriaController.ModifyMateria))]
    class ModifyMateriaPatch1
    {
        static bool Prefix(ref int amount)
        {
            if (!configNoCraftCost.Value)
                return true;
            if (amount < 0)
                amount = 0;
            return true;
        }
    }
    [HarmonyPatch(typeof(ShipMateriaController), nameof(ShipMateriaController.ModifyMateriaLevel))]
    class ModifyMateriaLevelPatch1
    {
        static bool Prefix(ref int modifyAmount)
        {
            if (!configNoCraftCost.Value)
                return true;
            if (modifyAmount < 0)
                modifyAmount = 0;
            return true;
        }
    }

    // configNoUpgradeCost

    [HarmonyPatch(typeof(MetaProgressionManager), nameof(MetaProgressionManager.ModifyCurrency))]
    class ModifyCurrencyPatch1
    {
        static bool Prefix(ref Currency currency, ref int delta)
        {
            if (!configNoUpgradeCost.Value || currency == null)
                return true;
            if (currency.m_CurrencyGroup != Currency.CurrencyGroup.Credits && delta < 0)
                delta = 0;
            return true;
        }
    }
    [HarmonyPatch(typeof(MetaProgressionManager), nameof(MetaProgressionManager.Update))]
    class BlueprintSlotPatch1
    {
        static void Postfix(ref MetaProgressionManager __instance)
        {
            if (!configNoUpgradeCost.Value)
                return;
            if (__instance.m_BlueprintSlotCapacities != null)
                for (int i = 0; i < __instance.m_BlueprintSlotCapacities.Count; i++)
                    __instance.m_BlueprintSlotCapacities[i].m_MaxSlots = 8;
        }
    }


    // configMaxRarity

    [HarmonyPatch(typeof(ItemGenerator), nameof(ItemGenerator.ComputeChanceOfDropWithAllModulesAtRarity))]
    class ItemGeneratorPatch1
    {

        static bool Prefix(ItemGenerationConfig config, ref float __result)
        {
            if (!configMaxRarity.Value)
                return true;
            __result = 1f;
            Log.LogInfo("Item generated with all modules: " + config.name);
            return false;
        }
    }
    /*[HarmonyPatch(typeof(ItemGenerator), nameof(ItemGenerator.GenerateForTemplate))]
    class ItemGeneratorPatch2
    {

        static bool Prefix(ItemGenerationConfig config, ref float __result)
        {
            if (!configMaxRarity.Value)
                return true;
            __result = 1f;
            Log.LogInfo("Item generated with all modules: " + config.name);
            return false;
        }
    }*/
    /*[HarmonyPatch(typeof(ItemGenerator), nameof(ItemGenerator.Generate), new System.Type[] { typeof(PickupableItem_Data), typeof(int), typeof(Il2CppSystem.Random) })]
    class ItemGeneratorPatch2
    {

        static void Postfix(ref Il2CppSystem.Nullable<GeneratedItem> __result)
        {
            if (!configMaxRarity.Value)
                return;
            if (__result.Value.m_Rarity == ItemRarity.Common || __result.Value.m_Rarity == ItemRarity.Rare)
                __result.Value.m_Rarity = ItemRarity.Epic;
            Log("Item generated with max rarity (pickupable): " + ItemGenerator.GenerationConfig?.name);
        }
    }*/
    /*[HarmonyPatch(typeof(ItemGenerator), nameof(ItemGenerator.Generate), new System.Type[] { typeof(string), typeof(int), typeof(ItemGenerationConfig), typeof(Il2CppSystem.Collections.Generic.IList<ItemModuleScriptable>), typeof(Il2CppSystem.Collections.Generic.IList<CosmeticData>), typeof(Il2CppSystem.Random), typeof(Il2CppSystem.Nullable<int>), typeof(Il2CppSystem.Nullable<ItemRarity>), typeof(ItemModuleSchool), typeof(Il2CppSystem.Nullable<BaseModuleSet>) })]
    class ItemGeneratorPatch3
    {
        static bool Prefix(ItemGenerationConfig config, ref Il2CppSystem.Nullable<ItemRarity> forcedRarity)
        {
            if (!configMaxRarity.Value)
                return true;
            if (forcedRarity == null)
            {
                Il2CppSystem.Nullable<ItemRarity> rarity = new();
                rarity.value = ItemRarity.Legendary;
                forcedRarity = rarity;
            }

            Log("Item generated with max rarity: " + config.name);
            return false;
        }
    }*/

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
    [HarmonyPatch(typeof(PickupableItem_Minigun), nameof(PickupableItem_Minigun.ConsumeAmmo))]
    class MinigunPatch1
    {
        static void Postfix(ref PickupableItem_Minigun __instance)
        {
            if (!configNoPlayerReload.Value)
                return;
            __instance.m_PickupableItemBlackboardData.m_ResourceAmount.Value = __instance.m_ItemData.m_MaxResourceAmountToCarry;
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

    // configFreeRoam

    [HarmonyPatch(typeof(JumpMapLine), nameof(JumpMapLine.OnHoveredPathChanged))]
    class JumpMapLinePatch3
    {
        static bool Prefix(ref JumpMapLine __instance)
        {
            if (!configFreeRoam.Value)
                return true;
            __instance.m_BlackboardData.m_IsReachable.SetValue(true, false, false);
            __instance.m_BlackboardData.m_IsLinkedToCurrent.SetValue(true, false, false);
            return true;
        }
    }
    [HarmonyPatch(typeof(JumpMapLine), nameof(JumpMapLine.LineIsPartOfReachablePath))]
    class JumpMapLinePatch4
    {
        static bool Prefix(ref JumpMapLine __instance, ref bool __result)
        {
            if (!configFreeRoam.Value)
                return true;
            __instance.m_BlackboardData.m_IsReachable.SetValue(true, false, false);
            __instance.m_BlackboardData.m_IsLinkedToCurrent.SetValue(true, false, false);
            __result = true;
            return false;
        }
    }
    [HarmonyPatch(typeof(JumpMapNode), nameof(JumpMapNode.UpdateCurrentPlayerDestination))]
    class JumpMapNodePatch1
    {
        static bool Prefix(ref JumpMapNode __instance)
        {
            if (!configFreeRoam.Value)
                return true;
            __instance.m_BlackboardData.m_IsReachable.SetValue(true, false, false);
            __instance.m_BlackboardData.m_IsLinkedToCurrent.SetValue(true, false, false);
            __instance.m_BlackboardData.m_ReachableDistance.SetValue(1, false, false);
            __instance.m_BlackboardData.m_DistanceFromCurrent.SetValue(1, false, false);
            return true;
        }
    }
    [HarmonyPatch(typeof(JumpMapNode), nameof(JumpMapNode.TargetSelected))]
    class JumpMapNodePatch3
    {
        static bool Prefix(ref JumpMapNode __instance)
        {
            if (!configFreeRoam.Value)
                return true;
            __instance.m_BlackboardData.m_IsReachable.SetValue(true, false, false);
            __instance.m_BlackboardData.m_IsLinkedToCurrent.SetValue(true, false, false);
            __instance.m_BlackboardData.m_ReachableDistance.SetValue(1, false, false);
            __instance.m_BlackboardData.m_DistanceFromCurrent.SetValue(1, false, false);
            return true;
        }
    }

    // configBuddyUpgrade

    static bool buddyDam = false;

    [HarmonyPatch(typeof(AI_Behaviour_OnFootBuddy_Hostile), nameof(AI_Behaviour_OnFootBuddy_Hostile.SafeStart))]
    class BuddyDamagePatch1
    {
        static bool Prefix(ref AI_Behaviour_OnFootBuddy_Hostile __instance)
        {
            if (configPlayerDamageMultiplier.Value <= 1f || buddyDam || !configBuddyUpgrade.Value)
                return true;
            __instance.m_ProjectileData.m_OnFootDamage *= configPlayerDamageMultiplier.Value;
            Log.LogInfo("BuddyBot damage " + __instance.m_ProjectileData.m_OnFootDamage);
            buddyDam = true;
            return true;
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
            if (configPlayerShipDamageMultiplier.Value <= 1 || __instance.TracerProjectileData == null)
            {
                Log.LogInfo("SHIP CANNON DAMAGE NORMAL");
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
                Log.LogInfo("Added SHIP CANNON Id " + __instance.CombatStationID);
                __instance.TracerProjectileData.m_ShipHullDamage = currentCannon.hullDamage;
                __instance.TracerProjectileData.m_ShipShieldDamage = currentCannon.shieldDamage;
            }
            else
                if (shipCannonsDict[__instance.CombatStationID].hullDamage != __instance.TracerProjectileData.m_ShipHullDamage || shipCannonsDict[__instance.CombatStationID].shieldDamage != __instance.TracerProjectileData.m_ShipShieldDamage)
                {
                    shipCannonsDict[__instance.CombatStationID].hullDamage = __instance.TracerProjectileData.m_ShipHullDamage * configPlayerShipDamageMultiplier.Value;
                    shipCannonsDict[__instance.CombatStationID].shieldDamage = __instance.TracerProjectileData.m_ShipShieldDamage * configPlayerShipDamageMultiplier.Value;
                    Log.LogInfo("SHIP CANNON Id " + __instance.CombatStationID + " Damage changed!");
                    __instance.TracerProjectileData.m_ShipHullDamage = currentCannon.hullDamage;
                    __instance.TracerProjectileData.m_ShipShieldDamage = currentCannon.shieldDamage;
                }

            Log.LogInfo("SHIP CANNON Id " + __instance.CombatStationID + " HULL DAMAGE: " + __instance.TracerProjectileData.ShipHullDamage + " SHIELD DAMAGE: " + __instance.TracerProjectileData.ShipshieldDamage);
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
            materiaValue = (int)(materiaValue * configMateriaMultiplier.Value);
            return true;
        }
    }
    [HarmonyPatch(typeof(Instapickup_MateriaPack), nameof(Instapickup_MateriaPack.OnPlayerPickup))]
    class MateriaPickupPatch1
    {
        static bool Prefix(ref Instapickup_MateriaPack __instance)
        {
            if (configMateriaMultiplier.Value <= 1)
                return true;
            __instance.m_MateriaAmount = Mathf.RoundToInt(__instance.m_MateriaAmount * configMateriaMultiplier.Value);
            return true;
        }
    }


    // configBoostTimeMult

    [HarmonyPatch(typeof(SpaceShip_EngineController), nameof(SpaceShip_EngineController.BoostTime), MethodType.Getter)]
    class Player_ShipBoostTimePatch1
    {
        static void Postfix(ref float __result)
        {
            if (configBoostTimeMult.Value <= 1)
                return;
            __result *= configBoostTimeMult.Value;

        }
    }

    // configIngotMultiplier
    // configPlayerXPMultiplier

    [HarmonyPatch(typeof(MissionData), nameof(MissionData.GetRewards))]
    class GetRewardsPatch1
    {
        static bool Prefix(MissionData __instance)
        {
            if (configIngotMultiplier.Value <= 1f && configPlayerXPMultiplier.Value <= 1f)
                return true;
            __instance.m_CurrencyRewardMultiplier = configIngotMultiplier.Value;
            __instance.m_ExperienceRewardMultiplier = configPlayerXPMultiplier.Value;
            Log.LogInfo("CURRENCY VALUE MULTIPLIER (mission): " + __instance.m_CurrencyRewardMultiplier);
            Log.LogInfo("EXPERIENCE VALUE MULTIPLIER (mission): " + __instance.m_ExperienceRewardMultiplier);
            return true;
        }
    }

    [HarmonyPatch(typeof(MissionRewards), nameof(MissionRewards.GetCurrencyAmount))]
    class GetRewardsPatch2
    {
        static void Postfix(ref int __result, ref Currency currency)
        {
            if (configIngotMultiplier.Value <= 1f && configCreditsMultiplier.Value <= 1f)
                return;
            if (currency.m_CurrencyGroup == Currency.CurrencyGroup.Ingots)
            {
                __result = Mathf.RoundToInt(__result * configIngotMultiplier.Value);
                //Log.LogInfo("GET INGOTS VALUE MULTIPLIER (mission): " + configIngotMultiplier.Value + " - Ingots: " + __result);
            }
            if (currency.m_CurrencyGroup == Currency.CurrencyGroup.Credits)
            {
                __result = Mathf.RoundToInt(__result * configCreditsMultiplier.Value);
                //Log.LogInfo("GET CREDITS VALUE MULTIPLIER (mission): " + configCreditsMultiplier.Value + " - Credits: " + __result);
            }
            //Log.LogInfo("GetCurrency");
        }
    }

    // configCreditsMultiplier

    [HarmonyPatch(typeof(Instapickup_Credits), nameof(Instapickup_Credits.OnPlayerPickup))]
    class CreditsPickupPatch1
    {
        static bool Prefix(ref Instapickup_Credits __instance)
        {
            if (configCreditsMultiplier.Value <= 1)
                return true;
            __instance.m_GoldAmount = Mathf.RoundToInt(__instance.m_GoldAmount * configCreditsMultiplier.Value);
            return true;
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

