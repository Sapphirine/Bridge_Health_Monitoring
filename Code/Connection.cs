using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OBrIM;
using Larsa4D;

namespace OBrIMLarsa
{
    public class OBrIMLarsaAgent
    {
        LarsaPlugin plugin;
        Larsa4D.LarsaApp larsa;
        OBrIM.OBrIMConn brim;
        
        bool PerformAnalysis = false;
        OBrIMObj brimProject = null;

        public string Name { get { return "OpenBrIM"; } }
        
        public void Initialize(Larsa4D.LarsaApp larsa, LarsaPlugin plugin)
        {
            this.plugin = plugin;
            this.larsa = larsa;
            brim = new OBrIM.OBrIMConn();
            brim.MessageReceived += this.BrIMMsgReceived;
        }

        public void BrIMMsgReceived(string eventName, object args)
        {
            // OpenBrIM app is fully loaded, we can setup and display OpenBrIM menu in LARSA
            if (eventName == "AppLoaded") {
                this.SetupMenus();
                return; 
            }

            // user logged in successfully, the app is ready
            if (eventName == "UserLoginSuccess") {
                this.UpdateMenuVisibility();
            }

            // user clicked on project to open, we should hide OpenBrIM dialog
            // and wait for project to load and compiled. (waiting for ProjectCompileComplete message)
            if (eventName == "ProjectOpened") { this.brim.AppHide(); }

            // the project is loaded and ready but it is not yet compiled
            if (eventName == "LoadProject") { return; }

            // the project is compiled and the objects are ready
            if (eventName == "ProjectCompileComplete")
            {
                this.RefreshLarsaModel();
                if (this.PerformAnalysis)
                {
                    string projectID = this.brimProject.ID;
                    OBrIMLogger.Info("Saving project " + projectID);
                    this.PerformAnalysis = false;
                    this.larsa.Save(OBrIMLogger.DataFolder() + projectID + ".lar");
                    OBrIMLogger.Info("Project save complete. Running analysis on " + projectID);
                    this.larsa.RunAnalysis(false);
                    OBrIMLogger.Info("Analysis complete for " + projectID);
                    this.larsa.ResultSelectCase("SW");
                    var results = this.larsa.ResultJointDisp(3);
                    string[] resStr = new string[results.Length];
                    for (int i = 0; i < results.Length; i++)
                    {
                        resStr[i] = results[i].ToString();
                    }
                    OBrIMLogger.Info("Joint Disp: " + String.Join(",", resStr));  
                }
            }

            // someone is sending me a direct message
            if (eventName == "AppDirectMsg")
            {
                string arguments = (string)args;
                string[] cols = arguments.Split('|');
                string sender = cols[0];
                string action = cols[1];
                
                // someone wants me to run analysis on a project
                if (action == "RunFEAnalysis") { 
                    string project = cols[2];
                    this.PerformAnalysis = true;
                    OBrIMLogger.Info("Openning project " + project);
                    this.brim.ProjectOpen(project); // first open the project at my side
                }
            }
        }

        public void LarsaToolClicked(string id)
        {
            if (id == "LogIn")
            {
                this.brim.UserLogin();
            }
            else if (id == "LogOut")
            {
                this.brim.UserLogout();
                this.UpdateMenuVisibility();
            }
            else if (id == "Open")
            {
                this.brim.AppShow("openproject");
            }
            else if (id == "DebugWindow")
            {
                this.brim.AppShow("console");
            }
            else
            {
                OBrIMLogger.Info("Tools Clicked: " + id);
            }
        }

        private void SetupMenus()
        {
            this.brim.AppSetLabel("LARSA 4D [" + Environment.MachineName + "]");

            string menuID = this.larsa.AddPluginMenu(this.plugin, "Menu", "OPENBRIM");
            //this.larsa.AddPluginTool(this.plugin, menuID, "LogIn", "Log-In");
            this.larsa.AddPluginTool(this.plugin, menuID, "LogOut", "Log-Out");

            this.larsa.AddPluginTool(this.plugin, menuID, "Open", "Open...", true);
            this.larsa.AddPluginTool(this.plugin, menuID, "DebugWindow", "Debug...", true);

            this.UpdateMenuVisibility();
        }

        private void UpdateMenuVisibility(Object source = null, System.Timers.ElapsedEventArgs e = null)
        {
            bool loggedIn = this.brim.UserIsAuth();
            this.larsa.MenuToolShowHide(this.Name, "LogIn", !loggedIn);
            this.larsa.MenuToolShowHide(this.Name, "LogOut", loggedIn);
        }

        private string ProjectsFolder 
        { 
            get 
            {
                string folder = Environment.SpecialFolder.MyDocuments + "\\OpenBrIM";
                if (!System.IO.Directory.Exists(folder)) System.IO.Directory.CreateDirectory(folder);
                return folder; 
            } 
        }

