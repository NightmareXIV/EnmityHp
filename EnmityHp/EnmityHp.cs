using Dalamud.Game;
using Dalamud.Game.Internal;
using Dalamud.Interface;
using Dalamud.Interface.Internal.Notifications;
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
        private Config Cfg;
        bool CfgOpen = false;

        public void Dispose()
        {
            Svc.Framework.Update -= Tick;
            Svc.PluginInterface.UiBuilder.Draw -= Draw;
        }

        public EnmityHp(DalamudPluginInterface pluginInterface)
        {
            pluginInterface.Create<Svc>();
            drawList = new List<(float x, float y, string str)>();
            Svc.Framework.Update += Tick;
            Svc.PluginInterface.UiBuilder.Draw += Draw;
            Cfg = Svc.PluginInterface.GetPluginConfig() as Config ?? new Config();
            Svc.PluginInterface.UiBuilder.OpenConfigUi += delegate { CfgOpen = true; };
        }

        [HandleProcessCorruptedStateExceptions]
        private void Tick(Framework framework)
        {
            drawList.Clear();
            if (!Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat]) return;
            try
            {
                var enlist = Svc.GameGui.GetAddonByName("_EnemyList", 1);
                if (enlist != IntPtr.Zero)
                {
                    var enlistAtk = (AtkUnitBase*)enlist;
                    if (enlistAtk->UldManager.NodeListCount < 12) return;
                    var baseX = enlistAtk->X;
                    var baseY = enlistAtk->Y;
                    if (enlistAtk->IsVisible)
                    {
                        for (int i = 4; i <= 11; i++)
                        {
                            var enemyTile = (AtkComponentNode*)enlistAtk->UldManager.NodeList[i];
                            if (enemyTile->AtkResNode.IsVisible)
                            {
                                if (enemyTile->Component->UldManager.NodeListCount < 11) continue;
                                var enemyBar = (AtkImageNode*)enemyTile->Component->UldManager.NodeList[10];
                                drawList.Add((enemyTile->AtkResNode.X * enlistAtk->Scale + baseX - 2f,
                                    enemyTile->AtkResNode.Y * enlistAtk->Scale + enemyTile->AtkResNode.Height * enlistAtk->Scale / 2f + baseY,
                                    (enemyBar->AtkResNode.ScaleX * 100f).ToString("0")+"%"));
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
            if (CfgOpen)
            {
                if(ImGui.Begin("EnmityHp configuration", ref CfgOpen, ImGuiWindowFlags.AlwaysAutoResize))
                {
                    ImGui.Checkbox("Left align (right otherwise)", ref Cfg.IsLeftAligned);
                    ImGui.SetNextItemWidth(100f);
                    ImGui.DragInt("X offset", ref Cfg.OffsetX, 0.5f);
                    ImGui.SetNextItemWidth(100f);
                    ImGui.DragInt("Y offset", ref Cfg.OffsetY, 0.5f);
                    ImGui.SetNextItemWidth(100f);
                    ImGui.DragFloat("Scale", ref Cfg.Size, 0.001f);
                }
                ImGui.End();
                if (!CfgOpen)
                {
                    Svc.PluginInterface.SavePluginConfig(Cfg);
                    Svc.PluginInterface.UiBuilder.AddNotification("EnmityHp", "Configuration saved", NotificationType.Success);
                }
            }
            for (int i = 0;i<drawList.Count;i++)
            {
                ImGuiHelpers.ForceNextWindowMainViewport();
                var textSize = ImGui.CalcTextSize(drawList[i].str) * Cfg.Size;
                ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(drawList[i].x - (Cfg.IsLeftAligned?0:textSize.X) + Cfg.OffsetX, drawList[i].y - textSize.Y / 2f + Cfg.OffsetY));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(2f, 0f));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(0f, 0f));
                ImGui.Begin("##enmityhp"+i, ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoScrollbar
                    | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMouseInputs | ImGuiWindowFlags.NoNavFocus 
                    | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings);
                ImGui.SetWindowFontScale(Cfg.Size);
                ImGui.TextUnformatted(drawList[i].str);
                ImGui.End();
                ImGui.PopStyleVar(2);
            }
        }
    }
}
