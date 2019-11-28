using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Colors;

namespace ECDQiangWuCha
{
    public class QiangWuCha
    {
        Document Doc = Application.DocumentManager.MdiActiveDocument;
        Editor Ed = Application.DocumentManager.MdiActiveDocument.Editor;
        Database Db = Application.DocumentManager.MdiActiveDocument.Database;

        [CommandMethod("EcdQiangWuCha")]
        public void TZWuCha()
        {

            int length = 100;


            TypedValue[] tv = new TypedValue[2] { new TypedValue((int)DxfCode.Start, "POLYLINE"), new TypedValue((int)DxfCode.Start, "DIMENSION") };

            SelectionFilter sf = new SelectionFilter(tv);

            //var selRes = Ed.GetSelection(sf);
            var selRes = Ed.GetSelection();

            if (selRes.Status != PromptStatus.OK) return;

            var listIds = selRes.Value.GetObjectIds().ToList();


            List<Polyline> listPl = new List<Polyline>();
            List<Dimension> listDim = new List<Dimension>();

            using (var trans = Db.TransactionManager.StartTransaction())
            {

                foreach (ObjectId oId in listIds)
                {

                    var ent = trans.GetObject(oId, OpenMode.ForRead) as Entity;

                    if ((ent as Polyline) != null)
                    {

                        listPl.Add((Polyline)ent);

                    }
                    else if ((ent as Dimension) != null)
                    {
                        listDim.Add((Dimension)ent);
                    }
                }
                trans.Commit();
            }

            var propDbOpts = new PromptDoubleOptions("请输入误差范围:");

            var dbRes = Ed.GetDouble(propDbOpts);

            if (dbRes.Status != PromptStatus.OK) return;

            double lMin = length - dbRes.Value;
            double lMax = length + dbRes.Value;

            List<Vector3d> listVec3d = new List<Vector3d>();
            List<double> listDis = new List<double>();

            List<Point3d> ptArr = new List<Point3d>();

            Polyline plSF = null;
            Polyline plST = null;
            Polyline plSW = null;


            int indexSF = 0;
            int indexST = 0;
            int indexSW = 0;

            List<Polyline> listJoinPL = new List<Polyline>();
            //找到相连的pl组成一组加入到集合中
            foreach (var pl in listPl)
            {
                ptArr = Get4Pt(pl);
                if (ptArr.Count == 3)
                {
                    plST = pl;
                    indexST = listPl.IndexOf(plST);
                    listJoinPL.Add(plST);
                    continue;
                }
                else if (ptArr.Count == 2)
                {
                    plSW = pl;
                    indexSW = listPl.IndexOf(plSW);
                    listJoinPL.Add(plSW);
                    continue;
                }
                else if (ptArr.Count == 4)
                {
                    var listVec = new List<Vector3d>();
                    var listDb = new List<double>();
                    for (int i = 0; i < ptArr.Count - 1; i++)
                    {
                        var ptF1 = ptArr[i];
                        var ptF2 = ptArr[i + 1];

                        var vecF = ptF2 - ptF1;

                        listVec.Add(vecF);
                        listDb.Add(Math.Round(vecF.Length, 3));
                    }


                    var v1 = listVec[0];
                    var v2 = listVec[1];
                    var v3 = listVec[2];


                    if (Math.Abs(listDb[listVec.IndexOf(v1)] - listDb[listVec.IndexOf(v3)]) > 2 * dbRes.Value)
                    {

                        plSF = pl;
                        indexSF = listPl.IndexOf(plSF);
                        listJoinPL.Add(plSF);

                        continue;

                    }


                }

            }

            bool flag = false;


            List<int> listInt = new List<int>();
            List<Polyline> listMerged = new List<Polyline>();
 
            if (listJoinPL.Count > 1)
            {


                using (var trans = Db.TransactionManager.StartTransaction())
                {
                    for (int j = 0; j < listJoinPL.Count; j++)
                    {
                        if (listInt.Contains(listPl.IndexOf(listJoinPL[j])))
                        {
                            continue;
                        }


                        var plJoin = trans.GetObject(listJoinPL[j].ObjectId, OpenMode.ForWrite) as Polyline;
                        listInt.Add(listPl.IndexOf(plJoin));
                        for (int i = j + 1; i < listJoinPL.Count; i++)
                        {
                            if (listInt.Contains(listPl.IndexOf(listJoinPL[i])))
                            {
                                continue;
                            }

                            var plJoin2 = trans.GetObject(listJoinPL[i].ObjectId, OpenMode.ForWrite) as Polyline;

                            try
                            {
                                plJoin.JoinEntity(plJoin2);
                                flag = true;
                                listInt.Add(listPl.IndexOf(plJoin2));

                            }
                            catch (System.Exception ex)
                            {


                                plJoin2.DowngradeOpen();

                                Ed.WriteMessage(ex.ToString());

                                continue;
                            }
                        }

                        if (flag)
                        {
                            listMerged.Add(plJoin);
                            flag = false;
                            plJoin.DowngradeOpen();
                        }
                    }
                    trans.Commit();
                }

            }
            listInt = listInt.Distinct().ToList();

            if (listInt.Count > 1)
            {

                for (int i = 0; i < listInt.Count; i++)
                {

                    listPl[listInt[i]] = null;

                }

                listPl = listPl.Where(p => p != null).ToList();

            }

            listPl.AddRange(listMerged);

            listInt.Clear();
            listJoinPL.Clear();
            listMerged.Clear();

            //分点的个数分别处理
            foreach (Polyline pl in listPl)
            {

                listVec3d.Clear();
                listDis.Clear();
                ptArr.Clear();


                ptArr = Get4Pt(pl);

                int idxAngle90 = GetAngle(ref ptArr);
                /*
                if (ptArr.Count == 5 && !pl.Closed && ptArr[0] != ptArr[4])
                {

                    ptArr = ptArr.Distinct(new PointCompare()).ToList();

                }

                if (ptArr.Count == 5)
                {
                    Vector3d v1 = ptArr[1] - ptArr[0];
                    Vector3d v2 = ptArr[2] - ptArr[1];
                    Vector3d v3 = ptArr[3] - ptArr[2];
                    Vector3d v4 = ptArr[4] - ptArr[3];

                    //dicVecDb.Add(v1, Math.Round(v1.Length, 3));
                    //dicVecDb.Add(v2, Math.Round(v2.Length, 3));
                    //dicVecDb.Add(v3, Math.Round(v3.Length, 3));
                    //dicVecDb.Add(v4, Math.Round(v4.Length, 3));

                    listVec3d.Add(v1);
                    listVec3d.Add(v2);
                    listVec3d.Add(v3);
                    listVec3d.Add(v4);

                    listDis.Add(Math.Round(v1.Length, 3));
                    listDis.Add(Math.Round(v2.Length, 3));
                    listDis.Add(Math.Round(v3.Length, 3));
                    listDis.Add(Math.Round(v4.Length, 3));


                    double v1agl = Math.Abs(v1.GetAngleTo(v2) - Math.PI / 2);
                    double v3agl = Math.Abs(v2.GetAngleTo(v3) - Math.PI / 2);
                    double v4agl = Math.Abs(v1.GetAngleTo(v4) - Math.PI / 2);

                    int indexPt1 = 0;
                    int indexPt2 = 0;
                    //求有问题得点
                    //v1不是需要计算的误差
                    if (listDis[listVec3d.IndexOf(v1)] > lMax || listDis[listVec3d.IndexOf(v1)] < lMin)
                    //if (dicVecDb[v1] > lMax || dicVecDb[v1] < lMin)
                    {
                        var v2Db = Math.Abs(length - listDis[listVec3d.IndexOf(v2)]);
                        var v4Db = Math.Abs(length - listDis[listVec3d.IndexOf(v4)]);
                        if (listDis[listVec3d.IndexOf(v1)] < listDis[listVec3d.IndexOf(v3)] || v1agl < v3agl)
                        //if (dicVecDb[v1] < dicVecDb[v3] || v1agl < v3agl)
                        {

                            if (v2Db > v4Db)
                            {
                                indexPt1 = 1;
                                indexPt2 = 2;
                            }
                            else
                            {

                                indexPt1 = 4;
                                indexPt2 = 3;
                            }

                        }
                        else
                        {
                            if (v2Db > v4Db)
                            {
                                indexPt1 = 2;
                                indexPt2 = 1;
                            }
                            else
                            {

                                indexPt1 = 3;
                                indexPt2 = 4;
                            }
                        }

                    }
                    else
                    {

                        var v1Db = Math.Abs(length - listDis[listVec3d.IndexOf(v1)]);
                        var v3Db = Math.Abs(length - listDis[listVec3d.IndexOf(v3)]);

                        //if (dicVecDb[v2] < dicVecDb[v4] || v1agl < v4agl)
                        if (listDis[listVec3d.IndexOf(v2)] < listDis[listVec3d.IndexOf(v4)] || v1agl < v4agl)
                        {

                            if (v1Db > v3Db)
                            {
                                indexPt1 = 1;
                                indexPt2 = 0;
                            }
                            else
                            {
                                indexPt1 = 2;
                                indexPt2 = 3;
                            }
                        }
                        else
                        {
                            if (v1Db > v3Db)
                            {
                                indexPt1 = 0;
                                indexPt2 = 1;
                            }
                            else
                            {
                                indexPt1 = 3;
                                indexPt2 = 2;
                            }
                        }
                    }

                    var pt1 = ptArr[indexPt1];
                    var wrongPt = ptArr[indexPt2];

                    var vec = (wrongPt - pt1).GetNormal();

                    var jzPt = pt1 + vec * length;

                    ptArr[indexPt2] = jzPt;

                    if (indexPt2 == 4)
                    {
                        ptArr[0] = jzPt;
                    }

                    DimToSpace(ptArr, listDim, pt1, wrongPt, indexPt2);

                    //Ed.WriteMessage($"{v1.Length}\n,{v2.Length}\n{v3.Length}\n{v4.Length}\n");
                    //Ed.WriteMessage($"{v1.GetAngleTo(v2) * 180 / Math.PI}\n");
                    //Ed.WriteMessage($"{v2.GetAngleTo(v3) * 180 / Math.PI}\n");
                    //Ed.WriteMessage($"{v3.GetAngleTo(v4) * 180 / Math.PI}\n");
                    //Ed.WriteMessage($"{v4.GetAngleTo(v1) * 180 / Math.PI}\n");
                }
                else if (ptArr.Count == 4 && !pl.Closed)
                {
                    for (int i = 0; i < ptArr.Count - 1; i++)
                    {
                        var ptF1 = ptArr[i];
                        var ptF2 = ptArr[i + 1];

                        var vecF = ptF2 - ptF1;

                        //dicVecDb.Add(vecF, Math.Round(vecF.Length, 3));

                        listVec3d.Add(vecF);
                        listDis.Add(Math.Round(vecF.Length, 3));

                    }

                    var v1 = listVec3d[0];
                    var v2 = listVec3d[1];
                    var v3 = listVec3d[2];
                    var v4 = ptArr[0] - ptArr[3];

                    double v1agl = Math.Abs(v1.GetAngleTo(v2) - Math.PI / 2);
                    double v4agl = Math.Abs(v1.GetAngleTo(v4) - Math.PI / 2);

                    int indexPt1 = 0;
                    int indexPt2 = 0;

                    double diffV1 = Math.Abs(dbRes.Value - Math.Abs(length - listDis[listVec3d.IndexOf(v1)]));
                    double diffV3 = Math.Abs(dbRes.Value - Math.Abs(length - listDis[listVec3d.IndexOf(v3)]));
                    if (diffV1 < diffV3)
                    {
                        if (v1.X > 0)
                        {
                            indexPt1 = 0;
                            indexPt2 = 1;
                        }
                        else
                        {
                            indexPt1 = 1;
                            indexPt2 = 0;
                        }
                    }
                    else if (diffV1 >= diffV3)
                    {
                        if (v3.X > 0)
                        {
                            indexPt1 = 2;
                            indexPt2 = 3;
                        }
                        else
                        {
                            indexPt1 = 3;
                            indexPt2 = 2;
                        }

                    }
                    var pt1 = ptArr[indexPt1];
                    var wrongPt = ptArr[indexPt2];

                    var vec = (wrongPt - pt1).GetNormal();

                    var jzPt = pt1 + vec * length;

                    ptArr[indexPt2] = jzPt;

                    DimToSpace(ptArr, listDim, pt1, wrongPt, indexPt2);

                }
                else if (ptArr.Count == 6 && !pl.Closed)
                {


                    Vector3d v1 = ptArr[2] - ptArr[1];
                    Vector3d v2 = ptArr[3] - ptArr[2];
                    Vector3d v3 = ptArr[4] - ptArr[3];
                    Vector3d v4 = ptArr[4] - ptArr[1];
                    Vector3d v5 = ptArr[0] - ptArr[1];
                    Vector3d v6 = ptArr[4] - ptArr[5];
                   
                    listVec3d.Add(v1);
                    listVec3d.Add(v2);
                    listVec3d.Add(v3);
                    listVec3d.Add(v4);
                    listVec3d.Add(v5);
                    listVec3d.Add(v6);

                    listDis.Add(Math.Round(v1.Length, 3));
                    listDis.Add(Math.Round(v2.Length, 3));
                    listDis.Add(Math.Round(v3.Length, 3));
                    listDis.Add(Math.Round(v4.Length, 3));
                    listDis.Add(Math.Round(v5.Length, 3));
                    listDis.Add(Math.Round(v6.Length, 3));



                    double v1agl = Math.Abs(v1.GetAngleTo(v2) - Math.PI / 2);
                    double v3agl = Math.Abs(v2.GetAngleTo(v3) - Math.PI / 2);
                    double v4agl = Math.Abs(v1.GetAngleTo(v4) - Math.PI / 2);
                    int indexPt1 = 0;
                    int indexPt2 = 0;


                    for (int m = 0; m <= 3; m++)
                    {


                        //求有问题得点
                        //v1 不是要求误差的边
                        if (listDis[m] >= lMax || listDis[m] <= lMin)
                        {
                            var v2Db = Math.Abs(length - listDis[(m + 1) % 2]);
                            var v4Db = Math.Abs(length - listDis[(m + 3) % 2 + 2]);
                            if (listDis[m] < listDis[(m + 2) % 2] || v1agl < v3agl)
                            {

                                if (v2Db > v4Db)
                                {
                                    indexPt1 = 2;
                                    indexPt2 = 3;
                                }
                                else
                                {

                                    indexPt1 = 1;
                                    indexPt2 = 4;
                                }

                            }
                            else
                            {
                                if (v2Db > v4Db)
                                {
                                    indexPt1 = 3;
                                    indexPt2 = 2;
                                }
                                else
                                {

                                    indexPt1 = 4;
                                    indexPt2 = 1;
                                }
                            }

                        }
                        else
                        {

                            var v1Db = Math.Abs(length - listDis[m]);
                            var v3Db = Math.Abs(length - listDis[m + 2]);
                            if (listDis[(m + 1) % 2] < listDis[(m + 3) % 2 + 2] || v1agl < v4agl)
                            {

                                if (v1Db > v3Db)
                                {
                                    indexPt1 = 2;
                                    indexPt2 = 1;
                                }
                                else
                                {
                                    indexPt1 = 3;
                                    indexPt2 = 4;
                                }
                            }
                            else
                            {
                                if (v1Db > v3Db)
                                {
                                    indexPt1 = 1;
                                    indexPt2 = 2;
                                }
                                else
                                {
                                    indexPt1 = 4;
                                    indexPt2 = 3;
                                }
                            }
                        }
                        var wrongPt = Point3d.Origin;
                        var pt1 = Point3d.Origin;
                        if (!((indexPt1 == 1 && indexPt2 == 4) && (indexPt1 == 4 && indexPt2 == 1)))
                        {
                            pt1 = ptArr[indexPt1];
                            wrongPt = ptArr[indexPt2];

                            var vec1 = wrongPt - pt1;
                            var vec2 = pt1 - wrongPt;

                            int index = -1;

                            if (listVec3d.IndexOf(vec1) != -1)
                            {
                                index = listVec3d.IndexOf(vec1);

                            }
                            else if (listVec3d.IndexOf(vec2) != -1)
                            {
                                index = listVec3d.IndexOf(vec2);
                            }


                            var vec = (wrongPt - pt1).GetNormal();

                            var jzPt = pt1 + vec * length;

                            ptArr[indexPt2] = jzPt;

                            if (index != -1)
                            {
                                listVec3d[index] = pt1 - jzPt;
                                listDis[index] = (pt1 - jzPt).Length;
                            }
                        }
                        else
                        {

                            if (indexPt1 == 1 && indexPt2 == 4)
                            {
                                wrongPt = ptArr[indexPt2];
                                var ptSpecial = ptArr[indexPt1];
                                var vec1 = wrongPt - ptSpecial;
                                var vec2 = ptSpecial - wrongPt;

                                if (ptArr[0].Y < ptArr[5].Y || ptArr[0].X < ptArr[5].X)

                                    pt1 = ptArr[0];
                                else
                                    pt1 = ptArr[5];



                                var vec = wrongPt - pt1;


                                int index = -1;

                                if (listVec3d.IndexOf(vec1) != -1)
                                {
                                    index = listVec3d.IndexOf(vec1);

                                }
                                else if (listVec3d.IndexOf(vec2) != -1)
                                {
                                    index = listVec3d.IndexOf(vec2);
                                }

                                var jzPt = pt1 + vec.GetNormal() * (vec.Length + dbRes.Value);

                                ptArr[indexPt2] = jzPt;

                                if (index != -1)
                                {
                                    listVec3d[index] = ptSpecial - jzPt;
                                    listDis[index] = (ptSpecial - jzPt).Length;
                                }

                            }
                            if (indexPt1 == 4 && indexPt2 == 1)
                            {
                                wrongPt = ptArr[indexPt2];
                                var ptSpecial = ptArr[indexPt1];
                                var vec1 = wrongPt - ptSpecial;
                                var vec2 = ptSpecial - wrongPt;

                                if (ptArr[0].Y < ptArr[5].Y || ptArr[0].X < ptArr[5].X)

                                    pt1 = ptArr[5];
                                else
                                    pt1 = ptArr[0];

                                int index = -1;

                                if (listVec3d.IndexOf(vec1) != -1)
                                {
                                    index = listVec3d.IndexOf(vec1);

                                }
                                else if (listVec3d.IndexOf(vec2) != -1)
                                {
                                    index = listVec3d.IndexOf(vec2);
                                }

                                var vec = wrongPt - pt1;

                                var jzPt = pt1 + vec.GetNormal() * (vec.Length + dbRes.Value);

                                ptArr[indexPt2] = jzPt;

                                if (index != -1)
                                {
                                    listVec3d[index] = ptSpecial - jzPt;
                                    listDis[index] = (ptSpecial - jzPt).Length;
                                }

                            }

                        }

                        if (!((indexPt1 == 1 && indexPt2 == 4) && (indexPt1 == 4 && indexPt2 == 1)))
                        {
                            DimToSpace(ptArr, listDim, pt1, wrongPt, indexPt2);

                        }
                        else
                        {
                            if (indexPt1 == 1 && indexPt2 == 4)
                            {
                                DimToSpace2(ptArr, listDim, ptArr[1], wrongPt, indexPt2);
                            }
                            if (indexPt1 == 4 && indexPt2 == 1)
                            {
                                DimToSpace2(ptArr, listDim, ptArr[4], wrongPt, indexPt2);
                            }
                        }

                    }
                }

                Polyline plJz2 = new Polyline();

                foreach (var point in ptArr)
                {
                    plJz2.AddVertexAt(plJz2.NumberOfVertices, new Point2d(point.X, point.Y), 0, 0, 0);
                }
               
                plJz2.ColorIndex = 6;

                Pl2Pl(plJz2, pl);

                plJz2.ToSpace();

                using (var trans = Db.TransactionManager.StartTransaction())
                {


                    var ent2 = trans.GetObject(pl.ObjectId, OpenMode.ForWrite) as Entity;

                    ent2.Erase(true);

                    trans.Commit();

                }*/
            }

        }

