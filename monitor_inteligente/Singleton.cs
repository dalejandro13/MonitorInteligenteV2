using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using static Android.OS.PowerManager;

namespace monitor_inteligente
{
    class Singleton
    {
        private static Singleton instance = null;
        public Intent intent;

        private static Object sync = new Object();

        public Singleton()
        {

        }

        public static Singleton Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (sync)
                    {
                        instance = new Singleton();
                    }
                }
                return instance;
            }
        }

        public bool OnOff { get; set; }
    }
}