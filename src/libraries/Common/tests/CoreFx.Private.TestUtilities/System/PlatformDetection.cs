// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Security;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Microsoft.Win32;

namespace System
{
    public static partial class PlatformDetection
    {
        //
        // Do not use the " { get; } = <expression> " pattern here. Having all the initialization happen in the type initializer
        // means that one exception anywhere means all tests using PlatformDetection fail. If you feel a value is worth latching,
        // do it in a way that failures don't cascade.
        //

        public static bool IsNetCore => RuntimeInformation.FrameworkDescription.StartsWith(".NET Core", StringComparison.OrdinalIgnoreCase);
        public static bool IsMonoRuntime => Type.GetType("Mono.RuntimeStructs") != null;
        public static bool IsFreeBSD => RuntimeInformation.IsOSPlatform(OSPlatform.Create("FREEBSD"));
        public static bool IsNetBSD => RuntimeInformation.IsOSPlatform(OSPlatform.Create("NETBSD"));

        public static bool IsArmProcess => RuntimeInformation.ProcessArchitecture == Architecture.Arm;
        public static bool IsNotArmProcess => !IsArmProcess;
        public static bool IsArm64Process => RuntimeInformation.ProcessArchitecture == Architecture.Arm64;
        public static bool IsNotArm64Process => !IsArm64Process;
        public static bool IsArmOrArm64Process => IsArmProcess || IsArm64Process;
        public static bool IsNotArmNorArm64Process => !IsArmOrArm64Process;
        public static bool IsArgIteratorSupported => IsMonoRuntime || (IsWindows && IsNotArmProcess);
        public static bool IsArgIteratorNotSupported => !IsArgIteratorSupported;
        public static bool Is32BitProcess => IntPtr.Size == 4;

        public static bool IsDrawingSupported
        {
            get
            {
#if NETCOREAPP
                if (!IsWindows)
                {
                    if (IsOSX)
                    {
                        return NativeLibrary.TryLoad("libgdiplus.dylib", out _);
                    }
                    else
                    {
                       return NativeLibrary.TryLoad("libgdiplus.so", out _) || NativeLibrary.TryLoad("libgdiplus.so.0", out _);
                    }
                }
#endif

                return IsNotWindowsNanoServer && IsNotWindowsServerCore;

            }
        }

        public static bool IsInContainer => GetIsInContainer();
        public static bool SupportsSsl3 => GetSsl3Support();

#if NETCOREAPP
        public static bool IsReflectionEmitSupported = RuntimeFeature.IsDynamicCodeSupported;
#else
        public static bool IsReflectionEmitSupported = true;
#endif

        public static bool IsInvokingStaticConstructorsSupported => true;

        // System.Security.Cryptography.Xml.XmlDsigXsltTransform.GetOutput() relies on XslCompiledTransform which relies
        // heavily on Reflection.Emit
        public static bool IsXmlDsigXsltTransformSupported => !PlatformDetection.IsInAppContainer;

        public static bool IsPreciseGcSupported => !IsMonoRuntime;

        public static bool IsNotIntMaxValueArrayIndexSupported => s_largeArrayIsNotSupported.Value;

        private static volatile Tuple<bool> s_lazyNonZeroLowerBoundArraySupported;
        public static bool IsNonZeroLowerBoundArraySupported
        {
            get
            {
                if (s_lazyNonZeroLowerBoundArraySupported == null)
                {
                    bool nonZeroLowerBoundArraysSupported = false;
                    try
                    {
                        Array.CreateInstance(typeof(int), new int[] { 5 }, new int[] { 5 });
                        nonZeroLowerBoundArraysSupported = true;
                    }
                    catch (PlatformNotSupportedException)
                    {
                    }
                    s_lazyNonZeroLowerBoundArraySupported = Tuple.Create<bool>(nonZeroLowerBoundArraysSupported);
                }
                return s_lazyNonZeroLowerBoundArraySupported.Item1;
            }
        }

        public static bool IsDomainJoinedMachine => !Environment.MachineName.Equals(Environment.UserDomainName, StringComparison.OrdinalIgnoreCase);
        public static bool IsNotDomainJoinedMachine => !IsDomainJoinedMachine;

        // Windows - Schannel supports alpn from win8.1/2012 R2 and higher.
        // Linux - OpenSsl supports alpn from openssl 1.0.2 and higher.
        // OSX - SecureTransport doesn't expose alpn APIs. TODO https://github.com/dotnet/corefx/issues/33016
        public static bool SupportsAlpn => (IsWindows && !IsWindows7) ||
            ((!IsOSX && !IsWindows) &&
            (OpenSslVersion.Major >= 1 && (OpenSslVersion.Minor >= 1 || OpenSslVersion.Build >= 2)));

        public static bool SupportsClientAlpn => SupportsAlpn || (IsOSX && PlatformDetection.OSXVersion > new Version(10, 12));

        // OpenSSL 1.1.1 and above.
        public static bool SupportsTls13 => !IsWindows && !IsOSX && (OpenSslVersion.CompareTo(new Version(1,1,1)) >= 0);

        private static Lazy<bool> s_largeArrayIsNotSupported = new Lazy<bool>(IsLargeArrayNotSupported);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        private static bool IsLargeArrayNotSupported()
        {
            try
            {
                var tmp = new byte[int.MaxValue];
                return tmp == null;
            }
            catch (OutOfMemoryException)
            {
                return true;
            }
        }

        public static string GetDistroVersionString()
        {
            if (IsWindows)
            {
                return "WindowsProductType=" + GetWindowsProductType() + " WindowsInstallationType=" + GetWindowsInstallationType();
            }
            else if (IsOSX)
            {
                return "OSX Version=" + m_osxProductVersion.Value.ToString();
            }
            else
            {
                DistroInfo v = GetDistroInfo();

                return $"Distro={v.Id} VersionId={v.VersionId}";
            }
        }

        private static bool GetIsInContainer()
        {
            if (IsWindows)
            {
                string key = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control";
                string value = "";

                try
                {
                    value = (string)Registry.GetValue(key, "ContainerType", defaultValue: "");
                }
                catch
                {
                }

                return !string.IsNullOrEmpty(value);
            }

            return (IsLinux && File.Exists("/.dockerenv"));
        }

        private static bool GetSsl3Support()
        {
            if (IsWindows)
            {
                string clientKey = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\SSL 3.0\Client";
                string serverKey = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\SSL 3.0\Server";

                object client, server;
                try
                {
                    client = Registry.GetValue(clientKey, "Enabled", null);
                    server = Registry.GetValue(serverKey, "Enabled", null);
                }
                catch (SecurityException)
                {
                    // Insufficient permission, assume that we don't have SSL3 (since we aren't exactly sure)
                    return false;
                }

                if (client is int c && server is int s)
                {
                    return c == 1 && s == 1;
                }

                // Missing key. If we're pre-20H1 then assume SSL3 is enabled.
                // Otherwise, disabled. (See comments on https://github.com/dotnet/runtime/issues/1166)
                // Alternatively the returned values must have been some other types.
                return !IsWindows10Version2004OrGreater;
            }

            return (IsOSX || (IsLinux && OpenSslVersion < new Version(1, 0, 2) && !IsDebian));
        }
    }
}
