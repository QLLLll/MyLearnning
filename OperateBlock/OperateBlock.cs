using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;

namespace OperateBlock
{
    public class OperateBlock
    {

        Document Doc = Application.DocumentManager.MdiActiveDocument;
        Editor Ed = Application.DocumentManager.MdiActiveDocument.Editor;
        Database Db = Application.DocumentManager.MdiActiveDocument.Database;

        public ObjectId AddBlock(Database db, string blockName, List<Entity> entis)
        {

            var space = db.CurrentSpaceId;

            using (var trans = db.TransactionManager.StartTransaction())
            {

                var tblBlk = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                if (tblBlk.Has(blockName)) return tblBlk[blockName];

                BlockTableRecord rec = new BlockTableRecord();

                rec.Name = blockName;

                entis.ForEach(enti => { rec.AppendEntity(enti); });

                tblBlk.UpgradeOpen();

                ObjectId oId = tblBlk.Add(rec);

                trans.AddNewlyCreatedDBObject(rec, true);

                trans.Commit();

                return oId;
            }
        }

        public ObjectId AddBlock(Database db,string blockName,params Entity[] enti)
        {
            return AddBlock(db, blockName, enti.ToList());
        }

        public ObjectId InsertBlockReference(Database db, string layerName,
            ObjectId blockId, Point3d position, double rotation, Scale3d scale)
        {

            using (var trans = db.TransactionManager.StartTransaction())
            {

                var spaceRec = trans.GetObject(db.CurrentSpaceId, OpenMode.ForRead) as BlockTableRecord;

                BlockReference br = new BlockReference(position, blockId);

                br.ScaleFactors = scale;

                br.Rotation = rotation;

                br.Layer = layerName;

                spaceRec.UpgradeOpen();

                ObjectId oId= spaceRec.AppendEntity(br);

                trans.AddNewlyCreatedDBObject(br, true);

                spaceRec.DowngradeOpen();

                trans.Commit();

                return oId;

            }

        }

        public ObjectId InsertBlockReference(Database db, string layerName,
            string blockName, Point3d position, double rotation, Scale3d scale)
        {

            using (var trans = db.TransactionManager.StartTransaction())
            {
                var blkTbl = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                if (!blkTbl.Has(blockName)) return ObjectId.Null;

                var oId = blkTbl[blockName];

                var spaceRec = trans.GetObject(db.CurrentSpaceId, OpenMode.ForRead) as BlockTableRecord;

                BlockReference br = new BlockReference(position, oId);

                br.ScaleFactors = scale;

                br.Rotation = rotation;

                br.Layer = layerName;

                spaceRec.UpgradeOpen();

                ObjectId brId = spaceRec.AppendEntity(br);

                trans.AddNewlyCreatedDBObject(br, true);

                spaceRec.DowngradeOpen();

                trans.Commit();

                return brId;
            }
        }

        public ObjectId InsertBlockReference(Database db, string layerName,
            string blockName, Point3d position, double rotation, Scale3d scale,Dictionary<string,string>attNamevalues)
        {

            using (var trans = db.TransactionManager.StartTransaction())
            {
                var blkTbl = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;


                if (!blkTbl.Has(blockName)) return ObjectId.Null;

                var oId = blkTbl[blockName];

                var spaceRec = trans.GetObject(db.CurrentSpaceId, OpenMode.ForRead) as BlockTableRecord;

                var blkTblRec = trans.GetObject(oId, OpenMode.ForRead) as BlockTableRecord;



                BlockReference br = new BlockReference(position, oId);

                br.ScaleFactors = scale;

                br.Rotation = rotation;

                br.Layer = layerName;
               

                spaceRec.UpgradeOpen();
                ObjectId brId = spaceRec.AppendEntity(br);
                trans.AddNewlyCreatedDBObject(br, true);
                spaceRec.DowngradeOpen();

                if (blkTblRec.HasAttributeDefinitions)
                {
                    foreach (ObjectId id in blkTblRec)
                    {

                        var attr = trans.GetObject(id, OpenMode.ForRead) as AttributeDefinition;

                        if (attr != null)
                        {

                            AttributeReference attrRef = new AttributeReference();

                            attrRef.SetAttributeFromBlock(attr, br.BlockTransform);

                            attrRef.Position=attr.Position.TransformBy(br.BlockTransform);

                            attrRef.Rotation = attr.Rotation;

                            attrRef.AdjustAlignment(db);

                            if (attNamevalues.ContainsKey(attr.Tag.ToString()))
                            {

                                attrRef.TextString = attNamevalues[attr.Tag.ToUpper()];

                            }

                            br.AttributeCollection.AppendAttribute(attrRef);
                            trans.AddNewlyCreatedDBObject(attrRef, true);  
                        }
                    }

                }
                trans.Commit();
                return brId;
            }
        }

        public void UpdateAttributesInBlock(Database db, ObjectId blockRefId,Dictionary<string,string>attNameValues)
        {

            using (var trans = db.TransactionManager.StartTransaction()) {

                BlockReference blockRef = trans.GetObject(blockRefId, OpenMode.ForRead) as BlockReference;

                if (blockRef != null)
                {

                    foreach(ObjectId id in blockRef.AttributeCollection)
                    {

                        AttributeReference attref = trans.GetObject(id, OpenMode.ForRead) as AttributeReference;

                        if (attNameValues.ContainsKey(attref.Tag.ToUpper()))
                        {

                            attref.UpgradeOpen();

                            attref.TextString = attNameValues[attref.Tag.ToUpper()].ToString();

                            attref.DowngradeOpen();

                        }
                    }
                }
                    trans.Commit();
            }
        }



