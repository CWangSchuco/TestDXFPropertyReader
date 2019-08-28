using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SBA
{
    public class MesherDataSet
    {
        public MesherDataSet()
        {
            fileName = "382290.dxf";
            meshDepth = 0;//whole
            meshWidth = 0;
            totalMeshArea = 0;//
            meshCenteroidX = 0;//
            meshCenteroidY = 0;//

            meshIxx = 0;
            meshIyy = 0;
            meshIxy = 0;
            meshIxxC = 0;//
            meshIyyC = 0;//
            meshIxyC = 0;
            meshRx = 0;
            meshRy = 0;

            meshSpx = 0;
            meshSnx = 0;
            meshSpy = 0;
            meshSny = 0;


            meshJ = 0;
            meshCw = 0;
            meshXs = 0;
            meshYs = 0;
            meshBeta = 0;

            minimumMeshArea = 50.0;

        }

        
        // information data

        public string projectTitle { get; set; }
        public string sectionID { get; set; }

        public string fileName { get; set; }

        public double meshDepth { get; set; }
        public double meshWidth { get; set; }
        public double totalMeshArea { get; set; }
        public double meshCenteroidX { get; set; }
        public double meshCenteroidY { get; set; }

        public double meshIxx { get; set; }
        public double meshIyy { get; set; }
        public double meshIxy { get; set; }
        public double meshIxxC { get; set; }
        public double meshIyyC { get; set; }
        public double meshIxyC { get; set; }
        public double meshRx { get; set; }
        public double meshRy { get; set; }

        public double meshSpx { get; set; }
        public double meshSnx { get; set; }
        public double meshSpy { get; set; }
        public double meshSny { get; set; }


        public double meshJ { get; set; }
        public double meshCw { get; set; }
        public double meshXs { get; set; }
        public double meshYs { get; set; }
        public double meshBeta { get; set; }

        public double minimumMeshArea { get; set; }


    }
}
