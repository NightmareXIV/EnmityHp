using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Logging;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ECommons.ImGuiMethods
{
    public static class KoFiButton
    {
        public static bool IsOfficialPlugin = false;
        public const string Text = "Support on Ko-fi";
        public const string DonateLink = "https://nightmarexiv.github.io/donate.html?official";

        public static void DrawButton()
        {
            ImGui.PushStyleColor(ImGuiCol.Button, 0xFF942502);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xFF942502);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xFF942502);
            ImGui.PushStyleColor(ImGuiCol.Text, 0xFFFFFFFF);
            if (ImGui.Button(Text))
            {
                try
                {
                    Process.Start(new ProcessStartInfo()
                    {
                        UseShellExecute = true,
                        FileName = DonateLink
                    });
                }
                catch(Exception ex)
                {
                    PluginLog.Error($"{ex.Message}\n{ex.StackTrace}");
                }
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }
            ImGui.PopStyleColor(4);
        }



    }
}
