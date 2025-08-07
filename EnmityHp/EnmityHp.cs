global using static EnmityHp.EnmityHp;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Utility;
using Dalamud.Plugin;
using ECommons;
using ECommons.DalamudServices;
using ECommons.Funding;
using ECommons.Singletons;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Bindings.ImGui;
using System;
using System.Numerics;
using ECommons.DalamudServices.Legacy;
using ECommons.ImGuiMethods;

namespace EnmityHp
{
    unsafe class EnmityHp : IDalamudPlugin
    {
        public string Name => "EnemyListHP";
        internal Config Config;
        internal static EnmityHp P;
        bool CfgOpen = false;

        public void Dispose()
        {
            Svc.PluginInterface.UiBuilder.Draw -= Draw;
            P = null;
        }

        public EnmityHp(IDalamudPluginInterface pluginInterface)
        {
            P = this;
            ECommonsMain.Init(pluginInterface, this);
            Config = Svc.PluginInterface.GetPluginConfig() as Config ?? new Config();
            SingletonServiceManager.Initialize(typeof(Service));
            Svc.PluginInterface.UiBuilder.Draw += Draw;
            Svc.PluginInterface.UiBuilder.OpenConfigUi += delegate { CfgOpen = true; };
            PatreonBanner.IsOfficialPlugin = () => true;
        }
        private void Draw()
        {
            if (CfgOpen)
            {
                if(ImGui.Begin("EnemyListHP configuration", ref CfgOpen, ImGuiWindowFlags.AlwaysAutoResize))
                {
                    ImGui.Checkbox("Left align (right otherwise)", ref Config.IsLeftAligned);
                    ImGui.SetNextItemWidth(100f);
                    ImGui.DragInt("X offset", ref Config.OffsetX, 0.5f);
                    ImGui.SetNextItemWidth(100f);
                    ImGui.DragInt("Y offset", ref Config.OffsetY, 0.5f);
                    ImGui.SetNextItemWidth(100f);
                    ImGui.DragFloat("Scale", ref Config.Size, 0.001f);

                    Util.NullColorEdit(ref Config.Color, "Customize text color", ImGuiCol.Text);
                    Util.NullColorEdit(ref Config.BGColor, "Customize background color", ImGuiCol.WindowBg);

                    if(ImGui.Checkbox($"Use custom font", ref Config.UseCustomFont))
                    {
                        if (Config.UseCustomFont) Service.FontManager.RebuildHandle();
                    }    
                    if (Config.UseCustomFont)
                    {
                        ImGui.Indent();
                        ImGuiEx.Text($"Current font: {Service.FontManager.FontConfiguration.Font?.ToString() ?? "None"}");
                        if(ImGui.Button("Select font"))
                        {
                            Service.FontManager.DisplayFontSelector();
                        }
                        ImGui.Unindent();
                    }

                    ImGui.SetNextItemWidth(100f);
                    ImGuiEx.SliderFloat("Overlay border", ref Config.FrameBorder, 0f, 5f);
                    if(Config.FrameBorder > 0f)
                    {
                        ImGui.SameLine();
                        ImGui.ColorEdit4("##colfb", ref Config.FrameBorderColor, ImGuiColorEditFlags.NoInputs);
                    }
                    ImGui.SetNextItemWidth(100f);
                    ImGuiEx.SliderFloat("Horizontal padding##x", ref Config.PaddingX, 0f, 10f);
                    ImGui.SetNextItemWidth(100f);
                    ImGuiEx.SliderFloat("Vertical padding##x", ref Config.PaddingY, 0f, 10f);

                    ImGui.NewLine();

                    Util.ImGuiLineCentered("donate", PatreonBanner.DrawRaw);
                }
                ImGui.End();
                if (!CfgOpen)
                {
                    Svc.PluginInterface.SavePluginConfig(Config);
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
                        var enlistAtk = (AtkUnitBase*)enlist.Address;
                        if (enlistAtk->UldManager.NodeListCount < 12) return;
                        var baseX = enlistAtk->X;
                        var baseY = enlistAtk->Y;
                        if (enlistAtk->IsVisible)
                        {
                            for (int i = 4; i <= 11; i++)
                            {
                                var enemyTile = (AtkComponentNode*)enlistAtk->UldManager.NodeList[i];
                                if (enemyTile->AtkResNode.IsVisible())
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
            if (Config.BGColor != null)
            {
                ImGui.PushStyleColor(ImGuiCol.WindowBg, Config.BGColor.Value);
            }
            var useFont = Config.UseCustomFont && Service.FontManager.FontReady;
            if (useFont)
            {
                Service.FontManager.PushFont();
            }
            try
            {
                ImGuiHelpers.ForceNextWindowMainViewport();
                var textSize = ImGui.CalcTextSize(str) * Config.Size;
                ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(x - (Config.IsLeftAligned ? 0 : textSize.X) + Config.OffsetX, y - textSize.Y / 2f + Config.OffsetY));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(Config.PaddingX, Config.PaddingY));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(0f, 0f));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, Config.FrameBorder);
                ImGui.PushStyleColor(ImGuiCol.Border, Config.FrameBorderColor);
                ImGui.Begin("##enmityhp" + y, ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoScrollbar
                    | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMouseInputs | ImGuiWindowFlags.NoNavFocus
                    | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings
                    | ImGuiWindowFlags.NoFocusOnAppearing);
                ImGui.SetWindowFontScale(Config.Size);
                if (Config.Color != null)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Config.Color.Value);
                }
                ImGui.TextUnformatted(str);
                if (Config.Color != null)
                {
                    ImGui.PopStyleColor();
                }
                ImGui.End();
                if (Config.BGColor != null)
                {
                    ImGui.PopStyleColor();
                }
                ImGui.PopStyleColor();
            }
            catch(Exception e)
            {
                e.Log();
            }
            if (useFont)
            {
                Service.FontManager.PopFont();
            }
            ImGui.PopStyleVar(3);
        }
    }
}
