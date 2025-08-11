using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.GraphicsInterface;
using Teigha.GraphicsSystem;
using Teigha.Runtime;
using Exception = System.Exception;

namespace EM.CAD
{
    public class Aux
    {
        /// <summary>
        /// 活动的视口ID
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        public static ObjectId Active_viewport_id(Database database)
        {
            if (database.TileMode)
            {
                return database.CurrentViewportTableRecordId;
            }
            else
            {
                using (BlockTableRecord paperBTR = (BlockTableRecord)database.CurrentSpaceId.GetObject(OpenMode.ForRead))
                {
                    Layout l = (Layout)paperBTR.LayoutId.GetObject(OpenMode.ForRead);
                    return l.CurrentViewportId;
                }
            }
        }
        /// <summary>
        /// 预览图类型
        /// </summary>
        /// <param name="database"></param>
        /// <param name="ctx"></param>
        public static void preparePlotstyles(Database database, ContextForDbDatabase ctx)
        {
            using (BlockTableRecord paperBTR = (BlockTableRecord)database.CurrentSpaceId.GetObject(OpenMode.ForRead))
            {
                //通过块表记录得到布局
                using (Layout pLayout = (Layout)paperBTR.LayoutId.GetObject(OpenMode.ForRead))
                {
                    if (ctx.IsPlotGeneration ? pLayout.PlotPlotStyles : pLayout.ShowPlotStyles)
                    {
                        string pssFile = pLayout.CurrentStyleSheet;
                        if (pssFile.Length > 0)
                        {
                            string testpath = ((HostAppServ)HostApplicationServices.Current).FindFile(pssFile, database, FindFileHint.Default);
                            if (testpath.Length > 0)
                            {
                                using (FileStreamBuf pFileBuf = new FileStreamBuf(testpath))
                                {
                                    ctx.LoadPlotStyleTable(pFileBuf);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public abstract class CadFunction : ICadFunction, IDisposable
        {
        public System.Drawing.Image ButtonImage { get; set; }

        public Bitmap CursorBitmap { get; set; }

        public bool Enabled { get; protected set; }

        public ICadControl CadControl { get; set; }
        public string Name { get; set; }

        public YieldStyles YieldStyle { get; set; }

        public event EventHandler FunctionActivated;
        public event EventHandler FunctionDeactivated;
        public event EventHandler<KeyEventArgs> KeyUp;
        public event EventHandler<MouseEventArgs> MouseDoubleClick;
        public event EventHandler<MouseEventArgs> MouseDown;
        public event EventHandler<MouseEventArgs> MouseMove;
        public event EventHandler<MouseEventArgs> MouseUp;
        public event EventHandler<MouseEventArgs> MouseWheel;

        public CadFunction(ICadControl cadControl)
            {
            CadControl = cadControl;
            }
        public virtual void Activate()
            {
            Enabled = true;
            FunctionActivated?.Invoke(this, EventArgs.Empty);
            }

        public virtual void Deactivate()
            {
            Enabled = false;
            FunctionDeactivated?.Invoke(this, EventArgs.Empty);
            }

        public virtual void DoKeyDown(KeyEventArgs e)
            {
            }

        public virtual void DoKeyUp(KeyEventArgs e)
            {
            KeyUp?.Invoke(this, e);
            }

        public virtual void DoMouseDoubleClick(MouseEventArgs e)
            {
            MouseDoubleClick?.Invoke(this, e);
            }

        public virtual void DoMouseDown(MouseEventArgs e)
            {
            MouseDown?.Invoke(this, e);
            }

        public virtual void DoMouseMove(MouseEventArgs e)
            {
            MouseMove?.Invoke(this, e);
            }

        public virtual void DoMouseUp(MouseEventArgs e)
            {
            MouseUp?.Invoke(this, e);
            }

        public virtual void DoMouseWheel(MouseEventArgs e)
            {
            MouseWheel?.Invoke(this, e);
            }

        public virtual void Unload()
            {
            }

        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
            {
            if (!disposedValue)
                {
                if (disposing)
                    {
                    // TODO: 释放托管状态(托管对象)。
                    ButtonImage?.Dispose();
                    ButtonImage = null;
                    CursorBitmap?.Dispose();
                    CursorBitmap = null;
                    }

                // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
                // TODO: 将大型字段设置为 null。

                disposedValue = true;
                }
            }

        // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
        // ~CadFunction()
        // {
        //   // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
        //   Dispose(false);
        // }

        // 添加此代码以正确实现可处置模式。
        public void Dispose()
            {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
            // TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
            // GC.SuppressFinalize(this);
            }
        #endregion
        }

    public static class Configuration
        {
        static Services _services;
        /// <summary>
        /// 配置Teigha环境
        /// </summary>
        public static void Configure()
            {
            if (_services == null)
                {
                _services = new Services();
                SystemObjects.DynamicLinker.LoadApp("GripPoints", false, false);
                SystemObjects.DynamicLinker.LoadApp("PlotSettingsValidator", false, false);
                HostAppServ hostAppServ = new HostAppServ(_services);
                HostApplicationServices.Current = hostAppServ;
                Environment.SetEnvironmentVariable("DDPLOTSTYLEPATHS", hostAppServ.FindConfigPath(string.Format("PrinterStyleSheetDir")));
                }
            }
        /// <summary>
        /// 关闭服务并释放资源（调用之前必须释放Teigha的所有资源）
        /// </summary>
        public static void Close()
            {
            if (_services != null)
                {
                HostApplicationServices.Current.Dispose();
                _services.Dispose();
                _services = null;
                }
            }
        }

    public interface ICadControl : IDisposable
        {
        Rectangle View { get; }
        BoundBlock3d ViewExtent { get; set; }
        string FileName { get; }
        LayoutHelperDevice HelperDevice { get; }
        Database Database { get; }
        List<ICadFunction> CadFunctions { get; }
        void Open(string fileName, FileOpenMode fileOpenMode);
        void Close();
        void Invalidate();
        void Invalidate(Rectangle clipRectangle);
        Point3d PixelToWorld(Point point);
        BoundBlock3d PixelToWorld(Rectangle rectangle);
        Point WorldToPixel(Point3d point3D);
        Rectangle WorldToPixel(BoundBlock3d boundBlock3D);
        void ActivateCadFunction(ICadFunction function);
        ObjectIdCollection GetSelection(Point location, Teigha.GraphicsSystem.SelectionMode selectionMode);
        }

    public interface ICadFunction
        {
        #region Events

        /// <summary>
        /// Occurs when the function is activated
        /// </summary>
        event EventHandler FunctionActivated;

        /// <summary>
        /// Occurs when the function is deactivated.
        /// </summary>
        event EventHandler FunctionDeactivated;

        #endregion

        #region Properties

        /// <summary>
        /// Gets a button image.
        /// </summary>
        System.Drawing.Image ButtonImage { get; }

        /// <summary>
        /// Gets or sets the cursor that this tool uses, unless the action has been cancelled by attempting
        /// to use the tool outside the bounds of the image.
        /// </summary>
        Bitmap CursorBitmap { get; set; }

        /// <summary>
        /// Gets a value indicating whether this tool should be active. If it is false,
        /// then this tool will not be sent mouse movement information.
        /// </summary>
        bool Enabled { get; }

        /// <summary>
        /// Gets or sets the basic map that this tool interacts with. This can alternately be set using
        /// the Init method.
        /// </summary>
        ICadControl CadControl { get; set; }

        /// <summary>
        /// Gets or sets the name that attempts to identify this plugin uniquely. If the
        /// name is already in the tools list, this will modify the name set here by appending a number.
        /// </summary>
        string Name { get; set; }


        /// <summary>
        /// Gets or sets the yield style. Different Pathways that allow functions to deactivate if another function that uses
        /// the specified UI domain activates. This allows a scrolling zoom function to stay
        /// active while changing between pan and select functions which use the left mouse
        /// button. The enumeration is flagged, and so can support multiple options.
        /// </summary>
        YieldStyles YieldStyle { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Forces activation
        /// </summary>
        void Activate();

        /// <summary>
        /// Forces deactivation.
        /// </summary>
        void Deactivate();

        /// <summary>
        /// When a key is pressed while the map has the focus, this occurs.
        /// </summary>
        /// <param name="e">The event args.</param>
        void DoKeyDown(KeyEventArgs e);

        /// <summary>
        /// When a key returns to the up position, this occurs.
        /// </summary>
        /// <param name="e">The event args.</param>
        void DoKeyUp(KeyEventArgs e);

        /// <summary>
        /// Forces this tool to execute whatever behavior should occur during a double click even on the panel
        /// </summary>
        /// <param name="e">The event args.</param>
        void DoMouseDoubleClick(MouseEventArgs e);

        /// <summary>
        /// Instructs this tool to perform any actions that should occur on the MouseDown event
        /// </summary>
        /// <param name="e">A MouseEventArgs relative to the drawing panel</param>
        void DoMouseDown(MouseEventArgs e);

        /// <summary>
        /// Instructs this tool to perform any actions that should occur on the MouseMove event
        /// </summary>
        /// <param name="e">A MouseEventArgs relative to the drawing panel</param>
        void DoMouseMove(MouseEventArgs e);

        /// <summary>
        /// Instructs this tool to perform any actions that should occur on the MouseUp event
        /// </summary>
        /// <param name="e">A MouseEventArgs relative to the drawing panel</param>
        void DoMouseUp(MouseEventArgs e);

        /// <summary>
        /// Instructs this tool to perform any actions that should occur on the MouseWheel event
        /// </summary>
        /// <param name="e">A MouseEventArgs relative to the drawing panel</param>
        void DoMouseWheel(MouseEventArgs e);

        ///// <summary>
        ///// This is the method that is called by the drawPanel. The graphics coordinates are
        ///// in pixels relative to the image being edited.
        ///// </summary>
        ///// <param name="e">The event args.</param>
        //void Draw(MapDrawArgs e);

        /// <summary>
        /// Here, the entire plugin is unloading, so if there are any residual states
        /// that are not taken care of, this should remove them.
        /// </summary>
        void Unload();

        #endregion
        }

    class HostAppServ : HostApplicationServices
        {
        Teigha.Runtime.Services dd;
        public HostAppServ(Teigha.Runtime.Services serv)
            {
            dd = serv;
            }

        public string FindConfigPath(string configType)
            {
            string subkey = GetRegistryAcadProfilesKey();
            if (subkey.Length > 0)
                {
                subkey += string.Format("\\General");
                string searchPath;
                if (GetRegistryString(Registry.CurrentUser, subkey, configType, out searchPath))
                    return searchPath;
                }
            return string.Format("");
            }

        private string FindConfigFile(string configType, string file)
            {
            string searchPath = FindConfigPath(configType);
            if (searchPath.Length > 0)
                {
                searchPath = string.Format("{0}\\{1}", searchPath, file);
                if (dd.AccessFileRead(searchPath))
                    return searchPath;
                }
            return string.Format("");
            }

        public override string FindFile(string file, Database db, FindFileHint hint)
            {
            string sFile = this.FindFileEx(file, db, hint);
            if (sFile.Length > 0)
                return sFile;

            string strFileName = file;
            string ext;
            if (strFileName.Length > 3)
                ext = strFileName.Substring(strFileName.Length - 4, 4).ToUpper();
            else
                ext = file.ToUpper();
            if (ext == string.Format(".PC3"))
                return FindConfigFile(string.Format("PrinterConfigDir"), file);
            if (ext == string.Format(".STB") || ext == string.Format(".CTB"))
                return FindConfigFile(string.Format("PrinterStyleSheetDir"), file);
            if (ext == string.Format(".PMP"))
                return FindConfigFile(string.Format("PrinterDescDir"), file);

            switch (hint)
                {
                case FindFileHint.FontFile:
                case FindFileHint.CompiledShapeFile:
                case FindFileHint.TrueTypeFontFile:
                case FindFileHint.PatternFile:
                case FindFileHint.FontMapFile:
                case FindFileHint.TextureMapFile:
                    break;
                default:
                    return sFile;
                }

            if (hint != FindFileHint.TextureMapFile && ext != string.Format(".SHX") && ext != string.Format(".PAT") && ext != string.Format(".TTF") && ext != string.Format(".TTC"))
                {
                strFileName += string.Format(".shx");
                }
            else if (hint == FindFileHint.TextureMapFile)
                {
                strFileName.Replace(string.Format("/"), string.Format("\\"));
                int last = strFileName.LastIndexOf("\\");
                strFileName = strFileName.Substring(0, last);
                }


            sFile = (hint != FindFileHint.TextureMapFile) ? GetRegistryACADFromProfile() : GetRegistryAVEMAPSFromProfile();
            while (sFile.Length > 0)
                {
                int nFindStr = sFile.IndexOf(";");
                string sPath;
                if (-1 == nFindStr)
                    {
                    sPath = sFile;
                    sFile = string.Format("");
                    }
                else
                    {
                    sPath = string.Format("{0}\\{1}", sFile.Substring(0, nFindStr), strFileName);
                    if (dd.AccessFileRead(sPath))
                        {
                        return sPath;
                        }
                    sFile = sFile.Substring(nFindStr + 1, sFile.Length - nFindStr - 1);
                    }
                }

            if (hint == FindFileHint.TextureMapFile)
                {
                return sFile;
                }

            if (sFile.Length <= 0)
                {
                string sAcadLocation = GetRegistryAcadLocation();
                if (sAcadLocation.Length > 0)
                    {
                    sFile = string.Format("{0}\\Fonts\\{1}", sAcadLocation, strFileName);
                    if (dd.AccessFileRead(sFile))
                        {
                        sFile = string.Format("{0}\\Support\\{1}", sAcadLocation, strFileName);
                        if (dd.AccessFileRead(sFile))
                            {
                            sFile = string.Format("");
                            }
                        }
                    }
                }
            return sFile;
            }

        public override string FontMapFileName
            {
            get
                {
                string subkey = GetRegistryAcadProfilesKey();
                if (subkey.Length > 0)
                    {
                    subkey += string.Format("\\Editor Configuration");
                    string fontMapFile;
                    if (GetRegistryString(Registry.CurrentUser, subkey, string.Format("FontMappingFile"), out fontMapFile))
                        return fontMapFile;
                    }
                return string.Format("");
                }
            }

        bool GetRegistryString(RegistryKey rKey, string subkey, string name, out string value)
            {
            bool rv = false;
            object objData = null;

            RegistryKey regKey;
            regKey = rKey.OpenSubKey(subkey);
            if (regKey != null)
                {
                objData = regKey.GetValue(name);
                if (objData != null)
                    {
                    rv = true;
                    }
                regKey.Close();
                }
            if (rv)
                value = objData.ToString();
            else
                value = string.Format("");

            rKey.Close();
            return rv;
            }

        string GetRegistryAVEMAPSFromProfile()
            {
            string subkey = GetRegistryAcadProfilesKey();
            if (subkey.Length > 0)
                {
                subkey += string.Format("\\General");
                // get the value for the ACAD entry in the registry
                string tmp;
                if (GetRegistryString(Registry.CurrentUser, subkey, string.Format("AVEMAPS"), out tmp))
                    return tmp;
                }
            return string.Format("");
            }

        string GetRegistryAcadProfilesKey()
            {
            string subkey = string.Format("SOFTWARE\\Autodesk\\AutoCAD");
            string tmp;

            if (!GetRegistryString(Registry.CurrentUser, subkey, string.Format("CurVer"), out tmp))
                return string.Format("");
            subkey += string.Format("\\{0}", tmp);

            if (!GetRegistryString(Registry.CurrentUser, subkey, string.Format("CurVer"), out tmp))
                return string.Format("");
            subkey += string.Format("\\{0}\\Profiles", tmp);

            if (!GetRegistryString(Registry.CurrentUser, subkey, string.Format(""), out tmp))
                return string.Format("");
            subkey += string.Format("\\{0}", tmp);
            return subkey;
            }

        string GetRegistryAcadLocation()
            {
            string subkey = string.Format("SOFTWARE\\Autodesk\\AutoCAD");
            string tmp;

            if (!GetRegistryString(Registry.CurrentUser, subkey, string.Format("CurVer"), out tmp))
                return string.Format("");
            subkey += string.Format("\\{0}", tmp);

            if (!GetRegistryString(Registry.CurrentUser, subkey, string.Format("CurVer"), out tmp))
                return string.Format("");
            subkey += string.Format("\\{0}", tmp);

            if (!GetRegistryString(Registry.CurrentUser, subkey, string.Format(""), out tmp))
                return string.Format("");
            return tmp;
            }

        string GetRegistryACADFromProfile()
            {
            string subkey = GetRegistryAcadProfilesKey();
            if (subkey.Length > 0)
                {
                subkey += string.Format("\\General");
                // get the value for the ACAD entry in the registry
                string tmp;
                if (GetRegistryString(Registry.CurrentUser, subkey, string.Format("ACAD"), out tmp))
                    return tmp;
                }
            return string.Format("");
            }
        };

    public class Selector : SelectionReactor
        {
        ObjectIdCollection _objectIdCollection;
        ObjectId _spaceId;
        public Selector(ObjectIdCollection objectIdCollection, ObjectId spaceId)
            {
            _spaceId = spaceId;
            _objectIdCollection = objectIdCollection;
            }
        public override bool Selected(DrawableDesc pDrawableDesc)
            {
            DrawableDesc pDesc = pDrawableDesc;
            if (pDesc.Parent != null)
                {
                // we walk up the GS node path to the root container primitive
                // to avoid e.g. selection of individual lines in a dimension 
                while (((DrawableDesc)pDesc.Parent).Parent != null)
                    pDesc = (DrawableDesc)pDesc.Parent;
                if (pDesc.PersistId != IntPtr.Zero && ((DrawableDesc)pDesc.Parent).PersistId == _spaceId.OldIdPtr)
                    {
                    pDesc.MarkedToSkip = true; // regen abort for selected drawable, to avoid duplicates
                    bool containItem = false;
                    foreach (ObjectId objectId in _objectIdCollection)
                        {
                        if (objectId.OldIdPtr == pDesc.PersistId)
                            {
                            containItem = true;
                            break;
                            }
                        }
                    if (!containItem)
                        {
                        _objectIdCollection.Add(new ObjectId(pDesc.PersistId));
                        }
                    }
                return true;
                }
            return false;
            }
        // this more informative callback may be used to implement subentities selection
        public override SelectionReactorResult Selected(PathNode pthNode, Teigha.GraphicsInterface.Viewport viewInfo)
            {
            return SelectionReactorResult.NotImplemented;
            }

        }

    public static class TeighaExtension
        {
        public static ObjectId GetActiveViewportId(this Database database)
            {
            if (database.TileMode)
                {
                return database.CurrentViewportTableRecordId;
                }
            else
                {
                using (BlockTableRecord paperBTR = (BlockTableRecord)database.CurrentSpaceId.GetObject(OpenMode.ForRead))
                    {
                    Layout l = (Layout)paperBTR.LayoutId.GetObject(OpenMode.ForRead);
                    return l.CurrentViewportId;
                    }
                }
            }
        public static void Transform(this Teigha.GraphicsSystem.View pView, Point2d startPoint, Point2d endPoint)
            {
            double dx = endPoint.X - startPoint.X;
            double dy = endPoint.Y - startPoint.Y;
            Transform(pView, dx, dy);
            }
        // helper function transforming parameters from screen to world coordinates
        public static void Transform(this Teigha.GraphicsSystem.View pView, double x, double y)
            {
            Vector3d vec = new Vector3d(-x, -y, 0.0);
            vec = vec.TransformBy((pView.ScreenMatrix * pView.ProjectionMatrix).Inverse());
            pView.Dolly(vec);
            }
        public static void ZoomMap(Teigha.GraphicsSystem.View pView, Point2d centerPoint, double zoomFactor)
            {
            // camera position in world coordinates
            Point3d pos = pView.Position;
            // TransformBy() returns a transformed copy
            pos = pos.TransformBy(pView.WorldToDeviceMatrix);
            double vx = (int)pos.X;
            double vy = (int)pos.Y;
            vx = centerPoint.X - vx;
            vy = centerPoint.Y - vy;
            // we move point of view to the mouse location, to create an illusion of scrolling in/out there
            Transform(pView, -vx, -vy);
            // note that we essentially ignore delta value (sign is enough for illustrative purposes)
            pView.Zoom(zoomFactor);
            Transform(pView, vx, vy);
            }
        public static bool GetLayoutExtents(Database db, Teigha.GraphicsSystem.View pView, ref BoundBlock3d bbox)
            {
            Extents3d ext = new Extents3d();
            using (BlockTable bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead))
                {
                using (BlockTableRecord pSpace = (BlockTableRecord)bt[BlockTableRecord.PaperSpace].GetObject(OpenMode.ForRead))
                    {
                    using (Layout pLayout = (Layout)pSpace.LayoutId.GetObject(OpenMode.ForRead))
                        {
                        if (pLayout.GetViewports().Count > 0)
                            {
                            bool bOverall = true;
                            foreach (ObjectId id in pLayout.GetViewports())
                                {
                                if (bOverall)
                                    {
                                    bOverall = false;
                                    continue;
                                    }
                                //Viewport pVp = (Viewport)id.GetObject(OpenMode.ForRead);
                                }
                            ext.TransformBy(pView.ViewingMatrix);
                            bbox.Set(ext.MinPoint, ext.MaxPoint);
                            }
                        else
                            {
                            ext = pLayout.Extents;
                            }
                        bbox.Set(ext.MinPoint, ext.MaxPoint);
                        }
                    }
                }

            return ext.MinPoint != ext.MaxPoint;
            }
        public static BoundBlock3d GetLayoutExtents(this LayoutHelperDevice helperDevice)
            {
            BoundBlock3d boundBlock3D = new BoundBlock3d();
            using (Teigha.GraphicsSystem.View pView = helperDevice.ActiveView)
                {
                // camera position in world coordinates
                Point3d pos = pView.Position;
                double halfWidth = pView.FieldWidth / 2;
                double halfHeight = pView.FieldHeight / 2;
                double xMin = pos.X - halfWidth;
                double xMax = pos.X + halfWidth;
                double yMin = pos.Y - halfHeight;
                double yMax = pos.Y + halfHeight;
                boundBlock3D.Set(new Point3d(xMin, yMin, 0), new Point3d(xMax, yMax, 0));
                }
            return boundBlock3D;
            }

        public static void ZoomToExtents(this Database database)
            {
            using (DBObject dbObj = GetActiveViewportId(database).GetObject(OpenMode.ForWrite))
                {
                // using protocol extensions we handle PS and MS viewports in the same manner
                using (AbstractViewportData viewportData = new AbstractViewportData(dbObj))
                    {
                    using (Teigha.GraphicsSystem.View view = viewportData.GsView)
                        {
                        // do actual zooming - change GS view
                        using (AbstractViewPE viewPE = new AbstractViewPE(view))
                            {
                            BoundBlock3d boundBlock = new BoundBlock3d();
                            bool bBboxValid = viewPE.GetViewExtents(boundBlock);
                            // paper space overall view
                            if (dbObj is Teigha.DatabaseServices.Viewport && ((Teigha.DatabaseServices.Viewport)dbObj).Number == 1)
                                {
                                if (!bBboxValid || !(boundBlock.GetMinimumPoint().X < boundBlock.GetMaximumPoint().X && boundBlock.GetMinimumPoint().Y < boundBlock.GetMaximumPoint().Y))
                                    {
                                    bBboxValid = GetLayoutExtents(database, view, ref boundBlock);
                                    }
                                }
                            else if (!bBboxValid) // model space viewport
                                {
                                bBboxValid = GetLayoutExtents(database, view, ref boundBlock);
                                }
                            if (!bBboxValid)
                                {
                                // set to somewhat reasonable (e.g. paper size)
                                if (database.Measurement == MeasurementValue.Metric)
                                    {
                                    boundBlock.Set(Point3d.Origin, new Point3d(297.0, 210.0, 0.0)); // set to papersize ISO A4 (portrait)
                                    }
                                else
                                    {
                                    boundBlock.Set(Point3d.Origin, new Point3d(11.0, 8.5, 0.0)); // ANSI A (8.50 x 11.00) (landscape)
                                    }
                                boundBlock.TransformBy(view.ViewingMatrix);
                                }
                            viewPE.ZoomExtents(boundBlock);
                            boundBlock.Dispose();
                            }
                        // save changes to database
                        viewportData.SetView(view);
                        }
                    }
                }
            }

        public static void Zoom(this Database database, BoundBlock3d box)
            {
            using (var vtr = (ViewportTableRecord)database.CurrentViewportTableRecordId.GetObject(OpenMode.ForWrite))
                {
                // using protocol extensions we handle PS and MS viewports in the same manner
                using (var vpd = new AbstractViewportData(vtr))
                    {
                    var view = vpd.GsView;
                    // do actual zooming - change GS view
                    // here protocol extension is used again, that provides some helpful functions
                    using (var vpe = new AbstractViewPE(view))
                        {
                        using (BoundBlock3d boundBlock3D = (BoundBlock3d)box.Clone())
                            {
                            boundBlock3D.TransformBy(view.ViewingMatrix);
                            vpe.ZoomExtents(boundBlock3D);
                            }
                        }
                    vpd.SetView(view);
                    }
                }
            //ReSize();
            }

        public static void Zoom(this Database database, Extents3d ext)
            {
            BoundBlock3d box = new BoundBlock3d();
            box.Set(ext.MinPoint, ext.MaxPoint);
            Zoom(database, box);
            }

        public static void Zoom(this Database database, Point3d minPoint, Point3d maxPoint)
            {
            BoundBlock3d box = new BoundBlock3d();
            box.Set(minPoint, maxPoint);
            Zoom(database, box);
            }
        /// <summary>
        /// 长度
        /// </summary>
        /// <param name="boundBlock3D"></param>
        /// <returns></returns>
        public static double Width(this BoundBlock3d boundBlock3D)
            {
            double value = 0;
            if (boundBlock3D != null)
                {
                value = boundBlock3D.GetMaximumPoint().X - boundBlock3D.GetMinimumPoint().X;
                }
            return value;
            }
        /// <summary>
        /// 宽度
        /// </summary>
        /// <param name="boundBlock3D"></param>
        /// <returns></returns>
        public static double Height(this BoundBlock3d boundBlock3D)
            {
            double value = 0;
            if (boundBlock3D != null)
                {
                value = boundBlock3D.GetMaximumPoint().Y - boundBlock3D.GetMinimumPoint().Y;
                }
            return value;
            }
        /// <summary>
        /// 高度
        /// </summary>
        /// <param name="boundBlock3D"></param>
        /// <returns></returns>
        public static double Depth(this BoundBlock3d boundBlock3D)
            {
            double value = 0;
            if (boundBlock3D != null)
                {
                value = boundBlock3D.GetMaximumPoint().Z - boundBlock3D.GetMinimumPoint().Z;
                }
            return value;
            }
        public static Point3d PixelToWorld(this Database database, Point point)
            {
            if (database == null)
                {
                throw new Exception("参数错误");
                }
            Point3d point3d = new Point3d(point.X, point.Y, 0);
            using (var vtr = (ViewportTableRecord)database.CurrentViewportTableRecordId.GetObject(OpenMode.ForRead))
                {
                // using protocol extensions we handle PS and MS viewports in the same manner
                using (var vpd = new AbstractViewportData(vtr))
                    {
                    point3d = point3d.TransformBy(vpd.GsView.ObjectToDeviceMatrix.Inverse());
                    }
                }
            return point3d;
            }
        public static Point3d[] PixelToWorld(this Database database, IEnumerable<Point> points)
            {
            if (database == null)
                {
                throw new Exception("参数错误");
                }
            List<Point3d> point3Ds = new List<Point3d>();
            if (points != null)
                {
                using (var vtr = (ViewportTableRecord)database.CurrentViewportTableRecordId.GetObject(OpenMode.ForRead))
                    {
                    // using protocol extensions we handle PS and MS viewports in the same manner
                    using (var vpd = new AbstractViewportData(vtr))
                        {
                        Matrix3d matrix3D = vpd.GsView.ObjectToDeviceMatrix.Inverse();
                        foreach (var point in points)
                            {
                            Point3d point3d = new Point3d(point.X, point.Y, 0);
                            point3d = point3d.TransformBy(matrix3D);
                            point3Ds.Add(point3d);
                            }
                        }
                    }
                }
            return point3Ds.ToArray();
            }
        public static BoundBlock3d PixelToWorld(this Database database, Rectangle srcRectangle)
            {
            Point bl = new Point(srcRectangle.Left, srcRectangle.Bottom);
            Point tr = new Point(srcRectangle.Right, srcRectangle.Top);
            var bottomLeft = PixelToWorld(database, bl);
            var topRight = PixelToWorld(database, tr);
            BoundBlock3d destBoundBlock3D = new BoundBlock3d();
            destBoundBlock3D.Set(bottomLeft, topRight);
            return destBoundBlock3D;
            }
        public static Point WorldToPixel(this Database database, Point3d point3D)
            {
            Point3d destPoint3d = new Point3d();
            using (var vtr = (ViewportTableRecord)database.CurrentViewportTableRecordId.GetObject(OpenMode.ForRead))
                {
                // using protocol extensions we handle PS and MS viewports in the same manner
                using (var vpd = new AbstractViewportData(vtr))
                    {
                    destPoint3d = point3D.TransformBy(vpd.GsView.ObjectToDeviceMatrix);//测试
                    }
                }
            Point point = new Point((int)destPoint3d.X, (int)destPoint3d.Y);
            return point;
            }

        public static Rectangle WorldToPixel(this Database database, BoundBlock3d srcBoundBlock3d)
            {
            Point3d minPoint3d = srcBoundBlock3d.GetMinimumPoint();
            Point3d maxPoint3d = srcBoundBlock3d.GetMaximumPoint();
            Point3d tl3d = new Point3d(minPoint3d.X, maxPoint3d.Y, 0);
            Point3d br3d = new Point3d(maxPoint3d.X, minPoint3d.Y, 0);
            Point tl = WorldToPixel(database, tl3d);
            Point br = WorldToPixel(database, br3d);
            Rectangle destRectangle = new Rectangle(tl.X, tl.Y, br.X - tl.X + 1, br.Y - tl.Y + 1);
            return destRectangle;
            }
        public static ObjectIdCollection GetSelection(Database database, LayoutHelperDevice layoutHelperDevice, Point location, Teigha.GraphicsSystem.SelectionMode selectionMode)
            {
            ObjectIdCollection objectIdCollection = new ObjectIdCollection();
            if (database != null && layoutHelperDevice != null)
                {
                int buffer = 5;
                using (Selector selector = new Selector(objectIdCollection, database.CurrentSpaceId))
                    {
                    using (Point2dCollection point2DCollection = new Point2dCollection(new Point2d[] { new Point2d(location.X - buffer, location.Y - buffer), new Point2d(location.X + buffer, location.Y + buffer) }))
                        {
                        using (Teigha.GraphicsSystem.View pView = layoutHelperDevice.ActiveView)
                            {
                            pView.Select(point2DCollection, selector, selectionMode);
                            }
                        }
                    }
                }
            return objectIdCollection;
            }
        }

    public class TextEditorFunction : CadFunction
        {
        private Panel _resizePanel;
        private Panel _handle;
        private ObjectId _selectedTextId;
        private bool _isResizing;
        private Point _dragStartPoint;

        public TextEditorFunction(ICadControl cadControl) : base(cadControl)
            {
            Enabled = false;
            }

        public override void Activate()
            {
            base.Activate();
            MessageBox.Show("文本编辑功能已激活，请在图纸上单击选择一个单行文本(DBText)进行编辑。");
            }

        public override void Deactivate()
            {
            base.Deactivate();
            RemoveResizePanel();
            }

        public override void DoMouseDown(MouseEventArgs e)
            {
            if (_resizePanel != null) return;

            var selection = CadControl.GetSelection(e.Location, Teigha.GraphicsSystem.SelectionMode.Crossing);
            if (selection.Count == 0)
                {
                RemoveResizePanel();
                return;
                }

            // 【修改 #1】将类型检查从 MText 改为 DBText
            foreach (ObjectId id in selection)
                {
                using (var dbObj = id.GetObject(OpenMode.ForRead))
                    {
                    if (dbObj is DBText dbtext) // <--- 关键修改
                        {
                        _selectedTextId = id;
                        ShowResizePanelFor(dbtext);
                        return;
                        }
                    }
                }
            }

        // 【修改 #2】修改方法签名以接收 DBText
        private void ShowResizePanelFor(DBText dbtext) // <--- 关键修改
            {
            RemoveResizePanel();

            var extents = dbtext.GeometricExtents;
            if (extents == null) return;

            var boundBlock = new BoundBlock3d();
            boundBlock.Set(extents.MinPoint, extents.MaxPoint);
            var screenRect = CadControl.WorldToPixel(boundBlock);

            _resizePanel = new Panel
                {
                Location = screenRect.Location,
                Size = screenRect.Size,
                BackColor = Color.FromArgb(100, 0, 100, 255),
                BorderStyle = BorderStyle.FixedSingle
                };

            _handle = new Panel
                {
                Size = new Size(8, 8),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.SizeWE
                };

            _handle.Location = new Point(_resizePanel.Width - _handle.Width / 2, _resizePanel.Height / 2 - _handle.Height / 2);
            _resizePanel.Controls.Add(_handle);

            _handle.MouseDown += Handle_MouseDown;
            _handle.MouseMove += Handle_MouseMove;
            _handle.MouseUp += Handle_MouseUp;

            (CadControl as Control).Controls.Add(_resizePanel);
            _resizePanel.BringToFront();
            }

        private void RemoveResizePanel()
            {
            if (_resizePanel != null)
                {
                (CadControl as Control).Controls.Remove(_resizePanel);
                _resizePanel.Dispose();
                _resizePanel = null;
                _handle = null;
                _selectedTextId = ObjectId.Null;
                }
            }

        #region Handle Dragging Events

        private void Handle_MouseDown(object sender, MouseEventArgs e) { _isResizing = true; _dragStartPoint = e.Location; }
        private void Handle_MouseMove(object sender, MouseEventArgs e)
            {
            if (!_isResizing || _resizePanel == null) return;
            int deltaX = e.X - _dragStartPoint.X;
            int newWidth = _resizePanel.Width + deltaX;
            if (newWidth > 10)
                {
                _resizePanel.Width = newWidth;
                _handle.Left = _resizePanel.Width - _handle.Width / 2;
                }
            }
        private void Handle_MouseUp(object sender, MouseEventArgs e)
            {
            if (!_isResizing) return;
            _isResizing = false;

            // 【修改 #3】调用新的更新方法
            UpdateDbTextHorizontalScale();
            }

        #endregion

        // 【修改 #4】重写整个更新逻辑，用于修改水平缩放比例
        private void UpdateDbTextHorizontalScale()
            {
            if (_selectedTextId.IsNull || _resizePanel == null) return;

            using (DBText dbtext = _selectedTextId.GetObject(OpenMode.ForWrite) as DBText)
                {
                if (dbtext != null)
                    {
                    // 1. 获取原始的几何范围和宽度
                    var originalExtents = dbtext.GeometricExtents;
                    if (originalExtents == null) return;
                    double originalWorldWidth = originalExtents.MaxPoint.X - originalExtents.MinPoint.X;

                    // 2. 获取面板拖拽后的新宽度（世界坐标）
                    Point panelTopLeft = _resizePanel.Location;
                    Point panelTopRight = new Point(_resizePanel.Right, _resizePanel.Top);
                    Point3d worldTopLeft = CadControl.PixelToWorld(panelTopLeft);
                    Point3d worldTopRight = CadControl.PixelToWorld(panelTopRight);
                    double newWorldWidth = worldTopRight.X - worldTopLeft.X;

                    // 3. 计算新的水平缩放比例
                    if (originalWorldWidth > 1e-6) // 防止除以零
                        {
                        // 新比例 = 旧比例 * (新宽度 / 旧宽度)
                        double scaleFactor = newWorldWidth / originalWorldWidth;
                        dbtext.WidthFactor *= scaleFactor;
                        }
                    }
                }

            (CadControl as Control).Invalidate();
            RemoveResizePanel();
            }
        }

    public enum YieldStyles
        {
        /// <summary>
        /// This is a null state for testing, and should not be used directly.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// This function will deactivate if another LeftButton function activates.
        /// </summary>
        LeftButton = 0x1,

        /// <summary>
        /// This function will deactivate if another RightButton function activates.
        /// </summary>
        RightButton = 0x2,

        /// <summary>
        /// This function will deactivate if another scroll function activates.
        /// </summary>
        Scroll = 0x4,

        /// <summary>
        /// This function will deactivate if another keyboard function activates.
        /// </summary>
        Keyboard = 0x8,

        /// <summary>
        /// This function is like a glyph and never yields to other functions.
        /// </summary>
        AlwaysOn = 16,
        }

    public class ZoomFunction : CadFunction
        {
        #region Fields

        private BoundBlock3d _client;
        BoundBlock3d Client
            {
            get => _client;
            set
                {
                if (_client != value)
                    {
                    if (_client != null)
                        {
                        _client.Dispose();
                        }
                    _client = value;
                    }
                }
            }
        private int _direction;
        private Point _dragStart;
        private bool _isDragging;
        private bool _preventDrag;

        private int _timerInterval;
        private System.Timers.Timer _zoomTimer;

        #endregion
        private Point2d GetResolution(BoundBlock3d boundBlock3D, Rectangle rectangle)
            {
            Point3d minPoint = boundBlock3D.GetMinimumPoint();
            Point3d maxPoint = boundBlock3D.GetMaximumPoint();
            double xRes = (maxPoint.X - minPoint.X) / rectangle.Width;
            double yRes = (maxPoint.Y - minPoint.Y) / rectangle.Height;
            Point2d point2D = new Point2d(xRes, yRes);
            return point2D;
            }
        public ZoomFunction(ICadControl cadControl) : base(cadControl)
            {
            Configure();
            BusySet = false;
            }
        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether the map function is currently interacting with the map.
        /// </summary>
        public bool BusySet { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether forward zooms in. This controls the sense (direction) of zoom (in or out) as you roll the mouse wheel.
        /// </summary>
        public bool ForwardZoomsIn
            {
            get
                {
                return _direction > 0;
                }

            set
                {
                _direction = value ? 1 : -1;
                }
            }

        /// <summary>
        /// Gets or sets the wheel zoom sensitivity. Increasing makes it more sensitive. Maximum is 0.5, Minimum is 0.01
        /// </summary>
        public double Sensitivity { get; set; }

        /// <summary>
        /// Gets or sets the full refresh timeout value in milliseconds
        /// </summary>
        public int TimerInterval
            {
            get
                {
                return _timerInterval;
                }

            set
                {
                _timerInterval = value;
                _zoomTimer.Interval = _timerInterval;
                }
            }

        #endregion

        #region Methods
        /// <summary>
        /// Handles the actions that the tool controls during the OnMouseDown event
        /// </summary>
        /// <param name="e">The event args.</param>
        public override void DoMouseDown(MouseEventArgs e)
            {
            _dragStart = new Point(e.X, e.Y);
            if (e.Button == MouseButtons.Middle && !_preventDrag)
                {
                _isDragging = true;
                Client = (BoundBlock3d)CadControl.ViewExtent.Clone();
                }

            base.DoMouseDown(e);
            }

        /// <summary>
        /// Handles the mouse move event, changing the viewing extents to match the movements
        /// of the mouse if the left mouse button is down.
        /// </summary>
        /// <param name="e">The event args.</param>
        public override void DoMouseMove(MouseEventArgs e)
            {
            if (_isDragging)
                {
                if (!BusySet)
                    {
                    BusySet = true;
                    }
                Point[] points = new Point[] { _dragStart, e.Location };
                Point3d[] point3Ds = CadControl.Database.PixelToWorld(points);
                Point3d startPoint3D = point3Ds[0];
                Point3d currentPoint3D = point3Ds[1];
                double xOff = startPoint3D.X - currentPoint3D.X;
                double yOff = startPoint3D.Y - currentPoint3D.Y;
                BoundBlock3d boundBlock3D = (BoundBlock3d)_client.Clone();
                boundBlock3D.TranslateBy(new Vector3d(xOff, yOff, 0));
                SetCadExtent(boundBlock3D);
                }

            base.DoMouseMove(e);
            }
        /// <summary>
        /// Mouse Up
        /// </summary>
        /// <param name="e">The event args.</param>
        public override void DoMouseUp(MouseEventArgs e)
            {
            if (e.Button == MouseButtons.Middle && _isDragging)
                {
                BusySet = false;
                _client = null;
                _isDragging = false;
                }
            _dragStart = Point.Empty;
            base.DoMouseUp(e);
            }

        /// <summary>
        /// Mouse Wheel
        /// </summary>
        /// <param name="e">The event args.</param>
        public override void DoMouseWheel(MouseEventArgs e)
            {
            if (!_isDragging)
                {
                // Fix this
                _zoomTimer.Stop(); // if the timer was already started, stop it.
                _preventDrag = true;
                if (_client == null)
                    {
                    _client = (BoundBlock3d)CadControl.ViewExtent.Clone();
                    }
                Point3d point3D = CadControl.Database.PixelToWorld(e.Location);
                double ratio;
                if (_direction * e.Delta > 0)
                    {
                    ratio = 1 - Sensitivity;
                    }
                else
                    {
                    ratio = 1 / (1 - Sensitivity);
                    }
                _client.ScaleBy(ratio, new Point3d(point3D.X, point3D.Y, 0));

                _zoomTimer.Start();
                if (!BusySet)
                    {
                    BusySet = true;
                    }
                }
            base.DoMouseWheel(e);
            }

        private void Configure()
            {
            YieldStyle = YieldStyles.Scroll;
            _timerInterval = 100;
            _zoomTimer = new System.Timers.Timer
                {
                Interval = _timerInterval
                };
            _zoomTimer.Elapsed += ZoomTimerTick;
            Sensitivity = 0.2;
            ForwardZoomsIn = true;
            Name = "ScrollZoom";
            }

        private void ZoomTimerTick(object sender, EventArgs e)
            {
            _zoomTimer.Stop();
            SetCadExtent(_client);
            _client = null;
            BusySet = false;
            _preventDrag = false;
            }
        private void SetCadExtent(BoundBlock3d boundBlock3D)
            {
            if (CadControl == null || _client == null) return;
            CadControl.ViewExtent = boundBlock3D;
            }
        #endregion
        }

    }
