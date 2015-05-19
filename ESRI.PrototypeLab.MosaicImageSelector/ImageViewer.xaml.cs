/* -----------------------------------------------------------------------------------
   Developed by the Applications Prototype Lab
   (c) 2015 Esri | http://www.esri.com/legal/software-license  
----------------------------------------------------------------------------------- */

using System.Windows;

namespace ESRI.PrototypeLab.MosaicImageSelector {
    public partial class ImageViewer : Window {
        public ImageViewer() {
            InitializeComponent();
        }
        public double PanelWidth {
            get { return ((double)base.GetValue(ImageViewer.PanelWidthProperty)); }
            set { base.SetValue(ImageViewer.PanelWidthProperty, value); }
        }
        public static readonly DependencyProperty PanelWidthProperty =
            DependencyProperty.Register(
                "PanelWidth",
                typeof(double),
                typeof(ImageViewer),
                new PropertyMetadata(
                    0d,
                    new PropertyChangedCallback(ImageViewer.DependencyPropertyChanged)));
        private static void DependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            switch (e.Property.Name) {
                case "PanelWidth":
                    ImageViewer v = d as ImageViewer;
                    v.PanelColumn.Width = new GridLength((double)e.NewValue);
                    break;
            }
        }
    }
}
