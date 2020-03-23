#region Header
/*	============================================
 *	작성자 : Strix
 *	작성일 : 2019-12-16 오전 11:10:06
 *	개요 : 
   ============================================ */
#endregion Header

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// https://answers.unity.com/questions/161858/startstop-playmode-from-editor-script.html
/// </summary>
public static class ApplicationWrapper
{
    public static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public static void VibrateDevice()
    {
        Debug.Log(nameof(VibrateDevice));

#if !UNITY_EDITOR && UNITY_ANDROID
        Handheld.Vibrate();
#endif
    }

    public static void VibrateDevice(long lMilliseconds)
    {
        Debug.Log($"{nameof(VibrateDevice)}_{lMilliseconds}");

        Unity_Pattern.Vibration.Vibrate(lMilliseconds);
    }

    public static void VibrateDevice(long lMilliseconds, int iPower)
    {
        Debug.Log($"{nameof(VibrateDevice)}_{lMilliseconds}_{iPower}");

        Unity_Pattern.Vibration.Vibrate(lMilliseconds, iPower);
    }

    public static void VibrateDevice(long[] arrMilliseconds, int[] arrPower, int iRepeatCount)
    {
        Debug.Log($"{nameof(VibrateDevice)}_{arrMilliseconds.Length}_{arrPower.Length}_{iRepeatCount}");

        Unity_Pattern.Vibration.Vibrate(arrMilliseconds, arrPower, iRepeatCount);
    }
}


namespace Unity_Pattern
{
    /// <summary>
    /// 출처 : https://gist.github.com/ruzrobert/d98220a3b7f71ccc90403e041967c46b
    /// </summary>

    /// <summary>
    /// Class for controlling Vibration. Automatically initializes before scene is loaded.
    /// </summary>
    public static class Vibration
    {
        // Component Parameters
        public static logLevel LogLevel = logLevel.Disabled;

        // Vibrator References
        private static AndroidJavaObject vibrator = null;
        private static AndroidJavaClass vibrationEffectClass = null;
        private static int defaultAmplitude = 255;

