using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using System.Windows.Forms;
using netDxf;
using netDxf.Blocks;
using netDxf.Collections;
using netDxf.Entities;
using netDxf.Header;
using netDxf.Objects;
using netDxf.Tables;
using netDxf.Units;
using Attribute = netDxf.Entities.Attribute;
using FontStyle = netDxf.Tables.FontStyle;
using Image = netDxf.Entities.Image;
using Point = netDxf.Entities.Point;
using Trace = netDxf.Entities.Trace;
using TriangleNet;
using TriangleNet.Geometry;
using TriangleNet.Meshing;
using TriangleNet.Smoothing;
using TriangleNet.Tools;
//using System.Drawing.Drawing2D;
using StructuralSolverSBA;

namespace SBA
{
    class DXFSectionProperty

    {
        private MesherDataSet mds;
        TriangleNet.Mesh mesh;
        Polygon poly;
        List<Vertex> centroids = new List<Vertex>();
        //        Dictionary<string, double> Results = new Dictionary<string, double>();

        public void ReadDXF(string dxfFileName, ref Section section)
        {

            mds = new MesherDataSet();
            string filename = dxfFileName;

            List<Polygon> polygons = getArea(filename);

            double d = 0.0;

            double[] aluFrame1dim = singledepth(polygons.ElementAt(0));
            double[] aluFrame2dim = singledepth(polygons.ElementAt(1));
            if (aluFrame1dim[2] > aluFrame2dim[2])
            {
                d = aluFrame1dim[2] - aluFrame2dim[3];
            }
            else
            {
                d = aluFrame2dim[2] - aluFrame1dim[3];
            }

            section.d = d / 10;

            double Zoo = 0.0;
            double Zou = 0.0;
            double Zuo = 0.0;
            double Zuu = 0.0;

            for (int i = 0; i < polygons.Count; i++)
            {
                poly = polygons.ElementAt(i);

                mesh = DxfMesh(poly);

                // compute mesh basic property
                MesherBasicProperty();

                double[] Z = singledepth(poly);

                if (i == 0)
                {
                    Zoo = Z.ElementAt(0);
                    Zou = Z.ElementAt(1);
                    section.Zoo = Zoo / 10;
                    section.Zou = Zou / 10;
                    section.Ao = mds.totalMeshArea / 100;
                    section.Io = mds.meshIxxC / 10000;
                    section.Ioyy = mds.meshIyyC / 10000;   // by Wei
                    section.Wo = 9.80665 / 100 * mds.totalMeshArea / 100 * 100 * 2.7 / 1000; // weight in N/cm

                }
                else if (i == 1)
                {
                    Zuo = Z.ElementAt(0);
                    Zuu = Z.ElementAt(1);
                    section.Zuo = Zuo / 10;
                    section.Zuu = Zuu / 10;
                    section.Au = mds.totalMeshArea / 100;
                    section.Iu = mds.meshIxxC / 10000;
                    section.Iuyy = mds.meshIyyC / 10000;   // by Wei
                    section.Wu = 9.80665 /100 * mds.totalMeshArea / 100 * 100 * 2.7 / 1000; // weight in N/cm
                }
                else if (i == 2)
                {
                    section.Wl = 9.80665 / 100 * mds.totalMeshArea / 100 * 100 * 1.2 * 1.27 / 1000;
                }
                else if (i == 3)
                {
                    section.Wr = 9.80665 / 100 * mds.totalMeshArea / 100 * 100 * 1.2 * 1.27 / 1000;
                }
            }
            section.Weight = section.Wo + section.Wu + section.Wl + section.Wr;
        }

        // Open profile

        private static DxfDocument OpenProfile(string file)
        {
            // open the profile file
            FileInfo fileInfo = new FileInfo(file);

            // check if profile file is valid
            if (!fileInfo.Exists)
            {
                Console.WriteLine("THE FILE {0} DOES NOT EXIST", file);
                Console.WriteLine();
                return null;
            }
            DxfDocument dxf = DxfDocument.Load(file, new List<string> { @".\Support" });

            // check if there has been any problems loading the file,
            if (dxf == null)
            {
                Console.WriteLine("ERROR LOADING {0}", file);
                Console.WriteLine();
                Console.WriteLine("Press a key to continue...");
                Console.ReadLine();
                return null;
            }

            return dxf;
        }

