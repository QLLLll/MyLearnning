using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

[assembly: ExtensionApplication(typeof(LearningDatabaseService.MyTestDatabase))]
[assembly: CommandClass(typeof(LearningDatabaseService.MyTestDatabase))]
namespace LearningDatabaseService
{
    public class MyTestDatabase : IExtensionApplication
    {
        public void Initialize()
        {

        }

        public void Terminate()
        {

        }
        [CommandMethod("TestDb")]
        public void TestDatabase()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;

            Database acCurDb = acDoc.Database;

            /*           Document acNewDoc= Application.DocumentManager.Add("");

                        using(DocumentLock acDocLock = acNewDoc.LockDocument())
                        {
                            Database acNewDb = acNewDoc.Database;

                            acNewDb.Insert(Matrix3d.Identity, acCurDb, true);

                            //acNewDb.SaveAs("1.dwg", DwgVersion.Current);
                        }*/
            try
            {
                using(Database db=new Database(false, true))
                {

                    db.ReadDwgFile(@"D:\Users\liu.qiang\Desktop\Drawing1.dwg", FileOpenMode.OpenForReadAndAllShare, true, null);

                    acCurDb.Insert(Matrix3d.Identity, db, false);

                }

            }
            catch (System.Exception ex)
            {

                
            }

            /*acCurDb.LoadLineTypeFile("*", "acad.lin");*/



           /* Document acNewDoc2 = Application.DocumentManager.Add("");

            using (DocumentLock acDocLock = acNewDoc2.LockDocument())
            {
                Database acNewDb = acNewDoc2.Database;

                Database copiedDb = acCurDb.Wblock();

                acNewDb.Insert(Matrix3d.Identity, copiedDb, false);

            }*/


        }

        [CommandMethod("cmd1")]
        public void LearningBlock()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;

            Database acCurDb = acDoc.Database;

            using(Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {

                var blkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                var mdlSpc = acTrans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                string blkRecName = "blkTblRec1";
                //块定义
                /*  var newBlkRec = new BlockTableRecord();

                  newBlkRec.Name = blkRecName;
                  newBlkRec.BlockScaling = BlockScaling.Any;

                  blkTbl.UpgradeOpen();

                  var entities = GetComponenty();

                  foreach (var ent in entities)
                  {

                      newBlkRec.AppendEntity(ent);

                  }
                  blkTbl.Add(newBlkRec);
                  acTrans.AddNewlyCreatedDBObject(newBlkRec, true);
                  */
                //块参照
                /* var blkRef = new BlockReference(Point3d.Origin + Vector3d.YAxis, blkTbl[blkRecName]);

                 //blkRef.ScaleFactors = new Scale3d(2, 0.5, 1);

                 mdlSpc.AppendEntity(blkRef);
                 acTrans.AddNewlyCreatedDBObject(blkRef, true);

                 var blkDef = acTrans.GetObject(blkTbl[blkRecName], OpenMode.ForRead) as BlockTableRecord;

                 //块定义往块参照添加属性
                 foreach (ObjectId ObjId in blkDef)
                 {
                     var ad = acTrans.GetObject(ObjId, OpenMode.ForRead) as AttributeDefinition;

                     if (ad != null)
                     {

                         var ar = new AttributeReference(ad.Position, ad.TextString, ad.Tag, ad.TextStyleId);

                         ar.TransformBy(blkRef.BlockTransform);

                         blkRef.AttributeCollection.AppendAttribute(ar);

                     }
                 }*/

                //组
                /*Group group = new Group("mygroup", true);

                var dicGroup = acTrans.GetObject(acCurDb.GroupDictionaryId, OpenMode.ForWrite) as DBDictionary;

                dicGroup.SetAt("mygroup", group);

                acTrans.AddNewlyCreatedDBObject(group, true);

                foreach (var ent2 in GetComponenty())
                {
                   var  objId= mdlSpc.AppendEntity(ent2);
                    acTrans.AddNewlyCreatedDBObject(ent2, true);
                    var targetGroup = acTrans.GetObject(dicGroup.GetAt("mygroup"), OpenMode.ForWrite) as Group;

                    targetGroup.Append(objId);
                }*/

                acTrans.Commit();

            }
        }