        // Api Level
        private static int ApiLevel = 1;
        private static bool doesSupportVibrationEffect() => ApiLevel >= 26;    // available only from Api >= 26
        private static bool doesSupportPredefinedEffect() => ApiLevel >= 29;   // available only from Api >= 29

#region Initialization
        private static bool isInitialized = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        [SuppressMessage("Code quality", "IDE0051", Justification = "Called on scene load")]
        private static void Initialize()
        {
            // Add APP VIBRATION PERMISSION to the Manifest
#if UNITY_ANDROID
            if (Application.isConsolePlatform) { Handheld.Vibrate(); }
#endif

            // load references safely
            if (isInitialized == false && Application.platform == RuntimePlatform.Android)
            {
                // Get Api Level
                using (AndroidJavaClass androidVersionClass = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    ApiLevel = androidVersionClass.GetStatic<int>("SDK_INT");
                }

                // Get UnityPlayer and CurrentActivity
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    if (currentActivity != null)
                    {
                        vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");

                        // if device supports vibration effects, get corresponding class
                        if (doesSupportVibrationEffect())
                        {
                            vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
                            defaultAmplitude = Mathf.Clamp(vibrationEffectClass.GetStatic<int>("DEFAULT_AMPLITUDE"), 1, 255);
                        }

                        // if device supports predefined effects, get their IDs
                        if (doesSupportPredefinedEffect())
                        {
                            PredefinedEffect.EFFECT_CLICK = vibrationEffectClass.GetStatic<int>("EFFECT_CLICK");
                            PredefinedEffect.EFFECT_DOUBLE_CLICK = vibrationEffectClass.GetStatic<int>("EFFECT_DOUBLE_CLICK");
                            PredefinedEffect.EFFECT_HEAVY_CLICK = vibrationEffectClass.GetStatic<int>("EFFECT_HEAVY_CLICK");
                            PredefinedEffect.EFFECT_TICK = vibrationEffectClass.GetStatic<int>("EFFECT_TICK");
                        }
                    }
                }

                logAuto("Vibration component initialized", logLevel.Info);
                isInitialized = true;
            }
        }
#endregion

#region Vibrate Public
        /// <summary>
        /// Vibrate for Milliseconds, with Amplitude (if available).
        /// If amplitude is -1, amplitude is Disabled. If -1, device DefaultAmplitude is used. Otherwise, values between 1-255 are allowed.
        /// If 'cancel' is true, Cancel() will be called automatically.
        /// </summary>
        public static void Vibrate(long milliseconds, int amplitude = -1, bool cancel = false)
        {
            string funcToStr() => string.Format("Vibrate ({0}, {1}, {2})", milliseconds, amplitude, cancel);

            Initialize(); // make sure script is initialized
            if (isInitialized == false)
            {
                logAuto(funcToStr() + ": Not initialized", logLevel.Warning);
            }
            else if (HasVibrator() == false)
            {
                logAuto(funcToStr() + ": Device doesn't have Vibrator", logLevel.Warning);
            }
            else
            {
                if (cancel) Cancel();
                if (doesSupportVibrationEffect())
                {
                    // validate amplitude
                    amplitude = Mathf.Clamp(amplitude, -1, 255);
                    if (amplitude == -1) amplitude = 255; // if -1, disable amplitude (use maximum amplitude)
                    if (amplitude != 255 && HasAmplitudeControl() == false)
                    { // if amplitude was set, but not supported, notify developer
                        logAuto(funcToStr() + ": Device doesn't have Amplitude Control, but Amplitude was set", logLevel.Warning);
                    }
                    if (amplitude == 0) amplitude = defaultAmplitude; // if 0, use device DefaultAmplitude

                    // if amplitude is not supported, use 255; if amplitude is -1, use systems DefaultAmplitude. Otherwise use user-defined value.
                    amplitude = HasAmplitudeControl() == false ? 255 : amplitude;
                    vibrateEffect(milliseconds, amplitude);
                    logAuto(funcToStr() + ": Effect called", logLevel.Info);
                }
                else
                {
                    vibrateLegacy(milliseconds);
                    logAuto(funcToStr() + ": Legacy called", logLevel.Info);
                }
            }
        }
        /// <summary>
        /// Vibrate Pattern (pattern of durations, with format Off-On-Off-On and so on).
        /// Amplitudes can be Null (for default) or array of Pattern array length with values between 1-255.
        /// To cause the pattern to repeat, pass the index into the pattern array at which to start the repeat, or -1 to disable repeating.
        /// If 'cancel' is true, Cancel() will be called automatically.
        /// </summary>
        public static void Vibrate(long[] pattern, int[] amplitudes = null, int repeat = -1, bool cancel = false)
        {
            string funcToStr() => string.Format("Vibrate (({0}), ({1}), {2}, {3})", arrToStr(pattern), arrToStr(amplitudes), repeat, cancel);

            Initialize(); // make sure script is initialized
            if (isInitialized == false)
            {
                logAuto(funcToStr() + ": Not initialized", logLevel.Warning);
            }
            else if (HasVibrator() == false)
            {
                logAuto(funcToStr() + ": Device doesn't have Vibrator", logLevel.Warning);
            }
            else
            {
                // check Amplitudes array length
                if (amplitudes != null && amplitudes.Length != pattern.Length)
                {
                    logAuto(funcToStr() + ": Length of Amplitudes array is not equal to Pattern array. Amplitudes will be ignored.", logLevel.Warning);
                    amplitudes = null;
                }
                // limit amplitudes between 1 and 255
                if (amplitudes != null)
                {
                    clampAmplitudesArray(amplitudes);
                }

                // vibrate
                if (cancel) Cancel();
                if (doesSupportVibrationEffect())
                {
                    if (amplitudes != null && HasAmplitudeControl() == false)
                    {
                        logAuto(funcToStr() + ": Device doesn't have Amplitude Control, but Amplitudes was set", logLevel.Warning);
                        amplitudes = null;
                    }
                    if (amplitudes != null)
                    {
                        vibrateEffect(pattern, amplitudes, repeat);
                        logAuto(funcToStr() + ": Effect with amplitudes called", logLevel.Info);
                    }
                    else
                    {
                        vibrateEffect(pattern, repeat);
                        logAuto(funcToStr() + ": Effect called", logLevel.Info);
                    }
                }
                else
                {
                    vibrateLegacy(pattern, repeat);
                    logAuto(funcToStr() + ": Legacy called", logLevel.Info);
                }
            }
        }

        /// <summary>
        /// Vibrate predefined effect (described in Vibration.PredefinedEffect). Available from Api Level >= 29.
        /// If 'cancel' is true, Cancel() will be called automatically.
        /// </summary>
        public static void VibratePredefined(int effectId, bool cancel = false)
        {
            string funcToStr() => string.Format("VibratePredefined ({0})", effectId);

            Initialize(); // make sure script is initialized
            if (isInitialized == false)
            {
                logAuto(funcToStr() + ": Not initialized", logLevel.Warning);
            }
            else if (HasVibrator() == false)
            {
                logAuto(funcToStr() + ": Device doesn't have Vibrator", logLevel.Warning);
            }
            else if (doesSupportPredefinedEffect() == false)
            {
                logAuto(funcToStr() + ": Device doesn't support Predefined Effects (Api Level >= 29)", logLevel.Warning);
            }
            else
            {
                if (cancel) Cancel();
                vibrateEffectPredefined(effectId);
                logAuto(funcToStr() + ": Predefined effect called", logLevel.Info);
            }
        }
#endregion

#region Public Properties & Controls
        public static long[] ParsePattern(string pattern)
        {
            if (pattern == null) return new long[0];
            pattern = pattern.Trim();
            string[] split = pattern.Split(',');

            long[] timings = new long[split.Length];
            for (int i = 0; i < split.Length; i++)
            {
                if (int.TryParse(split[i].Trim(), out int duration))
                {
                    timings[i] = duration < 0 ? 0 : duration;
                }
                else
                {
                    timings[i] = 0;
                }
            }

            return timings;
        }

