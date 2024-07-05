using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ImGuiNET;
using System;
using ECommons;
using Dalamud.Interface.ImGuiFontChooserDialog;
using System.Threading.Channels;
using ECommons.Reflection;
using Dalamud.Interface;

namespace EnmityHp.Font;

public unsafe class FontManager : IDisposable
{
    public FontConfiguration FontConfiguration;
    private IFontHandle Handle = null;
    private FontManager()
    {
        try
        {
            FontConfiguration = EzConfig.LoadConfiguration<FontConfiguration>("FontConfiguration.json");
        }
        catch (Exception e)
        {
            FontConfiguration = new();
            Notify.Error($"Failed to load font configuration.\nFont settings have been reset.");
            e.Log();
        }
        RebuildHandle();
    }

    public void RebuildHandle()
    {
        try
        {
            Handle?.Dispose();
            Handle = null;
            if (P.Config.UseCustomFont)
            {
                try
                {
                    Handle = FontConfiguration.Font.CreateFontHandle(Svc.PluginInterface.UiBuilder.FontAtlas);
                }
                catch (Exception e)
                {
                    e.Log();
                }
            }
        }
        catch(Exception ex)
        {
            ex.Log();
        }
    }

    public void Save()
    {
        FontConfiguration.SaveConfiguration("FontConfiguration.json");
    }

    public void Dispose()
    {
        Handle?.Dispose();
        Save();
    }

    public bool FontPushed = false;
    public bool FontReady => Handle?.Available == true;

    public void PushFont()
    {
        if (FontPushed)
        {
            PluginLog.Error($"A critical error occurred. Please send logs to developer.");
            throw new InvalidOperationException("Font is already pushed.");
        }
        if (P.Config.UseCustomFont)
        {
            if (Handle != null && Handle.Available)
            {
                Handle.Push();
                FontPushed = true;
            }
        }
    }

    public void PopFont()
    {
        if (FontPushed)
        {
            Handle.Pop();
            FontPushed = false;
        }
    }

    public void DisplayFontSelector()
    {
        var chooser = SingleFontChooserDialog.CreateAuto((UiBuilder)Svc.PluginInterface.UiBuilder);
        if(FontConfiguration.Font is SingleFontSpec spec)
        {
            chooser.SelectedFont = spec;
        }
        chooser.SelectedFontSpecChanged += Chooser_SelectedFontSpecChanged;
    }

    private void Chooser_SelectedFontSpecChanged(SingleFontSpec font)
    {
        FontConfiguration.Font = font;
        Save();
        RebuildHandle();
    }
}