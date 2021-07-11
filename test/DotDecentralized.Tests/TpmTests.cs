using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Tpm2Lib;
using Xunit;

namespace DotDecentralized.Tests
{
    /// <summary>
    /// Utility functions to interact with trusted platform module.
    /// </summary>
    public static class TpmUtilities
    {
        public static Tpm2Device CreateTpmDevice(bool isSimulator)
        {
            if(isSimulator)
            {
                return new TcpTpmDevice("127.0.0.1", 2321, stopTpm: true);
            }

            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new TbsDevice();
            }

            if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                //Note, it appears Linux version derives from TbsDevice, which is by default
                //a Windows version.
                return new LinuxTpmDevice();
            }

            throw new PlatformNotSupportedException(string.Format("The library doesn't support the current OS platform: {0}", RuntimeInformation.OSDescription));
        }
    }

    public sealed class TpmWrapper: IDisposable
    {
        /// <summary>
        /// The TPM instance <see cref="Tpm"/> refers to.
        /// </summary>
        private Tpm2Device TpmDevice { get; set; }

        /// <summary>
        /// The piece of trusted platform module that is used.
        /// </summary>
        public Tpm2 Tpm { get; set; }


        /// <summary>
        /// Default constructor for the TPM.
        /// </summary>
        public TpmWrapper()
        {
            TpmDevice = TpmUtilities.CreateTpmDevice(isSimulator: false);
            TpmDevice.Connect();
            Tpm = new Tpm2(TpmDevice);
        }


        /// <inheritdoc />
        public void Dispose()
        {
            Tpm?.Dispose();
            TpmDevice?.Dispose();
        }
    }

    //TODO: Creating two simulators won't work. Ensure only one exists always.
    //Might be good to ensure only some maximum number of concurrent users can
    //access the TPM also.
    /// <summary>
    /// Wraps the simular and has the correct start sequence for the simulator.
    /// </summary>
    public sealed class TpmSimulatorWrapper: IDisposable
    {
        /// <summary>
        /// A handle to the process that is connected to the TPM simulator
        /// executable.
        /// </summary>
        private Process TpmSimulatorProcessHandle { get; set; }

        /// <summary>
        /// The TPM instance <see cref="Tpm"/> refers to.
        /// </summary>
        private Tpm2Device TpmDevice { get; set; }

        /// <summary>
        /// The piece of trusted platform module that is used.
        /// </summary>
        public Tpm2 Tpm { get; set; }


        public TpmSimulatorWrapper()
        {
            TpmSimulatorProcessHandle = CreateTpmSimulatorHandle();
            _ = TpmSimulatorProcessHandle.Start();

            TpmDevice = TpmUtilities.CreateTpmDevice(isSimulator: true);
            Tpm = new Tpm2(TpmDevice);
            TpmDevice.Connect();
            if(TpmDevice is TcpTpmDevice)
            {
                TpmDevice.PowerCycle();
                Tpm.Startup(Su.Clear);
                ResetDALogic(Tpm);
            }
        }


        /// <inheritdoc />
        public void Dispose()
        {
            TpmSimulatorProcessHandle?.Dispose();
            Tpm?.Dispose();
            TpmDevice?.Dispose();
        }


        /// <summary>
        /// Creates a process handle for the simular.
        /// </summary>
        /// <returns>Process handle for the simulator.</returns>
        /// <remarks>Currently only for Windows.</remarks>
        private static Process CreateTpmSimulatorHandle()
        {
            return new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = @"..\..\..\TpmSimulator\Simulator.exe",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };
        }

        /// <summary>
        /// Reset the dictionary-attack logic.
        /// </summary>
        private static void ResetDALogic(Tpm2 tpm)
        {
            //Very forgiving parameters so that the simulator won't simulate
            //lock-out!
            tpm.DictionaryAttackParameters(TpmHandle.RhLockout, 1000, 10, 1);

            //Zero out all counters.
            tpm.DictionaryAttackLockReset(TpmHandle.RhLockout);
        }
    }


    /// <summary>
    /// Quick test container TPM check for tests...
    /// </summary>
    public class TpmTests: IDisposable
    {
        private TpmSimulatorWrapper Tpm { get; }
        private TpmWrapper TpmReal { get; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public TpmTests()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            string? usePlatformTpmString = Environment.GetEnvironmentVariable("USE_PLATFORM_TPM");
            string? dotNetPlatformString = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

            bool usePlatformTpm = string.IsNullOrWhiteSpace(usePlatformTpmString) && bool.TryParse(usePlatformTpmString, out usePlatformTpm);
            bool isCiEnvironment = dotNetPlatformString?.Equals("ci", StringComparison.InvariantCultureIgnoreCase) == true;

            //It is not possible to test TPM functionality at all unless on supported platforms.
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux),
                $"Trust Platform Module (TPM) 2.0 is supported only on {OSPlatform.Windows} and {OSPlatform.Linux}.");

            //Local builds are currently possible only on simulator and Windows.
            //CI builds are done using TPM, but currently it works only on Linux.
            //TODO: Add simulator for local Linux builds too. Running on physical TPM may cause unexpected
            //systemwide problems. Only test platform TPMs on CI as the environments can be thrown away.
            Skip.If(
                /* This first condition checks if this is a CI environment. Skip if parameters are not set correctly. */
                (!usePlatformTpm && isCiEnvironment && !RuntimeInformation.IsOSPlatform(OSPlatform.Linux))

                /* And this one if this this is a local Windows environment with simulator. */
                || (!isCiEnvironment && !usePlatformTpm && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows)),
                $"Trust Platform Module (TPM) 2.0 on continuous environment is supported only on {OSPlatform.Linux}.");

            //TODO: Linux simulator for local runs should be added and something like runSettings that makes it
            //easy enough to choose where to run (messing with hardware TPM can cause trouble, so can't be
            //used by default).
            if(!usePlatformTpm && !isCiEnvironment && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Tpm = new TpmSimulatorWrapper();
            }
            else
            {
                //The CI pipeline installs TPM libraries on Linux...
                TpmReal = new TpmWrapper();
            }
        }


        [SkippableFact]
        public void Test1()
        {
            TpmReal?.Tpm.ToString();
        }


        public void Dispose()
        {
            Tpm?.Dispose();
            TpmReal?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
