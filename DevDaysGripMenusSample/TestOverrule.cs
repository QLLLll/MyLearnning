using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;

namespace DevDaysGripMenusSample
{
    public class TestOverrule
    {
        private static DrawOverrule m_drawOverrule = new DrawOverrule();
        private static PropsOverrule m_propertiesOverrule = new PropsOverrule();
        private static XformOverrule m_xformOverrule = new XformOverrule();
        private static ObjectSnapOverrule m_osnapOverrule = new ObjectSnapOverrule();
        private static GripPointOverrule m_gripOverrule = new GripPointOverrule();
        private static HiliteOverrule m_hightlightOverrule = new HiliteOverrule();
        private static ObjOverrule m_objectOverrule = new ObjOverrule();

        private static Overrule[] m_overrules = new Overrule[]
        {
            m_drawOverrule,
            m_propertiesOverrule,
            m_xformOverrule,
            m_osnapOverrule,
            m_gripOverrule,
            m_hightlightOverrule,
            m_objectOverrule
        };

        private static System.Collections.Generic.List<ObjectId> m_overruledObjects = new System.Collections.Generic.List<ObjectId>();
        private static readonly string RegAppName = "AsdkOverruleTest";
        private static bool m_overruleAdded = false;

        [CommandMethod("overrule")]
        static public void Start()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            PromptEntityResult res = ed.GetEntity("Select a circle to overrule");
            if (res.Status != PromptStatus.OK)
                return;

            if (res.ObjectId.ObjectClass != RXObject.GetClass(typeof(Circle)))
            {
                ed.WriteMessage("Selected object is not a circle!\n");
                return;
            }

            if (m_overruledObjects.Count == 0)
            {
                Application.DocumentManager.DocumentToBeDestroyed += new DocumentCollectionEventHandler(DocumentManager_DocumentToBeDestroyed);
            }
            if (!m_overruledObjects.Contains(res.ObjectId))
            {
                Database db = res.ObjectId.Database;
                using (Transaction t = db.TransactionManager.StartTransaction())
                {
                    RegAppTable tbl = (RegAppTable)t.GetObject(db.RegAppTableId, OpenMode.ForRead, false);
                    if (!tbl.Has(RegAppName))
                    {
                        RegAppTableRecord app = new RegAppTableRecord();
                        app.Name = RegAppName;
                        tbl = (RegAppTable)t.GetObject(db.RegAppTableId, OpenMode.ForWrite, false);
                        tbl.Add(app);
                        t.AddNewlyCreatedDBObject(app, true);
                    }
                    Circle c = (Circle)t.GetObject(res.ObjectId, OpenMode.ForRead);

                    if (c.GetXDataForApplication(RegAppName) == null)
                    {
                        c = (Circle)t.GetObject(res.ObjectId, OpenMode.ForWrite);
                        c.XData = new ResultBuffer(
                            new TypedValue((int)DxfCode.ExtendedDataRegAppName, RegAppName),
                            new TypedValue((int)DxfCode.ExtendedDataReal, Math.PI / 6) //start with 30 degrees
                        );
                        m_overruledObjects.Add(res.ObjectId);
                    }
                    t.Commit();
                }
            }
            ObjectId[] ids = m_overruledObjects.ToArray();
            foreach (Overrule o in m_overrules)
            {
                o.SetIdFilter(ids);

                if (!m_overruleAdded)
                    Overrule.AddOverrule(RXObject.GetClass(typeof(Circle)), o, false);
            }

            m_overruleAdded = true;

            Application.DocumentManager.MdiActiveDocument.Editor.Regen();
        }

        private static void End()
        {
            foreach (Overrule o in m_overrules)
            {
                Overrule.RemoveOverrule(RXObject.GetClass(typeof(Circle)), o);
            }
            Overrule.Overruling = false;
            Application.DocumentManager.MdiActiveDocument.Editor.Regen();
        }

