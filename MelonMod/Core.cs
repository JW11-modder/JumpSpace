using HarmonyLib;
using Il2Cpp;
using Il2CppKeepsake;
using Il2CppKeepsake.HyperSpace.GameplayFeatures.Campaigns;
using Il2CppKeepsake.HyperSpace.GameplayFeatures.Pickupables.RocketLauncher;
using Il2CppKeepsake.HyperSpace.NewInputSystem;
using MelonLoader;
using MelonLoader.Preferences;
using MonoMod.RuntimeDetour;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using static MelonLoader.MelonLogger;



[assembly: MelonInfo(typeof(JSMelonMod.Core), "jw11-modder.JSMelonLoaderMod", "1.0.4", "jw11-modder", null)]
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
        private static MelonPreferences_Entry<bool> configNoShipAmmoCost;
        private static MelonPreferences_Entry<bool> configNoPlayerReload;
        private static MelonPreferences_Entry<bool> configInstantBoost;
        private static MelonPreferences_Entry<bool> configInfiniteJump;

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

        // configNoPlayerDamage

        [HarmonyPatch(typeof(HealthComponent_Base), nameof(HealthComponent_Base.DealDamage))]
        class HealthComponentPatch1
        {
            static bool Prefix(ref float damageToDeal, HealthComponent_Base __instance)
            {
                if (!configNoPlayerDamage.Value || __instance.m_ParentPlayer == null)
                {
                    //Log("PLAYER DAMAGE ALLOWED 4 - Damage: " + damageToDeal.ToString("0.00"));
                    return true;
                }
                // Skip original
                damageToDeal = 0;
                //Log("!!NO PLAYER DAMAGE ALLOWED 4!!");
                return true;
            }
        }

        // configNoPlayerShipHealthDamage

        [HarmonyPatch(typeof(Playership_DamageController), nameof(Playership_DamageController.ApplyDamageToShipCore))]
        class Playership_DamageControllerPatch1
        {
            static bool Prefix(ref int damage, ref Playership_DamageController __instance)
            {
                if (!configNoPlayerShipHealthDamage.Value)
                {
                    //Log("SHIP CORE DAMAGE ALLOWED - Damage value: " + damage);
                    return true;
                }
                damage = 0;
                //Log("!!NO SHIP CORE DAMAGE ALLOWED!! - Damage value: " + damage);
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
                    //Log("SHIP CORE DELAYED DAMAGE ALLOWED - Damage value: " + damage);
                    return true;
                }
                damage = 0;
                //Log("!!NO SHIP CORE DELAYED DAMAGE ALLOWED!! - Damage value: " + damage);
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

        //configNoPlayerShipShieldDamage

        [HarmonyPatch(typeof(ShieldUnitController), nameof(ShieldUnitController.OnPlateHealthChanged))]
        class ShieldUnitControllerPatch1
        {
            static void Postfix(ref int plateHealth, ref ShieldUnitController __instance)
            {
                if (!configNoPlayerShipShieldDamage.Value)
                {
                    //Log("SHIP SHIELD DAMAGE ALLOWED");
                    return;
                }
                if (plateHealth < __instance.MaxHealth)
                {
                    plateHealth = __instance.MaxHealth;
                    __instance.m_ShieldUnitBlackboardData.m_IsShieldUnitActivated.Value = true;
                    __instance.m_ShieldUnitBlackboardData.m_ShieldUnitHealth.Value = __instance.MaxHealth;
                    //Log("SHIP SHIELD STRENGTH: " + plateHealth);
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
                    //Log("SHIP DAMAGE ALLOWED - New component status: " + newCritical.ToString());
                    return true;
                }
                newCritical = SpaceShip_BaseComponent.ComponentHealthStatus.Healthy;
                //Log("!!NO SHIP DAMAGE ALLOWED!! - New component status: " + newCritical.ToString());
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
                {
                    //Log.LogInfo("CRAFT COST NORMAL - Cost value: " + blueprint.m_MateriaCraftCost.ToString());
                    return true;
                }
                blueprint.m_MateriaCraftCost = 0;
                //Log.LogInfo("CRAFT COST ZERO - Cost value: " + blueprint.m_MateriaCraftCost.ToString());
                return true;
            }
        }

        // configNoShipAmmoCost

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

        // configNoPlayerReload

        [HarmonyPatch(typeof(PlayerPickupableItemHandler), nameof(PlayerPickupableItemHandler.Handle_AmmoInMagChanged))]
        class ItemHandlerPatch1
        {
            static bool Prefix(ref PlayerPickupableItemHandler __instance, ref int ammoInMag)
            {
                if (!configNoPlayerReload.Value)
                {
                    return true;
                }
                int maxAmmoInMag = __instance.ItemHeldPersistentPickupable.PickupableBlackboardData.m_MagSize.Value;
                int currentAmmoInMag = __instance.ItemHeldPersistentPickupable.PickupableBlackboardData.m_AmmoInMag.Value;
                //Log("PLAYER NO RELOAD - Current bulletcount: " + __instance.CurrentAmmo + " max ammo " + __instance.MaxAmmo + " max ammo in mag: " + maxAmmoInMag  + " new ammo in mag: " + currentAmmoInMag);
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



        // configInstantBoost

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

        // configPlayerDamageMultiplier

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
                {
                    return true;
                }
                materiaValue = (int)(materiaValue * configMateriaMultiplier.Value);
                //Log("MATERIA MULTIPLIER " + configMateriaMultiplier.Value.ToString() + " - Materia disassemble value: " + materiaValue.ToString());
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
                {
                    return;
                }
                __result *= configBoostTimeMult.Value;
                //Log("Boost time: " + __result);

            }
        }

        // configCreditsMultiplier

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
                //Log("CREDITS VALUE MULTIPLIER (mission): " + configCreditsMultiplier.Value.ToString() + " - Credits: " + __result);
            }
        }

        // configIngotMultiplier

        [HarmonyPatch(typeof(MissionData), nameof(MissionData.GetIngotsReward))]
        class IngotValuePatch1
        {
            static void Postfix(ref int __result)
            {
                if (configIngotMultiplier.Value <= 1)
                {
                    return;
                }
                __result = (int)(__result * configIngotMultiplier.Value);
                //Log("INGOT VALUE MULTIPLIER (mission): " + configIngotMultiplier.Value.ToString() + " - Ingots: " + __result);
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
                //Log("XP VALUE MULTIPLIER (mission): " + configPlayerXPMultiplier.Value.ToString() + " - Experience: " + __result);
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


        // configInfiniteJump
        // Keepsake.PlayerGameplayAbility OnDeactivate
        [HarmonyPatch(typeof(DoubleJumpAbility), nameof(DoubleJumpAbility.ShouldActivate))]
        class DoubleJumpAbilityPatch1
        {
            static void Postfix(ref bool __result)
            {
                if (!configInfiniteJump.Value)
                {
                    return;
                }
                if (Il2CppKeepsake.HyperSpace.NewInputSystem.InputManager.IsActionPressed(Il2CppKeepsake.HyperSpace.NewInputSystem.InputManager.InputKeys.Jump))
                {
                    __result = true;
                }

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
            configInstantBoost = ToggleCategory.CreateEntry<bool>("configInstantBoost", false, "Enable instant ship boost");
            configInfiniteJump = ToggleCategory.CreateEntry<bool>("configInfiniteJump", false, "Enable infinite double jump");

            configPlayerDamageMultiplier = MultiplierFloatCategory.CreateEntry<float>("configPlayerDamageMultiplier", 1f, "Player damage multiplier (on foot)", validator: new ValueRange<float>(1f, 20f));
            configPlayerShipDamageMultiplier = MultiplierFloatCategory.CreateEntry<float>("configPlayerShipDamageMultiplier", 1f, "Player damage multiplier (spaceship)", validator: new ValueRange<float>(1f, 20f));
            configPlayerSpeedMultiplier = MultiplierFloatCategory.CreateEntry<float>("configPlayerSpeedMultiplier", 1f, "Player speed multiplier (on foot)", validator: new ValueRange<float>(1f, 5f));
            configBoostTimeMult = MultiplierFloatCategory.CreateEntry<float>("configBoostTimeMult", 1f, "Ship boost time multiplier", validator: new ValueRange<float>(1f, 20f));
            configMateriaMultiplier = MultiplierFloatCategory.CreateEntry<float>("configMateriaMultiplier", 1f, "Materia disassemble gains multiplier", validator: new ValueRange<float>(1f, 20f));
            configCreditsMultiplier = MultiplierFloatCategory.CreateEntry<float>("configCreditsMultiplier", 1f, "Mission credits reward multiplier", validator: new ValueRange<float>(1f, 20f));
            configIngotMultiplier = MultiplierFloatCategory.CreateEntry<float>("configIngotMultiplier", 1f, "Mission ingots reward multiplier", validator: new ValueRange<float>(1f, 20f));
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

            Log("JS Mod Initialized.");


        }
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
            }
            else
            {
                UnityEngine.Cursor.lockState = lastLockMode;
                UnityEngine.Cursor.visible = lastVisibleState;
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

                /*for (int i = 0; i < CustomCategoryList.Count; i++)
                {
                    var tmpcat = CustomCategoryList[i];
                    GUI.Label(new Rect(xAxis, yAxis, 810, 20), tmpcat.DisplayName, JModStyleH);
                    yAxis += 35;
                    switch (tmpcat.Entries[0].GetType().ToString())
                    {
                        case "bool":
                            {
                                ShowBoolMenu(ref xAxis, ref yAxis, ref tmpcat);
                                CustomCategoryList[i] = tmpcat;
                                continue;
                            }
                        case "float":
                            {
                                ShowFloatMenu(ref xAxis, ref yAxis, ref tmpcat);
                                CustomCategoryList[i] = tmpcat;
                                continue;
                            }
                        case "int":
                            {
                                ShowIntMenu(ref xAxis, ref yAxis, ref tmpcat);
                                CustomCategoryList[i] = tmpcat;
                                continue;
                            }
                        default:
                            continue;
                    }
                }*/

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