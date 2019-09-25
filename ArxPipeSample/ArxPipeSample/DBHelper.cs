using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace ArxDotNetLesson
{
    public static class DBHelper
    {
        /// <summary>
        /// 将实体添加到特定空间。
        /// </summary>
        /// <param name="ent"></param>
        /// <param name="db"></param>
        /// <param name="space"></param>
        /// <returns></returns>
        public static ObjectId ToSpace(this Entity ent, Database db = null, string space = null)
        {
            db = db ?? Application.DocumentManager.MdiActiveDocument.Database;
            var id = ObjectId.Null;

            using (var trans = db.TransactionManager.StartTransaction())
            {
                var blkTbl = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                var mdlSpc = trans.GetObject(blkTbl[space ?? BlockTableRecord.ModelSpace],
                    OpenMode.ForWrite) as BlockTableRecord;

                id = mdlSpc.AppendEntity(ent);
                trans.AddNewlyCreatedDBObject(ent, true);

                trans.Commit();
            }

            return id;
        }

        /// <summary>
        /// 将实体集合添加到特定空间。
        /// </summary>
        /// <param name="ents"></param>
        /// <param name="db"></param>
        /// <param name="space"></param>
        /// <returns></returns>
        public static ObjectIdCollection ToSpace(this IEnumerable<Entity> ents,
            Database db = null, string space = null)
        {
            db = db ?? Application.DocumentManager.MdiActiveDocument.Database;
            var ids = new ObjectIdCollection();

            using (var trans = db.TransactionManager.StartTransaction())
            {
                var blkTbl = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                var mdlSpc = trans.GetObject(blkTbl[space ?? BlockTableRecord.ModelSpace],
                    OpenMode.ForWrite) as BlockTableRecord;

                foreach (var ent in ents)
                {
                    ids.Add(mdlSpc.AppendEntity(ent));
                    trans.AddNewlyCreatedDBObject(ent, true);
                }

                trans.Commit();
            }

            return ids;
        }

        /// <summary>
        /// 对实体集合进行遍历并操作。
        /// </summary>
        /// <param name="ents"></param>
        /// <param name="act"></param>
        public static void ForEach(this IEnumerable<Entity> ents, Action<Entity> act)
        {
            foreach (var ent in ents)
            {
                act.Invoke(ent);
            }
        }

        /// <summary>
        /// 遍历特定空间，对实体进行操作。
        /// </summary>
        /// <param name="act"></param>
        /// <param name="db"></param>
        /// <param name="space"></param>
        public static void ForEach(Action<Entity> act, Database db = null, string space = null)
        {
            db = db ?? Application.DocumentManager.MdiActiveDocument.Database;

            using (var trans = db.TransactionManager.StartTransaction())
            {
                var blkTbl = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                var mdlSpc = trans.GetObject(blkTbl[space ?? BlockTableRecord.ModelSpace],
                    OpenMode.ForRead) as BlockTableRecord;

                foreach (var id in mdlSpc)
                {
                    var ent = trans.GetObject(id, OpenMode.ForWrite) as Entity;
                    act.Invoke(ent);
                }

                trans.Commit();
            }
        }

        /// <summary>
        /// 将实体集合转化为块定义。
        /// </summary>
        /// <param name="ents"></param>
        /// <param name="blockName"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static ObjectId ToBlockDefinition(this IEnumerable<Entity> ents,
            string blockName, Database db = null)
        {
            db = db ?? Application.DocumentManager.MdiActiveDocument.Database;
            var id = ObjectId.Null;

            var blkDef = new BlockTableRecord();
            blkDef.Name = blockName;

            foreach (var ent in ents)
            {
                blkDef.AppendEntity(ent);
            }

            using (var trans = db.TransactionManager.StartTransaction())
            {
                var blkTbl = trans.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;

                id = blkTbl.Add(blkDef);
                trans.AddNewlyCreatedDBObject(blkDef, true);

                trans.Commit();
            }

            return id;
        }

        /// <summary>
        /// 将块参照插入到特定空间。
        /// </summary>
        /// <param name="blkDefId"></param>
        /// <param name="transform"></param>
        /// <param name="db"></param>
        /// <param name="space"></param>
        /// <returns></returns>
        public static ObjectId Insert(ObjectId blkDefId, Matrix3d transform,
            Database db = null, string space = null)
        {
            db = db ?? Application.DocumentManager.MdiActiveDocument.Database;
            var id = ObjectId.Null;

            using (var trans = db.TransactionManager.StartTransaction())
            {
                var blkTbl = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                var mdlSpc = trans.GetObject(blkTbl[space ?? BlockTableRecord.ModelSpace],
                    OpenMode.ForWrite) as BlockTableRecord;

                var blkRef = new BlockReference(Point3d.Origin, blkDefId);
                blkRef.BlockTransform = transform;

                id = mdlSpc.AppendEntity(blkRef);
                trans.AddNewlyCreatedDBObject(blkRef, true);

                // 添加属性文字。
                var blkDef = trans.GetObject(blkDefId, OpenMode.ForRead) as BlockTableRecord;
                if (blkDef.HasAttributeDefinitions)
                {
                    foreach (var subId in blkDef)
                    {
                        if (subId.ObjectClass.Equals(RXClass.GetClass(typeof(AttributeDefinition))))
                        {
                            var attrDef = trans.GetObject(subId, OpenMode.ForRead) as AttributeDefinition;

                            //var attrRef = new AttributeReference(
                            //    attrDef.Position.TransformBy(transform),
                            //    attrDef.TextString,
                            //    attrDef.Tag,
                            //    attrDef.TextStyleId);

                            var attrRef = new AttributeReference();
                            attrRef.SetAttributeFromBlock(attrDef, transform);

                            blkRef.AttributeCollection.AppendAttribute(attrRef);
                        }
                    }
                }

                trans.Commit();
            }

            return id;
        }

        /// <summary>
        /// 制定块定义名称，将块参照插入到特定空间。
        /// </summary>
        /// <param name="name"></param>
        /// <param name="transform"></param>
        /// <param name="db"></param>
        /// <param name="space"></param>
        /// <returns></returns>
        public static ObjectId Insert(string name, Matrix3d transform, Database db = null, string space = null)
        {
            db = db ?? Application.DocumentManager.MdiActiveDocument.Database;
            var id = ObjectId.Null;

            using (var trans = db.TransactionManager.StartTransaction())
            {
                var blkTbl = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                if (!blkTbl.Has(name))
                {
                    return ObjectId.Null;
                }
                id = blkTbl[name];
            }

            return Insert(id, transform, db, space);
        }

        /// <summary>
        /// 获取符号。
        /// </summary>
        /// <param name="tblId"></param>
        /// <param name="name"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static ObjectId GetSymbol(ObjectId tblId, string name, Database db = null)
        {
            db = db ?? Application.DocumentManager.MdiActiveDocument.Database;

            using (var trans = db.TransactionManager.StartTransaction())
            {
                var tbl = trans.GetObject(tblId, OpenMode.ForRead) as SymbolTable;
                if (tbl.Has(name))
                {
                    return tbl[name];
                }
            }

            return ObjectId.Null;
        }

        /// <summary>
        /// 新建符号表记录。
        /// </summary>
        /// <param name="record"></param>
        /// <param name="tblId"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static ObjectId ToTable(this SymbolTableRecord record, ObjectId tblId,
            Database db = null)
        {
            db = db ?? Application.DocumentManager.MdiActiveDocument.Database;
            var id = ObjectId.Null;

            using (var trans = db.TransactionManager.StartTransaction())
            {
                var tbl = trans.GetObject(tblId, OpenMode.ForWrite) as SymbolTable;
                if (tbl.Has(record.Name))
                {
                    return tbl[record.Name];
                }

                tbl.Add(record);
                trans.AddNewlyCreatedDBObject(record, true);

                trans.Commit();
            }

            return id;
        }

        /// <summary>
        /// 修改符号表记录。
        /// </summary>
        /// <param name="tblId"></param>
        /// <param name="name"></param>
        /// <param name="act"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static bool ModifySymbol(ObjectId tblId, string name,
            Action<SymbolTableRecord> act, Database db = null)
        {
            db = db ?? Application.DocumentManager.MdiActiveDocument.Database;

            using (var trans = db.TransactionManager.StartTransaction())
            {
                var tbl = trans.GetObject(tblId, OpenMode.ForRead) as SymbolTable;
                if (!tbl.Has(name))
                {
                    return false;
                }

                var symbol = trans.GetObject(tbl[name], OpenMode.ForWrite) as SymbolTableRecord;
                act.Invoke(symbol);

                trans.Commit();
            }

            return true;
        }

        /// <summary>
        /// 删除符号表记录。
        /// </summary>
        /// <param name="tblId"></param>
        /// <param name="name"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static bool RemoveSymbol(ObjectId tblId, string name, Database db = null)
        {
            db = db ?? Application.DocumentManager.MdiActiveDocument.Database;

            using (var trans = db.TransactionManager.StartTransaction())
            {
                var tbl = trans.GetObject(tblId, OpenMode.ForWrite) as SymbolTable;
                if (!tbl.Has(name))
                {
                    return false;
                }

                SymbolTableRecord inUse = null;
                ObjectId inUseId = ObjectId.Null;

                if (tbl is LayerTable)
                {
                    inUseId = db.Clayer;
                }
                else if (tbl is TextStyleTable)
                {
                    inUseId = db.Textstyle;
                }
                else if (tbl is DimStyleTable)
                {
                    inUseId = db.Dimstyle;
                }
                else if (tbl is LinetypeTable)
                {
                    inUseId = db.Celtype;
                }

                if (inUseId.IsValid)
                {
                    inUse = trans.GetObject(inUseId, OpenMode.ForRead) as SymbolTableRecord;
                    if (inUse.Name.ToUpper() == name.ToUpper())
                    {
                        return false;
                    }
                }

                var record = trans.GetObject(tbl[name], OpenMode.ForWrite);
                if (record.IsErased)
                {
                    return false;
                }

                var idCol = new ObjectIdCollection() { record.ObjectId };
                db.Purge(idCol);
                if (idCol.Count == 0)
                {
                    return false;
                }

                record.Erase();
                trans.Commit();
            }

            return true;
        }

        /// <summary>
        /// 向数据库对象添加扩展数据。
        /// 可通过传递空数组实现删除。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="app"></param>
        /// <param name="datas"></param>
        /// <param name="db"></param>
        public static void AttachXData(this DBObject obj, string app,
            IEnumerable<TypedValue> datas, Database db = null)
        {
            db = db ?? Application.DocumentManager.MdiActiveDocument.Database;

            if (GetSymbol(db.RegAppTableId, app, db) == ObjectId.Null)
            {
                new RegAppTableRecord()
                {
                    Name = app,
                }.ToTable(db.RegAppTableId, db);
            }

            var rb = new ResultBuffer();
            rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, app));
            foreach (var data in datas)
            {
                rb.Add(data);
            }

            obj.XData = rb;
        }

        /// <summary>
        /// 向数据库对象添加扩展数据。
        /// 可通过传递空数组实现删除。
        /// </summary>
        /// <param name="objId"></param>
        /// <param name="app"></param>
        /// <param name="datas"></param>
        /// <param name="db"></param>
        public static void AttachXData(this ObjectId objId, string app,
            IEnumerable<TypedValue> datas, Database db = null)
        {
            db = db ?? Application.DocumentManager.MdiActiveDocument.Database;

            using (var trans = db.TransactionManager.StartTransaction())
            {
                var obj = trans.GetObject(objId, OpenMode.ForWrite);
                obj.AttachXData(app, datas, db);

                trans.Commit();
            }
        }

        /// <summary>
        /// 获取数据库对象上的扩展数据。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="app"></param>
        /// <returns></returns>
        public static TypedValue[] GetXData(this DBObject obj, string app)
        {
            return obj.GetXDataForApplication(app)?.AsArray()?.Skip(1)?.ToArray();
        }

        /// <summary>
        /// 获取数据库对象上的扩展数据。
        /// </summary>
        /// <param name="objId"></param>
        /// <param name="app"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static TypedValue[] GetXData(this ObjectId objId, string app,
            Database db = null)
        {
            db = db ?? Application.DocumentManager.MdiActiveDocument.Database;

            using (var trans = db.TransactionManager.StartTransaction())
            {
                var obj = trans.GetObject(objId, OpenMode.ForRead);
                return obj.GetXData(app);
            }
        }

        /// <summary>
        /// 设置数据库对象上的扩展数据（单个值）。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="app"></param>
        /// <param name="idx"></param>
        /// <param name="newVal"></param>
        /// <returns></returns>
        public static TypedValue? SetXData(this DBObject obj, string app, int idx,
            TypedValue newVal)
        {
            var valArr = obj.GetXDataForApplication(app)?.AsArray();
            if (valArr != null && idx + 1 < valArr.Length)
            {
                var oldVal = valArr[idx + 1];

                valArr[idx + 1] = newVal;
                obj.XData = new ResultBuffer(valArr);

                return oldVal;
            }

            return null;
        }

        /// <summary>
        /// 设置数据库对象上的扩展数据（单个值）。
        /// </summary>
        /// <param name="objId"></param>
        /// <param name="app"></param>
        /// <param name="idx"></param>
        /// <param name="newVal"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static TypedValue? SetXData(this ObjectId objId, string app, int idx,
            TypedValue newVal, Database db = null)
        {
            db = db ?? Application.DocumentManager.MdiActiveDocument.Database;

            using (var trans = db.TransactionManager.StartTransaction())
            {
                var obj = trans.GetObject(objId, OpenMode.ForRead);
                return obj.SetXData(app, idx, newVal);
            }
        }

        /// <summary>
        /// 向数据库对象的扩展字典设置值。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="app"></param>
        /// <param name="datas"></param>
        /// <param name="db"></param>
        public static void SetExtData(this DBObject obj, string key, DBObject data,
            Database db = null)
        {
            db = db ?? Application.DocumentManager.MdiActiveDocument.Database;

            if (!obj.ExtensionDictionary.IsValid)
            {
                obj.CreateExtensionDictionary();
            }

            using (var trans = db.TransactionManager.StartTransaction())
            {
                var dict = trans.GetObject(obj.ExtensionDictionary,
                    OpenMode.ForWrite) as DBDictionary;
                dict.SetAt(key, data);
            }
        }

        /// <summary>
        /// 向数据库对象的扩展字典设置值。
        /// </summary>
        /// <param name="objId"></param>
        /// <param name="app"></param>
        /// <param name="datas"></param>
        /// <param name="db"></param>
        public static void SetExtData(this ObjectId objId, string key, DBObject data,
            Database db = null)
        {
            db = db ?? Application.DocumentManager.MdiActiveDocument.Database;

            using (var trans = db.TransactionManager.StartTransaction())
            {
                var obj = trans.GetObject(objId, OpenMode.ForWrite);
                obj.SetExtData(key, data, db);
            }
        }

        /// <summary>
        /// 获取数据库对象的扩展字典数据。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="key"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static ObjectId GetExtData(this DBObject obj, string key, Database db = null)
        {
            db = db ?? Application.DocumentManager.MdiActiveDocument.Database;

            if (obj.ExtensionDictionary.IsValid)
            {
                using (var trans = db.TransactionManager.StartTransaction())
                {
                    var dict = trans.GetObject(obj.ExtensionDictionary,
                        OpenMode.ForRead) as DBDictionary;
                    if (dict.Contains(key))
                    {
                        return dict.GetAt(key);
                    }
                }
            }
            return ObjectId.Null;
        }

        /// <summary>
        /// 获取数据库对象的扩展字典数据。
        /// </summary>
        /// <param name="objId"></param>
        /// <param name="key"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static ObjectId GetExtData(this ObjectId objId, string key, Database db = null)
        {
            db = db ?? Application.DocumentManager.MdiActiveDocument.Database;

            using (var trans = db.TransactionManager.StartTransaction())
            {
                var obj = trans.GetObject(objId, OpenMode.ForRead);
                return obj.GetExtData(key, db);
            }
        }

        /// <summary>
        /// 修改扩展字典中的数据。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="act"></param>
        /// <param name="key"></param>
        /// <param name="db"></param>
        public static void ModifyExtData(this DBObject obj, Action<DBObject> act,
            string key, Database db = null)
        {
            db = db ?? Application.DocumentManager.MdiActiveDocument.Database;
            var dataId = obj.GetExtData(key, db);

            using (var trans = db.TransactionManager.StartTransaction())
            {
                var data = trans.GetObject(dataId, OpenMode.ForWrite);
                act.Invoke(data);
            }
        }

        /// <summary>
        /// 修改扩展字典中的数据。
        /// </summary>
        /// <param name="objId"></param>
        /// <param name="act"></param>
        /// <param name="key"></param>
        /// <param name="db"></param>
        public static void ModifyExtData(this ObjectId objId, Action<DBObject> act,
            string key, Database db = null)
        {
            db = db ?? Application.DocumentManager.MdiActiveDocument.Database;

            using (var trans = db.TransactionManager.StartTransaction())
            {
                var obj = trans.GetObject(objId, OpenMode.ForRead);
                obj.ModifyExtData(act, key, db);
            }
        }
    }
}
