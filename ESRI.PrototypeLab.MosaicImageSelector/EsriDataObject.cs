/* -----------------------------------------------------------------------------------
   Developed by the Applications Prototype Lab
   (c) 2015 Esri | http://www.esri.com/legal/software-license  
----------------------------------------------------------------------------------- */

using ESRI.ArcGIS.ArcMapUI;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.esriSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace ESRI.PrototypeLab.MosaicImageSelector {
    public class EsriDataObject {
        private readonly List<ILayer> m_layerCollection = null;
        private readonly List<ITableProperty> m_tablePropertyCollection = null;
        //
        // CONSTRUCTOR
        //
        public EsriDataObject() {
            this.m_layerCollection = new List<ILayer>();
            this.m_tablePropertyCollection = new List<ITableProperty>();
        }
        //
        // PROPERTIES
        //
        public List<ILayer> LayerCollection {
            get { return this.m_layerCollection; }
        }
        public List<ITableProperty> TablePropertyCollection {
            get { return this.m_tablePropertyCollection; }
        }
        //
        // STATIC METHODS
        //
        public static bool IsValid(IDataObject dataObject) {
            return dataObject.GetDataPresent(EsriRegistry.DATAOBJECT_ESRILAYERS);
        }
        public static EsriDataObject ConvertToEsriDataObject(IDataObject dataObject) {
            //
            EsriDataObject esriDataObject = new EsriDataObject();

            // Exit if dropped object is invalid
            if (EsriDataObject.IsValid(dataObject)) {
                // Get Byte Array from DataObject
                object esriLayers = dataObject.GetData(EsriRegistry.DATAOBJECT_ESRILAYERS);
                MemoryStream memoryStream = (MemoryStream)esriLayers;
                byte[] bytes = memoryStream.ToArray();

                // Load Byte Array into a Stream (ESRI Wrapper of IStream)
                IMemoryBlobStreamVariant memoryBlobStreamVariant = new MemoryBlobStreamClass();
                memoryBlobStreamVariant.ImportFromVariant(bytes);
                IMemoryBlobStream2 memoryBlobStream = (IMemoryBlobStream2)memoryBlobStreamVariant;
                IStream stream = (IStream)memoryBlobStream;

                // Load Stream into an ESRI ObjectStream
                IObjectStream objectStream = new ObjectStreamClass();
                objectStream.Stream = stream;

                // Get Number of Layers in Dropped Object
                byte pv;
                uint cb = sizeof(int);
                uint pcbRead;
                objectStream.RemoteRead(out pv, cb, out pcbRead);
                int count = Convert.ToInt32(pv);

                // Define Guids
                Guid guidLayer = new Guid(EsriRegistry.INTERFACE_ILAYER);
                Guid guidTable = new Guid(EsriRegistry.INTERFACE_ITABLEPROPERTY);

                // Get Dropped Layers
                for (int i = 0; i < count; i++) {
                    object o = objectStream.LoadObject(ref guidLayer, null);
                    ILayer layer = (ILayer)o;
                    esriDataObject.LayerCollection.Add(layer);
                }

                // Get Dropped TableProperties
                for (int i = 0; i < count; i++) {
                    object o = objectStream.LoadObject(ref guidTable, null);
                    if (o == null) { continue; }
                    ITableProperty tableProperty = (ITableProperty)o;
                    esriDataObject.TablePropertyCollection.Add(tableProperty);
                }
            }

            return esriDataObject;
        }
    }
}
