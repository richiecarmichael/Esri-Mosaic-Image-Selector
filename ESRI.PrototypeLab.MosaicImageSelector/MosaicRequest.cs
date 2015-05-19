/* -----------------------------------------------------------------------------------
   Developed by the Applications Prototype Lab
   (c) 2015 Esri | http://www.esri.com/legal/software-license  
----------------------------------------------------------------------------------- */

using System.Collections.Generic;
using ESRI.ArcGIS.Carto;

namespace ESRI.PrototypeLab.MosaicImageSelector {
    public class MosaicRequest {
        //
        // CONSTRUCTOR
        //
        public MosaicRequest() {
            this.Attributes = new Dictionary<string, object>();
        }
        //
        // PROPERTIES
        //
        public MosaicExtractor MosaicExtractor { private get; set; }
        public IImageServer3 ImageServer { private get; set; }
        public IGeoImageDescription GeoImageDescription { private get; set; }
        public double XMin { private get; set; }
        public double YMin { private get; set; }
        public double XMax { private get; set; }
        public double YMax { private get; set; }
        public Dictionary<string, object> Attributes { get; private set; }
        //
        // METHODS
        //
        public void Execute() {
            try {
                // Perform Synchronous Image Request
                byte[] bytes = this.ImageServer.GetImage(this.GeoImageDescription);

                // Build return event arguments
                ThumbnailEventArgs e = new ThumbnailEventArgs() {
                    XMin = this.XMin,
                    YMin = this.YMin,
                    XMax = this.XMax,
                    YMax = this.YMax,
                    Bytes = bytes
                };
                foreach (KeyValuePair<string, object> kvp in this.Attributes) {
                    e.Attributes.Add(kvp.Key, kvp.Value);
                }

                // Invoke event in parent class
                this.MosaicExtractor.OnThumbnailDownloaded(e);
            }
            catch { }
        }
    }
}
