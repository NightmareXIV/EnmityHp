using Dalamud.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