        private void LarsaOpenProject(string id)
        {
            larsa.Save(this.ProjectsFolder + id + ".lar");
        }

        private void RefreshLarsaModel()
        {
            try
            {
                OBrIMObj prj = this.brim.ObjectGet("").ObjData;

                var nodes = prj.SearchObjectsByType("Node");

                var nodeMap = new Dictionary<string, LarsaJoint>();
                for (int i = 0; i < nodes.Count; i++)
                {
                    LarsaJoint j = new LarsaJoint();
                    j.X = nodes[i].ParamValueAsNumber("X");
                    j.Y = nodes[i].ParamValueAsNumber("Y");
                    j.Z = nodes[i].ParamValueAsNumber("Z");

                    string constraint = "";
                    constraint += nodes[i].ParamValueAsNumber("Tx") == -1 ? "1" : "0";
                    constraint += nodes[i].ParamValueAsNumber("Ty") == -1 ? "1" : "0";
                    constraint += nodes[i].ParamValueAsNumber("Tz") == -1 ? "1" : "0";
                    constraint += nodes[i].ParamValueAsNumber("Rx") == -1 ? "1" : "0";
                    constraint += nodes[i].ParamValueAsNumber("Ry") == -1 ? "1" : "0";
                    constraint += nodes[i].ParamValueAsNumber("Rz") == -1 ? "1" : "0";
                    j.Constraint = constraint;

                    larsa.Joints.Add(j);

                    nodeMap.Add(nodes[i].ID, j);
                }

                var materialMap = new Dictionary<string, LarsaMaterial>();
                var sectionMap = new Dictionary<string, LarsaSection>();

                var lines = prj.SearchObjectsByType("FELine");
                var lineMap = new Dictionary<string, LarsaMember>();
                for (int i = 0; i < lines.Count; i++)
                {
                    LarsaMember m = new LarsaMember();
                    OBrIMObj n1 = lines[i].ParamValueAsObject("Node1");
                    if (n1 != null)
                    {
                        m.IJoint = nodeMap[n1.ID];
                    }

                    OBrIMObj n2 = lines[i].ParamValueAsObject("Node2");
                    if (n2 != null)
                    {
                        m.JJoint = nodeMap[n2.ID];
                    }

                    OBrIMObj brimsec = lines[i].ParamValueAsObject("Section");
                    OBrIMObj brimmat = brimsec.ParamValueAsObject("Material");

                    m.Material = this.GetMaterial(brimmat, materialMap);
                    m.Section1 = this.GetSection(brimsec, sectionMap);

                    larsa.Members.Add(m);

                    lineMap.Add(lines[i].ID, m);
                }

                var surfaces = prj.SearchObjectsByType("FESurface");
                var surfaceMap = new Dictionary<string, LarsaPlate>();
                for (int i = 0; i < surfaces.Count; i++)
                {
                    LarsaPlate s = new LarsaPlate();

                    OBrIMObj n1 = surfaces[i].ParamValueAsObject("Node1");
                    if (n1 != null)
                    {
                        s.IJoint = nodeMap[n1.ID];
                    }

                    OBrIMObj n2 = surfaces[i].ParamValueAsObject("Node2");
                    if (n2 != null)
                    {
                        s.JJoint = nodeMap[n2.ID];
                    }

                    OBrIMObj n3 = surfaces[i].ParamValueAsObject("Node3");
                    if (n3 != null)
                    {
                        s.KJoint = nodeMap[n3.ID];
                    }

                    OBrIMObj n4 = surfaces[i].ParamValueAsObject("Node4");
                    if (n4 != null)
                    {
                        s.LJoint = nodeMap[n4.ID];
                    }

                    OBrIMObj brimmat = surfaces[i].ParamValueAsObject("Material");
                    s.Material = this.GetMaterial(brimmat, materialMap);
                    s.Thickness = surfaces[i].ParamValueAsNumber("Thickness");

                    larsa.Plates.Add(s);

                    surfaceMap.Add(surfaces[i].ID, s);
                }


                var loadcases = prj.SearchObjectsByType("AnalysisCase");
                var loadcaseMap = new Dictionary<string, LarsaLoadCase>();
                for (int i = 0; i < loadcases.Count; i++)
                {
                    LarsaLoadCase lc = new LarsaLoadCase();
                    lc.Name = loadcases[i].Name;
                    double factor = loadcases[i].ParamValueAsNumber("WeightFactor");
                    if (factor != 0)
                    {
                         lc.WeigthFactorZ = -1;
                    }

                    larsa.LoadCases.Add(lc);

                    loadcaseMap.Add(loadcases[i].ID, lc);
                }

                var nodalLoads = prj.SearchObjectsByType("NodeLoad");
                var nodalLoadMap = new Dictionary<string, LarsaJointLoad>();
                for (int i = 0; i < nodalLoads.Count; i++)
                {
                    LarsaJointLoad jl = new LarsaJointLoad();

                    OBrIMObj blc = nodalLoads[i].ParamValueAsObject("LC");
                    if (blc == null) { continue; }
                    LarsaLoadCase lc = loadcaseMap[blc.ID];

                    OBrIMObj bn = nodalLoads[i].ParamValueAsObject("Node");
                    if (bn == null) { continue; }
                    LarsaJoint node = nodeMap[bn.ID];

                    lc.JointLoads.Add(jl);
                    jl.Joint = node;

                    double fx = nodalLoads[i].ParamValueAsNumber("Fx");
                    if (fx != 0) jl.Fx = fx;

                    double fy = nodalLoads[i].ParamValueAsNumber("Fy");
                    if (fy != 0) jl.Fy = fy;

                    double fz = nodalLoads[i].ParamValueAsNumber("Fz");
                    if (fz != 0) jl.Fz = fz;

                    double mx = nodalLoads[i].ParamValueAsNumber("Mx");
                    if (mx != 0) jl.Mx = mx;

                    double my = nodalLoads[i].ParamValueAsNumber("My");
                    if (my != 0) jl.My = my;

                    double mz = nodalLoads[i].ParamValueAsNumber("Mz");
                    if (mz != 0) jl.Mz = mz;
                    
                    nodalLoadMap.Add(nodalLoads[i].ID, jl);
                }

                larsa.Update();
                larsa.GraphicsZoomExtend();

                this.brimProject = prj;
            }
            catch (Exception e)
            {
                OBrIMLogger.Log("Error    -> " + e.ToString());
            }
        }