        private static void DocumentManager_DocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)
        {
            //remove the ids that are going away
            for (int i = m_overruledObjects.Count - 1; i >= 0; i--)
                if (m_overruledObjects[i].Database == e.Document.Database)
                    m_overruledObjects.RemoveAt(i);
            ObjectId[] ids = m_overruledObjects.ToArray();
            foreach (Overrule o in m_overrules)
                o.SetIdFilter(ids);
        }

        private static double GetAngle(Circle c)
        {
            ResultBuffer rb = c.GetXDataForApplication(RegAppName);
            return (double)rb.AsArray()[1].Value;
        }

        private static LineSegment3d[] GetLines(Circle c)
        {
            LineSegment3d[] lines = new LineSegment3d[2];
            double angle = GetAngle(c);
            Point3d center = c.Center;
            Vector3d normal = c.Normal;
            Vector3d axis = normal.GetPerpendicularVector() * c.Radius;
            Vector3d vec = axis.RotateBy(angle, normal);
            lines[0] = new LineSegment3d(center - vec, center + vec);
            vec = axis.RotateBy(-angle, normal);
            lines[1] = new LineSegment3d(center - vec, center + vec);
            return lines;
        }

        private class DrawOverrule : DrawableOverrule
        {
            public override bool WorldDraw(Drawable drawable, WorldDraw wd)
            {
                Circle c = (Circle)drawable;
                LineSegment3d[] lines = GetLines(c);
                foreach (LineSegment3d l in lines)
                    wd.Geometry.WorldLine(l.StartPoint, l.EndPoint);
                return base.WorldDraw(drawable, wd);
            }
        }

        private class PropsOverrule : PropertiesOverrule
        {
            public override void List(Entity entity)
            {
                Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
                ed.WriteMessage("\tOverrule test\n");
                ed.WriteMessage("\t\tAngle is {0}", GetAngle((Circle)entity));
            }
        }

        private class XformOverrule : TransformOverrule
        {
            public override void Explode(Entity entity, DBObjectCollection entitySet)
            {
                Circle c = (Circle)entity;
                entitySet.Add((Circle)entity.Clone());
                LineSegment3d[] lines = GetLines(c);
                foreach (LineSegment3d l in lines)
                    entitySet.Add(new Line(l.StartPoint, l.EndPoint));
            }
        }

        private class CircleMFMGPE : MultiModesGripPE
        {
            internal CircleMFMGPE(GripPointOverrule overrule)
                : base()
            {
                m_overrule = overrule;
            }

            public override GripMode CurrentMode(Entity entity, GripData gripData)
            {
                if (gripData is GripPointOverrule.MyGrip)
                {
                    int index = (int)((GripPointOverrule.MyGrip)gripData).CurrentModeId - (int)GripMode.ModeIdentifier.CustomStart;
                    return ((GripPointOverrule.MyGrip)gripData).Modes[index];
                }
                else
                    return null;
            }

            public override uint CurrentModeId(Entity entity, GripData gripData)
            {
                if (gripData is GripPointOverrule.MyGrip)
                    return (uint)(gripData as GripPointOverrule.MyGrip).CurrentModeId;
                return 0;
            }

            public override bool GetGripModes(Entity entity, GripData gripData, GripModeCollection modes, ref uint curMode)
            {
                if (!(gripData is GripPointOverrule.MyGrip))
                    return false;
                return (gripData as GripPointOverrule.MyGrip).GetGripModes(ref modes, ref curMode);
            }

            public override MultiModesGripPE.GripType GetGripType(Entity entity, GripData gripData)
            {
                return (gripData is GripPointOverrule.MyGrip) ? MultiModesGripPE.GripType.Secondary : MultiModesGripPE.GripType.Primary;
            }

            public override void Reset(Entity entity)
            {
            }

            public override bool SetCurrentMode(Entity entity, GripData gripData, uint curMode)
            {
                if (!(gripData is GripPointOverrule.MyGrip))
                    return false;
                (gripData as GripPointOverrule.MyGrip).CurrentModeId = (GripMode.ModeIdentifier)curMode;
                return true;
            }

            private GripPointOverrule m_overrule;
        }

        private class GripPointOverrule : GripOverrule
        {
            private CircleMFMGPE __theMFMGPE = null;

            internal GripPointOverrule()
            {
                __theMFMGPE = new CircleMFMGPE(this);
                RXObject.GetClass(typeof(Circle)).AddX(RXObject.GetClass(typeof(CircleMFMGPE)), __theMFMGPE);
            }

            internal abstract class MyGrip : GripData
            {
                protected MyGrip()
                {
                    __modes = new GripModeCollection();
                }

                private GripMode.ModeIdentifier __curModeId = GripMode.ModeIdentifier.CustomStart;

                public virtual GripMode.ModeIdentifier CurrentModeId
                {
                    get { return __curModeId; }
                    set { __curModeId = value; }
                }

                private GripModeCollection __modes;

                public virtual GripModeCollection Modes
                {
                    get { return __modes; }
                }

                public abstract void Move(Entity entity, Vector3d offset);

                public abstract bool GetGripModes(ref GripModeCollection modes, ref uint curMode);

                public override bool ViewportDraw(ViewportDraw vd, ObjectId entityId, GripData.DrawType type, Point3d? imageGripPoint, int gripSize)
                {
                    Point2d unit = vd.Viewport.GetNumPixelsInUnitSquare(GripPoint);
                    vd.Geometry.Circle(GripPoint, gripSize / unit.X, vd.Viewport.ViewDirection);
                    return true;
                }

                protected void MoveWorker(Entity entity, Vector3d offset)
                {
                    Circle c = (Circle)entity;
                    Point3d newGripPoint = GripPoint + offset;
                    c.Radius = newGripPoint.DistanceTo(c.Center);
                }
            }

            public class RowGripMenuItem : Autodesk.AutoCAD.Runtime.IMenuItem
            {
                private GripData _Grip;
                private Action _Action;

                private List<IMenuItem> _Items = new List<IMenuItem>();

                public delegate void Action(GripData g, RowGripMenuItem r);

                public RowGripMenuItem(GripData g, Action a)
                {
                    _Grip = g;
                    _Action = a;
                }

                public bool Checked { get; set; }

                public bool Enabled { get; set; }

                public System.Drawing.Icon Icon { get; set; }

                public string Text { get; set; }

                public bool Visible { get; set; }

                public System.Collections.Generic.IEnumerable<Autodesk.AutoCAD.Runtime.IMenuItem> Items
                {
                    get { return _Items; }
                }

                public void OnClicked(System.EventArgs eventArgs)
                {
                    if (_Action != null)
                        _Action.Invoke(_Grip, this);
                }

                public void Add(IMenuItem m)
                {
                    _Items.Add(m);
                }

                public void Remove(IMenuItem m)
                {
                    _Items.Remove(m);
                }

                public event EventHandler Click;
            }

            private class LowerLeftGrip : MyGrip
            {
                public enum myMFMGPEModeId
                {
                    kStretchRadiusX = GripMode.ModeIdentifier.CustomStart,
                    kStretchRadiusY,
                    kIncrementRadiusByOne
                }

                public LowerLeftGrip()
                {
                    GripMode m1 = new GripMode();
                    m1.ModeId = (uint)myMFMGPEModeId.kStretchRadiusX;
                    m1.DisplayString = "Stretch radius, offset mapped to X diection.";
                    m1.CLIPromptString = "\nSpecify new vertex point:";
                    m1.CLIKeywordList = "STretch MOve ROtate SCale MIrror Base Copy Undo X EXit dummy GMove CGizmo _STretch MOve ROtate SCale MIrror Base Copy Undo X EXit dummy GMove CGizmo";
                    m1.CLIDisplayString = "\n** STRETCH X **";
                    m1.Action = GripMode.ActionType.DragOn;
                    Modes.Add(m1);
                    GripMode m2 = new GripMode();
                    m2.ModeId = (uint)myMFMGPEModeId.kStretchRadiusY;
                    m2.DisplayString = "Stretch radius, offset mapped to Y diection.";
                    m2.CLIDisplayString = "\n** STRETCH Y **";
                    m1.CLIKeywordList = "STretch MOve ROtate SCale MIrror Base Copy Undo X EXit dummy GMove CGizmo _STretch MOve ROtate SCale MIrror Base Copy Undo X EXit dummy GMove CGizmo";
                    m2.CLIPromptString = "\nSpecify new vertex point:";
                    m2.Action = GripMode.ActionType.DragOn;
                    Modes.Add(m2);
                    GripMode m3 = new GripMode();
                    m3.ModeId = (uint)myMFMGPEModeId.kIncrementRadiusByOne;
                    m3.DisplayString = "Increment radius by 1.";
                    m3.CLIPromptString = "\nSpecify new vertex point:";
                    m3.CLIKeywordList = "STretch MOve ROtate SCale MIrror Base Copy Undo X EXit dummy GMove CGizmo _STretch MOve ROtate SCale MIrror Base Copy Undo X EXit dummy GMove CGizmo";
                    m3.CLIDisplayString = "\n** INCREMENT RADIUS BY 1 **";
                    m3.Action = GripMode.ActionType.Immediate;
                    Modes.Add(m3);
                    CurrentModeId = 0;
                }

                public override bool GetGripModes(ref GripModeCollection modes, ref uint curMode)
                {
                    //modes = __modes;

                    foreach (GripMode m in Modes)
                        modes.Add(m);

                    //curMode = (uint)CurrentModeId;

                    return true;
                }

                public override void Move(Entity entity, Vector3d offset)
                {
                    switch ((myMFMGPEModeId)CurrentModeId)
                    {
                        case myMFMGPEModeId.kStretchRadiusX:
                            offset = offset.Subtract(new Vector3d(0, offset.Y, 0));
                            break;

                        case myMFMGPEModeId.kStretchRadiusY:
                            offset = offset.Subtract(new Vector3d(offset.X, 0, 0));
                            break;

                        case myMFMGPEModeId.kIncrementRadiusByOne:
                            Vector3d v = GripPoint - ((Circle)entity).Center;
                            offset += v.GetNormal();
                            break;
                    }
                    MoveWorker(entity, offset);
                    return;
                }

                public override IEnumerable<Autodesk.AutoCAD.Runtime.IMenuItem> OnRightClick(GripDataCollection hotGrips, ObjectIdCollection entities)
                {
                    List<RowGripMenuItem> GripMenu = new List<RowGripMenuItem>();

                    GripMenu.Add(new RowGripMenuItem(this, null)
                    {
                        Text = "Test-1",
                        Icon = System.Drawing.Icon.FromHandle(Resource1.MyTestIcon.GetHicon())
                    }
                                );

                    GripMenu.Add(new RowGripMenuItem(this, null)
                    {
                        Text = "Test - 2",
                        Icon = System.Drawing.Icon.FromHandle(Resource1.MyTestIcon.GetHicon())
                    }
                                );

                    return GripMenu;
                }
            }

            private class UpperRightGrip : MyGrip
            {
                public override bool GetGripModes(ref GripModeCollection modes, ref uint curMode)
                {
                    GripMode m1 = new GripMode();
                    m1.ModeId = 0;
                    m1.DisplayString = "Upper right not implemeted";
                    m1.Action = GripMode.ActionType.Immediate;
                    modes.Add(m1);
                    curMode = 0;

                    return true;
                }

                public override void Move(Entity entity, Vector3d offset)
                {
                    MoveWorker(entity, offset);
                }
            }

            private class UpperLeftGrip : MyGrip
            {
                public override bool GetGripModes(ref GripModeCollection modes, ref uint curMode)
                {
                    GripMode m1 = new GripMode();
                    m1.ModeId = 0;
                    m1.DisplayString = "Upper left not implemeted";
                    m1.Action = GripMode.ActionType.Immediate;
                    modes.Add(m1);
                    curMode = 0;

                    return true;
                }

                public override void Move(Entity entity, Vector3d offset)
                {
                    MoveWorker(entity, offset);
                }
            }

            private class LowerRightGrip : MyGrip
            {
                public override bool GetGripModes(ref GripModeCollection modes, ref uint curMode)
                {
                    GripMode m1 = new GripMode();
                    m1.ModeId = 0;
                    m1.DisplayString = "Lower right not implemeted";
                    m1.Action = GripMode.ActionType.Immediate;
                    modes.Add(m1);
                    curMode = 0;

                    return true;
                }

                public override void Move(Entity entity, Vector3d offset)
                {
                    MoveWorker(entity, offset);
                }
            }

            private GripData[] m_grips = new GripData[4];

            public override void GetGripPoints(Entity entity, GripDataCollection grips, double curViewUnitSize, int gripSize, Vector3d curViewDir, Autodesk.AutoCAD.DatabaseServices.GetGripPointsFlags bitFlags)
            {
                base.GetGripPoints(entity, grips, curViewUnitSize, gripSize, curViewDir, bitFlags);

                Circle c = (Circle)entity;
                LineSegment3d[] lines = GetLines(c);
                m_grips[0] = new LowerLeftGrip();
                m_grips[0].GripPoint = lines[0].StartPoint;
                m_grips[1] = new UpperRightGrip();
                m_grips[1].GripPoint = lines[0].EndPoint;
                m_grips[2] = new UpperLeftGrip();
                m_grips[2].GripPoint = lines[1].StartPoint;
                m_grips[3] = new LowerRightGrip();
                m_grips[3].GripPoint = lines[1].EndPoint;
                foreach (GripData g in m_grips)
                    grips.Add(g);
            }

            public override void MoveGripPointsAt(Entity entity, GripDataCollection grips, Vector3d offset, Autodesk.AutoCAD.DatabaseServices.MoveGripPointsFlags bitFlags)
            {
                foreach (GripData grip in grips)
                {
                    MyGrip myGrip = grip as MyGrip;
                    if (myGrip != null)
                        myGrip.Move(entity, offset);
                    else
                        base.MoveGripPointsAt(entity, grips, offset, bitFlags);
                }
            }
        }

        private class ObjectSnapOverrule : OsnapOverrule
        {
            public override void GetObjectSnapPoints(Entity entity, ObjectSnapModes snapMode, IntPtr gsSelectionMark, Point3d pickPoint, Point3d lastPoint, Matrix3d viewTransform, Point3dCollection snapPoints, IntegerCollection geometryIds)
            {
                base.GetObjectSnapPoints(entity, snapMode, gsSelectionMark, pickPoint, lastPoint, viewTransform, snapPoints, geometryIds);
                if ((snapMode & ObjectSnapModes.ModeEnd) == ObjectSnapModes.ModeEnd)
                {
                    Circle c = (Circle)entity;
                    LineSegment3d[] lines = GetLines(c);
                    foreach (LineSegment3d l in lines)
                    {
                        snapPoints.Add(l.StartPoint);
                        snapPoints.Add(l.EndPoint);
                    }
                }
            }
        }

        private class HiliteOverrule : HighlightOverrule
        {
            public override void Highlight(Entity entity, FullSubentityPath subId, bool highlightAll)
            {
                base.Highlight(entity, subId, highlightAll);
            }

            public override void Unhighlight(Entity entity, FullSubentityPath subId, bool highlightAll)
            {
                base.Unhighlight(entity, subId, highlightAll);
            }
        }

        private class ObjOverrule : ObjectOverrule
        {
            public override void Erase(DBObject dbObject, bool erasing)
            {
                //prevent the object from being deleted
                base.Erase(dbObject, erasing);
                if (erasing)
                    throw new Autodesk.AutoCAD.Runtime.Exception(ErrorStatus.CannotBeErasedByCaller);
            }
        }
    }
}
