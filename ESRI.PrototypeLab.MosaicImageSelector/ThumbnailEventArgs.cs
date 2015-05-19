/* -----------------------------------------------------------------------------------
   Developed by the Applications Prototype Lab
   (c) 2015 Esri | http://www.esri.com/legal/software-license  
----------------------------------------------------------------------------------- */

using System;
using System.Collections.Generic;

namespace ESRI.PrototypeLab.MosaicImageSelector {
    public class ThumbnailEventArgs : EventArgs {
        //
        // CONSTRUCTOR
        //
        public ThumbnailEventArgs() : base() {
            this.Attributes = new Dictionary<string, object>();
        }
        //
        // PROPERTIES
        //
        public double XMin { get; set; }
        public double YMin { get; set; }
        public double XMax { get; set; }
        public double YMax { get; set; }
        public byte[] Bytes { get; set; }
        public Dictionary<string, object> Attributes { get; private set; }
    }
}
