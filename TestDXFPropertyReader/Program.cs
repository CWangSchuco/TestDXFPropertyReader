using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StructuralSolverSBA;
using SBA;

namespace TestDXFPropertyReader
{
    class Program
    {
        static void Main(string[] args)
        {
            Section section = new Section();
            section.dxf = "..\\..\\382130.dxf";
            var DSP = new DXFSectionProperty();
            DSP.ReadDXF(section.dxf, ref section);
        }
    }
}