        public LarsaMaterial FindMaterial(double E, double G, double d)
        {
            for(int i=0; i < larsa.Materials.Count; i++)
            {
                LarsaMaterial mat = larsa.Materials[i];
                if (mat.E == E && mat.G == G && mat.UnitWeight == d)
                    return mat;
            }
            return null;
        }

        public LarsaMaterial GetMaterial(OBrIMObj brimmat, Dictionary<string, LarsaMaterial> materialMap)
        {
            LarsaMaterial mat;
            if (materialMap.ContainsKey(brimmat.ID))
            {
                mat = materialMap[brimmat.ID];
            }
            else
            {
                mat = new LarsaMaterial();
                mat.Name = brimmat.Name;
                var E = brimmat.ParamValueAsNumber("E");
                var G = E / (2 * (brimmat.ParamValueAsNumber("Nu") + 1));
                var d = brimmat.ParamValueAsNumber("d");

                var dmat = this.FindMaterial(E, G, d);
                if (dmat == null)
                {
                    mat.E = E;
                    mat.G = G;
                    mat.UnitWeight = d;
                    larsa.Materials.Add(mat);
                }
                else
                {
                    mat = dmat;
                }
            }

            return mat;
        }

        public LarsaSection FindSection(double Ax, double Ay, double Az, double J, double Iy, double Iz)
        {
            for (int i = 0; i < larsa.Sections.Count; i++)
            {
                LarsaSection sec = larsa.Sections[i];
                if (sec.Area == Ax && sec.ShearAreaY == Ay && sec.ShearAreaZ == Az && sec.J == J && sec.Iy == Iy && sec.Iz == Iz)
                    return sec;
            }
            return null;
        }

        public LarsaSection GetSection(OBrIMObj brimsec, Dictionary<string, LarsaSection> sectionMap)
        {
            LarsaSection sec;
            if (sectionMap.ContainsKey(brimsec.ID))
            {
                sec = sectionMap[brimsec.ID];
            }
            else
            {
                sec = new LarsaSection();
                sec.Name = brimsec.Name;
                var Ax = brimsec.ParamValueAsNumber("Ax");
                var Ay = brimsec.ParamValueAsNumber("Ay");
                var Az = brimsec.ParamValueAsNumber("Az");
                var J = brimsec.ParamValueAsNumber("J");
                var Iy = brimsec.ParamValueAsNumber("Iy");
                var Iz = brimsec.ParamValueAsNumber("Iz");

                var dsec = this.FindSection(Ax, Ay, Az, J, Iy, Iz);
                if (dsec == null)
                {
                    sec.Area = Ax;
                    sec.ShearAreaY = Ay;
                    sec.ShearAreaZ = Az;
                    sec.J = J;
                    sec.Iy = Iy;
                    sec.Iz = Iz;

                    larsa.Sections.Add(sec);
                }
                else
                {
                    sec = dsec;
                }
            }

            return sec;
        }
    }
}
