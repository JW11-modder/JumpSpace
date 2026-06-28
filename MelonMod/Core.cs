using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppKeepsake;
using Il2CppKeepsake.GameplayFeatures.Assembler;
using Il2CppKeepsake.GameplayFeatures.JumpMap;
using Il2CppKeepsake.GeneratedItems;
using Il2CppKeepsake.GeneratedItems.Cosmetics;
using Il2CppKeepsake.HyperSpace.GameplayFeatures.AI.BuddyBot;
using Il2CppKeepsake.HyperSpace.GameplayFeatures.Campaigns;
using Il2CppKeepsake.HyperSpace.GameplayFeatures.Campaigns.Destination;
using Il2CppKeepsake.HyperSpace.GameplayFeatures.GUI.UpgradeScreen;
using Il2CppKeepsake.HyperSpace.GameplayFeatures.MetaProgression;
using Il2CppKeepsake.HyperSpace.GameplayFeatures.Pickupables.RocketLauncher;
using Il2CppKeepsake.HyperSpace.NewInputSystem;
using Il2CppKeepsake.HyperSpace.System.Modifiers.ItemModule;
using Il2CppKeepsake.MetaProgression;
using Il2CppSystem;
using Il2CppUniRx;
using MelonLoader;
using MelonLoader.Preferences;
using MonoMod.RuntimeDetour;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using static MelonLoader.MelonLogger;



[assembly: MelonInfo(typeof(JSMelonMod.Core), "jw11-modder.JSMelonLoaderMod", "1.1.6", "jw11-modder", null)]
[assembly: MelonGame("Keepsake Games", "Jump Space")]

namespace JSMelonMod
{
    public class Core : MelonMod
    {
        public static MelonMod Instance { get; private set; }
        private static MelonPreferences_Category MultiplierFloatCategory;
        private static MelonPreferences_Category MultiplierIntCategory;
        private static MelonPreferences_Category ToggleCategory;

        private static MelonPreferences_Entry<bool> configNoPlayerDamage;
        private static MelonPreferences_Entry<bool> configNoPlayerShipHealthDamage;
        private static MelonPreferences_Entry<bool> configNoPlayerShipShieldDamage;
        private static MelonPreferences_Entry<bool> configNoCraftCost;
        private static MelonPreferences_Entry<bool> configNoUpgradeCost;
        private static MelonPreferences_Entry<bool> configMaxRarity;
        private static MelonPreferences_Entry<bool> configNoShipAmmoCost;
        private static MelonPreferences_Entry<bool> configNoPlayerReload;
        private static MelonPreferences_Entry<bool> configInstantBoost;
        private static MelonPreferences_Entry<bool> configInfiniteJump;
        private static MelonPreferences_Entry<bool> configFreeRoam;
        private static MelonPreferences_Entry<bool> configBuddyUpgrade;

        private static MelonPreferences_Entry<float> configPlayerDamageMultiplier;
        private static MelonPreferences_Entry<float> configPlayerShipDamageMultiplier;
        private static MelonPreferences_Entry<float> configPlayerSpeedMultiplier;
        private static MelonPreferences_Entry<float> configBoostTimeMult;
        private static MelonPreferences_Entry<float> configMateriaMultiplier;
        private static MelonPreferences_Entry<float> configCreditsMultiplier;
        private static MelonPreferences_Entry<float> configIngotMultiplier;
        private static MelonPreferences_Entry<float> configPlayerXPMultiplier;

        public static MelonPreferences_Entry<KeyCode> configMenuToggle;

        private static MelonPreferences_Category ModConfCategory;
        private static List<MelonPreferences_Category> CustomCategoryList = new List<MelonPreferences_Category>();

        public static bool showCheatsPopup = false;

