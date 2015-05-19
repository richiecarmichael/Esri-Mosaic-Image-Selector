/* -----------------------------------------------------------------------------------
   Developed by the Applications Prototype Lab
   (c) 2015 Esri | http://www.esri.com/legal/software-license  
----------------------------------------------------------------------------------- */

using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using System.Xml;

namespace ESRI.PrototypeLab.MosaicImageSelector {
    public class ButtonMosaicViewer : ESRI.ArcGIS.Desktop.AddIns.Button {
        private const double ACCELERATIONRATIO = 0.2d;
        private const double DECELERATIONRATIO = 0.6d;
        private ImageViewer _imageViewer = null;
        private ILayer _droplayer = null;
        private IEnvelope _baseExtent = null;
        private double _baseWidth_ = 0d;
        private double _baseHeight = 0d;
        private List<MosaicFootprint> _tiles = new List<MosaicFootprint>();
        private List<Field> _fields = new List<Field>();
        private Random _random = new Random();
        private DispatcherTimer _timer = null;
        //
        // CONSTRUCTOR
        //
        public ButtonMosaicViewer() {
            this._timer = new DispatcherTimer() {
                Interval = TimeSpan.FromMilliseconds(300d),
                IsEnabled = true
            };
            this._timer.Tick += new EventHandler(this.Timer_Tick);
        }
        //
        // METHODS
        //
        protected override void OnClick() {
            try {
                if (this._imageViewer == null) {
                    // Get the ArcMap Window Position
                    IWindowPosition windowPosition = (IWindowPosition)ArcMap.Application;

                    // Create a new window
                    this._imageViewer = new ImageViewer();
                    this._imageViewer.Left = windowPosition.Left + (windowPosition.Width / 2) - (this._imageViewer.Width / 2);
                    this._imageViewer.Top = windowPosition.Top + (windowPosition.Height / 2) - (this._imageViewer.Height / 2);
                    this._imageViewer.Closing += (s, e) => {
                        e.Cancel = true;
                        this._imageViewer.Hide();
                    };
                    this._imageViewer.DragEnter += (s, e) => {
                        e.Handled = true;
                        e.Effects = e.Data.GetDataPresent(EsriRegistry.DATAOBJECT_ESRILAYERS) ? DragDropEffects.All : DragDropEffects.None;
                    };
                    this._imageViewer.DragLeave += (s, e) => {
                        e.Handled = true;
                    };
                    this._imageViewer.DragOver += (s, e) => {
                        e.Handled = true;
                        e.Effects = e.Data.GetDataPresent(EsriRegistry.DATAOBJECT_ESRILAYERS) ? DragDropEffects.All : DragDropEffects.None;
                    };
                    this._imageViewer.Drop += new DragEventHandler(this.Window_Drop);
                    this._imageViewer.Loaded += new RoutedEventHandler(this.Page_Loaded);
                    this._imageViewer.ListBoxFields.SelectionChanged += new SelectionChangedEventHandler(this.ListBox_SelectionChanged);
                    this._imageViewer.RadioButtonAscend.Checked += new RoutedEventHandler(this.RadioButton_Checked);
                    this._imageViewer.RadioButtonDescend.Checked += new RoutedEventHandler(this.RadioButton_Checked);
                    this._imageViewer.SliderExaggeration.ValueChanged += new RoutedPropertyChangedEventHandler<double>(this.Slider_ValueChanged);
                    this._imageViewer.SliderFilter.ValueChanged += new RoutedPropertyChangedEventHandler<double>(this.Slider_ValueChanged);
                    this._imageViewer.ButtonClear.Click += new RoutedEventHandler(this.Button_Click);
                    this._imageViewer.ButtonClearSelection.Click += new RoutedEventHandler(this.Button_Click);
                    this._imageViewer.ButtonAdd.Click += new RoutedEventHandler(this.Button_Click);
                    this._imageViewer.ButtonStop.Click += new RoutedEventHandler(this.Button_Click);
                    this._imageViewer.Show();
                }
                else {
                    if (this._imageViewer.IsVisible) {
                        this._imageViewer.Hide();
                    }
                    else {
                        this._imageViewer.Show();
                    }
                }
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
        protected override void OnUpdate() {
            // Always Enabled
            Enabled = ArcMap.Application != null;
        }
        private void Timer_Tick(object sender, EventArgs e) {
            try {
                // Exit if window hidden or does not exist
                if (this._imageViewer == null) { return; }
                if (!this._imageViewer.IsLoaded) { return; }
                if (this._imageViewer.Visibility != Visibility.Visible) { return; }
                if (this._imageViewer.WindowState == WindowState.Minimized) { return; }

                // Footprint count
                int count = this._tiles.Count;

                // Sorting
                bool sorting = count != 0;
                if (this._imageViewer.GridSorting.IsEnabled != sorting) {
                    this._imageViewer.GridSorting.IsEnabled = sorting;
                }

                // Exaggeration
                bool exag = count != 0;
                if (this._imageViewer.GridExaggeration.IsEnabled != exag) {
                    this._imageViewer.GridExaggeration.IsEnabled = exag;
                }

                // Filter
                bool filer = count != 0;
                if (this._imageViewer.GridFilter.IsEnabled != filer) {
                    this._imageViewer.GridFilter.IsEnabled = filer;
                }

                // Resolution
                bool resolution = true;
                if (this._imageViewer.GridResolution.IsEnabled != resolution) {
                    this._imageViewer.GridResolution.IsEnabled = resolution;
                }

                // Selection
                bool selection = count != 0 && this._imageViewer.ItemsControl.ItemsSource != null;
                if (this._imageViewer.GridSelection.IsEnabled != selection) {
                    this._imageViewer.GridSelection.IsEnabled = selection;
                }

                // Add
                bool add = count != 0 && this._imageViewer.ItemsControl.ItemsSource != null;
                if (this._imageViewer.ButtonAdd.IsEnabled != add) {
                    this._imageViewer.ButtonAdd.IsEnabled = add;
                }
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
        private void Page_Loaded(object sender, RoutedEventArgs e) {
            try {
                // Define normals
                Vector3DCollection normals = new Vector3DCollection();
                normals.Add(new System.Windows.Media.Media3D.Vector3D(0, 0, 1));
                normals.Add(new System.Windows.Media.Media3D.Vector3D(0, 0, 1));
                normals.Add(new System.Windows.Media.Media3D.Vector3D(0, 0, 1));
                normals.Add(new System.Windows.Media.Media3D.Vector3D(0, 0, 1));
                normals.Add(new System.Windows.Media.Media3D.Vector3D(0, 0, 1));
                normals.Add(new System.Windows.Media.Media3D.Vector3D(0, 0, 1));

                // Define vertex positions
                Point3DCollection positions = new Point3DCollection();
                positions.Add(new Point3D(-0.25d, -0.25d, 0d));
                positions.Add(new Point3D(-0.25d, 0.25d, 0d));
                positions.Add(new Point3D(0.25d, 0.25d, 0d));
                positions.Add(new Point3D(0.25d, -0.25d, 0d));

                // Define textures
                PointCollection textureCoordinates = new PointCollection();
                textureCoordinates.Add(new System.Windows.Point(0, 1));
                textureCoordinates.Add(new System.Windows.Point(0, 0));
                textureCoordinates.Add(new System.Windows.Point(1, 0));
                textureCoordinates.Add(new System.Windows.Point(1, 1));

                // Define triangles
                Int32Collection triangleIndices = new Int32Collection();
                triangleIndices.Add(0);
                triangleIndices.Add(3);
                triangleIndices.Add(2);
                triangleIndices.Add(2);
                triangleIndices.Add(1);
                triangleIndices.Add(0);

                DropPrompt dropprompt = new DropPrompt() {
                    AllowDrop = true
                };
                dropprompt.DragEnter += (a, b) => {
                    b.Handled = true;
                    b.Effects = b.Data.GetDataPresent(EsriRegistry.DATAOBJECT_ESRILAYERS) ? DragDropEffects.All : DragDropEffects.None;
                };
                dropprompt.DragLeave += (a, b) => {
                    b.Handled = true;
                };
                dropprompt.DragOver += (a, b) => {
                    b.Handled = true;
                    b.Effects = b.Data.GetDataPresent(EsriRegistry.DATAOBJECT_ESRILAYERS) ? DragDropEffects.All : DragDropEffects.None;
                };
                dropprompt.Drop += new DragEventHandler(this.Window_Drop);

                Viewport2DVisual3D viewport = new Viewport2DVisual3D() {
                    Geometry = new MeshGeometry3D() {
                        Normals = normals,
                        Positions = positions,
                        TextureCoordinates = textureCoordinates,
                        TriangleIndices = triangleIndices
                    },
                    Material = new DiffuseMaterial() {
                        AmbientColor = Colors.White
                    },
                    Visual = dropprompt
                };
                Viewport2DVisual3D.SetIsVisualHostMaterial(viewport.Material, true);

                this._imageViewer.Message.Children.Add(viewport);
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e) {
            try {
                if (sender == this._imageViewer.ButtonClear) {
                    this.InitializeDisplay();
                }
                else if (sender == this._imageViewer.ButtonClearSelection) {
                    this._imageViewer.ItemsControl.ItemsSource = null;
                    this._tiles.ForEach(
                        tile => {
                            if (tile.IsSelected) {
                                tile.IsSelected = false;
                            }
                        }
                    );
                }
                else if (sender == this._imageViewer.ButtonAdd) {
                    // Get Selected Footprint
                    MosaicFootprint footprint = this._tiles.First(t => t.IsSelected);

                    // Get OID Field
                    Field field = this._fields.First(f => f.Type == esriFieldType.esriFieldTypeOID);

                    // Get OID
                    double oid = this.GetFieldValue(footprint, field);

                    // Close Source Layer
                    ILayer clone = this._droplayer.Clone();

                    // Get ImageServer Layer
                    IImageServerLayer2 imageLayer = null;
                    if (clone is IImageServerLayer) {
                        imageLayer = clone as IImageServerLayer2;
                    }
                    else if (clone is IMosaicLayer) {
                        IMosaicLayer mosaicLayer = (IMosaicLayer)clone;
                        imageLayer = mosaicLayer.PreviewLayer as IImageServerLayer2;
                    }
                    if (imageLayer == null) { return; }

                    // Rename Layer
                    clone.Name = string.Format("{0} {1}", clone.Name, "selection");

                    // Edit Mosaic Properties
                    XmlDocumentFragment xmlMosaicProps = new XmlDocument().CreateDocumentFragment();
                    xmlMosaicProps.InnerXml = imageLayer.MosaicProperties;
                    xmlMosaicProps.SelectSingleNode("MosaicMethod").InnerText = "LockRaster";
                    xmlMosaicProps.SelectSingleNode("LockImageID").InnerText = oid.ToString("F0", CultureInfo.InvariantCulture);

                    // Set new mosaic properties
                    imageLayer.MosaicProperties = xmlMosaicProps.InnerXml;

                    // Add layer 
                    ArcMap.Document.FocusMap.AddLayer(clone);
                }
                else if (sender == this._imageViewer.ButtonStop) {
                    MosaicEnvironment.Default.Threads.ForEach(t => t.Abort());
                    MosaicEnvironment.Default.Threads.Clear();
                }
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
        private void Window_Drop(object sender, DragEventArgs e) {
            try {
                // Event handled
                e.Handled = true;

                // Exit if ESRI Data Object is Invalid
                EsriDataObject dataObject = EsriDataObject.ConvertToEsriDataObject(e.Data);
                switch (dataObject.LayerCollection.Count) {
                    case 0:
                        MessageBox.Show(
                            "No layer added",
                            "Ice for ArcMap",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information,
                            MessageBoxResult.OK);
                        return;
                    case 1:
                        break;
                    default:
                        MessageBox.Show(
                            "You can only add one layer at a time",
                            "Ice for ArcMap",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information,
                            MessageBoxResult.OK);
                        return;
                }

                // Initalize Display (clear variables, graphics etc)
                this.InitializeDisplay();

                // Get Mosaic Layer
                this._droplayer = dataObject.LayerCollection[0];
                if (this._droplayer == null) { return; }

                // Get ImageLayer
                IImageServerLayer imageLayer = null;
                if (this._droplayer is IImageServerLayer) {
                    imageLayer = this._droplayer as IImageServerLayer;
                }
                else if (this._droplayer is IMosaicLayer) {
                    IMosaicLayer mosaicLayer = (IMosaicLayer)this._droplayer;
                    imageLayer = mosaicLayer.PreviewLayer as IImageServerLayer;
                }
                if (imageLayer == null) {
                    MessageBox.Show("Not an image layer");
                    return;
                }
                
                // Test for valid Mosaic
                IImageServer imageServer = imageLayer.DataSource as IImageServer;
                if (imageServer == null) {
                    MessageBox.Show("Image layer does not support IImageServer");
                    return;
                }
                IImageServiceInfo imageServiceInfo = imageServer.ServiceInfo;
                if (imageServiceInfo == null) {
                    MessageBox.Show("Image layer does not support IImageServiceInfo");
                    return;
                }
                if (imageServiceInfo.ServiceSourceType != esriImageServiceSourceType.esriImageServiceSourceTypeMosaicDataset) {
                    MessageBox.Show("This image layer does not represent a valid mosaic dataset");
                    return;
                }

                // Hide Prompt Message
                Viewport2DVisual3D v = this._imageViewer.Message.Children[0] as Viewport2DVisual3D;
                DropPrompt d = v.Visual as DropPrompt;
                d.Visibility = Visibility.Collapsed;

                // Open Panel
                if (!this._imageViewer.ToggleButton.IsChecked.Value) {
                    this._imageViewer.ToggleButton.IsChecked = true;
                }

                // Add Base
                this._baseExtent = ArcMap.Document.ActiveView.Extent.Clone();
                this.AddBase();

                // 
                string folder = System.IO.Path.GetTempPath();
                string file = string.Format("{0}.lyr", Guid.NewGuid().ToString("N").ToLowerInvariant());
                string path = System.IO.Path.Combine(folder, file);

                // Save Layer
                ILayerFile layerFile = new LayerFileClass();
                layerFile.New(path);
                layerFile.ReplaceContents(this._droplayer);
                layerFile.Save();
                layerFile.Close();

                //
                int bytes = 0;
                string buffer = null;
                IESRISpatialReferenceGEN2 parameterExport = (IESRISpatialReferenceGEN2)this._baseExtent.SpatialReference;
                parameterExport.ExportToESRISpatialReference2(out buffer, out bytes);

                // Extract Images
                MosaicExtractor extractor = new MosaicExtractor() {
                    Path = path,
                    XMin = this._baseExtent.XMin,
                    YMin = this._baseExtent.YMin,
                    XMax = this._baseExtent.XMax,
                    YMax = this._baseExtent.YMax,
                    SRef = buffer,
                    MaxImageSize = (int)this._imageViewer.SliderResolution.Value
                };
                extractor.ThumbnailSummary += new EventHandler<SummaryEventArgs>(this.Extractor_ThumbnailSummary);
                extractor.ThumbnailDownloaded += new EventHandler<ThumbnailEventArgs>(this.Extractor_ThumbnailDownloaded);

                //
                MosaicEnvironment.Default.Threads.ForEach(t => t.Abort());
                MosaicEnvironment.Default.Threads.Clear();

                // Start Extraction in Background Thread
                Thread thread = new Thread(new ThreadStart(extractor.Execute));
                MosaicEnvironment.Default.Threads.Add(thread);
                thread.Start();
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
        private void Extractor_ThumbnailDownloaded(object sender, ThumbnailEventArgs e) {
            this._imageViewer.Dispatcher.Invoke(
                DispatcherPriority.Normal,
                new Action(
                    delegate() {
                        try {
                            // Calculate Height
                            double count = (double)this._imageViewer.Footprints.Children.Count;

                            // Convert footprint extents to WPF3D extents
                            double xmin = this._baseWidth_ * (e.XMin - this._baseExtent.XMin) / (this._baseExtent.XMax - this._baseExtent.XMin);
                            double ymin = this._baseHeight * (e.YMin - this._baseExtent.YMin) / (this._baseExtent.YMax - this._baseExtent.YMin);
                            double xmax = this._baseWidth_ * (e.XMax - this._baseExtent.XMin) / (this._baseExtent.XMax - this._baseExtent.XMin);
                            double ymax = this._baseHeight * (e.YMax - this._baseExtent.YMin) / (this._baseExtent.YMax - this._baseExtent.YMin);

                            // Create Bitmap Image
                            BitmapImage image = null;
                            try {
                                MemoryStream stream = new MemoryStream(e.Bytes);
                                image = new BitmapImage();
                                image.BeginInit();
                                image.StreamSource = stream;
                                image.EndInit();
                            }
                            catch { }
                            if (image == null) { return; }
                            if (image.Width == 0d || image.Height == 0d) { return; }

                            // Add Shadow
                            this.AddImageShadow(xmin, ymin, xmax, ymax, 0.01d + (count * 0.0001d));

                            //
                            MosaicFootprint tile = new MosaicFootprint(image, xmin, ymin, xmax, ymax, (count + 1d) * 0.01d, this._imageViewer.HighlightLines, this._imageViewer.SelectedLines);
                            tile.Selected += new EventHandler<EventArgs>(this.Footprint_Selected);
                            tile.Element.MouseLeftButtonDown += new MouseButtonEventHandler(this.Element_MouseLeftButtonDown);

                            foreach (KeyValuePair<string, object> kvp in e.Attributes) {
                                tile.Attributes.Add(kvp.Key, kvp.Value);
                            }
                            this._tiles.Add(tile);

                            // Add Footprint to 3D display
                            this._imageViewer.Footprints.Children.Add(tile.ModelVisual);
                        }
                        catch (Exception ex) {
                            MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace);
                        }
                    }
                )
            );
        }
        private void Extractor_ThumbnailSummary(object sender, SummaryEventArgs e) {
            this._imageViewer.Dispatcher.Invoke(
                DispatcherPriority.Normal,
                new Action(
                    delegate() {
                        try {
                            // Exit if no footprints found
                            if (e.Count == 0) {
                                MessageBox.Show("Nothing Selected");
                                return;
                            }

                            // Zoom out to full extent of all imagery
                            double xmin = this._baseWidth_ * (e.XMin - this._baseExtent.XMin) / (this._baseExtent.XMax - this._baseExtent.XMin);
                            double xmax = this._baseWidth_ * (e.XMax - this._baseExtent.XMin) / (this._baseExtent.XMax - this._baseExtent.XMin);
                            Point3D position = this._imageViewer.Camera.Position;
                            position.Z = 2 * (xmax - xmin);
                            Point3DAnimation animation = new Point3DAnimation() {
                                Duration = new Duration(TimeSpan.FromSeconds(3d)),
                                To = position,
                                AccelerationRatio = ACCELERATIONRATIO,
                                DecelerationRatio = DECELERATIONRATIO
                            };
                            this._imageViewer.Camera.BeginAnimation(PerspectiveCamera.PositionProperty, animation);

                            //
                            this._fields.Clear();
                            e.Fields.ForEach(
                                f => {
                                    this._fields.Add(f);
                                }
                            );

                            // Assign Fields to UI
                            this._imageViewer.ListBoxFields.ItemsSource = this._fields.Where(
                                f => (
                                        f.Type == esriFieldType.esriFieldTypeDate ||
                                        f.Type == esriFieldType.esriFieldTypeDouble ||
                                        f.Type == esriFieldType.esriFieldTypeInteger ||
                                        f.Type == esriFieldType.esriFieldTypeOID ||
                                        f.Type == esriFieldType.esriFieldTypeSingle ||
                                        f.Type == esriFieldType.esriFieldTypeSmallInteger
                                     )
                                     &&
                                     (
                                        f.Name.ToLowerInvariant() != "minps" &&
                                        f.Name.ToLowerInvariant() != "maxps" &&
                                        f.Name.ToLowerInvariant() != "lowps" &&
                                        f.Name.ToLowerInvariant() != "highps" &&
                                        f.Name.ToLowerInvariant() != "category" &&
                                        f.Name.ToLowerInvariant() != "centerx" &&
                                        f.Name.ToLowerInvariant() != "centery" &&
                                        f.Name.ToLowerInvariant() != "zorder" &&
                                        f.Name.ToLowerInvariant() != "sorder" &&
                                        f.Name.ToLowerInvariant() != "shape_length"
                                     )
                            );
                        }
                        catch (Exception ex) {
                            MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace);
                        }
                    }
                )
            );
        }
        private void Footprint_Selected(object sender, EventArgs e) {
            MosaicFootprint footprint = sender as MosaicFootprint;
            this._imageViewer.ItemsControl.ItemsSource = footprint.Attributes.Where(
                kvp =>
                    kvp.Key.ToLowerInvariant() != "minps" &&
                    kvp.Key.ToLowerInvariant() != "maxps" &&
                    kvp.Key.ToLowerInvariant() != "lowps" &&
                    kvp.Key.ToLowerInvariant() != "highps" &&
                    kvp.Key.ToLowerInvariant() != "category" &&
                    kvp.Key.ToLowerInvariant() != "centerx" &&
                    kvp.Key.ToLowerInvariant() != "centery" &&
                    kvp.Key.ToLowerInvariant() != "zorder" &&
                    kvp.Key.ToLowerInvariant() != "sorder" &&
                    kvp.Key.ToLowerInvariant() != "shape_length"
            );
        }
        private void Element_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            try {
                // Toggle Selected State for Clicked Footprint
                FrameworkElement element = sender as FrameworkElement;
                this._tiles.ForEach(
                    tile => {
                        if (tile.Element == element) {
                            tile.IsSelected = !tile.IsSelected;
                        }
                        else {
                            if (tile.IsSelected) {
                                tile.IsSelected = false;
                            }
                        }
                    }
                );

                // Nothing selected?
                bool nothing = this._tiles.TrueForAll(tile => !tile.IsSelected);
                if (nothing) {
                    this._imageViewer.ItemsControl.ItemsSource = null;
                }
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            try {
                if (sender == this._imageViewer.SliderExaggeration) {
                    // Get Selected Field
                    Field field = this._imageViewer.ListBoxFields.SelectedItem as Field;
                    if (field == null) { return; }

                    // Get Sort
                    Sort? sort = null;
                    if (this._imageViewer.RadioButtonAscend.IsChecked.Value) {
                        sort = Sort.Ascending;
                    }
                    else if (this._imageViewer.RadioButtonDescend.IsChecked.Value) {
                        sort = Sort.Descending;
                    }
                    if (sort == null) { return; }

                    // Sort Footprints
                    this.SortFootprints(field, sort.Value);
                }
                else if (sender == this._imageViewer.SliderFilter) {
                    // Exit if no images loaded
                    if (this._tiles.Count == 0) { return; }

                    // Calculate Percentage
                    double adjustedValue = (this._imageViewer.SliderFilter.Value - this._imageViewer.SliderFilter.Minimum) / (this._imageViewer.SliderFilter.Maximum - this._imageViewer.SliderFilter.Minimum);

                    // Show None
                    if (adjustedValue == 0d) {
                        this._tiles.ForEach(
                            t => {
                                if (t.Visible) {
                                    t.Visible = false;
                                }
                            }
                        );
                        return;
                    };

                    // Show All
                    if (adjustedValue == 1d) {
                        this._tiles.ForEach(
                            t => {
                                if (!t.Visible) {
                                    t.Visible = true;
                                }
                            }
                        );
                        return;
                    }

                    // Find Min/Max
                    double min = this._tiles.Min(t => t.Height);
                    double max = this._tiles.Max(t => t.Height);

                    // Calc mid point
                    double mid = min + (adjustedValue * (max - min));

                    // Show Some
                    this._tiles.ForEach(
                        t => {
                            bool show = t.Height <= mid;
                            if (t.Visible != show) {
                                t.Visible = show;
                            }
                        }
                    );
                }
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
        private void RadioButton_Checked(object sender, RoutedEventArgs e) {
            try {
                if (sender == this._imageViewer.RadioButtonAscend) {
                    // Get Selected Field
                    Field field = this._imageViewer.ListBoxFields.SelectedItem as Field;
                    if (field == null) { return; }

                    // Remove Filter
                    if (this._imageViewer.SliderFilter.Value != this._imageViewer.SliderFilter.Maximum) {
                        this._imageViewer.SliderFilter.Value = this._imageViewer.SliderFilter.Maximum;
                    }

                    // Sort
                    this.SortFootprints(field, Sort.Ascending);
                }
                else if (sender == this._imageViewer.RadioButtonDescend) {
                    // Get Selected Field
                    Field field = this._imageViewer.ListBoxFields.SelectedItem as Field;
                    if (field == null) { return; }

                    // Remove Filter
                    if (this._imageViewer.SliderFilter.Value != this._imageViewer.SliderFilter.Maximum) {
                        this._imageViewer.SliderFilter.Value = this._imageViewer.SliderFilter.Maximum;
                    }

                    // Sort
                    this.SortFootprints(field, Sort.Descending);
                }
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            try {
                if (sender == this._imageViewer.ListBoxFields) {
                    // Exit if invalid
                    if (e == null) { return; }
                    if (e.AddedItems.Count == 0) { return; }

                    // Get Selected field
                    Field field = e.AddedItems[0] as Field;
                    if (field == null) { return; }

                    // Get Sort
                    Sort? sort = null;
                    if (this._imageViewer.RadioButtonAscend.IsChecked.Value) {
                        sort = Sort.Ascending;
                    }
                    else if (this._imageViewer.RadioButtonDescend.IsChecked.Value) {
                        sort = Sort.Descending;
                    }
                    if (sort == null) { return; }

                    // Remove Filter
                    if (this._imageViewer.SliderFilter.Value != this._imageViewer.SliderFilter.Maximum) {
                        this._imageViewer.SliderFilter.Value = this._imageViewer.SliderFilter.Maximum;
                    }

                    // Sort Footprints
                    this.SortFootprints(field, sort.Value);
                }
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
        private void SortFootprints(Field field, Sort sort) {
            // Find the maximum width/height
            double max = Math.Max(
                this._tiles.Max(t => t.XMax) - this._tiles.Max(t => t.XMin),
                this._tiles.Max(t => t.YMax) - this._tiles.Max(t => t.YMin)
            );
            if (max < 2d) {
                max = 2d;
            }

            //
            double min = 0.1d * max;

            // Find the range in values
            double minvalue = this._tiles.Min(
                t => { return this.GetFieldValue(t, field); }
            );
            double maxvalue = this._tiles.Max(
                t => { return this.GetFieldValue(t, field); }
            );

            // Create a new storyboard
            Storyboard storyboard = new Storyboard();
            storyboard.Completed += (a, b) => {
                // Change vertical offset if exageration has changed
                double ceiling = -0.5d * this._imageViewer.SliderExaggeration.Value * max;
                if (this._imageViewer.TranslateRoot.OffsetZ != ceiling) {
                    DoubleAnimation doubleAnimation = new DoubleAnimation() {
                        Duration = new Duration(TimeSpan.FromMilliseconds(1500d)),
                        To = ceiling,
                        AccelerationRatio = ButtonMosaicViewer.ACCELERATIONRATIO,
                        DecelerationRatio = ButtonMosaicViewer.DECELERATIONRATIO
                    };
                    doubleAnimation.Completed += (s, e) => {
                        this.Reorder();
                    };
                    this._imageViewer.TranslateRoot.BeginAnimation(TranslateTransform3D.OffsetZProperty, doubleAnimation);
                }
                else {
                    this.Reorder();
                }
                
            };

            // Loop for each tile
            foreach (MosaicFootprint tile in this._tiles) {
                // Get Current Tile Value
                double v1 = this.GetFieldValue(tile, field);

                // Get Scaled Value
                double v2 = min;
                if (maxvalue != minvalue) {
                    switch (sort) {
                        case Sort.Ascending:
                            v2 = min + (max - min) * (v1 - minvalue) / (maxvalue - minvalue);
                            break;
                        case Sort.Descending:
                            v2 = min + (max - min) * (maxvalue - v1) / (maxvalue - minvalue);
                            break;
                    }
                }

                // Apply Exaggeration
                v2 *= this._imageViewer.SliderExaggeration.Value;

                // Apply Random addition to prevent overlaps
                v2 += this._random.NextDouble() * 0.01d;

                // Create Animation
                DoubleAnimation d = new DoubleAnimation() {
                    Duration = new Duration(TimeSpan.FromSeconds(2d)),
                    To = v2,
                    AccelerationRatio = 0.2,
                    DecelerationRatio = 0.6,
                };
                Storyboard.SetTarget(d, tile);
                Storyboard.SetTargetProperty(d, new PropertyPath(MosaicFootprint.HeightProperty));
                storyboard.Children.Add(d);
            }

            // Start the animation
            storyboard.Begin();
        }
        private void Reorder() {
            // Reorder
            this._tiles.Sort(
                delegate(MosaicFootprint a, MosaicFootprint b) {
                    return a.Height.CompareTo(b.Height);
                }
            );
            this._tiles.Sort(
                (MosaicFootprint a, MosaicFootprint b) => {
                    return a.Height.CompareTo(b.Height);
                }
            );
            foreach(MosaicFootprint f in this._tiles) {
                int index1 = this._tiles.IndexOf(f);
                int index2 = this._imageViewer.Footprints.Children.IndexOf(f.ModelVisual);
                if (index1 != index2) {
                    this._imageViewer.Footprints.Children.RemoveAt(index2);
                    this._imageViewer.Footprints.Children.Insert(index1, f.ModelVisual);
                }
            }
        }
        private void AddImageShadow(double xmin, double ymin, double xmax, double ymax, double height) {
            // Define vertex positions
            Point3DCollection vertexPositions = new Point3DCollection();
            vertexPositions.Add(new Point3D(xmin, ymin, height));
            vertexPositions.Add(new Point3D(xmin, ymax, height));
            vertexPositions.Add(new Point3D(xmax, ymax, height));
            vertexPositions.Add(new Point3D(xmax, ymin, height));

            // Define triangles
            Int32Collection triangles = new Int32Collection();
            triangles.Add(0);
            triangles.Add(3);
            triangles.Add(2);
            triangles.Add(2);
            triangles.Add(1);
            triangles.Add(0);

            // Define textures
            System.Windows.Media.PointCollection textures = new System.Windows.Media.PointCollection();
            textures.Add(new System.Windows.Point(0, 1));
            textures.Add(new System.Windows.Point(0, 0));
            textures.Add(new System.Windows.Point(1, 0));
            textures.Add(new System.Windows.Point(1, 1));

            // Define normals
            Vector3DCollection normals = new Vector3DCollection();
            normals.Add(new System.Windows.Media.Media3D.Vector3D(0, 0, 1));
            normals.Add(new System.Windows.Media.Media3D.Vector3D(0, 0, 1));
            normals.Add(new System.Windows.Media.Media3D.Vector3D(0, 0, 1));
            normals.Add(new System.Windows.Media.Media3D.Vector3D(0, 0, 1));
            normals.Add(new System.Windows.Media.Media3D.Vector3D(0, 0, 1));
            normals.Add(new System.Windows.Media.Media3D.Vector3D(0, 0, 1));

            double h = 0d;
            double w = 0d;
            const double SHADOWMAX = 100d;
            if (xmax - xmin > ymax - ymin) {
                w = SHADOWMAX;
                h = w * (ymax - ymin) / (xmax - xmin);
            }
            else {
                h = SHADOWMAX;
                w = h * (xmax - xmin) / (ymax - ymin);
            }

            // Create Border (up facing) with image texture
            Border element = new Border() {
                Height = h,
                Width = w,
                Background = new SolidColorBrush() {
                    Color = Colors.LightGray
                },
                BorderBrush = new SolidColorBrush() {
                    Color = Colors.Black
                },
                BorderThickness = new Thickness() {
                    Bottom = 2d,
                    Top = 2d,
                    Left = 2d,
                    Right = 2d
                },
                Opacity = 0.3d
            };

            // Create Viewport2DVisual3D from Border
            Viewport2DVisual3D top = new Viewport2DVisual3D() {
                Geometry = new MeshGeometry3D() {
                    Normals = normals,
                    Positions = vertexPositions,
                    TextureCoordinates = textures,
                    TriangleIndices = triangles
                },
                Material = new DiffuseMaterial() {
                    Brush = new SolidColorBrush() {
                        Color = Colors.White
                    },
                    AmbientColor = Colors.White
                },
                Visual = element
            };
            Viewport2DVisual3D.SetIsVisualHostMaterial(top.Material, true);

            // Create down facing texture
            ModelUIElement3D bottom = new ModelUIElement3D() {
                IsHitTestVisible = true,
                Model = new GeometryModel3D() {
                    Geometry = new MeshGeometry3D() {
                        Normals = normals,
                        Positions = vertexPositions,
                        TextureCoordinates = textures,
                        TriangleIndices = triangles
                    },
                    BackMaterial = new DiffuseMaterial() {
                        Brush = new SolidColorBrush() {
                            Color = Colors.LightGray
                        }
                    }
                }
            };

            // Create Model
            ModelVisual3D model = new ModelVisual3D();
            model.Children.Add(top);
            model.Children.Add(bottom);

            //
            this._imageViewer.Shadow.Children.Add(model);
        }
        private void AddBase() {
            if (this._baseExtent.Width > this._baseExtent.Height) {
                this._baseWidth_ = 1d;
                this._baseHeight = 1d * this._baseExtent.Height / this._baseExtent.Width;
            }
            else {
                this._baseWidth_ = 1d * this._baseExtent.Width / this._baseExtent.Height;
                this._baseHeight = 1d;
            }

            // Define vertex positions
            Point3DCollection vertexPositions = new Point3DCollection();
            vertexPositions.Add(new Point3D(0, 0, 0));
            vertexPositions.Add(new Point3D(0, this._baseHeight, 0));
            vertexPositions.Add(new Point3D(this._baseWidth_, this._baseHeight, 0));
            vertexPositions.Add(new Point3D(this._baseWidth_, 0, 0));

            // Define triangles
            Int32Collection triangles = new Int32Collection();
            triangles.Add(0);
            triangles.Add(3);
            triangles.Add(2);
            triangles.Add(2);
            triangles.Add(1);
            triangles.Add(0);

            // Define textures
            System.Windows.Media.PointCollection textures = new System.Windows.Media.PointCollection();
            textures.Add(new System.Windows.Point(0, 1));
            textures.Add(new System.Windows.Point(0, 0));
            textures.Add(new System.Windows.Point(1, 0));
            textures.Add(new System.Windows.Point(1, 1));

            // Define normals
            Vector3DCollection normals = new Vector3DCollection();
            normals.Add(new System.Windows.Media.Media3D.Vector3D(0, 0, 1));
            normals.Add(new System.Windows.Media.Media3D.Vector3D(0, 0, 1));
            normals.Add(new System.Windows.Media.Media3D.Vector3D(0, 0, 1));
            normals.Add(new System.Windows.Media.Media3D.Vector3D(0, 0, 1));
            normals.Add(new System.Windows.Media.Media3D.Vector3D(0, 0, 1));
            normals.Add(new System.Windows.Media.Media3D.Vector3D(0, 0, 1));

            ModelUIElement3D model = new ModelUIElement3D() {
                IsHitTestVisible = false,
                Model = new GeometryModel3D() {
                    Geometry = new MeshGeometry3D() {
                        Normals = normals,
                        Positions = vertexPositions,
                        TextureCoordinates = textures,
                        TriangleIndices = triangles
                    },
                    Material = new DiffuseMaterial() {
                        Brush = this._imageViewer.Grid.Resources["GreenBrush"] as LinearGradientBrush
                    },
                    BackMaterial = new DiffuseMaterial() {
                        Brush = new SolidColorBrush() {
                            Color = Colors.LightGray
                        }
                    }
                }
            };
            this._imageViewer.Base.Children.Add(model);

            // Reset Offset
            DoubleAnimation x = new DoubleAnimation() {
                BeginTime = TimeSpan.FromMilliseconds(0d),
                Duration = new Duration(TimeSpan.FromMilliseconds(2000d)),
                To = -0.5d * this._baseWidth_,
                AccelerationRatio = ACCELERATIONRATIO,
                DecelerationRatio = DECELERATIONRATIO
            };
            DoubleAnimation y = new DoubleAnimation() {
                BeginTime = TimeSpan.FromMilliseconds(0d),
                Duration = new Duration(TimeSpan.FromMilliseconds(2000d)),
                To = -0.5d * this._baseHeight,
                AccelerationRatio = ACCELERATIONRATIO,
                DecelerationRatio = DECELERATIONRATIO
            };
            DoubleAnimation z = new DoubleAnimation() {
                BeginTime = TimeSpan.FromMilliseconds(0d),
                Duration = new Duration(TimeSpan.FromMilliseconds(2000d)),
                To = 0d,
                AccelerationRatio = ACCELERATIONRATIO,
                DecelerationRatio = DECELERATIONRATIO
            };
            this._imageViewer.TranslateRoot.BeginAnimation(TranslateTransform3D.OffsetXProperty, x);
            this._imageViewer.TranslateRoot.BeginAnimation(TranslateTransform3D.OffsetYProperty, y);
            this._imageViewer.TranslateRoot.BeginAnimation(TranslateTransform3D.OffsetZProperty, z);
        }
        private void InitializeDisplay() {
            // Clear Graphics
            this._imageViewer.Base.Children.Clear();
            this._imageViewer.Shadow.Children.Clear();
            this._imageViewer.Footprints.Children.Clear();
            this._imageViewer.HighlightLines.Children.Clear();
            this._imageViewer.SelectedLines.Children.Clear();

            // Clear ListBox
            this._imageViewer.ListBoxFields.ItemsSource = null;

            // Clear Selected Attributes
            this._imageViewer.ItemsControl.ItemsSource = null;

            // Reset Filter Slider
            this._imageViewer.SliderFilter.Value = this._imageViewer.SliderFilter.Maximum;

            // Clear Collections
            this._fields.Clear();
            this._tiles.Clear();

            // Clear Fields
            this._droplayer = null;
            this._baseExtent = null;
            this._baseWidth_ = 0d;
            this._baseHeight = 0d;

            // Show Prompt Message
            Viewport2DVisual3D visual = this._imageViewer.Message.Children[0] as Viewport2DVisual3D;
            DropPrompt prompt = visual.Visual as DropPrompt;
            prompt.Visibility = Visibility.Visible;

            // Reset Scale
            Transform3DGroup group = this._imageViewer.inter3D.Transform as Transform3DGroup;
            ScaleTransform3D scale = group.Children[0] as ScaleTransform3D;
            scale.ScaleX = 1d;
            scale.ScaleY = 1d;
            scale.ScaleZ = 1d;

            // Reset Rotation
            RotateTransform3D rotate = group.Children[1] as RotateTransform3D;
            AxisAngleRotation3D angle = rotate.Rotation as AxisAngleRotation3D;
            angle.Angle = 0d;
            angle.Axis = new System.Windows.Media.Media3D.Vector3D(0d, 1d, 0d);

            // Reset Offset
            DoubleAnimation x = new DoubleAnimation() {
                BeginTime = TimeSpan.FromMilliseconds(0d),
                Duration = new Duration(TimeSpan.FromMilliseconds(2000d)),
                To = 0d,
                AccelerationRatio = ACCELERATIONRATIO,
                DecelerationRatio = DECELERATIONRATIO
            };
            DoubleAnimation y = new DoubleAnimation() {
                BeginTime = TimeSpan.FromMilliseconds(0d),
                Duration = new Duration(TimeSpan.FromMilliseconds(2000d)),
                To = 0d,
                AccelerationRatio = ACCELERATIONRATIO,
                DecelerationRatio = DECELERATIONRATIO
            };
            DoubleAnimation z = new DoubleAnimation() {
                BeginTime = TimeSpan.FromMilliseconds(0d),
                Duration = new Duration(TimeSpan.FromMilliseconds(2000d)),
                To = 0d,
                AccelerationRatio = ACCELERATIONRATIO,
                DecelerationRatio = DECELERATIONRATIO
            };
            this._imageViewer.TranslateRoot.BeginAnimation(TranslateTransform3D.OffsetXProperty, x);
            this._imageViewer.TranslateRoot.BeginAnimation(TranslateTransform3D.OffsetYProperty, y);
            this._imageViewer.TranslateRoot.BeginAnimation(TranslateTransform3D.OffsetZProperty, z);

            // Reset Camera Position
            Point3DAnimation p = new Point3DAnimation() {
                BeginTime = TimeSpan.FromMilliseconds(0d),
                Duration = new Duration(TimeSpan.FromMilliseconds(2000d)),
                To = new Point3D() {
                    X = 0d,
                    Y = 0d,
                    Z = 1d
                },
                AccelerationRatio = ACCELERATIONRATIO,
                DecelerationRatio = DECELERATIONRATIO
            };
            this._imageViewer.Camera.BeginAnimation(PerspectiveCamera.PositionProperty, p);
        }
        private double GetFieldValue(MosaicFootprint tile, Field field) {
            object obj = tile.Attributes[field.Name];
            if (obj == null) { return 0d; }
            if (obj == DBNull.Value) { return 0d; }
            string str = obj.ToString();
            if (string.IsNullOrEmpty(str)) { return 0d; }

            switch (field.Type) {
                case esriFieldType.esriFieldTypeDate:
                    DateTime date;
                    if (!DateTime.TryParse(str, out date)) { return 0d; }
                    return date.Ticks;
                case esriFieldType.esriFieldTypeDouble:
                case esriFieldType.esriFieldTypeInteger:
                case esriFieldType.esriFieldTypeOID:
                case esriFieldType.esriFieldTypeSingle:
                case esriFieldType.esriFieldTypeSmallInteger:
                    double dble;
                    if (!double.TryParse(str, out dble)) { return 0d; }
                    return dble;
                default:
                    return 0d;
            }
        }
    }
}
