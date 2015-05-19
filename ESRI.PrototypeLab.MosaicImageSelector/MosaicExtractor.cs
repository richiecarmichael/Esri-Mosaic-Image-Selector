/* -----------------------------------------------------------------------------------
   Developed by the Applications Prototype Lab
   (c) 2015 Esri | http://www.esri.com/legal/software-license  
----------------------------------------------------------------------------------- */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.DataSourcesRaster;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

namespace ESRI.PrototypeLab.MosaicImageSelector {
    public class MosaicExtractor {
        //private int _requests = 0;
        //
        // PROPERTIES
        //
        public string Path { private get; set; }
        public double XMin { private get; set; }
        public double YMin { private get; set; }
        public double XMax { private get; set; }
        public double YMax { private get; set; }
        public string SRef { private get; set; }
        public int MaxImageSize { private get; set; }
        //
        // CONSTRUCTOR
        //
        public MosaicExtractor() { }
        //
        // EVENTS
        //
        public event EventHandler<ThumbnailEventArgs> ThumbnailDownloaded;
        public event EventHandler<SummaryEventArgs> ThumbnailSummary;
        //
        // METHODS
        //
        public void OnThumbnailDownloaded(ThumbnailEventArgs e) {
            if (this.ThumbnailDownloaded != null) {
                this.ThumbnailDownloaded(this, e);
            }
        }
        public void OnThumbnailSummary(SummaryEventArgs e) {
            if (this.ThumbnailSummary != null) {
                this.ThumbnailSummary(this, e);
            }
        }
        public void Execute() {
            try {
                // Create new spatial reference
                ISpatialReference spatialReference = null;
                if (this.SRef.ToLowerInvariant().Contains("projcs")) {
                    spatialReference = new ProjectedCoordinateSystemClass();
                }
                else {
                    spatialReference = new GeographicCoordinateSystemClass();
                }

                // Import SpatialReference Definition
                int bytes2;
                IESRISpatialReferenceGEN2 gen = (IESRISpatialReferenceGEN2)spatialReference;
                gen.ImportFromESRISpatialReference(this.SRef, out bytes2);

                // Create Search Extent
                IEnvelope extent = new EnvelopeClass();
                extent.PutCoords(this.XMin, this.YMin, this.XMax, this.YMax);
                extent.SpatialReference = spatialReference;

                // Open Saved Layer File
                ILayerFile layerFile = new LayerFileClass();
                layerFile.Open(this.Path);
                ILayer layer = layerFile.Layer;
                layerFile.Close();

                // Where clause and list of selected OIDs
                string where = string.Empty;
                List<int> ids = new List<int>();

                IImageServerLayer imageLayer = null;
                if (layer is IImageServerLayer) {
                    imageLayer = (IImageServerLayer)layer;

                    // Get Selection Set
                    IFeatureLayerDefinition definition = layer as IFeatureLayerDefinition;
                    if (definition != null) {
                        // Find Selected OIDs
                        if (definition.DefinitionSelectionSet != null) {
                            if (definition.DefinitionSelectionSet.Count > 0) {
                                IEnumIDs emumids = definition.DefinitionSelectionSet.IDs;
                                int id = emumids.Next();
                                while (id != -1) {
                                    ids.Add(id);
                                    id = emumids.Next();
                                }
                            }
                        }

                        // Update Where Clause
                        if (!string.IsNullOrEmpty(definition.DefinitionExpression)) {
                            where = definition.DefinitionExpression;
                        }
                    }
                }
                else if (layer is IMosaicLayer) {
                    IMosaicLayer mosaicLayer = (IMosaicLayer)layer;
                    imageLayer = mosaicLayer.PreviewLayer;
                    ITableDefinition tableDefinition = mosaicLayer as ITableDefinition;
                    if (tableDefinition != null) {
                        // Find Selected OIDs
                        if (tableDefinition.DefinitionSelectionSet != null) {
                            if (tableDefinition.DefinitionSelectionSet.Count > 0) {
                                IEnumIDs emumids = tableDefinition.DefinitionSelectionSet.IDs;
                                int id = emumids.Next();
                                while (id != -1) {
                                    ids.Add(id);
                                    id = emumids.Next();
                                }
                            }
                        }

                        // Update Where Clause
                        if (!string.IsNullOrEmpty(tableDefinition.DefinitionExpression)) {
                            where = tableDefinition.DefinitionExpression;
                        }
                    }
                }

                // Use FeatureSelected (if any)
                IFeatureSelection featureSelection = imageLayer as IFeatureSelection;
                if (featureSelection != null) {
                    if (featureSelection.SelectionSet != null) {
                        if (featureSelection.SelectionSet.Count > 0) {
                            IEnumIDs emumids = featureSelection.SelectionSet.IDs;
                            int id = emumids.Next();
                            while (id != -1) {
                                ids.Add(id);
                                id = emumids.Next();
                            }
                        }
                    }
                }

                // Get Bands
                ILongArray bands = new LongArrayClass();
                IRasterRenderer rasterRenderer = imageLayer.Renderer;
                if (rasterRenderer != null) {
                    IRasterRGBRenderer2 rasterRGBRenderer = rasterRenderer as IRasterRGBRenderer2;
                    if (rasterRGBRenderer != null) {
                        bands.Add(rasterRGBRenderer.UseRedBand ? rasterRGBRenderer.RedBandIndex : -1);
                        bands.Add(rasterRGBRenderer.UseGreenBand ? rasterRGBRenderer.GreenBandIndex : -1);
                        bands.Add(rasterRGBRenderer.UseBlueBand ? rasterRGBRenderer.BlueBandIndex : -1);
                        bands.Add(rasterRGBRenderer.UseAlphaBand ? rasterRGBRenderer.AlphaBandIndex : -1);
                    }
                }

                // Create Spatial Filter
                ISpatialFilter spatialFilter = new SpatialFilterClass() {
                    Geometry = extent,
                    SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects,
                    WhereClause = where
                };

                IImageServer imageServer = imageLayer.DataSource as IImageServer;
                if (imageServer == null) { return; }
                IImageServer3 imageServer3 = imageServer as IImageServer3;
                if (imageServer3 == null) { return; }
                IRecordSet recordSet = imageServer3.GetCatalogItems(spatialFilter);
                ICursor cursor = recordSet.get_Cursor(false);
                IFeatureCursor featureCursor = (IFeatureCursor)cursor;
                IFeature feature = featureCursor.NextFeature();
                List<IFeature> list = new List<IFeature>();

                int indexCategory = featureCursor.FindField("Category");

                while (feature != null) {
                    // Exclude non-primary images (no overviews)
                    if (indexCategory != -1) {
                        int cat = (int)feature.get_Value(indexCategory);
                        if (cat != 1) {
                            feature = featureCursor.NextFeature();
                            continue;
                        }
                    }

                    // Exclude unselected features
                    if (ids.Count > 0) {
                        int oid = feature.OID;
                        if (!ids.Contains(oid)) {
                            feature = featureCursor.NextFeature();
                            continue;
                        }
                    }

                    list.Add(feature);
                    feature = featureCursor.NextFeature();
                }

                // If nothing, fire event then exit
                if (list.Count == 0) {
                    this.OnThumbnailSummary(new SummaryEventArgs());
                    return;
                }

                // Get Full Extent of Features
                SummaryEventArgs summary = new SummaryEventArgs() {
                    XMin = list.Min(f => f.Shape.Envelope.CloneProject(spatialReference).XMin),
                    YMin = list.Min(f => f.Shape.Envelope.CloneProject(spatialReference).YMin),
                    XMax = list.Max(f => f.Shape.Envelope.CloneProject(spatialReference).XMax),
                    YMax = list.Max(f => f.Shape.Envelope.CloneProject(spatialReference).YMax),
                    Count = list.Count
                };

                // Add Fields
                IFields fields = featureCursor.Fields;
                for (int i = 0; i < fields.FieldCount; i++) {
                    IField field = fields.get_Field(i);
                    switch (field.Type) {
                        case esriFieldType.esriFieldTypeBlob:
                        case esriFieldType.esriFieldTypeGeometry:
                        case esriFieldType.esriFieldTypeGlobalID:
                        case esriFieldType.esriFieldTypeGUID:
                        case esriFieldType.esriFieldTypeRaster:
                        case esriFieldType.esriFieldTypeXML:
                            break;
                        case esriFieldType.esriFieldTypeOID:
                        case esriFieldType.esriFieldTypeDate:
                        case esriFieldType.esriFieldTypeDouble:
                        case esriFieldType.esriFieldTypeInteger:
                        case esriFieldType.esriFieldTypeSingle:
                        case esriFieldType.esriFieldTypeSmallInteger:
                        case esriFieldType.esriFieldTypeString:
                            summary.Fields.Add(
                                new Field() {
                                    Name = field.Name,
                                    Alias = field.AliasName,
                                    Type = field.Type
                                }
                            );
                            break;
                        default:
                            break;
                    }
                }

                // Raise Summary Event
                this.OnThumbnailSummary(summary);

                // Loop for each feature
                foreach (IFeature feat in list) {
                    // Project Extent to Current Map Spatial Reference
                    IEnvelope extentMap = feat.Shape.Envelope.CloneProject(spatialReference);

                    int width;
                    int height;
                    if (extentMap.Width > extentMap.Height) {
                        width = this.MaxImageSize;
                        height = Convert.ToInt32((double)this.MaxImageSize * (double)extentMap.Height / (double)extentMap.Width);
                    }
                    else {
                        width = Convert.ToInt32((double)this.MaxImageSize * (double)extentMap.Width / (double)extentMap.Height);
                        height = this.MaxImageSize;
                    }

                    IMosaicRule mosaicRule = new MosaicRuleClass() {
                        MosaicMethod = esriMosaicMethod.esriMosaicLockRaster,
                        LockRasterID = feat.OID.ToString()
                    };
                    IGeoImageDescription geoImageDescription = new GeoImageDescriptionClass() {
                        BandSelection = bands.Count > 0 ? bands : null,
                        MosaicRule = mosaicRule,
                        Compression = "PNG",
                        Width = width,
                        Height = height,
                        SpatialReference = spatialReference,
                        Extent = extentMap,
                        Interpolation = rstResamplingTypes.RSP_BilinearInterpolation,
                    };

                    // Assembly MosaicRequest (will be executed in background thread)
                    MosaicRequest mosaicRequest = new MosaicRequest() {
                        MosaicExtractor = this,
                        ImageServer = imageServer3,
                        GeoImageDescription = geoImageDescription,
                        XMin = extentMap.XMin,
                        YMin = extentMap.YMin,
                        XMax = extentMap.XMax,
                        YMax = extentMap.YMax,
                    };

                    // Add Attributes Names and Values
                    foreach (Field field in summary.Fields) {
                        int index = feat.Fields.FindField(field.Name);
                        if (index < 0) { continue; }
                        mosaicRequest.Attributes.Add(field.Name, feat.get_Value(index));
                    }

                    // Start Mosaic Request in Background Thread
                    Thread thread = new Thread(new ThreadStart(mosaicRequest.Execute));
                    MosaicEnvironment.Default.Threads.Add(thread);
                    thread.Start();
                }
            }
            catch { }
        }
    }
}