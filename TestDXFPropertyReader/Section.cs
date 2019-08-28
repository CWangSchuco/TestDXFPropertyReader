using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace StructuralSolverSBA
{
    public class Section
    {
        public int SectionID { get; set; }
        public int ArticleID { get; set; }
        public string dxf { get; set; }
        public double d { get; set; }
        public double width { get; set; }
        public double Ao { get; set; }
        public double Au { get; set; }
        public double Io { get; set; }
        public double Iu { get; set; }
        public double Ioyy { get; set; }
        public double Iuyy { get; set; }
        public double Zoo { get; set; }
        public double Zuo { get; set; }
        public double Zou { get; set; }
        public double Zuu { get; set; }
        public double RSn20 { get; set; }
        public double RSp80 { get; set; }
        public double RTn20 { get; set; }
        public double RTp80 { get; set; }
        public double Cn20 { get; set; }
        public double Cp20 { get; set; }
        public double Cp80 { get; set; }
        public double A2 { get; set; }
        public double E { get; set; }
        public double beta { get; set; }
        public double alpha { get; set; }
        public double Weight { get; set; }
        public double Wo { get; set; }
        public double Wu { get; set; }
        public double Wl { get; set; }
        public double Wr { get; set; }
    }
}