        public void InsertAttrDef(Database db, ObjectId blockId, List<AttributeDefinition> listAttrDefs)
        {
            using(var trans = db.TransactionManager.StartTransaction())
            {

                var blkTblRec = trans.GetObject(blockId, OpenMode.ForWrite )as BlockTableRecord;


                listAttrDefs.ForEach(attr => { blkTblRec.AppendEntity(attr); trans.AddNewlyCreatedDBObject(attr, true); });


                blkTblRec.DowngradeOpen();

            }

        }

        public void InsertAttrDef(Database db, ObjectId blockId, params AttributeDefinition[] listAttrDefs)
        {
            InsertAttrDef(db, blockId, listAttrDefs.ToList());
        }
        [CommandMethod("EcdDoor")]
        public ObjectId MakeDoor()
        {

            Point3d pt1 = Point3d.Origin;

            Point3d pt2 = new Point3d(0, 1.0, 0);

            Line leftLine = new Line(pt1, pt2);

            Line bottomLine = new Line(pt1, pt1.PolarPoint(0,0.05));

            Arc arc = new Arc();

            arc.CreateArc(pt1.PolarPoint(0, 1), pt1, Math.PI / 2);

            Line rightLine = new Line(bottomLine.EndPoint, leftLine.EndPoint.PolarPoint(0, 0.05));

            Point3dCollection pts = new Point3dCollection();

            rightLine.IntersectWith(arc, Intersect.OnBothOperands, pts, IntPtr.Zero, IntPtr.Zero);

            if (pts.Count == 0) return ObjectId.Null;

            rightLine.EndPoint = pts[0];

          ObjectId blockId=  AddBlock(Db, "DOOR", leftLine, bottomLine, rightLine, arc);

            return blockId;

        }

        [CommandMethod("ECDAddAtt")]
        public void AddAttributes()
        {
            ObjectId blockId = MakeDoor();

            using(var trans = Db.TransactionManager.StartTransaction())
            {

                AttributeDefinition attSYM = new AttributeDefinition(Point3d.Origin, "1", "SYM", "输入门的符号", ObjectId.Null);

                SetStyleForAtt(attSYM, false);

                attSYM.AlignmentPoint = new Point3d(32, 28, 0);

                AttributeDefinition attWidth=new AttributeDefinition(Point3d.Origin,"1m","WIDTH","输入门的宽度",ObjectId.Null);
                SetStyleForAtt(attWidth, true);

                AttributeDefinition attHeight = new AttributeDefinition(Point3d.Origin, "2m", "HEIGHT", "输入门的高度", ObjectId.Null);
                SetStyleForAtt(attHeight, true);

                AttributeDefinition attStyle = new AttributeDefinition(Point3d.Origin, "TWO PANEL", "Style", "输入门的样式", ObjectId.Null);
                SetStyleForAtt(attStyle, true);

                AttributeDefinition attRef = new AttributeDefinition(Point3d.Origin, "TS 3010", "REF", "输入门的参考图编号", ObjectId.Null);
                SetStyleForAtt(attRef, true);

                AttributeDefinition attManufacturer = new AttributeDefinition(Point3d.Origin, "TRU STYLE", "MANUFACTURER", "输入生产产家", ObjectId.Null);
                SetStyleForAtt(attManufacturer, true);

                AttributeDefinition attCost = new AttributeDefinition(Point3d.Origin, "189.00", "COST", "输入门的单价", ObjectId.Null);
                SetStyleForAtt(attCost, true);

                InsertAttrDef(Db, blockId, attSYM, attWidth, attHeight, attStyle, attRef, attManufacturer, attCost);
            }


        }


        [CommandMethod("EcdAddDoor")]
        public void InsertDoor()
        {

            InsertBlockReference(Db, "0", "DOOR", Point3d.Origin, 0, new Scale3d(10));

        }

        [CommandMethod("EcdInsertDoor")]
        public void InsertDoor2()
        {

                Dictionary<string, string> atts = new Dictionary<string, string>();

                atts.Add("SYM", "1");
                atts.Add("WIDTH", "0.90m");
                atts.Add("HEIGHT", "2.2m");
                atts.Add("COST", "200.0");

                InsertBlockReference(Db, "0","DOOR", Point3d.Origin, 0, new Scale3d(20), atts);
        }
        [CommandMethod("EcdUpdateDoor")]
        public void UpdarteDoor()
        {

            PromptEntityOptions opt = new PromptEntityOptions("请选择一个块参照");

            opt.SetRejectMessage("你选择的不是块");

            opt.AddAllowedClass(typeof(BlockReference), true);

            var result = Ed.GetEntity(opt);

            if (result.Status != PromptStatus.OK) return;

            Dictionary<string,string> atts = new Dictionary<string, string>();

            atts.Add("SYM", "2");
            atts.Add("COST", "300.0");
            UpdateAttributesInBlock(Db, result.ObjectId, atts);

        }


        public void SetStyleForAtt(AttributeDefinition att,bool isvisible)
        {
            att.Height = 60;
            att.HorizontalMode = TextHorizontalMode.TextCenter;
            att.VerticalMode = TextVerticalMode.TextVerticalMid;
            att.Invisible = isvisible;
        }

    }
}
