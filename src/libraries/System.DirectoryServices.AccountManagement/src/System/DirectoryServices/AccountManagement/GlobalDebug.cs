// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Globalization;

namespace System.DirectoryServices.AccountManagement
{
    internal enum DebugLevel
    {
        None = 0,
        Info,
        Warn,
        Error
    }

    internal static class GlobalDebug
    {
        static GlobalDebug()
        {
            GlobalDebug.s_debugLevel = GlobalConfig.DebugLevel;
#if SUPPORTDEBUGLOGFILE // not defined
            string debugLogFile = GlobalConfig.DebugLogFile;

            if (debugLogFile != null)
            {
                foreach (TraceListener listener in Debug.Listeners)
                {
                    if (listener is DefaultTraceListener)
                        ((DefaultTraceListener)listener).LogFileName = debugLogFile;
                }

                //
                Debug.WriteLine(
                            string.Format(
                                System.Globalization.CultureInfo.InvariantCulture,
                                "Principal API Debug Log - AppDomain {0} with ID {1} - {2} (UTC)",
                                System.Threading.Thread.GetDomain().FriendlyName,
                                System.Threading.Thread.GetDomainID(),
                                DateTime.UtcNow));
            }
#endif
        }

        public static bool Error
        {
            get { return DebugLevel.Error >= GlobalDebug.s_debugLevel; }
        }

        public static bool Warn
        {
            get { return DebugLevel.Warn >= GlobalDebug.s_debugLevel; }
        }

        public static bool Info
        {
            get { return DebugLevel.Info >= GlobalDebug.s_debugLevel; }
        }

        [ConditionalAttribute("DEBUG")]
        public static void WriteLineIf(bool f, string category, string message, params object[] args)
        {
            message = "[" + SafeNativeMethods.GetCurrentThreadId().ToString("x", CultureInfo.InvariantCulture) + "] " + message;

            Debug.WriteLineIf(
                            f,
                            string.Format(
                                CultureInfo.InvariantCulture,
                                message,
                                args),
                            category);
        }

        [ConditionalAttribute("DEBUG")]
        public static void WriteLineIf(bool f, string category, string message)
        {
            message = "[" + SafeNativeMethods.GetCurrentThreadId().ToString("x", CultureInfo.InvariantCulture) + "] " + message;

            Debug.WriteLineIf(
                            f,
                            message,
                            category);
        }

        private static readonly DebugLevel s_debugLevel;
    }
}