        private int GetAngle(ref List<Point3d> ptArr)
        {
            int count = ptArr.Count;

            int sIndex = 0;
            int eIndex = 0;

            if (count == 4)
            {
                sIndex = 0;
                eIndex = 4;

            }
            else if (count == 5)
            {
                sIndex = 0;
                eIndex = 5;
            }
            else if (count == 6)
            {

                sIndex = 1;
                eIndex = 5;
            }

            List<Vector3d> listVec = new List<Vector3d>();
            List<double> listAngle = new List<double>();

            for (int i = sIndex; i < eIndex; i++)
            {
                Vector3d vecF = new Vector3d();
                if (eIndex == 5 && sIndex == 0)
                {
                    if (i == 4) { continue; }

                    var ptF1 = ptArr[i];
                    var ptF2 = ptArr[(i + 1) % eIndex];
                    vecF = ptF2 - ptF1;
                }
                else if (eIndex == 4 && sIndex == 0)
                {
                    if (i == 3)
                    {
                        break;
                    }

                    var ptF1 = ptArr[i];
                    var ptF2 = ptArr[i + 1];
                    vecF = ptF2 - ptF1;
                }
                else if (eIndex == 5 && sIndex == 1)
                {
                    var ptF1 = ptArr[i];
                    Point3d ptF2 = Point3d.Origin;

                    if (i != 4)
                        ptF2 = ptArr[i + 1];
                    else
                        ptF2 = ptArr[(i + 2) % eIndex];

                    vecF = ptF2 - ptF1;
                }

                listVec.Add(vecF);
            }
            for (int j = 0; j < listVec.Count; j++)
            {
                var v0 = listVec[j];
                var v1 = listVec[(j + 1) % listVec.Count];

                listAngle.Add(v0.GetAngleTo(v1));
            }

            int bigangleIndex = -1;

            double find90 = 90.0d;

            for (int m = 0; m < listAngle.Count; m++)
            {
                double diff = Math.Abs(Math.PI / 2 - listAngle[m]);

                if (find90 >= diff)
                {
                    find90 = diff;
                    bigangleIndex = m;
                }
            }
            var findVec = new Vector3d();
            var findPt = Point3d.Origin;
            if (listVec.Count == 3)
            {

                findVec = listVec[1 + bigangleIndex];
                findPt = ptArr[1+bigangleIndex];

            }
            else if (listVec.Count == 4 && ptArr.Count == 5)
            {
                findVec = listVec[(bigangleIndex + 1) % listAngle.Count];
                findPt = ptArr[(bigangleIndex + 1) % listAngle.Count];
            }
            else if (listVec.Count == 4 && ptArr.Count == 6)
            {
                findVec = listVec[(bigangleIndex + 1) % listAngle.Count];
                if(bigangleIndex!=3)
                findPt = ptArr[(bigangleIndex +2)];
                else
                    findPt= ptArr[1];
            }

            if (find90 >= 1.0 / 180 * Math.PI)
            {

                var vn = findVec.GetNormal();

                var v = new Vector3d(Math.Round(vn.X), Math.Round(vn.Y), Math.Round(vn.Z));

                var ptNew = findPt + v * findVec.Length;

                int i = ptArr.IndexOf(findPt);


                if (count == 5)
                {

                    if (i != 3)
                    {
                        ptArr[(i + 1)] = ptNew;

                    }
                    else
                    {
                        ptArr[0] = ptNew;
                    }

                }
                else if (count == 6)
                {
                    if (i != 4)
                    {
                        ptArr[(i + 1)] = ptNew;
                    }
                    else
                    {
                        ptArr[1] = ptNew;
                    }

                }
                else
                {
                    ptArr[(i + 1)] = ptNew;
                }

            }
            Ed.WriteMessage($"find90={find90 / Math.PI * 180.0 }\nbigangleIndex={bigangleIndex}\nfindPt={findPt}");

            return bigangleIndex;
        }

