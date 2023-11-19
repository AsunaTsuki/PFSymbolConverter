using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using PFSymbolConverter.Windows;
using System.Data.Common;
using System.Net;
using System.Numerics;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace PFSymbolConverter
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "PF Symbol Convert";
        public string apiResponse;
        private IChatGui _chatGui;
        [PluginService] private static IChatGui ChatGui { get; set; }  // Use Dalamud's IoC container to get the IChatGui service

        private DalamudPluginInterface PluginInterface { get; init; }
        private ICommandManager CommandManager { get; init; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("PF Symbol Convert");

        private ConfigWindow ConfigWindow { get; init; }
        private MainWindow MainWindow { get; init; }

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] ICommandManager commandManager)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            // you might normally want to embed resources and load them from the manifest stream
            var imagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");
            var goatImage = this.PluginInterface.UiBuilder.LoadImage(imagePath);

            ConfigWindow = new ConfigWindow(this);
            MainWindow = new MainWindow(this, goatImage);
            
            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(MainWindow);

            this.CommandManager.AddHandler("/pfconvert", new CommandInfo(PFConvert)
                {
                HelpMessage = "Converts the text passed to this command to party finder symbols instead"
            });

            this.CommandManager.AddHandler("/symbols", new CommandInfo(PFShow)
            {
                HelpMessage = "Shows all available symbols for FFXIV so you can copy paste"
            });

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        }

        public void Dispose()
        {
            this.WindowSystem.RemoveAllWindows();
            
            ConfigWindow.Dispose();
            MainWindow.Dispose();
            
            this.CommandManager.RemoveHandler("/pfshow");
            this.CommandManager.RemoveHandler("/pfconvert");

        }


        private void PFShow(string command, string args)
        {

            string[] symbolLines = new string[]
        {
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            ""
        };

            foreach (string line in symbolLines)
            {
                ChatGui.Print(line);
            }
        }






        private void PFConvert(string command, string args)
        {
            var symbolSquareMappings = new Dictionary<char, char>
        {
            {'a', ''}, {'b', ''}, {'c', ''}, {'d', ''}, {'e', ''},
            {'f', ''}, {'g', ''}, {'h', ''}, {'i', ''}, {'j', ''},
            {'k', ''}, {'l', ''}, {'m', ''}, {'n', ''}, {'o', ''},
            {'p', ''}, {'q', ''}, {'r', ''}, {'s', ''}, {'t', ''},
            {'u', ''}, {'v', ''}, {'w', ''}, {'x', ''}, {'y', ''},
            {'z', ''},
            {'*', '' }, {'?', '' }, {'+', ''},
            {'0', '' }, { '1', ''}, {'2', ''}, {'3', ''}, {'4', ''},
            {'5', ''}, {'6', ''}, {'7', ''}, {'8', ''}, {'9', ''},
        };
            var symbolCircleMappings = new Dictionary<char, char>
        {
            {'a', ''}, {'b', ''}, {'c', ''}, {'d', ''}, {'e', ''},
            {'f', ''}, {'g', ''}, {'h', ''}, {'i', ''}, {'j', ''},
            {'k', ''}, {'l', ''}, {'m', ''}, {'n', ''}, {'o', ''},
            {'p', ''}, {'q', ''}, {'r', ''}, {'s', ''}, {'t', ''},
            {'u', ''}, {'v', ''}, {'w', ''}, {'x', ''}, {'y', ''},
            {'z', ''},
            {'*', '' }, {'?', '' }, {'+', ''},
            {'0', '' }, { '1', ''}, {'2', ''}, {'3', ''}, {'4', ''},
            {'5', ''}, {'6', ''}, {'7', ''}, {'8', ''}, {'9', ''},
        };


            string input = args;
            // string convertedString = ConvertToSymbols(input, symbolMappings);
            //string convertedString = ConvertBrackets(input, symbolMappings);
            string convertedString = ConvertText(input, symbolSquareMappings, symbolCircleMappings);
            ChatGui.Print(convertedString);
        }

        static string ConvertText(string input, Dictionary<char, char> defaultMappings, Dictionary<char, char> specialMappings)
        {
            StringBuilder converted = new StringBuilder();
            var bracketMatches = Regex.Matches(input, @"\[(.*?)\]");

            int lastIndex = 0;
            foreach (Match bracketMatch in bracketMatches)
            {
                converted.Append(input, lastIndex, bracketMatch.Index - lastIndex);

                var bracketContent = bracketMatch.Groups[1].Value;
                var curlyMatches = Regex.Matches(bracketContent, @"\{(.*?)\}");
                int lastCurlyIndex = 0;

                foreach (Match curlyMatch in curlyMatches)
                {
                    // Convert text before curly brace with default mappings
                    converted.Append(ConvertToSymbols(bracketContent.Substring(lastCurlyIndex, curlyMatch.Index - lastCurlyIndex), defaultMappings));
                    // Convert text within curly braces with special mappings
                    converted.Append(ConvertToSymbols(curlyMatch.Groups[1].Value, specialMappings));
                    lastCurlyIndex = curlyMatch.Index + curlyMatch.Length;
                }

                // Convert remaining text after the last curly brace (if any) with default mappings
                if (lastCurlyIndex < bracketContent.Length)
                {
                    converted.Append(ConvertToSymbols(bracketContent.Substring(lastCurlyIndex), defaultMappings));
                }

                lastIndex = bracketMatch.Index + bracketMatch.Length;
            }

            // Append any remaining text after the last bracket
            if (lastIndex < input.Length)
            {
                converted.Append(input.Substring(lastIndex));
            }

            return converted.ToString();
        }

        static string ConvertToSymbols(string input, Dictionary<char, char> mappings)
        {
            StringBuilder converted = new StringBuilder();

            foreach (char c in input.ToLower())
            {
                if (mappings.ContainsKey(c))
                {
                    converted.Append(mappings[c]);
                }
                else
                {
                    converted.Append(c); // Keep the character as it is if not found in the dictionary
                }
            }

            return converted.ToString();
        }

        private void DrawUI()
        {
            this.WindowSystem.Draw();
        }

        public void DrawConfigUI()
        {
            ConfigWindow.IsOpen = true;
        }


    }
}
