/* -----------------------------------------------------------------------------------
   Developed by the Applications Prototype Lab
   (c) 2015 Esri | http://www.esri.com/legal/software-license  
----------------------------------------------------------------------------------- */

using ESRI.ArcGIS.Geodatabase;

namespace ESRI.PrototypeLab.MosaicImageSelector {
    public class Field {
        //
        // CONSTRUCTOR
        //
        public Field() { }
        //
        // PROPERTIES
        //
        public string Name { get; set; }
        public string Alias { get; set; }
        public esriFieldType Type { get; set; }
    }
}