        private void DimToSpace(List<Point3d> ptArr, List<Dimension> listDim, Point3d pt1, Point3d wrongPt, int indexPt2)
        {

            RotatedDimension dimOld = null;
            RotatedDimension dimOld2 = null;
            int indexPt = ptArr.IndexOf(pt1);

            Point3d pt3 = Point3d.Origin;

            if (indexPt < indexPt2)
            {
                pt3 = ptArr[(indexPt2 + 1) % ptArr.Count];
            }
            else
            {
                pt3 = ptArr[Math.Abs((indexPt2 - 1)) % ptArr.Count];
            }
            foreach (var d in listDim)
            {

                if ((d as RotatedDimension) != null)
                {

                    dimOld = d as RotatedDimension;


                    if ((PtEqual(dimOld.XLine1Point, pt1) && PtEqual(dimOld.XLine2Point, wrongPt))
                        || (PtEqual(dimOld.XLine1Point, wrongPt) && PtEqual(dimOld.XLine2Point, pt1)))
                    {
                        dimOld = d as RotatedDimension;
                        break;

                    }
                    else
                    {
                        dimOld = null;
                    }

                }

            }

            foreach (var d in listDim)
            {

                if ((d as RotatedDimension) != null)
                {

                    dimOld2 = d as RotatedDimension;


                    if ((PtEqual(dimOld2.XLine1Point, pt3) && PtEqual(dimOld2.XLine2Point, wrongPt)) ||
                        (PtEqual(dimOld2.XLine1Point, wrongPt) && PtEqual(dimOld2.XLine2Point, pt3)))
                    {
                        dimOld2 = d as RotatedDimension;

                        if (dimOld2 != dimOld)
                        {
                            break;
                        }
                        else
                        {
                            dimOld2 = null;
                        }

                    }
                    else
                    {
                        dimOld2 = null;
                    }
                }

            }
            double dimLen = 20d;
            if (null != dimOld)
            {

                var line = new Line(dimOld.XLine1Point, dimOld.XLine2Point);
                var ptDim = dimOld.DimLinePoint;

                var ptDim2 = line.GetClosestPointTo(ptDim, true);

                var v2 = ptDim - ptDim2;

                dimLen = v2.Length;

                var ptGet = pt1 + v2;

                Vector3d v = pt1 - ptArr[indexPt2];

                //var dimNew = new RotatedDimension(0, pt1, ptArr[indexPt2], ptGet, v.Length.ToString("f2"), dimOld.DimensionStyle);

                var dimNew = new AlignedDimension(pt1, ptArr[indexPt2], ptGet, v.Length.ToString("f2"), dimOld.DimensionStyle);
                Dim2Dim(dimNew, dimOld);
                dimNew.ToSpace();

                using (var trans = Db.TransactionManager.StartTransaction())
                {
                    var ent = trans.GetObject(dimOld.ObjectId, OpenMode.ForWrite) as Entity;

                    ent.Erase(true);

                    trans.Commit();

                }
            }
            if (dimOld2 != null)
            {
                var line = new Line(dimOld2.XLine1Point, dimOld2.XLine2Point);
                var ptDim = dimOld2.DimLinePoint;

                var ptDim2 = line.GetClosestPointTo(ptDim, true);

                var v2 = ptDim - ptDim2;

                dimLen = v2.Length;

                var ptGet = ptArr[indexPt2] + v2;
                Vector3d v3 = ptArr[indexPt2] - pt3;
                //Point3d midPoint = new Point3d((ptArr[indexPt2].X + pt3.X) / 2.0,
                //(ptArr[indexPt2].Y + pt3.Y) / 2.0,
                //                  (ptArr[indexPt2].Z + pt3.Z) / 2.0);

                //var pt4 = midPoint + v4.GetNormal() * dimLen;
                //var dimNew2 = new RotatedDimension(0, ptArr[indexPt2], pt3, pt4, v3.Length.ToString(), dimOld2.DimensionStyle);
                var dimNew2 = new AlignedDimension(ptArr[indexPt2], pt3, ptGet, v3.Length.ToString("f2"), dimOld2.DimensionStyle);

                Dim2Dim(dimNew2, dimOld2);

                dimNew2.ToSpace();

                using (var trans = Db.TransactionManager.StartTransaction())
                {
                    //var ent = trans.GetObject(dimOld.ObjectId, OpenMode.ForWrite) as Entity;

                    // ent.Erase(true);

                    var ent2 = trans.GetObject(dimOld2.ObjectId, OpenMode.ForWrite) as Entity;

                    ent2.Erase(true);

                    trans.Commit();

                }

            }

        }