        // Store polygon from dxf file
        public List<Polygon> getArea(string filename)
        {
            // read the dxf file
            DxfDocument dxfTest;
            dxfTest = OpenProfile(filename);

            int numberSegments = 16;
            int blockNumber = -1;

            var polygons = new List<Polygon>();
            var Poly = new Polygon();

            // loop over all relevant blacks and store the hatch boundaries
            foreach (var bl in dxfTest.Blocks)
            {
                // loop over the enteties in the block and decompose them if they belong to an aluminum layer
                foreach (var ent in bl.Entities)
                {
                    if (ent.Layer.Name.ToString() == "0S-Alu hatch")
                    {
                        Poly = new Polygon();
                        blockNumber++;
                        HatchPattern hp = HatchPattern.Solid;
                        Hatch myHatch = new Hatch(hp, false);
                        myHatch = (Hatch)ent;
                        int pathNumber = -1;


                        foreach (var bPath in myHatch.BoundaryPaths)
                        {
                            pathNumber++;
                            var contour = new List<Vertex>();


                            // Store the contour
                            for (int i = 0; i < bPath.Edges.Count; i++)
                            {

                                switch (bPath.Edges[i].Type.ToString().ToLower())
                                {
                                    case "line":
                                        var myLine = (netDxf.Entities.HatchBoundaryPath.Line)bPath.Edges[i];
                                        var vLine = new Vertex();

                                        vLine.X = myLine.Start.X;
                                        vLine.Y = myLine.Start.Y;
                                        contour.Add(vLine);
                                        break;

                                    case "arc":
                                        var myArc = (netDxf.Entities.HatchBoundaryPath.Arc)bPath.Edges[i];
                                        double delta = (myArc.EndAngle - myArc.StartAngle) / numberSegments;

                                        for (int j = 0; j < numberSegments; j++)
                                        {
                                            var vArc = new Vertex();
                                            double angleArc = (myArc.StartAngle + j * delta) * Math.PI / 180.0;
                                            if (myArc.IsCounterclockwise == true)
                                            {
                                                vArc.X = myArc.Center.X + myArc.Radius * Math.Cos(angleArc);
                                                vArc.Y = myArc.Center.Y + myArc.Radius * Math.Sin(angleArc);
                                            }
                                            else
                                            {
                                                vArc.X = myArc.Center.X + myArc.Radius * Math.Cos(Math.PI + angleArc);
                                                vArc.Y = myArc.Center.Y + myArc.Radius * Math.Sin(Math.PI - angleArc);
                                            }
                                            contour.Add(vArc);
                                        }
                                        break;

                                    case "ellipse":
                                        var myEllipse = (netDxf.Entities.HatchBoundaryPath.Ellipse)bPath.Edges[i];
                                        double deltaEllipse = (myEllipse.EndAngle - myEllipse.StartAngle) / numberSegments;

                                        for (int j = 0; j < numberSegments; j++)
                                        {
                                            var vEllipse = new Vertex();
                                            var ellipseRadius = Math.Sqrt(Math.Pow(myEllipse.EndMajorAxis.X, 2) + Math.Pow(myEllipse.EndMajorAxis.Y, 2));

                                            double angleEllipse = (myEllipse.StartAngle + j * deltaEllipse) * Math.PI / 180.0;
                                            if (myEllipse.IsCounterclockwise == true)
                                            {
                                                vEllipse.X = myEllipse.Center.X + ellipseRadius * Math.Cos(angleEllipse);
                                                vEllipse.Y = myEllipse.Center.Y + ellipseRadius * Math.Sin(angleEllipse);
                                            }
                                            else
                                            {
                                                vEllipse.X = myEllipse.Center.X + ellipseRadius * Math.Cos(Math.PI + angleEllipse);
                                                vEllipse.Y = myEllipse.Center.Y + ellipseRadius * Math.Sin(Math.PI - angleEllipse);
                                            }
                                            contour.Add(vEllipse);
                                        }
                                        break;
                                }
                            }

                            bool hole = true;
                            // Add to the poly
                            if (blockNumber == 0 || blockNumber == 1)
                            {
                                if (pathNumber == 0)
                                {
                                    hole = false;
                                }
                                Poly.AddContour(points: contour, marker: 0, hole: hole);
                            }
                        }
                        polygons.Add(Poly);

                    }
                }
            }

            numberSegments = 16; // changed now
            blockNumber = -1;
            foreach (var bl in dxfTest.Blocks)
            {
                // loop over the enteties in the block and decompose them if they belong to an aluminum layer
                foreach (var ent in bl.Entities)
                {
                    if (ent.Layer.Name.ToString() == "0S-Plastic hatch")
                    {
                        Poly = new Polygon();
                        blockNumber++;
                        HatchPattern hp = HatchPattern.Solid;
                        Hatch myHatch = new Hatch(hp, false);
                        myHatch = (Hatch)ent;
                        int pathNumber = -1;


                        foreach (var bPath in myHatch.BoundaryPaths)
                        {
                            pathNumber++;
                            var contour = new List<Vertex>();


                            // Store the contour
                            for (int i = 0; i < bPath.Edges.Count; i++)
                            {

                                switch (bPath.Edges[i].Type.ToString().ToLower())
                                {
                                    case "line":
                                        var myLine = (netDxf.Entities.HatchBoundaryPath.Line)bPath.Edges[i];
                                        var vLine = new Vertex();

                                        vLine.X = myLine.Start.X;
                                        vLine.Y = myLine.Start.Y;
                                        contour.Add(vLine);
                                        break;

                                    case "arc":
                                        var myArc = (netDxf.Entities.HatchBoundaryPath.Arc)bPath.Edges[i];
                                        double delta = (myArc.EndAngle - myArc.StartAngle) / numberSegments;

                                        for (int j = 0; j < numberSegments; j++)
                                        {
                                            var vArc = new Vertex();
                                            double angleArc = (myArc.StartAngle + j * delta) * Math.PI / 180.0;
                                            if (myArc.IsCounterclockwise == true)
                                            {
                                                vArc.X = myArc.Center.X + myArc.Radius * Math.Cos(angleArc);
                                                vArc.Y = myArc.Center.Y + myArc.Radius * Math.Sin(angleArc);
                                            }
                                            else
                                            {
                                                vArc.X = myArc.Center.X + myArc.Radius * Math.Cos(Math.PI + angleArc);
                                                vArc.Y = myArc.Center.Y + myArc.Radius * Math.Sin(Math.PI - angleArc);
                                            }
                                            contour.Add(vArc);
                                        }
                                        break;

                                    case "ellipse":
                                        var myEllipse = (netDxf.Entities.HatchBoundaryPath.Ellipse)bPath.Edges[i];
                                        double deltaEllipse = (myEllipse.EndAngle - myEllipse.StartAngle) / numberSegments;

                                        for (int j = 0; j < numberSegments; j++)
                                        {
                                            var vEllipse = new Vertex();
                                            var ellipseRadius = Math.Sqrt(Math.Pow(myEllipse.EndMajorAxis.X, 2) + Math.Pow(myEllipse.EndMajorAxis.Y, 2));

                                            double angleEllipse = (myEllipse.StartAngle + j * deltaEllipse) * Math.PI / 180.0;
                                            if (myEllipse.IsCounterclockwise == true)
                                            {
                                                vEllipse.X = myEllipse.Center.X + ellipseRadius * Math.Cos(angleEllipse);
                                                vEllipse.Y = myEllipse.Center.Y + ellipseRadius * Math.Sin(angleEllipse);
                                            }
                                            else
                                            {
                                                vEllipse.X = myEllipse.Center.X + ellipseRadius * Math.Cos(Math.PI + angleEllipse);
                                                vEllipse.Y = myEllipse.Center.Y + ellipseRadius * Math.Sin(Math.PI - angleEllipse);
                                            }
                                            contour.Add(vEllipse);
                                        }
                                        break;
                                }
                            }

                            bool hole = true;
                            // Add to the poly
                            if (blockNumber == 0 || blockNumber == 1)
                            {
                                if (pathNumber == 0)
                                {
                                    hole = false;
                                }
                                Poly.AddContour(points: contour, marker: 0, hole: hole);
                            }
                        }
                        polygons.Add(Poly);
                    }
                }
            }

            numberSegments = 16; // changed now
            blockNumber = -1;
            foreach (var bl in dxfTest.Blocks)
            {
                // loop over the enteties in the block and decompose them if they belong to an aluminum layer
                foreach (var ent in bl.Entities)
                {
                    if (ent.Layer.Name.ToString() == "0S-PT hatch")
                    {
                        Poly = new Polygon();
                        blockNumber++;
                        HatchPattern hp = HatchPattern.Solid;
                        Hatch myHatch = new Hatch(hp, false);
                        myHatch = (Hatch)ent;
                        int pathNumber = -1;


                        foreach (var bPath in myHatch.BoundaryPaths)
                        {
                            pathNumber++;
                            var contour = new List<Vertex>();


                            // Store the contour
                            for (int i = 0; i < bPath.Edges.Count; i++)
                            {

                                switch (bPath.Edges[i].Type.ToString().ToLower())
                                {
                                    case "line":
                                        var myLine = (netDxf.Entities.HatchBoundaryPath.Line)bPath.Edges[i];
                                        var vLine = new Vertex();

                                        vLine.X = myLine.Start.X;
                                        vLine.Y = myLine.Start.Y;
                                        contour.Add(vLine);
                                        break;

                                    case "arc":
                                        var myArc = (netDxf.Entities.HatchBoundaryPath.Arc)bPath.Edges[i];
                                        double delta = (myArc.EndAngle - myArc.StartAngle) / numberSegments;

                                        for (int j = 0; j < numberSegments; j++)
                                        {
                                            var vArc = new Vertex();
                                            double angleArc = (myArc.StartAngle + j * delta) * Math.PI / 180.0;
                                            if (myArc.IsCounterclockwise == true)
                                            {
                                                vArc.X = myArc.Center.X + myArc.Radius * Math.Cos(angleArc);
                                                vArc.Y = myArc.Center.Y + myArc.Radius * Math.Sin(angleArc);
                                            }
                                            else
                                            {
                                                vArc.X = myArc.Center.X + myArc.Radius * Math.Cos(Math.PI + angleArc);
                                                vArc.Y = myArc.Center.Y + myArc.Radius * Math.Sin(Math.PI - angleArc);
                                            }
                                            contour.Add(vArc);
                                        }
                                        break;

                                    case "ellipse":
                                        var myEllipse = (netDxf.Entities.HatchBoundaryPath.Ellipse)bPath.Edges[i];
                                        double deltaEllipse = (myEllipse.EndAngle - myEllipse.StartAngle) / numberSegments;

                                        for (int j = 0; j < numberSegments; j++)
                                        {
                                            var vEllipse = new Vertex();
                                            var ellipseRadius = Math.Sqrt(Math.Pow(myEllipse.EndMajorAxis.X, 2) + Math.Pow(myEllipse.EndMajorAxis.Y, 2));

                                            double angleEllipse = (myEllipse.StartAngle + j * deltaEllipse) * Math.PI / 180.0;
                                            if (myEllipse.IsCounterclockwise == true)
                                            {
                                                vEllipse.X = myEllipse.Center.X + ellipseRadius * Math.Cos(angleEllipse);
                                                vEllipse.Y = myEllipse.Center.Y + ellipseRadius * Math.Sin(angleEllipse);
                                            }
                                            else
                                            {
                                                vEllipse.X = myEllipse.Center.X + ellipseRadius * Math.Cos(Math.PI + angleEllipse);
                                                vEllipse.Y = myEllipse.Center.Y + ellipseRadius * Math.Sin(Math.PI - angleEllipse);
                                            }
                                            contour.Add(vEllipse);
                                        }
                                        break;
                                }
                            }

                            bool hole = true;
                            // Add to the poly
                            if (blockNumber == 0 || blockNumber == 1)
                            {
                                if (pathNumber == 0)
                                {
                                    hole = false;
                                }
                                Poly.AddContour(points: contour, marker: 0, hole: hole);
                            }
                        }
                        polygons.Add(Poly);
                    }
                }
            }
            return polygons;
        }

