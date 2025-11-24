using System;
using System.Collections.Generic;
using SabreTools.CommandLine;
using SabreTools.CommandLine.Features;
using WiseUnpacker.Features;

namespace WiseUnpacker
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            // Create the command set
            var mainFeature = new MainFeature();
            var commandSet = CreateCommands(mainFeature);

            // If we have no args, show the help and quit
            if (args == null || args.Length == 0)
            {
                commandSet.OutputAllHelp();
                return;
            }

            // Cache the first argument and starting index
            string featureName = args[0];

            // Try processing the standalone arguments
            var topLevel = commandSet.GetTopLevel(featureName);
            switch (topLevel)
            {
                // Standalone Options
                case Help help: help.ProcessArgs(args, 0, commandSet); return;

                // Default Behavior
                default:
                    if (!mainFeature.ProcessArgs(args, 0))
                    {
                        commandSet.OutputAllHelp();
                        return;
                    }
                    else if (!mainFeature.VerifyInputs())
                    {
                        Console.Error.WriteLine("At least one input is required");
                        commandSet.OutputAllHelp();
                        return;
                    }

                    mainFeature.Execute();
                    break;
            }
        }

        /// <summary>
        /// Create the command set for the program
        /// </summary>
        private static CommandSet CreateCommands(MainFeature mainFeature)
        {
            List<string> header = [
                "Wise Installer Reference Implementation",
                string.Empty,
                "WiseUnpacker <options> file|directory ...",
                string.Empty,
            ];

            var commandSet = new CommandSet(header);

            commandSet.Add(new Help(["-?", "-h", "--help"]));
            commandSet.Add(mainFeature.DebugInput);
            commandSet.Add(mainFeature.InfoInput);
            commandSet.Add(mainFeature.ExtractInput);
            commandSet.Add(mainFeature.OutputPathInput);

            return commandSet;
        }
    }
}
