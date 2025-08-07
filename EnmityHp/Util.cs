using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static EnmityHp.EnmityHp;

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

        public static Dictionary<string, Box<string>> EnumComboSearch = new();
        public static bool EnumCombo<T>(string name, ref T refConfigField, Dictionary<T, string> names) where T : IConvertible
        {
            return EnumCombo(name, ref refConfigField, null, names);
        }

        public static bool EnumCombo<T>(string name, ref T refConfigField, Func<T, bool> filter = null, Dictionary<T, string> names = null) where T : IConvertible
        {
            var ret = false;
            if (ImGui.BeginCombo(name, (names != null && names.TryGetValue(refConfigField, out var n)) ? n : refConfigField.ToString().Replace("_", " ")))
            {
                var values = Enum.GetValues(typeof(T));
                Box<string> fltr = null;
                if (values.Length > 10)
                {
                    if (!EnumComboSearch.ContainsKey(name)) EnumComboSearch.Add(name, new(""));
                    fltr = EnumComboSearch[name];
                    SetNextItemFullWidth();
                    ImGui.InputTextWithHint($"##{name.Replace("#", "_")}", "Filter...", ref fltr.Value, 50);
                }
                foreach (var x in values)
                {
                    var equals = EqualityComparer<T>.Default.Equals((T)x, refConfigField);
                    var element = (names != null && names.TryGetValue((T)x, out n)) ? n : x.ToString().Replace("_", " ");
                    if ((filter == null || filter((T)x))
                        && (fltr == null || element.Contains(fltr.Value, StringComparison.OrdinalIgnoreCase))
                        && ImGui.Selectable(element, equals)
                        )
                    {
                        ret = true;
                        refConfigField = (T)x;
                    }
                    if (ImGui.IsWindowAppearing() && equals) ImGui.SetScrollHereY();
                }
                ImGui.EndCombo();
            }
            return ret;
        }

        public static void SetNextItemFullWidth(int mod = 0)
        {
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X + mod);
        }

        public static void NullColorEdit(ref Vector4? color, string id, ImGuiCol defCol)
        {
            var col = color != null;
            if (ImGui.Checkbox(id, ref col))
            {
                if (col)
                {
                    color = ImGui.GetStyle().Colors[(int)defCol];
                }
                else
                {
                    color = null;
                }
            }
            if (color != null)
            {
                ImGui.SameLine();
                var temp = color.Value;
                if (ImGui.ColorEdit4("##col"+id, ref temp, ImGuiColorEditFlags.NoInputs))
                {
                    color = temp;
                }
            }
        }

        public class Box<T>
        {
            public T Value;

            public Box(T value)
            {
                Value = value;
            }
        }


    }
}