        private static GUIStyle JModStyleT = new GUIStyle();
        private static GUIStyle JModStyleH = new GUIStyle();
        private static GUIStyle JModStyleP = new GUIStyle();
        private static GUIStyle JModStylePV = new GUIStyle();
        private static GUIStyle JModStyleB = new GUIStyle();
        private static GUIStyle JModStyleBlank = new GUIStyle();

        private static Color JModColor = new(0.0f, 0.85f, 0.85f);

        private static Rect jModWindowRect;
        private static Rect _screenRect;

        private static CursorLockMode lastLockMode;
        private static bool lastVisibleState;

        public static GameObject CanvasRoot { get; private set; }

        private static EventSystem jModEventSys;
        private static EventSystem lastEventSys;
        private static BaseInputModule lastInputModule;

        public static Key KeycodeToKey(KeyCode keyCode)
        {
            foreach (Key key in System.Enum.GetValues(typeof(Key)))
            {
                if (key.ToString().ToLower() == keyCode.ToString().ToLower())
                {
                    return key;
                }
            }

            LogWarning("can't find Key matching KeyCode " + keyCode.ToString());
            return Key.None;
        }

        public void LogHandler(string log, LogType level)
        {
            switch (level)
            {
                case LogType.Log:
                    base.LoggerInstance.Msg(log);
                    return;
                case LogType.Warning:
                case LogType.Assert:
                    base.LoggerInstance.Warning(log);
                    return;
                case LogType.Exception:
                case LogType.Error:
                    base.LoggerInstance.Error(log);
                    return;
            }
        }
        public static void Log(object message)
            => Log(message, LogType.Log);

        public static void LogWarning(object message)
            => Log(message, LogType.Warning);

        public static void LogError(object message)
            => Log(message, LogType.Error);

        internal static void Log(object message, LogType logType)
        {
            string log = message?.ToString() ?? "";

            switch (logType)
            {
                case LogType.Log:
                case LogType.Assert:
                    Instance.LoggerInstance.Msg(log); break;

                case LogType.Warning:
                    Instance.LoggerInstance.Warning(log); break;

                case LogType.Error:
                case LogType.Exception:
                    Instance.LoggerInstance.Error(log); break;
            }
        }

