//#define NOINPUTSYSTEM
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Cpp2IL.Core.Api;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Unity.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JSPlayerModPlugin
{
    public class JMod 
    {
        public const string GUID = "jw11-modder.JMod";
        public const string NAME = "JMod_bepin";
        public const string AUTHOR = "jw11-modder";
        public const string VERSION = "1.0.0";

        public static ConfigEntry<bool> JModEnabled;

        private static BasePlugin jPlugin;
        private static bool showCheatsPopup = false;
        public static bool isConfigLoaded = false;

        public static ConfigEntry<KeyCode> configMenuToggle;

        private static GUIStyle JModStyleT = new GUIStyle();
        private static GUIStyle JModStyleH = new GUIStyle();
        private static GUIStyle JModStyleP = new GUIStyle();
        private static GUIStyle JModStylePV = new GUIStyle();
        private static GUIStyle JModStyleB = new GUIStyle();
        private static GUIStyle JModStyleS = new GUIStyle();
        private static GUIStyle JModStyleST = new GUIStyle();
        private static GUIStyle JModStyleBlank = new GUIStyle();

        private static Color JModColor = new Color(0.0f, 0.85f, 0.85f);

        private static Texture2D sliderBackground = new Texture2D(1, 1, TextureFormat.RGBAFloat, false);

        private enum ConfigCategory
        {
            Toggles,
            MultFloat,
            MultInt
        }

        private static List<List<ConfigDefinition>> configCategoryList = new List<List<ConfigDefinition>>();

        private static List<string> configCategoryNameList = new List<string>();

        private static CursorLockMode lastLockMode;
        private static bool lastVisibleState;

#if !NOINPUTSYSTEM
        public Key KeycodeToKey(KeyCode keyCode)
        {
            foreach (Key key in System.Enum.GetValues(typeof(Key)))
            {
                if (key.ToString().ToLower() == keyCode.ToString().ToLower())
                {
                    return key;
                }
            }

            jPlugin.Log.LogWarning("can't find Key matching KeyCode " + keyCode.ToString());
            return Key.None;
        }
#endif

        public void LogHandler(string log, LogType level)
        {
            switch (level)
            {
                case LogType.Log:
                    jPlugin.Log.LogMessage(log);
                    return;
                case LogType.Warning:
                case LogType.Assert:
                    jPlugin.Log.LogWarning(log);
                    return;
                case LogType.Exception:
                case LogType.Error:
                    jPlugin.Log.LogError(log);
                    return;
            }
        }
        /*public static void Log( object message)
            => Log( message, LogType.Log);

        public static void LogWarning( object message)
            => Log( message, LogType.Warning);

        public static void LogError( object message)
            => Log( message, LogType.Error);

        internal static override void Log( object message, LogType logType)
        {
            string log = message?.ToString() ?? "";

            switch (logType)
            {
                case LogType.Log:
                case LogType.Assert:
                    Logger.LogMessage(log); break;

                case LogType.Warning:
                    Logger.LogWarning(log); break;

                case LogType.Error:
                case LogType.Exception:
                    Logger.LogError(log); break;
            }
        }*/


        public void ConfFileInit()
        {
            configCategoryList.Clear();
            configCategoryNameList.Clear();

            foreach (ConfigCategory category in Enum.GetValues(typeof(ConfigCategory)))
            {
                configCategoryNameList.Add(Enum.GetNames(typeof(ConfigCategory))[(int)category]);
                configCategoryList.Add(new List<ConfigDefinition>());
                jPlugin.Log.LogInfo("Category: " + configCategoryNameList[(int)category] + " added!");
            }

            foreach (ConfigDefinition definition in jPlugin.Config.Keys)
            {

                if (!configCategoryNameList.Contains(definition.Section))
                {
                    configCategoryNameList.Add(definition.Section);
                    configCategoryList.Add(new List<ConfigDefinition>());
                    jPlugin.Log.LogInfo("Custom category: " + definition.Section + " added!");
                }

                configCategoryList[configCategoryNameList.IndexOf(definition.Section)].Add(definition);
                jPlugin.Log.LogInfo("Definition: " + definition.ToString() + " from section " + definition.Section + " added!");
            }

            isConfigLoaded = true;
            jPlugin.Log.LogInfo("JMod Config Init complete!");
        }

        public void JModInit(BasePlugin instance)
        {
            //Logger = base.;
            jPlugin = instance;
            jPlugin.Log.LogInfo("Starting JMod init!");
            
            ConfFileInit();
            //base.enabled = true;

            JModEnabled = jPlugin.Config.Bind("JMod", "Mod Enabled", true, "Enable config mod GUI");

            configMenuToggle = jPlugin.Config.Bind("JMod", "MenuToggle", KeyCode.F7, "Main menu toggle key");

            JModStyleH.alignment = TextAnchor.MiddleCenter;
            JModStyleH.fontSize = 20;
            JModStyleH.fontStyle = FontStyle.Bold;
            JModStyleH.normal.textColor = JModColor;

            JModStyleP.fontSize = 16;
            JModStyleP.normal.textColor = JModColor;

            JModStylePV.fontSize = 16;
            JModStylePV.fontStyle = FontStyle.Bold;
            JModStylePV.normal.textColor = JModColor;


            //JModStyleB.normal.background = consoleBackground;

            //consoleBackground.SetPixel(0, 0, new Color(0.003f, 0.003f, 0.01f, 0.92f));
            //consoleBackground.Apply();

            sliderBackground.SetPixel(0, 0, new Color(0.05f, 0.05f, 0.05f));
            sliderBackground.Apply();


            jPlugin.Log.LogInfo("JMod Init complete!");
            if (!JModEnabled.Value)
                jPlugin.Log.LogInfo("GUI disabled!");
            else
                jPlugin.Log.LogInfo("Menu key: " + configMenuToggle.Value.ToString());
        }



        public void SwitchMenu()
        {
            if (!showCheatsPopup && JModEnabled.Value)
            {
                showCheatsPopup = !showCheatsPopup;
                lastLockMode = Cursor.lockState;
                lastVisibleState = Cursor.visible;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                showCheatsPopup = !showCheatsPopup;
                Cursor.lockState = lastLockMode;
                Cursor.visible = lastVisibleState;
                jPlugin.Config.Save();
            }
        }

        public static void CreateTogglesMenu(ref int xAxis, ref int yAxis, int catIndex)
        {
            foreach (ConfigDefinition definition in configCategoryList[catIndex])
            {
                jPlugin.Config[definition].BoxedValue = GUI.Toggle(new Rect(xAxis, yAxis, 400, 20), (bool)jPlugin.Config[definition].BoxedValue, jPlugin.Config[definition].Description.Description, JModStyleT);
                xAxis += 405;
                if (xAxis > 800)
                {
                    xAxis = 20;
                    yAxis += 25;
                }
            }
            if (xAxis != 20)
            {
                xAxis = 20;
                yAxis += 25;
            }
            yAxis += 10;
        }

        public static void CreateMultFloatMenu(ref int xAxis, ref int yAxis, int catIndex)
        {
            foreach (ConfigDefinition definition in configCategoryList[catIndex])
            {
                string multLabel = jPlugin.Config[definition].Description.Description;
                float minValue;
                float maxValue;
                if (jPlugin.Config[definition].Description.AcceptableValues != null)
                {
                    AcceptableValueRange<float> minMaxValues = (AcceptableValueRange<float>)jPlugin.Config[definition].Description.AcceptableValues;
                    minValue = minMaxValues.MinValue;
                    maxValue = minMaxValues.MaxValue;
                }
                else
                {
                    minValue = 1f;
                    maxValue = 20f;
                }
                multLabel += " (" + minValue.ToString() + " - " + maxValue.ToString() + ")";
                GUI.Label(new Rect(xAxis, yAxis, 680, 20), multLabel, JModStyleP);
                float value = (float)jPlugin.Config[definition].BoxedValue;
                if (GUI.Button(new Rect(xAxis + 680, yAxis, 40, 20), " - "))
                {
                    if (value > minValue)
                        jPlugin.Config[definition].BoxedValue = value - 0.5f;
                }
                GUI.Label(new Rect(xAxis + 730, yAxis, 40, 20), value.ToString("0.0"), JModStylePV);
                if (GUI.Button(new Rect(xAxis + 780, yAxis, 40, 20), " + "))
                {
                    if (value < maxValue)
                        jPlugin.Config[definition].BoxedValue = value + 0.5f;
                }
                
                yAxis += 25;
                //jPlugin.Config[definition].BoxedValue = GUI.HorizontalSlider(new Rect(xAxis, yAxis, 810, 20), value, minValue, maxValue, JModStyleS, JModStyleST);
                yAxis += 15;
            }
        }

        public static void CreateMultIntMenu(ref int xAxis, ref int yAxis, int catIndex)
        {
            foreach (ConfigDefinition definition in configCategoryList[catIndex])
            {
                string multLabel = jPlugin.Config[definition].Description.Description;
                int minValue;
                int maxValue;
                if (jPlugin.Config[definition].Description.AcceptableValues != null)
                {
                    AcceptableValueRange<int> minMaxValues = (AcceptableValueRange<int>)jPlugin.Config[definition].Description.AcceptableValues;
                    minValue = minMaxValues.MinValue;
                    maxValue = minMaxValues.MaxValue;
                }
                else
                {
                    minValue = 1;
                    maxValue = 20;
                }
                multLabel += " (" + minValue.ToString() + " - " + maxValue.ToString() + ")";
                GUI.Label(new Rect(xAxis, yAxis, 680, 20), multLabel, JModStyleP);
                int value = (int)jPlugin.Config[definition].BoxedValue;
                if (GUI.Button(new Rect(xAxis + 680, yAxis, 40, 20), " - "))
                {
                    if (value > minValue)
                        jPlugin.Config[definition].BoxedValue = value - 1;
                }
                GUI.Label(new Rect(xAxis + 730, yAxis, 40, 20), value.ToString(), JModStylePV);
                if (GUI.Button(new Rect(xAxis + 780, yAxis, 40, 20), " + "))
                {
                    if (value < maxValue)
                        jPlugin.Config[definition].BoxedValue = value + 1;
                }
                yAxis += 25;

                //jPlugin.Config[definition].BoxedValue = (int)GUI.HorizontalSlider(new Rect(xAxis, yAxis, 810, 20), value, minValue, maxValue, JModStyleS, JModStyleST);
                yAxis += 15;
            }
        }

        public void ShowMenu()
        {
            if (showCheatsPopup)
            {

                Rect jModWindowRect = new Rect(Screen.width / 2 - 425, Screen.height / 2 - 425, 850, 850);
                Rect _screenRect = new Rect(0, 0, Screen.width, Screen.height);

                var yAxis = 40;
                var xAxis = 20;

                JModStyleT = GUI.skin.GetStyle("toggle");
                JModStyleT.fontSize = 16;
                JModStyleT.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
                JModStyleT.onNormal.textColor = JModColor;

                JModStyleB = GUI.skin.GetStyle("box");
                JModStyleB.alignment = TextAnchor.UpperCenter;
                JModStyleB.fontSize = 24;
                JModStyleB.fontStyle = FontStyle.Bold;
                JModStyleB.normal.textColor = JModColor;

                JModStyleS = GUI.skin.GetStyle("horizontalslider");
                JModStyleS.normal.background = sliderBackground;

                JModStyleST = GUI.skin.GetStyle("horizontalsliderthumb");

                GUI.BeginGroup(jModWindowRect);
                for (int i = 1; i < 4; i++)
                    GUI.Box(new Rect(0, 0, 850, 850), "", JModStyleB);
                GUI.Box(new Rect(0, 0, 850, 850), "MOD OPTIONS", JModStyleB);

                GUI.Label(new Rect(xAxis, yAxis, 810, 20), "Toggle Mod Options", JModStyleH);
                yAxis += 35;

                CreateTogglesMenu(ref xAxis, ref yAxis, 0);

                GUI.Label(new Rect(xAxis, yAxis, 810, 20), "Multiplier Mod Options", JModStyleH);
                yAxis += 45;

                CreateMultFloatMenu(ref xAxis, ref yAxis, 1);

                CreateMultIntMenu(ref xAxis, ref yAxis, 2);

                yAxis += 15;
                /*foreach (MelonPreferences_Category category in CustomCategoryList)
                {
                    GUI.Label(new Rect(xAxis, yAxis, 810, 20), category.DisplayName, JModStyleH);
                    yAxis += 35;
                    if (category.Identifier.EndsWith("Int"))
                    {
                        foreach (MelonPreferences_Entry<int> entry in category.Entries)
                        {
                            string multLabel = entry.DisplayName;
                            ValueRange<int> range = (ValueRange<int>)entry.Validator;
                            multLabel += " (" + range.MinValue.ToString() + " - " + range.MaxValue.ToString() + ") " + entry.Value.ToString();
                            GUI.Label(new Rect(xAxis, yAxis, 780, 20), multLabel, JModStyleP);
                            GUI.Label(new Rect(xAxis + 780, yAxis, 40, 20), entry.Value.ToString(), JModStylePV);
                            yAxis += 25;
                            entry.Value = (int)GUI.HorizontalSlider(new Rect(xAxis, yAxis, 810, 20), (float)entry.Value, (float)range.MinValue, (float)range.MaxValue, JModStyleS, JModStyleST);
                            yAxis += 15;
                        }
                    }
                    yAxis += 25;
                }*/


                if (GUI.Button(new Rect(325, 810, 200, 35),"Save settings and close"))
                {
                    SwitchMenu();
                }

                // Dirty little hack to disable mouse clickthrough

                Vector2 mousePosition = Mouse.current.position.value;
                mousePosition.y = Screen.height - mousePosition.y;

                if (GUI.Button(_screenRect, string.Empty, JModStyleBlank) && !jModWindowRect.Contains(mousePosition))
                {

                }

                if (jModWindowRect.Contains(mousePosition) && !(Event.current.keyCode == (configMenuToggle.Value)) && (Event.current.type == EventType.KeyDown))
                {
                    Event.current.Use();
                }

                GUI.EndGroup();
            }
        }

    }

}
