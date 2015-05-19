/* -----------------------------------------------------------------------------------
   Developed by the Applications Prototype Lab
   (c) 2015 Esri | http://www.esri.com/legal/software-license  
----------------------------------------------------------------------------------- */

using System;
using System.Collections.Generic;
using System.Threading;

namespace ESRI.PrototypeLab.MosaicImageSelector {
    public sealed class MosaicEnvironment {
        private static volatile MosaicEnvironment instance;
        private static object sync = new Object();
        private MosaicEnvironment() {
            this.Threads = new List<Thread>();
        }
        public static MosaicEnvironment Default {
            get {
                if (instance == null) {
                    lock (sync) {
                        if (instance == null){
                            instance = new MosaicEnvironment();
                        }
                    }
                }
                return instance;
            }
        }
        public List<Thread> Threads;
    }
}
