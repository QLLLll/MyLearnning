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

                spaceRec.UpgradeOpen();

                ObjectId brId = spaceRec.AppendEntity(br);

                trans.AddNewlyCreatedDBObject(br, true);

                spaceRec.DowngradeOpen();

                trans.Commit();

                return brId;
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
        public void MakeDoor()
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

            if (pts.Count == 0) return;

            rightLine.EndPoint = pts[0];

            AddBlock(Db, "DOOR", leftLine, bottomLine, rightLine, arc);

        }

        [CommandMethod("EcdInsertDoor")]
        public void InsertDoor()
        {

            InsertBlockReference(Db, "0", "DOOR", Point3d.Origin, 0, new Scale3d(10));

        }


    }
}