        public double[] singledepth(Polygon poly)
        {
            double ytop = double.NegativeInfinity;
            double ybottom = double.PositiveInfinity;

            foreach (var vertex in poly.Points)
            {
                if (ytop <= vertex.Y)
                {
                    ytop = vertex.Y;
                }
                if (ybottom >= vertex.Y)
                {
                    ybottom = vertex.Y;
                }
            }
            double[] results = { ytop - mds.meshCenteroidY, mds.meshCenteroidY - ybottom, ytop, ybottom };
            return results;
        }

        // generate mesh
        private TriangleNet.Mesh DxfMesh(Polygon poly)
        {
            // routine to generate a mesh from the contnet of poly
            // Set quality and constraint options.
            var options = new ConstraintOptions() { ConformingDelaunay = true };
            var quality = new QualityOptions() { MinimumAngle = 15.0, MaximumArea = mds.minimumMeshArea };

            // create the mesh
            mesh = (TriangleNet.Mesh)poly.Triangulate(options, quality);

            // make sure there are at least 1000 elements in the mesh
            while (mesh.Triangles.Count < 1000)
            {
                mds.minimumMeshArea = mds.minimumMeshArea / 2;
                quality.MaximumArea = mds.minimumMeshArea;
                mesh = (TriangleNet.Mesh)poly.Triangulate(options, quality);
            }

            // smooth the mesh
            var smoother = new SimpleSmoother();
            smoother.Smooth(mesh);

            return mesh;
        }

