/* -----------------------------------------------------------------------------------
   Developed by the Applications Prototype Lab
   (c) 2015 Esri | http://www.esri.com/legal/software-license  
----------------------------------------------------------------------------------- */

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using _3DTools;

namespace ESRI.PrototypeLab.MosaicImageSelector {
    public class MosaicFootprint : DependencyObject {
        //
        // CONSTRUCTOR
        //
        public MosaicFootprint(BitmapImage image, double xmin, double ymin, double xmax, double ymax, double height, ModelVisual3D highlightlines, ModelVisual3D selectedlines) {
            //
            this.Attributes = new Dictionary<string, object>();
            this.Image = image;
            this.XMin = xmin;
            this.YMin = ymin;
            this.XMax = xmax;
            this.YMax = ymax;
            this.Height = height;
            this.HighlightLines = highlightlines;
            this.SelectedLines = selectedlines;

            // Define normals
            Vector3DCollection normals = new Vector3DCollection();
            normals.Add(new Vector3D(0, 0, 1));
            normals.Add(new Vector3D(0, 0, 1));
            normals.Add(new Vector3D(0, 0, 1));
            normals.Add(new Vector3D(0, 0, 1));
            normals.Add(new Vector3D(0, 0, 1));
            normals.Add(new Vector3D(0, 0, 1));

            // Define vertex positions
            Point3DCollection positions = new Point3DCollection();
            positions.Add(new Point3D(this.XMin, this.YMin, this.Height));
            positions.Add(new Point3D(this.XMin, this.YMax, this.Height));
            positions.Add(new Point3D(this.XMax, this.YMax, this.Height));
            positions.Add(new Point3D(this.XMax, this.YMin, this.Height));

            // Define textures
            PointCollection textureCoordinates = new PointCollection();
            textureCoordinates.Add(new Point(0, 1));
            textureCoordinates.Add(new Point(0, 0));
            textureCoordinates.Add(new Point(1, 0));
            textureCoordinates.Add(new Point(1, 1));

            // Define triangles
            Int32Collection triangleIndices = new Int32Collection();
            triangleIndices.Add(0);
            triangleIndices.Add(3);
            triangleIndices.Add(2);
            triangleIndices.Add(2);
            triangleIndices.Add(1);
            triangleIndices.Add(0);

            // Create Border (up facing) with image texture
            this.Element = new Border() {
                Cursor = Cursors.Hand,
                Height = 100,
                Width = 100,
                Background = new ImageBrush() {
                    ImageSource = this.Image,
                    Stretch = Stretch.Fill
                }
            };
            this.Element.MouseEnter += (s, e) => {
                this.IsMouseOver = true;
            };
            this.Element.MouseLeave += (s, e) => {
                this.IsMouseOver = false;
            };

            // Create Viewport2DVisual3D from Border
            Viewport2DVisual3D top = new Viewport2DVisual3D() {
                Geometry = new MeshGeometry3D() {
                    Normals = normals,
                    Positions = positions,
                    TextureCoordinates = textureCoordinates,
                    TriangleIndices = triangleIndices
                },
                Material = new DiffuseMaterial() {
                    Brush = new SolidColorBrush() {
                        Color = Colors.White
                    },
                    AmbientColor = Colors.White
                },
                Visual = this.Element
            };
            Viewport2DVisual3D.SetIsVisualHostMaterial(top.Material, true);

            // Create down facing texture
            ModelUIElement3D bottom = new ModelUIElement3D() {
                IsHitTestVisible = true, 
                Model = new GeometryModel3D() {
                    Geometry = new MeshGeometry3D() {
                        Normals = normals,
                        Positions = positions,
                        TextureCoordinates = textureCoordinates,
                        TriangleIndices = triangleIndices
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
            this.ModelVisual = model;
        }
        //
        // EVENTS
        //
        public event EventHandler<EventArgs> Selected;
        //
        // PROPERTIES
        //
        public BitmapImage Image { get; private set; }
        public double XMin { get; private  set; }
        public double YMin { get; private  set; }
        public double XMax { get; private  set; }
        public double YMax { get; private set; }
        public ModelVisual3D ModelVisual { get; private set; }
        public ModelVisual3D HighlightLines { get; private set; }
        public ModelVisual3D SelectedLines { get; private set; }
        public FrameworkElement Element { get; private set; }
        public Dictionary<string, object> Attributes { get; private set; }
        public double Height {
            get { return ((double)base.GetValue(MosaicFootprint.HeightProperty)); }
            set { base.SetValue(MosaicFootprint.HeightProperty, value); }
        }
        public bool Visible {
            get { return ((bool)base.GetValue(MosaicFootprint.VisibleProperty)); }
            set { base.SetValue(MosaicFootprint.VisibleProperty, value); }
        }
        public bool IsSelected {
            get { return ((bool)base.GetValue(MosaicFootprint.IsSelectedProperty)); }
            set { base.SetValue(MosaicFootprint.IsSelectedProperty, value); }
        }
        public bool IsMouseOver {
            get { return ((bool)base.GetValue(MosaicFootprint.IsMouseOverProperty)); }
            set { base.SetValue(MosaicFootprint.IsMouseOverProperty, value); }
        }
        //
        // METHODS
        //
        protected void OnSelected(EventArgs e) {
            if (this.Selected != null) {
                this.Selected(this, e);
            }
        }
        public static readonly DependencyProperty HeightProperty = DependencyProperty.Register(
            "Height",
            typeof(double),
            typeof(MosaicFootprint),
            new PropertyMetadata(
                0d,
                new PropertyChangedCallback(
                    MosaicFootprint.DependencyPropertyChanged)));
        public static readonly DependencyProperty VisibleProperty = DependencyProperty.Register(
            "Visible",
            typeof(bool),
            typeof(MosaicFootprint),
            new PropertyMetadata(
                true,
                new PropertyChangedCallback(
                    MosaicFootprint.DependencyPropertyChanged)));
        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
            "IsSelected",
            typeof(bool),
            typeof(MosaicFootprint),
            new PropertyMetadata(
                false,
                new PropertyChangedCallback(
                    MosaicFootprint.DependencyPropertyChanged)));
        public static readonly DependencyProperty IsMouseOverProperty = DependencyProperty.Register(
            "IsMouseOver",
            typeof(bool),
            typeof(MosaicFootprint),
            new PropertyMetadata(
                false,
                new PropertyChangedCallback(
                    MosaicFootprint.DependencyPropertyChanged)));
        private static void DependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            // Get Footprint
            MosaicFootprint footprint = d as MosaicFootprint;

            // Get Parent ModelVisual
            ModelVisual3D modelVisual = footprint.ModelVisual as ModelVisual3D;
            if (modelVisual == null) { return; }

            // Update mesh vertices of up facing element
            Viewport2DVisual3D viewport2d = modelVisual.Children[0] as Viewport2DVisual3D;
            MeshGeometry3D mesh1 = viewport2d.Geometry as MeshGeometry3D;

            // Update mesh vertices of down facing element
            ModelUIElement3D element3d = modelVisual.Children[1] as ModelUIElement3D;
            GeometryModel3D geometry = element3d.Model as GeometryModel3D;
            MeshGeometry3D mesh2 = geometry.Geometry as MeshGeometry3D;
            if (mesh2 == null) { return; }

            // Update Footprint
            switch (e.Property.Name) {
                case "Height":
                    // Elevate Top & Bottom
                    mesh1.Positions = MosaicFootprint.Elevate(mesh1.Positions, (double)e.NewValue);
                    mesh2.Positions = MosaicFootprint.Elevate(mesh2.Positions, (double)e.NewValue);

                    if (footprint.IsMouseOver) {
                        footprint.HighlightLines.Children.Clear();

                        // Create Lines
                        Point3DCollection points1 = new Point3DCollection();
                        points1.Add(new Point3D(footprint.XMin, footprint.YMin, 0d));
                        points1.Add(new Point3D(footprint.XMin, footprint.YMin, footprint.Height));
                        points1.Add(new Point3D(footprint.XMin, footprint.YMax, 0d));
                        points1.Add(new Point3D(footprint.XMin, footprint.YMax, footprint.Height));
                        points1.Add(new Point3D(footprint.XMax, footprint.YMin, 0d));
                        points1.Add(new Point3D(footprint.XMax, footprint.YMin, footprint.Height));
                        points1.Add(new Point3D(footprint.XMax, footprint.YMax, 0d));
                        points1.Add(new Point3D(footprint.XMax, footprint.YMax, footprint.Height));
                        points1.Add(new Point3D(footprint.XMin, footprint.YMin, footprint.Height));
                        points1.Add(new Point3D(footprint.XMin, footprint.YMax, footprint.Height));
                        points1.Add(new Point3D(footprint.XMin, footprint.YMax, footprint.Height));
                        points1.Add(new Point3D(footprint.XMax, footprint.YMax, footprint.Height));
                        points1.Add(new Point3D(footprint.XMax, footprint.YMax, footprint.Height));
                        points1.Add(new Point3D(footprint.XMax, footprint.YMin, footprint.Height));
                        points1.Add(new Point3D(footprint.XMax, footprint.YMin, footprint.Height));
                        points1.Add(new Point3D(footprint.XMin, footprint.YMin, footprint.Height));
                        points1.Add(new Point3D(footprint.XMin, footprint.YMin, 0d));
                        points1.Add(new Point3D(footprint.XMin, footprint.YMax, 0d));
                        points1.Add(new Point3D(footprint.XMin, footprint.YMax, 0d));
                        points1.Add(new Point3D(footprint.XMax, footprint.YMax, 0d));
                        points1.Add(new Point3D(footprint.XMax, footprint.YMax, 0d));
                        points1.Add(new Point3D(footprint.XMax, footprint.YMin, 0d));
                        points1.Add(new Point3D(footprint.XMax, footprint.YMin, 0d));
                        points1.Add(new Point3D(footprint.XMin, footprint.YMin, 0d));

                        footprint.HighlightLines.Children.Add(
                            new ScreenSpaceLines3D() {
                                Color = Colors.White,
                                Thickness = 1d,
                                Points = points1
                            }
                        );   
                    }

                    if (footprint.IsSelected) {
                        // Remove old lines
                        List<ScreenSpaceLines3D> lines = new List<ScreenSpaceLines3D>();
                        foreach (Visual3D visual in footprint.SelectedLines.Children) {
                            ScreenSpaceLines3D s = visual as ScreenSpaceLines3D;
                            if (s == null) { continue; }
                            if (s.Tag == footprint) {
                                lines.Add(s);
                            }
                        }
                        lines.ForEach(
                            l => footprint.SelectedLines.Children.Remove(l)
                        );

                        // Create Lines
                        Point3DCollection points2 = new Point3DCollection();
                        points2.Add(new Point3D(footprint.XMin, footprint.YMin, 0d));
                        points2.Add(new Point3D(footprint.XMin, footprint.YMin, footprint.Height));
                        points2.Add(new Point3D(footprint.XMin, footprint.YMax, 0d));
                        points2.Add(new Point3D(footprint.XMin, footprint.YMax, footprint.Height));
                        points2.Add(new Point3D(footprint.XMax, footprint.YMin, 0d));
                        points2.Add(new Point3D(footprint.XMax, footprint.YMin, footprint.Height));
                        points2.Add(new Point3D(footprint.XMax, footprint.YMax, 0d));
                        points2.Add(new Point3D(footprint.XMax, footprint.YMax, footprint.Height));
                        points2.Add(new Point3D(footprint.XMin, footprint.YMin, footprint.Height));
                        points2.Add(new Point3D(footprint.XMin, footprint.YMax, footprint.Height));
                        points2.Add(new Point3D(footprint.XMin, footprint.YMax, footprint.Height));
                        points2.Add(new Point3D(footprint.XMax, footprint.YMax, footprint.Height));
                        points2.Add(new Point3D(footprint.XMax, footprint.YMax, footprint.Height));
                        points2.Add(new Point3D(footprint.XMax, footprint.YMin, footprint.Height));
                        points2.Add(new Point3D(footprint.XMax, footprint.YMin, footprint.Height));
                        points2.Add(new Point3D(footprint.XMin, footprint.YMin, footprint.Height));
                        points2.Add(new Point3D(footprint.XMin, footprint.YMin, 0d));
                        points2.Add(new Point3D(footprint.XMin, footprint.YMax, 0d));
                        points2.Add(new Point3D(footprint.XMin, footprint.YMax, 0d));
                        points2.Add(new Point3D(footprint.XMax, footprint.YMax, 0d));
                        points2.Add(new Point3D(footprint.XMax, footprint.YMax, 0d));
                        points2.Add(new Point3D(footprint.XMax, footprint.YMin, 0d));
                        points2.Add(new Point3D(footprint.XMax, footprint.YMin, 0d));
                        points2.Add(new Point3D(footprint.XMin, footprint.YMin, 0d));

                        footprint.SelectedLines.Children.Add(
                            new ScreenSpaceLines3D() {
                                Color = Colors.Red,
                                Thickness = 1d,
                                Points = points2,
                                Tag = footprint
                            }
                        );  
                    }

                    break;
                case "Visible":
                    bool vis = (bool)e.NewValue;
                    Visibility visibility = (vis) ? Visibility.Visible : Visibility.Collapsed;
                    if (footprint.Element.Visibility != visibility) {
                        footprint.Element.Visibility = visibility;
                    }
                    if (element3d.Visibility != visibility) {
                        element3d.Visibility = visibility;
                    }

                    if (vis) {
                        mesh1.Positions = MosaicFootprint.Elevate(mesh1.Positions, footprint.Height);
                        mesh2.Positions = MosaicFootprint.Elevate(mesh2.Positions, footprint.Height);
                    }
                    else {
                        mesh1.Positions = MosaicFootprint.Elevate(mesh1.Positions, -1d);
                        mesh2.Positions = MosaicFootprint.Elevate(mesh2.Positions, -1d);
                    }
                    break;
                case "IsSelected":
                    bool isselected = (bool)e.NewValue;

                    // Remove old lines
                    List<ScreenSpaceLines3D> lines2 = new List<ScreenSpaceLines3D>();
                    foreach(Visual3D visual in footprint.SelectedLines.Children){
                        ScreenSpaceLines3D s = visual as ScreenSpaceLines3D;
                        if (s == null){continue;}
                        if (s.Tag == footprint){
                            lines2.Add(s);
                        }
                    }
                    lines2.ForEach(
                        l => footprint.SelectedLines.Children.Remove(l)
                    );

                    if (isselected) {
                        // Create Lines
                        Point3DCollection points3 = new Point3DCollection();
                        points3.Add(new Point3D(footprint.XMin, footprint.YMin, 0d));
                        points3.Add(new Point3D(footprint.XMin, footprint.YMin, footprint.Height));
                        points3.Add(new Point3D(footprint.XMin, footprint.YMax, 0d));
                        points3.Add(new Point3D(footprint.XMin, footprint.YMax, footprint.Height));
                        points3.Add(new Point3D(footprint.XMax, footprint.YMin, 0d));
                        points3.Add(new Point3D(footprint.XMax, footprint.YMin, footprint.Height));
                        points3.Add(new Point3D(footprint.XMax, footprint.YMax, 0d));
                        points3.Add(new Point3D(footprint.XMax, footprint.YMax, footprint.Height));
                        points3.Add(new Point3D(footprint.XMin, footprint.YMin, footprint.Height));
                        points3.Add(new Point3D(footprint.XMin, footprint.YMax, footprint.Height));
                        points3.Add(new Point3D(footprint.XMin, footprint.YMax, footprint.Height));
                        points3.Add(new Point3D(footprint.XMax, footprint.YMax, footprint.Height));
                        points3.Add(new Point3D(footprint.XMax, footprint.YMax, footprint.Height));
                        points3.Add(new Point3D(footprint.XMax, footprint.YMin, footprint.Height));
                        points3.Add(new Point3D(footprint.XMax, footprint.YMin, footprint.Height));
                        points3.Add(new Point3D(footprint.XMin, footprint.YMin, footprint.Height));
                        points3.Add(new Point3D(footprint.XMin, footprint.YMin, 0d));
                        points3.Add(new Point3D(footprint.XMin, footprint.YMax, 0d));
                        points3.Add(new Point3D(footprint.XMin, footprint.YMax, 0d));
                        points3.Add(new Point3D(footprint.XMax, footprint.YMax, 0d));
                        points3.Add(new Point3D(footprint.XMax, footprint.YMax, 0d));
                        points3.Add(new Point3D(footprint.XMax, footprint.YMin, 0d));
                        points3.Add(new Point3D(footprint.XMax, footprint.YMin, 0d));
                        points3.Add(new Point3D(footprint.XMin, footprint.YMin, 0d));

                        footprint.SelectedLines.Children.Add(
                            new ScreenSpaceLines3D() {
                                Color = Colors.Red,
                                Thickness = 1d,
                                Points = points3,
                                Tag = footprint
                            }
                        );

                        footprint.OnSelected(EventArgs.Empty);
                    }

                    break;
                case "IsMouseOver":
                    footprint.HighlightLines.Children.Clear();

                    bool ismouseover = (bool)e.NewValue;
                    if (ismouseover) {
                        // Create Lines
                        Point3DCollection points1 = new Point3DCollection();
                        points1.Add(new Point3D(footprint.XMin, footprint.YMin, 0d));
                        points1.Add(new Point3D(footprint.XMin, footprint.YMin, footprint.Height));
                        points1.Add(new Point3D(footprint.XMin, footprint.YMax, 0d));
                        points1.Add(new Point3D(footprint.XMin, footprint.YMax, footprint.Height));
                        points1.Add(new Point3D(footprint.XMax, footprint.YMin, 0d));
                        points1.Add(new Point3D(footprint.XMax, footprint.YMin, footprint.Height));
                        points1.Add(new Point3D(footprint.XMax, footprint.YMax, 0d));
                        points1.Add(new Point3D(footprint.XMax, footprint.YMax, footprint.Height));
                        points1.Add(new Point3D(footprint.XMin, footprint.YMin, footprint.Height));
                        points1.Add(new Point3D(footprint.XMin, footprint.YMax, footprint.Height));
                        points1.Add(new Point3D(footprint.XMin, footprint.YMax, footprint.Height));
                        points1.Add(new Point3D(footprint.XMax, footprint.YMax, footprint.Height));
                        points1.Add(new Point3D(footprint.XMax, footprint.YMax, footprint.Height));
                        points1.Add(new Point3D(footprint.XMax, footprint.YMin, footprint.Height));
                        points1.Add(new Point3D(footprint.XMax, footprint.YMin, footprint.Height));
                        points1.Add(new Point3D(footprint.XMin, footprint.YMin, footprint.Height));
                        points1.Add(new Point3D(footprint.XMin, footprint.YMin, 0d));
                        points1.Add(new Point3D(footprint.XMin, footprint.YMax, 0d));
                        points1.Add(new Point3D(footprint.XMin, footprint.YMax, 0d));
                        points1.Add(new Point3D(footprint.XMax, footprint.YMax, 0d));
                        points1.Add(new Point3D(footprint.XMax, footprint.YMax, 0d));
                        points1.Add(new Point3D(footprint.XMax, footprint.YMin, 0d));
                        points1.Add(new Point3D(footprint.XMax, footprint.YMin, 0d));
                        points1.Add(new Point3D(footprint.XMin, footprint.YMin, 0d));

                        footprint.HighlightLines.Children.Add(
                            new ScreenSpaceLines3D() {
                                Color = Colors.White,
                                Thickness = 1d,
                                Points = points1
                            }
                        );                     
                    }
   
                    break;
            }
        }
        private static Point3DCollection Elevate(Point3DCollection old, double height) {
            Point3DCollection new_ = new Point3DCollection();
            foreach (Point3D point in old) {
                new_.Add(new Point3D(point.X, point.Y, height));
            }
            return new_;
        }
    }
}
