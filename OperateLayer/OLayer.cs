using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;

namespace OperateLayer
{
    public class OLayer
    {

        Document Doc = Application.DocumentManager.MdiActiveDocument;
        Editor Ed = Application.DocumentManager.MdiActiveDocument.Editor;
        Database Db = Application.DocumentManager.MdiActiveDocument.Database;

        /// <summary>
        /// 图层0和图层Defpoints不能被删除，当前图层不能被删除，图层上有块参照，实体时不能被删除
        /// 删除前需刷新，使用LayerTableRecord.GenerateUsageData();
        /// </summary>
        /// <returns></returns>
        public bool DeleteLayer(Database db, string layerName)
        {

            using (var trans = db.TransactionManager.StartTransaction())
            {

                var lyTbl = trans.GetObject(db.LayerTableId, OpenMode.ForWrite) as LayerTable;

                if (!lyTbl.Has(layerName)) return false;

                if (layerName == "0" || layerName == "Defpoints") return false;

                ObjectId oId = lyTbl[layerName];

                if (oId == db.Clayer) return false;

                var lyTblRec = trans.GetObject(lyTbl[layerName], OpenMode.ForRead) as LayerTableRecord;

                lyTbl.GenerateUsageData();

                if (lyTblRec.IsUsed) return false;

                lyTblRec.UpgradeOpen();

                lyTblRec.Erase(true);

                trans.Commit();

                return true;
            }
        }

        //添加层
        public ObjectId AddLayer(Database db, string layerName)
        {
            ObjectId oId = ObjectId.Null;

            using (var trans = db.TransactionManager.StartTransaction())
            {
                var lyerTbl = db.LayerTableId.GetObject(OpenMode.ForWrite) as LayerTable;

                if (lyerTbl.Has(layerName))
                {
                    trans.Commit();
                    return lyerTbl[layerName];
                }
                var lyerTblRec = new LayerTableRecord();
                lyerTblRec.Name = layerName;

                lyerTbl.Add(lyerTblRec);
                trans.AddNewlyCreatedDBObject(lyerTblRec, true);

                lyerTbl.DowngradeOpen();

                trans.Commit();

                return lyerTbl[layerName];
            }
        }

        //修改层的颜色
        public bool SetLayerColor(Database db, string layerName, short colorIndexs)
        {

            using (var trans = db.TransactionManager.StartTransaction())
            {

                var lyTbl = db.LayerTableId.GetObject(OpenMode.ForRead) as LayerTable;

                if (lyTbl.Has(layerName))
                {
                    var lyTblRec = trans.GetObject(lyTbl[layerName], OpenMode.ForWrite) as LayerTableRecord;

                    if (colorIndexs < 0 || colorIndexs > 255)
                    {
                        colorIndexs = 1;
                    }
                    lyTblRec.Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndexs);

                    lyTblRec.DowngradeOpen();

                    trans.Commit();
                    return true;

                }
                else
                {
                    return false;
                }
            }
        }

        public bool SetCurrentLayer(Database db, string layerName)
        {

            using (var trans = db.TransactionManager.StartTransaction())
            {

                var lyTbl = trans.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                if (!lyTbl.Has(layerName)) return false;

                //var lyTblRec = trans.GetObject(lyTbl[layerName], OpenMode.ForRead) as LayerTableRecord;

                if (db.Clayer != lyTbl[layerName])
                {
                    db.Clayer = lyTbl[layerName];
                }
                return true;
            }

        }

        public List<LayerTableRecord> GetAllLayer(Database db)
        {
            List<LayerTableRecord> listLyTblRec = new List<LayerTableRecord>();

            using (var trans = db.TransactionManager.StartTransaction())
            {

                var lyTbl = trans.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                foreach (ObjectId oId in lyTbl)
                {

                    var lyTblRec = trans.GetObject(oId, OpenMode.ForRead) as LayerTableRecord;

                    listLyTblRec.Add(lyTblRec);

                }

                return listLyTblRec;
            }
        }


        [CommandMethod("ECDLayerOps")]
        public void LayerOps()
        {
            string layerName = "";

            PromptStringOptions propStrOps = new PromptStringOptions("请输入图层名称:\n");

            do
            {
                var propStrRes = Ed.GetString(propStrOps);

                if (propStrRes.Status == PromptStatus.OK)
                {
                    layerName = propStrRes.StringResult;
                }


            } while (layerName == "");

            try
            {

                SymbolUtilityServices.ValidateSymbolName(layerName, false);

                ObjectId lyId = AddLayer(Db, layerName);

                if (lyId != null)
                {
                    PromptIntegerOptions propIntOps = new PromptIntegerOptions("请输入层的颜色索引:\n");

                    propIntOps.AllowNegative = false;
                    propIntOps.AllowNone = false;
                    propIntOps.AllowZero = true;

                    var propIntRes = Ed.GetInteger(propIntOps);

                    if (propIntRes.Status == PromptStatus.OK)
                    {

                        SetLayerColor(Db, layerName, (short)propIntRes.Value);

                    }
                    SetCurrentLayer(Db, layerName);

                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {

                Ed.WriteMessage(e.Message + "\n");

            }
        }
        [CommandMethod("ECDLayerDel")]
        public void LayerDel()
        {
            string layerName = "";

            PromptStringOptions propStrOps = new PromptStringOptions("请输入图层名称:\n");

            do
            {
                var propStrRes = Ed.GetString(propStrOps);

                if (propStrRes.Status == PromptStatus.OK)
                {
                    layerName = propStrRes.StringResult;
                }


            } while (layerName == "");
            try
            {
                SymbolUtilityServices.ValidateSymbolName(layerName, false);

                if (DeleteLayer(Db, layerName))
                {
                    Ed.WriteMessage("delete ok");
                }
                else
                {
                    Ed.WriteMessage("delete not ok");
                }

            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {

                Ed.WriteMessage(e.Message + "\n");
            }

        }

        [CommandMethod("ECDLayerHide")]
        public  void HiddenSelectLayer()
        {
            var propSel = new PromptSelectionOptions();

            var propRes = Ed.GetSelection(propSel);

            if (propRes.Status != PromptStatus.OK)
            {

                return;

            }

            ObjectId[] oIds = propRes.Value.GetObjectIds();

            using(var trans = Db.TransactionManager.StartTransaction())
            {

                var lyTbl = trans.GetObject(Db.LayerTableId, OpenMode.ForRead) as LayerTable;

                var blkTbl = trans.GetObject(Db.BlockTableId, OpenMode.ForRead) as BlockTable;

                for (int i = 0; i < oIds.Length; i++)
                {

                    var ent = trans.GetObject(oIds[i], OpenMode.ForRead) as Entity;

                    var layerId = ent.LayerId;  

                    var lyTblRec = trans.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;

                    if (lyTblRec.Name == "0") { return; }

                    lyTblRec.IsFrozen = true;
                    lyTblRec.IsHidden = true;

                }
                trans.Commit();

            }


        }
    }
}