        public override void OnInitializeMelon()
        {
            Instance = this;
            MultiplierFloatCategory = MelonPreferences.CreateCategory("FloatMultipliers");
            MultiplierIntCategory = MelonPreferences.CreateCategory("IntMultipliers");
            ToggleCategory = MelonPreferences.CreateCategory("Toggles");

            configNoPlayerDamage = ToggleCategory.CreateEntry<bool>("configNoPlayerDamage", false, "Disable damage to player");
            configNoPlayerShipHealthDamage = ToggleCategory.CreateEntry<bool>("configNoPlayerShipHealthDamage", false, "Disable damage to player ship's health");
            configNoPlayerShipShieldDamage = ToggleCategory.CreateEntry<bool>("configNoPlayerShipShieldDamage", false, "Disable damage to player ship's shields");
            configNoShipAmmoCost = ToggleCategory.CreateEntry<bool>("configNoShipAmmoCost", false, "No reload and ammo cost (spaceship)");
            configNoPlayerReload = ToggleCategory.CreateEntry<bool>("configNoPlayerAmmoCost", false, "No reload and ammo cost (on foot)");
            configNoCraftCost = ToggleCategory.CreateEntry<bool>("configNoCraftCost", false, "No assembler craft cost");
            configNoUpgradeCost = ToggleCategory.CreateEntry<bool>("configNoUpgradeCost", false, "No blueprint upgrade cost");
            //configMaxRarity = ToggleCategory.CreateEntry<bool>("configMaxRarity", false, "Get items of maximum rarity");
            configInstantBoost = ToggleCategory.CreateEntry<bool>("configInstantBoost", false, "Enable instant ship boost");
            configInfiniteJump = ToggleCategory.CreateEntry<bool>("configInfiniteJump", false, "Enable infinite double jump");
            configFreeRoam = ToggleCategory.CreateEntry<bool>("configFreeRoam", false, "Enable ship jump to any sector");
            configBuddyUpgrade = ToggleCategory.CreateEntry<bool>("configBuddyUpgrade", false, "Apply on foot damage multiplier to Buddy bot");

            configPlayerDamageMultiplier = MultiplierFloatCategory.CreateEntry<float>("configPlayerDamageMultiplier", 1f, "Player damage multiplier (on foot)", validator: new ValueRange<float>(1f, 20f));
            configPlayerShipDamageMultiplier = MultiplierFloatCategory.CreateEntry<float>("configPlayerShipDamageMultiplier", 1f, "Player damage multiplier (spaceship)", validator: new ValueRange<float>(1f, 20f));
            configPlayerSpeedMultiplier = MultiplierFloatCategory.CreateEntry<float>("configPlayerSpeedMultiplier", 1f, "Player speed multiplier (on foot)", validator: new ValueRange<float>(1f, 5f));
            configBoostTimeMult = MultiplierFloatCategory.CreateEntry<float>("configBoostTimeMult", 1f, "Ship boost time multiplier", validator: new ValueRange<float>(1f, 20f));
            configMateriaMultiplier = MultiplierFloatCategory.CreateEntry<float>("configMateriaMultiplier", 1f, "Materia gains multiplier", validator: new ValueRange<float>(1f, 20f));
            configCreditsMultiplier = MultiplierFloatCategory.CreateEntry<float>("configCreditsMultiplier", 1f, "Pickup credits multiplier", validator: new ValueRange<float>(1f, 20f));
            configIngotMultiplier = MultiplierFloatCategory.CreateEntry<float>("configIngotMultiplier", 1f, "Mission reward multiplier (credits and ingots)", validator: new ValueRange<float>(1f, 20f));
            configPlayerXPMultiplier = MultiplierFloatCategory.CreateEntry<float>("configPlayerXPMultiplier", 1f, "Mission player XP reward multiplier", validator: new ValueRange<float>(1f, 20f));

            JModStyleH.alignment = TextAnchor.MiddleCenter;
            JModStyleH.fontSize = 20;
            JModStyleH.fontStyle = FontStyle.Bold;
            JModStyleH.normal.textColor = JModColor;

            JModStyleP.fontSize = 16;
            JModStyleP.normal.textColor = JModColor;

            JModStylePV.fontSize = 16;
            JModStylePV.fontStyle = FontStyle.Bold;
            JModStylePV.normal.textColor = JModColor;
            JModStylePV.alignment = TextAnchor.MiddleCenter;

            ModConfCategory = MelonPreferences.CreateCategory("JModConfiguration");
            configMenuToggle = ModConfCategory.CreateEntry("ToggleKey", KeyCode.F7, "Main Menu Toggle Key");
            CustomCategoryList.Clear();
            foreach (var category in MelonPreferences.Categories)
            {
                switch (category.Identifier)
                {
                    case "FloatMultipliers":
                        {
                            MultiplierFloatCategory = category;
                            Log("Float Multipliers loaded!");
                            break;
                        }
                    case "IntMultipliers":
                        {
                            MultiplierIntCategory = category;
                            Log("Int Multipliers loaded!");
                            break;
                        }
                    case "Toggles":
                        {
                            ToggleCategory = category;
                            Log("Toggles loaded!");
                            break;
                        }
                    case "JModConfiguration":
                        {
                            break;
                        }
                    default:
                        {
                            CustomCategoryList.Add(category);
                            Log("Custom category: " + category.DisplayName + " loaded!");
                            break;
                        }

                }
            }

            Log("Menu key: " + configMenuToggle.Value.ToString());

            CanvasRoot = new GameObject("JModCanvas");
            UnityEngine.Object.DontDestroyOnLoad(CanvasRoot);
            CanvasRoot.hideFlags |= HideFlags.HideAndDontSave;
            CanvasRoot.layer = 5;
            CanvasRoot.transform.position = new Vector3(0f, 0f, 1f);

            CanvasRoot.SetActive(false);

            jModEventSys = CanvasRoot.AddComponent<EventSystem>();
            jModEventSys.enabled = false;

            CanvasRoot.SetActive(true);

            Log("JS Mod Initialized.");
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
            public static void ILManipulator(MethodBase original)
            {
                /*if (!configNoPlayerShipShieldDamage.Value)
                    return true;
                return false;*/
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
                if (currency.m_CurrencyGroup != Currency.CurrencyGroup.Credits &&  delta < 0)
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
                Log("Item generated with all modules: " + config.name);
                return false;
            }
        }
        /*[HarmonyPatch(typeof(ItemGenerator), nameof(ItemGenerator.GenerateForTemplate))]
        class ItemGeneratorPatch2
        {
            static bool Prefix()
            {
                return true;
            }
        }*/
        /*[HarmonyPatch(typeof(ItemGenerator), nameof(ItemGenerator.GenerationConfig), MethodType.Getter)]
        class ItemGeneratorPatch3
        {

            static void Postfix(ref ItemGenerationConfig __result)
            {
                if (!configMaxRarity.Value)
                    return;
                
                Log("Item generated with max rarity (getter): " + __result?.name);
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
                if (Il2CppKeepsake.HyperSpace.NewInputSystem.InputManager.IsActionPressed(Il2CppKeepsake.HyperSpace.NewInputSystem.InputManager.InputKeys.Jump))
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
                Log("BuddyBot damage " + __instance.m_ProjectileData.m_OnFootDamage);
                buddyDam = true;
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
                if (configPlayerShipDamageMultiplier.Value <= 1 || __instance.TracerProjectileData == null)
                {
                    Log("SHIP CANNON DAMAGE NORMAL");
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
                    Log("Added SHIP CANNON Id " + __instance.CombatStationID);
                    __instance.TracerProjectileData.m_ShipHullDamage = currentCannon.hullDamage;
                    __instance.TracerProjectileData.m_ShipShieldDamage = currentCannon.shieldDamage;
                }
                else
                    if (shipCannonsDict[__instance.CombatStationID].hullDamage != __instance.TracerProjectileData.m_ShipHullDamage || shipCannonsDict[__instance.CombatStationID].shieldDamage != __instance.TracerProjectileData.m_ShipShieldDamage)
                    {
                        shipCannonsDict[__instance.CombatStationID].hullDamage = __instance.TracerProjectileData.m_ShipHullDamage * configPlayerShipDamageMultiplier.Value;
                        shipCannonsDict[__instance.CombatStationID].shieldDamage = __instance.TracerProjectileData.m_ShipShieldDamage * configPlayerShipDamageMultiplier.Value;
                        Log("SHIP CANNON Id " + __instance.CombatStationID + " Damage changed!");
                        __instance.TracerProjectileData.m_ShipHullDamage = currentCannon.hullDamage;
                        __instance.TracerProjectileData.m_ShipShieldDamage = currentCannon.shieldDamage;
                    }

                Log("SHIP CANNON Id " + __instance.CombatStationID + " HULL DAMAGE: " + __instance.TracerProjectileData.ShipHullDamage + " SHIELD DAMAGE: " + __instance.TracerProjectileData.ShipshieldDamage);
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
            static bool Prefix(ref MissionData __instance)
            {
                if (configIngotMultiplier.Value <= 1f && configPlayerXPMultiplier.Value <= 1f)
                    return true;
                __instance.m_CurrencyRewardMultiplier = configIngotMultiplier.Value;
                __instance.m_ExperienceRewardMultiplier = configPlayerXPMultiplier.Value;
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
                    //Log("GET INGOTS VALUE MULTIPLIER (mission): " + configIngotMultiplier.Value + " - Ingots: " + __result);
                }
                if (currency.m_CurrencyGroup == Currency.CurrencyGroup.Credits)
                {
                    __result = Mathf.RoundToInt(__result * configCreditsMultiplier.Value);
                    //Log("GET CREDITS VALUE MULTIPLIER (mission): " + configCreditsMultiplier.Value + " - Credits: " + __result);
                }
                //Log("GetCurrency");
            }
        }

        // configCreditsMultiplier

        [HarmonyPatch(typeof(Il2CppKeepsake.Gold.Instapickup_Credits), nameof(Il2CppKeepsake.Gold.Instapickup_Credits.OnPlayerPickup))]
        class CreditsPickupPatch1
        {
            static bool Prefix(ref Il2CppKeepsake.Gold.Instapickup_Credits __instance)
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

        // MAIN MOD FUNCTIONS

        public override void OnUpdate()
        {
            if (Event.current != null)
                if ((Event.current.keyCode == (configMenuToggle.Value)) && (Event.current.type == EventType.KeyDown))
                {
                    SwitchMenu();
                    Log("Menu switched!");
                }
        }

        public override void OnGUI()
        {
            ShowMenu();
        }


        public static void SwitchMenu()
        {
            if (!showCheatsPopup)
            {
                lastLockMode = UnityEngine.Cursor.lockState;
                lastVisibleState = UnityEngine.Cursor.visible;
                UnityEngine.Cursor.lockState = CursorLockMode.None;
                UnityEngine.Cursor.visible = true;
                lastEventSys = EventSystem.current;
                lastInputModule = EventSystem.current.currentInputModule;
                lastEventSys.enabled = false;
                lastInputModule.DeactivateModule();
                jModEventSys.enabled = true;
                jModEventSys.m_CurrentInputModule?.ActivateModule();
            }
            else
            {
                UnityEngine.Cursor.lockState = lastLockMode;
                UnityEngine.Cursor.visible = lastVisibleState;
                InputSystem.TryResetDevice(Mouse.current);
                jModEventSys.enabled = false;
                jModEventSys.currentInputModule?.DeactivateModule();
                lastEventSys.enabled = true;
                lastInputModule.ActivateModule();
                lastEventSys.m_CurrentInputModule = lastInputModule;
                MelonPreferences.Save();
            }
            showCheatsPopup = !showCheatsPopup;
        }

        public static void ShowMenu()
        {
            if (showCheatsPopup)
            {
                JModStyleT = GUI.skin.GetStyle("toggle");
                JModStyleT.fontSize = 16;
                JModStyleT.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
                JModStyleT.onNormal.textColor = JModColor;

                JModStyleB = GUI.skin.GetStyle("box");
                JModStyleB.alignment = TextAnchor.UpperCenter;
                JModStyleB.fontSize = 24;
                JModStyleB.fontStyle = FontStyle.Bold;
                JModStyleB.normal.textColor = JModColor;

                jModWindowRect = new Rect(Screen.width / 2 - 425, Screen.height / 2 - 425, 850, 850);
                _screenRect = new Rect(0, 0, Screen.width, Screen.height);

                GUI.BeginGroup(jModWindowRect);
                for (int i = 0; i < 5; i++)
                    GUI.Box(new Rect(0, 0, 850, 850), "", JModStyleB);

                GUI.Box(new Rect(0, 0, 850, 850), "MOD OPTIONS", JModStyleB);

                var yAxis = 40;
                var xAxis = 20;
                GUI.Label(new Rect(xAxis, yAxis, 810, 20), "Toggle Mod Options", JModStyleH);
                yAxis += 35;
                ShowBoolMenu(ref xAxis, ref yAxis, ref ToggleCategory);
                yAxis += 10;
                GUI.Label(new Rect(xAxis, yAxis, 810, 20), "Multiplier Mod Options", JModStyleH);
                yAxis += 45;
                ShowFloatMenu(ref xAxis, ref yAxis, ref MultiplierFloatCategory);
                ShowIntMenu(ref xAxis, ref yAxis, ref MultiplierIntCategory);
                yAxis += 15;

                if (GUI.Button(new Rect(325, 810, 200, 35), "Save settings and close"))
                {
                    SwitchMenu();
                }


                Vector2 mousePosition = Mouse.current.position.value;
                mousePosition.y = Screen.height - mousePosition.y;

                if (GUI.Button(_screenRect, string.Empty, JModStyleBlank) && !jModWindowRect.Contains(mousePosition))
                {

                }

                if (jModWindowRect.Contains(mousePosition) && !((Event.current.keyCode == (configMenuToggle.Value)) && (Event.current.type == EventType.KeyDown)))
                {
                    Event.current.Use();
                }
                GUI.EndGroup();
            }
        }

        public static void ShowBoolMenu(ref int xAxis, ref int yAxis, ref MelonPreferences_Category cat)
        {
            foreach (MelonPreferences_Entry<bool> toggle in cat.Entries)
            {
                toggle.Value = GUI.Toggle(new Rect(xAxis, yAxis, 400, 20), toggle.Value, toggle.DisplayName, JModStyleT);
                xAxis += 405;
                if (xAxis > 800)
                {
                    xAxis = 20;
                    yAxis += 35;
                }
            }
            if (xAxis != 20)
            {
                xAxis = 20;
                yAxis += 25;
            }
        }
        public static void ShowFloatMenu(ref int xAxis, ref int yAxis, ref MelonPreferences_Category cat)
        {
            foreach (MelonPreferences_Entry<float> mult in cat.Entries)
            {
                string multLabel = mult.DisplayName;
                ValueRange<float> range;
                if (mult.Validator != null)
                    range = (ValueRange<float>)mult.Validator;
                else
                    range = new ValueRange<float>(1f, 20f);
                float step;
                if (range.MaxValue < 10)
                    step = 0.1f;
                else
                    step = 0.5f;
                multLabel += " (" + range.MinValue.ToString() + " - " + range.MaxValue.ToString() + ")";
                GUI.Label(new Rect(xAxis, yAxis, 680, 20), multLabel, JModStyleP);

                if (GUI.Button(new Rect(xAxis + 680, yAxis, 40, 20), " - "))
                {
                    if (mult.Value > range.MinValue)
                        mult.Value -= step;
                }
                GUI.Label(new Rect(xAxis + 730, yAxis, 40, 20), mult.Value.ToString("0.0"), JModStylePV);
                if (GUI.Button(new Rect(xAxis + 780, yAxis, 40, 20), " + "))
                {
                    if (mult.Value < range.MaxValue)
                        mult.Value += step;
                }

                yAxis += 35;
            }
        }
        public static void ShowIntMenu(ref int xAxis, ref int yAxis, ref MelonPreferences_Category cat)
        {
            foreach (MelonPreferences_Entry<int> mult in cat.Entries)
            {
                string multLabel = mult.DisplayName;
                ValueRange<int> range;
                if (mult.Validator != null)
                    range = (ValueRange<int>)mult.Validator;
                else
                    range = new ValueRange<int>(1, 20);
                multLabel += " (" + range.MinValue+ " - " + range.MaxValue + ")";
                GUI.Label(new Rect(xAxis, yAxis, 680, 20), multLabel, JModStyleP);
                if (GUI.Button(new Rect(xAxis + 680, yAxis, 40, 20), " - "))
                {
                    if (mult.Value > range.MinValue)
                        mult.Value -= 1;
                }
                GUI.Label(new Rect(xAxis + 730, yAxis, 40, 20), mult.Value.ToString(), JModStylePV);
                if (GUI.Button(new Rect(xAxis + 780, yAxis, 40, 20), " + "))
                {
                    if (mult.Value < range.MaxValue)
                        mult.Value += 1;
                }
                yAxis += 35;
            }
        }
    }
}