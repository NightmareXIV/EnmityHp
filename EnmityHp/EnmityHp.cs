using Dalamud.Game;
using Dalamud.Game.Internal;
using Dalamud.Interface;
using Dalamud.Logging;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace EnmityHp
{
    unsafe class EnmityHp : IDalamudPlugin
    {
        public string Name => "EnmityHp";
        internal List<(float x, float y, string str)> drawList;

        public void Dispose()
        {
            Svc.Framework.Update += Tick;
            Svc.PluginInterface.UiBuilder.Draw += Draw;
        }

        public EnmityHp(DalamudPluginInterface pluginInterface)
        {
            pluginInterface.Create<Svc>();
            drawList = new List<(float x, float y, string str)>();
            Svc.Framework.Update += Tick;
            Svc.PluginInterface.UiBuilder.Draw += Draw;
        }

        [HandleProcessCorruptedStateExceptions]
        private void Tick(Framework framework)
        {
            drawList.Clear();
            try
            {
                var enlist = Svc.GameGui.GetAddonByName("_EnemyList", 1);
                if (enlist != IntPtr.Zero)
                {
                    var enlistAtk = (AtkUnitBase*)enlist;
                    var baseX = enlistAtk->X;
                    var baseY = enlistAtk->Y;
                    if (enlistAtk->IsVisible)
                    {
                        for (int i = 4; i <= 11; i++)
                        {
                            var enemyTile = (AtkComponentNode*)enlistAtk->UldManager.NodeList[i];
                            if (enemyTile->AtkResNode.IsVisible)
                            {
                                var enemyBar = (AtkImageNode*)enemyTile->Component->UldManager.NodeList[8];
                                drawList.Add((enemyTile->AtkResNode.X * enlistAtk->Scale + baseX - 2f,
                                    enemyTile->AtkResNode.Y * enlistAtk->Scale + enemyTile->AtkResNode.Height * enlistAtk->Scale / 2f + baseY,
                                    (enemyBar->AtkResNode.ScaleX * 100f).ToString()+"%"));
                            }

                        }
                    }
                }
            }
            catch (Exception e)
            {
                PluginLog.Error("[EnmityHp] " + e.Message + "\n" + e.StackTrace);
            }
        }

        private void Draw()
        {
            var _ = true;
            for (int i = 0;i<drawList.Count;i++)
            {
                ImGuiHelpers.ForceNextWindowMainViewport();
                var textSize = ImGui.CalcTextSize(drawList[i].str);
                ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(drawList[i].x - textSize.X, drawList[i].y - textSize.Y / 2f));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(2f, 0f));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(0f, 0f));
                ImGui.Begin("##enmityhp"+i, ref _, ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoScrollbar
                    | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMouseInputs | ImGuiWindowFlags.NoNavFocus 
                    | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoTitleBar);
                ImGui.TextUnformatted(drawList[i].str);
                ImGui.End();
                ImGui.PopStyleVar(2);
            }
        }
    }
}