        // calculate mesh basic property

        private void MesherBasicProperty()
        {
            int nt = mesh.Triangles.Count;

            mds.totalMeshArea = 0;
            mds.meshCenteroidX = 0;
            mds.meshCenteroidY = 0;
            mds.meshIxx = 0;
            mds.meshIyy = 0;
            mds.meshIxy = 0;

            // set depth and width
            double xmin = 0;
            double xmax = 0;
            double ymin = 0;
            double ymax = 0;
            MeshBox(ref xmin, ref xmax, ref ymin, ref ymax);
            mds.meshDepth = Math.Abs(ymax - ymin);
            mds.meshWidth = Math.Abs(xmax - xmin);


            for (int i = 0; i < nt; i++)
            {
                MesherTriangle myTriangle = new MesherTriangle(mesh.Triangles.ElementAt(i));

                mds.totalMeshArea += myTriangle.Area;

                mds.meshCenteroidX += myTriangle.Xc * myTriangle.Area;
                mds.meshCenteroidY += myTriangle.Yc * myTriangle.Area;
                mds.meshIxx += myTriangle.Ixx();
                mds.meshIyy += myTriangle.Iyy();
                mds.meshIxy += myTriangle.Ixy();
            }


            mds.meshCenteroidX = (mds.meshCenteroidX / mds.totalMeshArea);
            mds.meshCenteroidY = (mds.meshCenteroidY / mds.totalMeshArea);

            mds.meshIxxC = mds.meshIxx - mds.totalMeshArea * mds.meshCenteroidY * mds.meshCenteroidY;
            mds.meshIyyC = mds.meshIyy - mds.totalMeshArea * mds.meshCenteroidX * mds.meshCenteroidX;
            mds.meshIxyC = mds.meshIxy - mds.totalMeshArea * mds.meshCenteroidX * mds.meshCenteroidY;

            mds.meshRx = Math.Sqrt((mds.meshIxxC / mds.totalMeshArea));
            mds.meshRy = Math.Sqrt((mds.meshIyyC / mds.totalMeshArea));

            mds.meshSpx = (mds.meshIxxC / (ymax - mds.meshCenteroidY));
            mds.meshSnx = (mds.meshIxxC / (mds.meshCenteroidY - ymin));
            mds.meshSpy = (mds.meshIyyC / (xmax - mds.meshCenteroidX));
            mds.meshSny = (mds.meshIyyC / (mds.meshCenteroidX - xmin));
        }

