/* -----------------------------------------------------------------------------------
   Developed by the Applications Prototype Lab
   (c) 2015 Esri | http://www.esri.com/legal/software-license  
----------------------------------------------------------------------------------- */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geometry;
using System.Reflection;

namespace ESRI.PrototypeLab.MosaicImageSelector {
    public static class EsriExtension {
        public static ILayer Clone(this ILayer layer) {
            if (layer is IPersistStream) {
                IObjectCopy objectCopy = new ObjectCopyClass();
                object clone = objectCopy.Copy(layer);
                ILayer layerClone = clone as ILayer;
                return layerClone;
            }
            if (layer is IPersistVariant) {
                // Create an XML Stream
                IXMLStream xmlStream = new XMLStreamClass();
                IVariantStreamIO variantStreamIO = new VariantStreamIOClass() {
                    Stream = (IStream)xmlStream
                };

                // Save Layer to Stream
                IVariantStream variantStream = (IVariantStream)variantStreamIO;
                IPersistVariant save = (IPersistVariant)layer;
                save.Save(variantStream);

                // Move Seek Pointer to beginning of Stream
                xmlStream.Reset();

                // Create New Layer
                ILayer newlayer = null;
                if (layer is IImageServerLayer) {
                    newlayer = new ImageServerLayerClass();
                }
                if (newlayer == null) { return null; }

                // Create new Layer
                IPersistVariant load = (IPersistVariant)newlayer;
                load.Load(variantStream);

                // Return Cloned Layer
                return newlayer;
            }
            return null;
        }
        public static IEnvelope CloneProject(this IEnvelope envelope, ISpatialReference spatialReference) {
            IEnvelope clone = envelope.Clone();
            clone.Project(spatialReference);
            return clone;
        }
        public static IEnvelope Clone(this IEnvelope envelope) {
            IClone clone = (IClone)envelope;
            object copy = clone.Clone();
            IEnvelope envelopeClone = (IEnvelope)copy;
            return envelopeClone;
        }
    }
}
