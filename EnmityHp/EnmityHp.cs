using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Internal;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.Utility;
using Dalamud.Logging;
using Dalamud.Plugin;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Havok;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace EnmityHp
{
    unsafe class EnmityHp : IDalamudPlugin
    {
        public string Name => "EnemyListHP";
        internal Config Cfg;
        internal static EnmityHp P;
        bool CfgOpen = false;

        public void Dispose()
        {
            Svc.PluginInterface.UiBuilder.Draw -= Draw;
            P = null;
        }

        public EnmityHp(DalamudPluginInterface pluginInterface)
        {
            P = this;
            pluginInterface.Create<Svc>();
            Svc.PluginInterface.UiBuilder.Draw += Draw;
            Cfg = Svc.PluginInterface.GetPluginConfig() as Config ?? new Config();
            Svc.PluginInterface.UiBuilder.OpenConfigUi += delegate { CfgOpen = true; };
            Svc.Framework.RunOnFrameworkThread(() =>
            {
                if (Cfg.Font != null)
                {
                    _ = Svc.PluginInterface.UiBuilder.GetGameFontHandle(new GameFontStyle(Cfg.Font.Value));
                }
            });
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

                    Util.NullColorEdit(ref Cfg.Color, "Customize text color", ImGuiCol.Text);
                    Util.NullColorEdit(ref Cfg.BGColor, "Customize background color", ImGuiCol.WindowBg);

                    var useFont = Cfg.Font != null;
                    if(ImGui.Checkbox($"Use specific font", ref useFont))
                    {
                        if (useFont)
                        {
                            Cfg.Font = GameFontFamilyAndSize.Axis18;
                        }
                        else
                        {
                            Cfg.Font = null;
                        }
                    }
                    if(Cfg.Font != null)
                    {
                        var temp = Cfg.Font.Value;
                        ImGui.SetNextItemWidth(200f);
                        if(Util.EnumCombo("Select font", ref temp, (item) => item != GameFontFamilyAndSize.Undefined))
                        {
                            Cfg.Font = temp;
                        }
                        if (ImGui.CollapsingHeader("Preview font"))
                        {
                            ImGui.PushFont(Svc.PluginInterface.UiBuilder.GetGameFontHandle(new GameFontStyle(Cfg.Font.Value)).ImFont);
                            ImGui.SetWindowFontScale(Cfg.Size);
                            ImGui.TextUnformatted("Preview 100% 50% 1%");
                            ImGui.SetWindowFontScale(1f);
                            ImGui.PopFont();
                        }
                    }

                    Util.ImGuiLineCentered("donate", KoFiButton.DrawButton);
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
                    Svc.Log.Error(e.Message + "\n" + e.StackTrace);
                }
            }
        }

        void DrawEnemyHp(float x, float y, string str)
        {
            if (Cfg.BGColor != null)
            {
                ImGui.PushStyleColor(ImGuiCol.WindowBg, Cfg.BGColor.Value);
            }
            if (Cfg.Font != null)
            {
                ImGui.PushFont(Svc.PluginInterface.UiBuilder.GetGameFontHandle(new GameFontStyle(Cfg.Font.Value)).ImFont);
            }
            ImGuiHelpers.ForceNextWindowMainViewport();
            var textSize = ImGui.CalcTextSize(str) * Cfg.Size;
            ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(x - (Cfg.IsLeftAligned ? 0 : textSize.X) + Cfg.OffsetX, y - textSize.Y / 2f + Cfg.OffsetY));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(2f, 0f));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(0f, 0f));
            ImGui.Begin("##enmityhp" + y, ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoScrollbar
                | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMouseInputs | ImGuiWindowFlags.NoNavFocus
                | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings 
                | ImGuiWindowFlags.NoFocusOnAppearing);
            ImGui.SetWindowFontScale(Cfg.Size);
            if (Cfg.Color != null)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Cfg.Color.Value);
            }
            ImGui.TextUnformatted(str);
            if (Cfg.Color != null)
            {
                ImGui.PopStyleColor();
            }
            ImGui.End();
            if (Cfg.BGColor != null)
            {
                ImGui.PopStyleColor();
            }
            if (Cfg.Font != null)
            {
                ImGui.PopFont();
            }
            ImGui.PopStyleVar(2);
        }
    }
}
