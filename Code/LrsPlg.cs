using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace OBrIMLarsa
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [GuidAttribute("37016B49-7EB1-4CF8-A97D-B77BBD2690F5"), ComVisible(true)]
    public class LarsaPlugin
    {
        OBrIMLarsaAgent agent = new OBrIMLarsaAgent();

        [ComVisible(true)]
        public void Registered()
        {
            Larsa4D.LarsaApp larsa = new Larsa4D.LarsaApp(Larsa4D.LarsaAppMode.AttachToLARSA);
            this.agent.Initialize(larsa, this);
        }

        [ComVisible(true)]
        public void ToolClicked(ref string id)
        {
            this.agent.LarsaToolClicked(id);
        }

        [ComVisible(true)]
        public void Unregistered() { }
        [ComVisible(true)]
        public object getSpreadInterface(short category, short subcategory, int id) { return null; }
        [ComVisible(true)]
        public void getSpreadTabs(short category, short subcategory, int id, ref Array data) { }
        [ComVisible(true)]
        public string name { get { return this.agent.Name; } }
        [ComVisible(true)]
        public void LarsaEvent(string eventName, object arg1 = null, object arg2 = null, object arg3 = null, object arg4 = null, object arg5 = null) { }
    }
}