        private void DimToSpace2(List<Point3d> ptArr, List<Dimension> listDim, Point3d pt1, Point3d wrongPt, int indexPt2)
        {

            RotatedDimension dimOld = null;
            RotatedDimension dimOld2 = null;
            int indexPt = ptArr.IndexOf(pt1);

            Point3d pt3 = Point3d.Origin;

            if (indexPt < indexPt2)
            {
                pt3 = ptArr[Math.Abs((indexPt2 - 1)) % ptArr.Count];
            }
            else
            {
                pt3 = ptArr[(indexPt2 + 1) % ptArr.Count];
            }
            foreach (var d in listDim)
            {

                if ((d as RotatedDimension) != null)
                {

                    dimOld = d as RotatedDimension;


                    if ((PtEqual(dimOld.XLine1Point, pt1) && PtEqual(dimOld.XLine2Point, wrongPt))
                        || (PtEqual(dimOld.XLine1Point, wrongPt) && PtEqual(dimOld.XLine2Point, pt1)))
                    {
                        dimOld = d as RotatedDimension;
                        break;

                    }
                    else
                    {
                        dimOld = null;
                    }

                }

            }

            foreach (var d in listDim)
            {

                if ((d as RotatedDimension) != null)
                {

                    dimOld2 = d as RotatedDimension;


                    if ((PtEqual(dimOld2.XLine1Point, pt3) && PtEqual(dimOld2.XLine2Point, wrongPt)) ||
                        (PtEqual(dimOld2.XLine1Point, wrongPt) && PtEqual(dimOld2.XLine2Point, pt3)))
                    {
                        dimOld2 = d as RotatedDimension;

                        if (dimOld2 != dimOld)
                        {
                            break;
                        }
                        else
                        {
                            dimOld2 = null;
                        }

                    }
                    else
                    {
                        dimOld2 = null;
                    }
                }

            }
            double dimLen = 20d;
            if (null != dimOld)
            {

                var line = new Line(dimOld.XLine1Point, dimOld.XLine2Point);
                var ptDim = dimOld.DimLinePoint;

                var ptDim2 = line.GetClosestPointTo(ptDim, true);

                var v2 = ptDim - ptDim2;

                dimLen = v2.Length;

                var ptGet = pt1 + v2;

                Vector3d v = pt1 - ptArr[indexPt2];

                //var dimNew = new RotatedDimension(0, pt1, ptArr[indexPt2], ptGet, v.Length.ToString(), dimOld.DimensionStyle);
                var dimNew = new AlignedDimension(pt1, ptArr[indexPt2], ptGet, v.Length.ToString("f2"), dimOld.DimensionStyle);
                Dim2Dim(dimNew, dimOld);
                dimNew.ToSpace();

                using (var trans = Db.TransactionManager.StartTransaction())
                {
                    var ent = trans.GetObject(dimOld.ObjectId, OpenMode.ForWrite) as Entity;

                    ent.Erase(true);

                    trans.Commit();

                }
            }
            if (dimOld2 != null)
            {
                var line = new Line(dimOld2.XLine1Point, dimOld2.XLine2Point);
                var ptDim = dimOld2.DimLinePoint;

                var ptDim2 = line.GetClosestPointTo(ptDim, true);

                var v2 = ptDim - ptDim2;

                dimLen = v2.Length;

                var ptGet = ptArr[indexPt2] + v2;
                Vector3d v3 = ptArr[indexPt2] - pt3;
                //var dimNew2 = new RotatedDimension(0, ptArr[indexPt2], pt3, pt4, v3.Length.ToString(), dimOld2.DimensionStyle);
                var dimNew2 = new AlignedDimension(ptArr[indexPt2], pt3, ptGet, v3.Length.ToString("f2"), dimOld2.DimensionStyle);

                Dim2Dim(dimNew2, dimOld2);

                dimNew2.ToSpace();

                using (var trans = Db.TransactionManager.StartTransaction())
                {
                    //var ent = trans.GetObject(dimOld.ObjectId, OpenMode.ForWrite) as Entity;

                    // ent.Erase(true);

                    var ent2 = trans.GetObject(dimOld2.ObjectId, OpenMode.ForWrite) as Entity;

                    ent2.Erase(true);

                    trans.Commit();

                }

            }

        }

