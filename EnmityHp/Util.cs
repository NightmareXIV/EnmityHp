using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace EnmityHp
{
    internal static class Util
    {
        static readonly Dictionary<string, float> CenteredLineWidths = new();
        public static void ImGuiLineCentered(string id, Action func)
        {
            if (CenteredLineWidths.TryGetValue(id, out var dims))
            {
                ImGui.SetCursorPosX(ImGui.GetContentRegionAvail().X / 2 - dims / 2);
            }
            var oldCur = ImGui.GetCursorPosX();
            func();
            ImGui.SameLine(0, 0);
            CenteredLineWidths[id] = ImGui.GetCursorPosX() - oldCur;
            ImGui.Dummy(Vector2.Zero);
        }
    }
}
