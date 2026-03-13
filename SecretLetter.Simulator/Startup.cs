using OpenSilver.Simulator;
using System;

namespace SecretLetter.Simulator
{
    internal static class Startup
    {
        [STAThread]
        static int Main(string[] args)
        {
            return SimulatorLauncher.Start(typeof(SecretLetter.App));
        }
    }
}