        private bool PtEqual(Point3d p1, Point3d p2)
        {

            if (p1.X.ToString("f5") == p2.X.ToString("f5") && p1.Y.ToString("f5") == p2.Y.ToString("f5") && p1.Z.ToString("f5") == p2.Z.ToString("f5"))
                return true;
            return false;
        }
        private void Pl2Pl(Polyline plJz, Polyline pl)
        {
            plJz.LayerId = pl.LayerId;
            plJz.LinetypeId = pl.LinetypeId;
            plJz.Thickness = pl.Thickness;
            plJz.Transparency = pl.Transparency;
            plJz.XData = pl.XData;
            plJz.LineWeight = pl.LineWeight;
            plJz.LinetypeScale = pl.LinetypeScale;
        }

        void Dim2Dim(Dimension rDim1, Dimension rDim)
        {

            rDim1.TextStyleId = rDim.TextStyleId;
            rDim1.TextRotation = rDim.TextRotation;
            rDim1.TextPosition = rDim.TextPosition;
            rDim1.ToleranceSuppressLeadingZeros = rDim.ToleranceSuppressLeadingZeros;
            rDim1.ToleranceSuppressTrailingZeros = rDim.ToleranceSuppressTrailingZeros;
            rDim1.ToleranceSuppressZeroFeet = rDim.ToleranceSuppressZeroFeet;
            rDim1.ToleranceSuppressZeroInches = rDim.ToleranceSuppressZeroInches;
            rDim1.Transparency = rDim.Transparency;
            rDim1.UsingDefaultTextPosition = rDim.UsingDefaultTextPosition;
            rDim1.Visible = rDim.Visible;
            rDim1.VisualStyleId = rDim.VisualStyleId;
            rDim1.XData = rDim.XData;
            rDim1.TextLineSpacingStyle = rDim.TextLineSpacingStyle;
            rDim1.TextLineSpacingFactor = rDim.TextLineSpacingFactor;
            rDim1.TextDefinedSize = rDim.TextDefinedSize;
            rDim1.TextAttachment = rDim.TextAttachment;
            rDim1.SuppressZeroInches = rDim.SuppressZeroInches;
            rDim1.SuppressZeroFeet = rDim.SuppressZeroFeet;
            rDim1.SuppressTrailingZeros = rDim.SuppressTrailingZeros;
            rDim1.SuppressLeadingZeros = rDim.SuppressLeadingZeros;
            rDim1.SuppressAngularTrailingZeros = rDim.SuppressAngularTrailingZeros;
            rDim1.SuppressAngularLeadingZeros = rDim.SuppressAngularLeadingZeros;
            rDim1.Suffix = rDim.Suffix;
            rDim1.Prefix = rDim.Prefix;
            rDim1.PlotStyleNameId = rDim.PlotStyleNameId;



            rDim1.LineWeight = rDim.LineWeight;
            rDim1.LinetypeScale = rDim.LinetypeScale;
            rDim1.Linetype = rDim.Linetype;
            rDim1.LayerId = rDim.LayerId;

            rDim1.HasSaveVersionOverride = rDim.HasSaveVersionOverride;
            rDim1.ForceAnnoAllVisible = rDim.ForceAnnoAllVisible;
            rDim1.FaceStyleId = rDim.FaceStyleId;
            rDim1.Elevation = rDim.Elevation;
            rDim1.EdgeStyleId = rDim.EdgeStyleId;
        }

        private List<Point3d> Get4Pt(Polyline pl)
        {

            List<Point3d> listPt = new List<Point3d>();
            for (int i = 0; i < pl.NumberOfVertices; i++)
            {
                listPt.Add(pl.GetPoint3dAt(i));
            }
            return listPt;
        }
    }

    public class PointCompare : IEqualityComparer<Point3d>
    {
        public bool Equals(Point3d x, Point3d y)
        {

            return PtEqual(x, y);

        }

        public int GetHashCode(Point3d obj)
        {
            return obj.GetHashCode();
        }

        private bool PtEqual(Point3d p1, Point3d p2)
        {

            if (p1.X.ToString("f7") == p2.X.ToString("f7") && p1.Y.ToString("f7") == p2.Y.ToString("f7") && p1.Z.ToString("f7") == p2.Z.ToString("f7"))
                return true;
            return false;
        }
    }
}