        /// <summary>
        /// Returns Android Api Level
        /// </summary>
        public static int GetApiLevel() => ApiLevel;
        /// <summary>
        /// Returns Default Amplitude of device, or 0.
        /// </summary>
        public static int GetDefaultAmplitude() => defaultAmplitude;

        /// <summary>
        /// Returns true if device has vibrator
        /// </summary>
        public static bool HasVibrator()
        {
            return vibrator != null && vibrator.Call<bool>("hasVibrator");
        }
        /// <summary>
        /// Return true if device supports amplitude control
        /// </summary>
        public static bool HasAmplitudeControl()
        {
            if (HasVibrator() && doesSupportVibrationEffect())
            {
                return vibrator.Call<bool>("hasAmplitudeControl"); // API 26+ specific
            }
            else
            {
                return false; // no amplitude control below API level 26
            }
        }

        /// <summary>
        /// Tries to cancel current vibration
        /// </summary>
        public static void Cancel()
        {
            if (HasVibrator())
            {
                vibrator.Call("cancel");
                logAuto("Cancel (): Called", logLevel.Info);
            }
        }
#endregion

#region Vibrate Internal
#region Vibration Callers
        private static void vibrateEffect(long milliseconds, int amplitude)
        {
            using (AndroidJavaObject effect = createEffect_OneShot(milliseconds, amplitude))
            {
                vibrator.Call("vibrate", effect);
            }
        }
        private static void vibrateLegacy(long milliseconds)
        {
            vibrator.Call("vibrate", milliseconds);
        }

        private static void vibrateEffect(long[] pattern, int repeat)
        {
            using (AndroidJavaObject effect = createEffect_Waveform(pattern, repeat))
            {
                vibrator.Call("vibrate", effect);
            }
        }
        private static void vibrateLegacy(long[] pattern, int repeat)
        {
            vibrator.Call("vibrate", pattern, repeat);
        }

        private static void vibrateEffect(long[] pattern, int[] amplitudes, int repeat)
        {
            using (AndroidJavaObject effect = createEffect_Waveform(pattern, amplitudes, repeat))
            {
                vibrator.Call("vibrate", effect);
            }
        }
        private static void vibrateEffectPredefined(int effectId)
        {
            using (AndroidJavaObject effect = createEffect_Predefined(effectId))
            {
                vibrator.Call("vibrate", effect);
            }
        }
#endregion

#region Vibration Effect
        /// <summary>
        /// Wrapper for public static VibrationEffect createOneShot (long milliseconds, int amplitude). API >= 26
        /// </summary>
        private static AndroidJavaObject createEffect_OneShot(long milliseconds, int amplitude)
        {
            return vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, amplitude);
        }
        /// <summary>
        /// Wrapper for public static VibrationEffect createPredefined (int effectId). API >= 29
        /// </summary>
        private static AndroidJavaObject createEffect_Predefined(int effectId)
        {
            return vibrationEffectClass.CallStatic<AndroidJavaObject>("createPredefined", effectId);
        }
        /// <summary>
        /// Wrapper for public static VibrationEffect createWaveform (long[] timings, int[] amplitudes, int repeat). API >= 26
        /// </summary>
        private static AndroidJavaObject createEffect_Waveform(long[] timings, int[] amplitudes, int repeat)
        {
            return vibrationEffectClass.CallStatic<AndroidJavaObject>("createWaveform", timings, amplitudes, repeat);
        }
        /// <summary>
        /// Wrapper for public static VibrationEffect createWaveform (long[] timings, int repeat). API >= 26
        /// </summary>
        private static AndroidJavaObject createEffect_Waveform(long[] timings, int repeat)
        {
            return vibrationEffectClass.CallStatic<AndroidJavaObject>("createWaveform", timings, repeat);
        }
#endregion
#endregion

#region Internal
        private static void logAuto(string text, logLevel level)
        {
            if (level == logLevel.Disabled) level = logLevel.Info;

            if (text != null)
            {
                if (level == logLevel.Warning && LogLevel == logLevel.Warning)
                {
                    Debug.LogWarning(text);
                }
                else if (level == logLevel.Info && LogLevel >= logLevel.Info)
                {
                    Debug.Log(text);
                }
            }
        }
        private static string arrToStr(long[] array) => array == null ? "null" : string.Join(", ", array);
        private static string arrToStr(int[] array) => array == null ? "null" : string.Join(", ", array);

        private static void clampAmplitudesArray(int[] amplitudes)
        {
            for (int i = 0; i < amplitudes.Length; i++)
            {
                amplitudes[i] = Mathf.Clamp(amplitudes[i], 1, 255);
            }
        }
#endregion

        public static class PredefinedEffect
        {
            public static int EFFECT_CLICK;         // public static final int EFFECT_CLICK
            public static int EFFECT_DOUBLE_CLICK;  // public static final int EFFECT_DOUBLE_CLICK
            public static int EFFECT_HEAVY_CLICK;   // public static final int EFFECT_HEAVY_CLICK
            public static int EFFECT_TICK;          // public static final int EFFECT_TICK
        }
        public enum logLevel
        {
            Disabled,
            Info,
            Warning,
        }
    }
}
