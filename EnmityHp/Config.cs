using Dalamud.Configuration;
using Dalamud.Interface.GameFonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace EnmityHp
{
    [Serializable]
    class Config : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        public bool IsLeftAligned = false;
        public int OffsetX = 0;
        public int OffsetY = 0;
        public float Size = 1f;
        public Vector4? Color = null;
        public Vector4? BGColor = null;
        public GameFontFamilyAndSize? Font = null;
    }
}