        [CommandMethod("cmd2")]
        public void learnningStyle()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;

            Database acCurDb = acDoc.Database;

           
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {

                var blkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                var mdlSpc = acTrans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                
                //获取斜线箭头
                /* var old = Application.GetSystemVariable("DIMBLK") as string;

                 Application.SetSystemVariable("DIMBLK", "_OBLIQUE");

                 if (!string.IsNullOrEmpty(old))
                 {
                     Application.SetSystemVariable("DIMBLK", old);
                 }

                 var dimStyle = new DimStyleTableRecord();

                 dimStyle.Name = "DIM1";
                 // dimStyle.Dimblk = ObjectId.Null;
                 dimStyle.Dimblk = blkTbl["_OBLIQUE"];
                 dimStyle.Dimasz = 1.5;
                 dimStyle.Dimdec = 2;
                 dimStyle.Dimtxt = 2.5;
                 dimStyle.Dimlfac = 0.5;
                 dimStyle.Dimscale = 0.1;

                 var dimStyleTable = acTrans.GetObject(acCurDb.DimStyleTableId, OpenMode.ForWrite) as DimStyleTable;
                 dimStyleTable.Add(dimStyle);
                 acTrans.AddNewlyCreatedDBObject(dimStyle, true);*/

                //插入样式
                /*
                var dimStyleTable2 = acTrans.GetObject(acCurDb.DimStyleTableId, OpenMode.ForWrite) as DimStyleTable;

                var alignDim = new AlignedDimension(Point3d.Origin - Vector3d.XAxis,
                    Point3d.Origin + Vector3d.XAxis, Point3d.Origin - Vector3d.YAxis * 0.25, null, dimStyleTable2["DIM1"]);

                mdlSpc.AppendEntity(alignDim);
                acTrans.AddNewlyCreatedDBObject(alignDim, true);
                */
                //线型
                /*
                var lineType = new LinetypeTableRecord();

                lineType.Name = "MyLineType";
                lineType.PatternLength = 0.2;
                lineType.NumDashes = 2;
                lineType.SetDashLengthAt(0, 0.1);
                lineType.SetDashLengthAt(1, -0.1);

                var lineTable = acTrans.GetObject(acCurDb.LinetypeTableId, OpenMode.ForWrite) as LinetypeTable;

                lineTable.Add(lineType);
                acTrans.AddNewlyCreatedDBObject(lineType, true);
                */

                //文字样式
                /*
                var txtStyle = new TextStyleTableRecord();
                txtStyle.Name = "MYTXTSTYLE";
                txtStyle.Font = new Autodesk.AutoCAD.GraphicsInterface.FontDescriptor("Consolas", false, false, 0, 0);
                txtStyle.IsShapeFile = false;

                var txtStyleTable = acTrans.GetObject(acCurDb.TextStyleTableId, OpenMode.ForWrite) as TextStyleTable;

                txtStyleTable.Add(txtStyle);
                acTrans.AddNewlyCreatedDBObject(txtStyle, true);*/

                var txtStyleTable2 = acTrans.GetObject(acCurDb.TextStyleTableId, OpenMode.ForWrite) as TextStyleTable;

                var txt = new DBText();
                txt.TextString = "Consolas测试";
                txt.TextStyleId = txtStyleTable2["MYTXTSTYLE"];
                txt.HorizontalMode = TextHorizontalMode.TextMid;
                txt.AlignmentPoint = Point3d.Origin + Vector3d.YAxis * 2;

                mdlSpc.AppendEntity(txt);
                acTrans.AddNewlyCreatedDBObject(txt, true);


                acTrans.Commit();
            }
        }

        Entity[] GetComponenty()
        {
            Polyline poly0 = new Polyline();
            poly0.AddVertexAt(poly0.NumberOfVertices, Point2d.Origin + Vector2d.XAxis, 0, 0, 0);
            poly0.AddVertexAt(poly0.NumberOfVertices, Point2d.Origin + Vector2d.XAxis + Vector2d.YAxis * 0.8, 0, 0, 0);
            poly0.AddVertexAt(poly0.NumberOfVertices, Point2d.Origin + Vector2d.XAxis * 0.8 + Vector2d.YAxis, 0, 0, 0);
            poly0.AddVertexAt(poly0.NumberOfVertices, Point2d.Origin + Vector2d.XAxis * 0.5 + Vector2d.YAxis, Math.Tan(0.25 * -1 * Math.PI), 0, 0);
            poly0.AddVertexAt(poly0.NumberOfVertices, Point2d.Origin + Vector2d.XAxis * -0.5 + Vector2d.YAxis, 0, 0, 0);
            poly0.AddVertexAt(poly0.NumberOfVertices, Point2d.Origin + Vector2d.XAxis * -1 + Vector2d.YAxis, 0, 0, 0);
            poly0.AddVertexAt(poly0.NumberOfVertices, Point2d.Origin + Vector2d.XAxis * -1, 0, 0, 0);
            poly0.Closed = true;

            Polyline poly1 = new Polyline();
            poly1.AddVertexAt(poly1.NumberOfVertices, Point2d.Origin + Vector2d.XAxis * 0.4 + Vector2d.YAxis * 0.25, Math.Tan(0.25 * Math.PI), 0, 0);
            poly1.AddVertexAt(poly1.NumberOfVertices, Point2d.Origin + Vector2d.XAxis * 0.6 + Vector2d.YAxis * 0.25, Math.Tan(0.25 * Math.PI), 0, 0);
            poly1.Closed = true;

            Polyline poly2 = new Polyline();
            poly2.AddVertexAt(poly2.NumberOfVertices, Point2d.Origin + Vector2d.XAxis * -0.4 + Vector2d.YAxis * 0.25, Math.Tan(0.25 * Math.PI), 0, 0);
            poly2.AddVertexAt(poly2.NumberOfVertices, Point2d.Origin + Vector2d.XAxis * -0.6 + Vector2d.YAxis * 0.25, Math.Tan(0.25 * Math.PI), 0, 0);
            poly2.Closed = true;

            Hatch acHatch = new Hatch();

            acHatch.SetHatchPattern(HatchPatternType.PreDefined, "BOX");
          
            HatchLoop hLoop0 = new HatchLoop(HatchLoopTypes.Polyline);
            for (int i = 0; i < poly0.NumberOfVertices; i++)
            {

                hLoop0.Polyline.Add(new BulgeVertex(poly0.GetPoint2dAt(i), poly0.GetBulgeAt(i)));

            }
            hLoop0.Polyline.Add(new BulgeVertex(poly0.GetPoint2dAt(0), poly0.GetBulgeAt(0)));

            HatchLoop hLoop1 = new HatchLoop(HatchLoopTypes.Polyline);
            for (int i = 0; i < poly1.NumberOfVertices; i++)
            {

                hLoop1.Polyline.Add(new BulgeVertex(poly1.GetPoint2dAt(i), poly1.GetBulgeAt(i)));

            }
            hLoop1.Polyline.Add(new BulgeVertex(poly1.GetPoint2dAt(0), poly1.GetBulgeAt(0)));

            HatchLoop hLoop2 = new HatchLoop(HatchLoopTypes.Polyline);
            for (int i = 0; i < poly2.NumberOfVertices; i++)
            {

                hLoop2.Polyline.Add(new BulgeVertex(poly2.GetPoint2dAt(i), poly2.GetBulgeAt(i)));

            }
            hLoop2.Polyline.Add(new BulgeVertex(poly2.GetPoint2dAt(0), poly2.GetBulgeAt(0)));

            acHatch.AppendLoop(hLoop0);
            acHatch.AppendLoop(hLoop1);
            acHatch.AppendLoop(hLoop2);

            return new Entity[4] { poly0, poly1, poly2, acHatch };



        }

    }
}
