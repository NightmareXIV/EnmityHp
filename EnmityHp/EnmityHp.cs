using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Internal;
using Dalamud.Interface;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Logging;
using Dalamud.Plugin;
using ECommons.ImGuiMethods;
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
        public string Name => "EnemyListHP";
        private Config Cfg;
        bool CfgOpen = false;

        public void Dispose()
        {
            Svc.PluginInterface.UiBuilder.Draw -= Draw;
        }

        public EnmityHp(DalamudPluginInterface pluginInterface)
        {
            pluginInterface.Create<Svc>();
            Svc.PluginInterface.UiBuilder.Draw += Draw;
            Cfg = Svc.PluginInterface.GetPluginConfig() as Config ?? new Config();
            Svc.PluginInterface.UiBuilder.OpenConfigUi += delegate { CfgOpen = true; };
        }
        private void Draw()
        {
            if (CfgOpen)
            {
                if(ImGui.Begin("EnemyListHP configuration", ref CfgOpen, ImGuiWindowFlags.AlwaysAutoResize))
                {
                    ImGui.Checkbox("Left align (right otherwise)", ref Cfg.IsLeftAligned);
                    ImGui.SetNextItemWidth(100f);
                    ImGui.DragInt("X offset", ref Cfg.OffsetX, 0.5f);
                    ImGui.SetNextItemWidth(100f);
                    ImGui.DragInt("Y offset", ref Cfg.OffsetY, 0.5f);
                    ImGui.SetNextItemWidth(100f);
                    ImGui.DragFloat("Scale", ref Cfg.Size, 0.001f);

                    Util.ImGuiLineCentered("donate", delegate
                    {
                        KoFiButton.DrawButton();
                    });
                }
                ImGui.End();
                if (!CfgOpen)
                {
                    Svc.PluginInterface.SavePluginConfig(Cfg);
                    Svc.PluginInterface.UiBuilder.AddNotification("EnemyListHP", "Configuration saved", NotificationType.Success);
                }
            }
            if (Svc.Condition[ConditionFlag.InCombat] && !Svc.ClientState.IsPvP)
            {
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
                                    DrawEnemyHp(enemyTile->AtkResNode.X * enlistAtk->Scale + baseX - 2f,
                                        enemyTile->AtkResNode.Y * enlistAtk->Scale + enemyTile->AtkResNode.Height * enlistAtk->Scale / 2f + baseY,
                                        (enemyBar->AtkResNode.ScaleX * 100f).ToString("0") + "%");
                                }

                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    PluginLog.Error(e.Message + "\n" + e.StackTrace);
                }
            }
        }

        void DrawEnemyHp(float x, float y, string str)
        {
            ImGuiHelpers.ForceNextWindowMainViewport();
            var textSize = ImGui.CalcTextSize(str) * Cfg.Size;
            ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(x - (Cfg.IsLeftAligned ? 0 : textSize.X) + Cfg.OffsetX, y - textSize.Y / 2f + Cfg.OffsetY));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(2f, 0f));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(0f, 0f));
            ImGui.Begin("##enmityhp" + y, ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoScrollbar
                | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMouseInputs | ImGuiWindowFlags.NoNavFocus
                | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings);
            ImGui.SetWindowFontScale(Cfg.Size);
            ImGui.TextUnformatted(str);
            ImGui.End();
            ImGui.PopStyleVar(2);
        }
    }
}
