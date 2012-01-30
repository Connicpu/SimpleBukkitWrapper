using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SBWAPI
{
    public interface IPlugin
    {
        void Load(PluginManager pm);
    }
}
