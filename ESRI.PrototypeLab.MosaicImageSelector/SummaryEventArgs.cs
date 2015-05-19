/* -----------------------------------------------------------------------------------
   Developed by the Applications Prototype Lab
   (c) 2015 Esri | http://www.esri.com/legal/software-license  
----------------------------------------------------------------------------------- */

using System;
using System.Collections.Generic;

namespace ESRI.PrototypeLab.MosaicImageSelector {
    public class SummaryEventArgs : EventArgs {
        //
        // CONSTRUCTOR
        //
        public SummaryEventArgs() : base() {
            this.Fields = new List<Field>();
        }
        //
        // PROPERTIES
        //
        public double XMin { get; set; }
        public double YMin { get; set; }
        public double XMax { get; set; }
        public double YMax { get; set; }
        public int Count { get; set; }
        public List<Field> Fields { get; private set; }
    }
}
