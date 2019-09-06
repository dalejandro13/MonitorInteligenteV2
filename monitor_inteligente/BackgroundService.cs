using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Net;
using Android.Net.Wifi;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Plugin.Connectivity;
using static Android.App.ActivityManager;
using static Android.OS.PowerManager;

namespace monitor_inteligente
{
    [Service]
    public class BackgroundService : Service
    {
        string path_archivos = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).ToString() + "/archivos";
        WifiManager wm;
        WifiManager.WifiLock wifiLock;
        PowerManager pm;
        WakeLock wake;
        DateTime time, date;
        string dia, mes, año, fecha_actual, hora, min;

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            try
            {
                //var combina = Path.Combine(path_archivos, "historial.txt");
                //if (!Directory.Exists(path_archivos))
                //{
                //    Directory.CreateDirectory(path_archivos);
                //}
                //if (!File.Exists(combina))
                //{
                //    File.WriteAllText(combina, string.Empty);
                //}

                //date = DateTime.Today;
                //dia = date.Day.ToString();
                //mes = date.Month.ToString();
                //año = date.Year.ToString();
                //fecha_actual = dia + "/" + mes + "/" + año; //obtengo la fecha actual

                //time = DateTime.Now.ToLocalTime();
                //hora = time.Hour.ToString();
                //min = time.Minute.ToString();
                //string tiempo = hora + ":" + min;

                //if (!File.Exists(Path.Combine(combina)))
                //{
                //    using (var escribe = new StreamWriter(combina, true))
                //    {
                //        escribe.WriteLine("inicio de servicio");
                //        escribe.WriteLine(fecha_actual);
                //        escribe.WriteLine(tiempo);
                //    }
                //}
                //else
                //{
                //    using (var escribe = new StreamWriter(combina, true))
                //    {
                //        escribe.WriteLine("inicio de servicio");
                //        escribe.WriteLine(fecha_actual);
                //        escribe.WriteLine(tiempo);
                //    }
                //}

                WifiManager wm = (WifiManager)GetSystemService(WifiService);
                wifiLock = wm.CreateWifiLock(WifiMode.FullHighPerf, "keep wifi on");
                wifiLock.Acquire();

                pm = (PowerManager)GetSystemService(Context.PowerService);
                wake = pm.NewWakeLock(WakeLockFlags.Partial, "stay awake gently");
                wake.Acquire();

                bool isInBackground;
                RunningAppProcessInfo myProcess = new RunningAppProcessInfo();
                GetMyMemoryState(myProcess);
                isInBackground = myProcess.Importance != Importance.Foreground;
                if (isInBackground)
                {
                    var intento = new Intent(Application.Context, typeof(MainActivity));
                    intento.AddCategory(Intent.CategoryLauncher);
                    intento.AddFlags(ActivityFlags.NewTask);
                    Application.Context.StartActivity(intento);
                }

                return StartCommandResult.Sticky;
            }
            catch (Exception)
            {
                //var combina = Path.Combine(path_archivos, "historial.txt");
                //date = DateTime.Today;
                //dia = date.Day.ToString();
                //mes = date.Month.ToString("d2");
                //año = date.Year.ToString();
                //fecha_actual = dia + "/" + mes + "/" + año; //obtengo la fecha actual

                //time = DateTime.Now.ToLocalTime();
                //hora = time.Hour.ToString();
                //min = time.Minute.ToString();
                //string tiempo = hora + ":" + min;

                //if (!File.Exists(Path.Combine(combina)))
                //{
                //    using (var escribe = new StreamWriter(combina, true))
                //    {
                //        escribe.WriteLine("excepcion de inicio de servicio");
                //        escribe.WriteLine(fecha_actual);
                //        escribe.WriteLine(tiempo);
                //    }
                //}
                //else
                //{
                //    using (var escribe = new StreamWriter(combina, true))
                //    {
                //        escribe.WriteLine("excepcion de inicio de servicio");
                //        escribe.WriteLine(fecha_actual);
                //        escribe.WriteLine(tiempo);
                //    }
                //}

                bool isInBackground;
                RunningAppProcessInfo myProcess = new RunningAppProcessInfo();
                GetMyMemoryState(myProcess);
                isInBackground = myProcess.Importance != Importance.Foreground;
                if (isInBackground)
                {
                    var intento = new Intent(Application.Context, typeof(MainActivity));
                    intento.AddCategory(Intent.CategoryLauncher);
                    intento.AddFlags(ActivityFlags.NewTask);
                    Application.Context.StartActivity(intento);
                }

                return StartCommandResult.Sticky;
            }
        }

        public override bool StopService(Intent name)
        {
            Toast.MakeText(this, "se ha detenido el servicio", ToastLength.Long).Show();
            return base.StopService(name);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            //var combina = Path.Combine(path_archivos, "historial.txt");
            //date = DateTime.Today;
            //dia = date.Day.ToString();
            //mes = date.Month.ToString();
            //año = date.Year.ToString();
            //fecha_actual = dia + "/" + mes + "/" + año; //obtengo la fecha actual

            //time = DateTime.Now.ToLocalTime();
            //hora = time.Hour.ToString();
            //min = time.Minute.ToString();
            //string tiempo = hora + ":" + min;

            //if (!File.Exists(Path.Combine(combina)))
            //{
            //    using (var escribe = new StreamWriter(combina, true))
            //    {
            //        escribe.WriteLine("se detuvo el servicio");
            //        escribe.WriteLine(fecha_actual);
            //        escribe.WriteLine(tiempo);
            //    }
            //}
            //else
            //{
            //    using (var escribe = new StreamWriter(combina, true))
            //    {
            //        escribe.WriteLine("se detuvo el servicio");
            //        escribe.WriteLine(fecha_actual);
            //        escribe.WriteLine(tiempo);
            //    }
            //}

            bool isInBackground;
            RunningAppProcessInfo myProcess = new RunningAppProcessInfo();
            GetMyMemoryState(myProcess);
            isInBackground = myProcess.Importance != Importance.Foreground;
            if (isInBackground)
            {
                var intento = new Intent(Application.Context, typeof(MainActivity));
                intento.AddCategory(Intent.CategoryLauncher);
                intento.AddFlags(ActivityFlags.NewTask);
                Application.Context.StartActivity(intento);
            }

            wifiLock.Release();
            wake.Release();
        }
    }
}