        // find mesh bounding box

        public void MeshBox(ref double minx, ref double maxx, ref double miny, ref double maxy)
        {
            // function to find the linits of mesh

            maxx = -1E30;
            maxy = -1E30;
            minx = +1E30;
            miny = +1E30;

            foreach (var t in mesh.Triangles)
            {
                for (int i = 0; i < 3; i++)
                {
                    minx = Math.Min(minx, t.GetVertex(i).X);
                    maxx = Math.Max(maxx, t.GetVertex(i).X);
                    miny = Math.Min(miny, t.GetVertex(i).Y);
                    maxy = Math.Max(maxy, t.GetVertex(i).Y);
                }
            }
        }

        // find the centroide of a mesh

        public Vertex MesherPolygonCentroid(List<Vertex> contour)
        {
            Vertex VC = new Vertex(0, 0);
            int np = contour.Count;
            double area = 0;
            for (int i = 0; i < np - 1; i++)
            {
                area += contour.ElementAt(i).X * contour.ElementAt(i + 1).Y - contour.ElementAt(i).Y * contour.ElementAt(i + 1).X;
            }
            area += contour.ElementAt(np - 1).X * contour.ElementAt(0).Y - contour.ElementAt(np - 1).Y * contour.ElementAt(0).X;
            area = area / 2;

            for (int i = 0; i < np - 1; i++)
            {
                VC.X += (contour.ElementAt(i).X + contour.ElementAt(i + 1).X)
                      * (contour.ElementAt(i).X * contour.ElementAt(i + 1).Y - contour.ElementAt(i).Y * contour.ElementAt(i + 1).X);
                VC.Y += (contour.ElementAt(i).Y + contour.ElementAt(i + 1).Y)
                      * (contour.ElementAt(i).X * contour.ElementAt(i + 1).Y - contour.ElementAt(i).Y * contour.ElementAt(i + 1).X);
            }

            VC.X += (contour.ElementAt(np - 1).X + contour.ElementAt(0).X)
                  * (contour.ElementAt(np - 1).X * contour.ElementAt(0).Y - contour.ElementAt(np - 1).Y * contour.ElementAt(0).X);
            VC.Y += (contour.ElementAt(np - 1).Y + contour.ElementAt(0).Y)
                  * (contour.ElementAt(np - 1).X * contour.ElementAt(0).Y - contour.ElementAt(np - 1).Y * contour.ElementAt(0).X);

            VC.X = VC.X / (6 * area);
            VC.Y = VC.Y / (6 * area);

            return VC;
        }

    }
}